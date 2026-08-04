#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization.Metadata;

namespace Krampus.BinJson.Serialization
{
    internal sealed class BJsonObjectSerializer
    {
        private static readonly ConcurrentDictionary<Type, object?> DefaultValueCache = new ConcurrentDictionary<Type, object?>();
        private static readonly ConcurrentDictionary<Type, LifecycleHooks> LifecycleHookCache = new ConcurrentDictionary<Type, LifecycleHooks>();
        private static readonly ConcurrentDictionary<Type, Dictionary<string, Type>> PolymorphicDiscriminatorCache = new ConcurrentDictionary<Type, Dictionary<string, Type>>();
        private static readonly ConcurrentDictionary<Type, IBJsonConverterFactory?> ConverterFactoryInstanceCache = new ConcurrentDictionary<Type, IBJsonConverterFactory?>();

        private readonly BJsonSerializerOptions _options;
        private readonly BJsonSerializationContext _context;
        private readonly MetadataCache _metadataCache;
        private readonly Dictionary<Type, IBJsonConverter?> _converterCache;

        public BJsonObjectSerializer(BJsonSerializerOptions? options)
            : this(
                options ?? new BJsonSerializerOptions(),
                new MetadataCache(),
                new Dictionary<Type, IBJsonConverter?>())
        {
        }

        internal BJsonObjectSerializer(
            BJsonSerializerOptions options,
            MetadataCache metadataCache,
            Dictionary<Type, IBJsonConverter?> converterCache)
        {
            _options = options;
            _context = new BJsonSerializationContext(this, _options);
            _metadataCache = metadataCache;
            _converterCache = converterCache;
        }

        public BJsonValue SerializeValue(object? value, Type declaredType)
        {
            try
            {
                if (value is null)
                    return BJsonValue.Null;

                if (value is BJsonValue jsonValue)
                    return jsonValue;

                var runtimeType = value.GetType();
                var polymorphicType = ResolvePolymorphicRuntimeType(declaredType, runtimeType);
                if (polymorphicType is not null)
                    runtimeType = polymorphicType;

                if (value is IBJsonSerializable serializable)
                    return serializable.Serialize(_context);

                if (TryGetConverter(runtimeType, out var converter))
                    return converter.Serialize(value, _context);

                if (TrySerializePrimitive(value, runtimeType, out var primitive))
                    return primitive;

                if (value is IDictionary dictionary)
                    return SerializeDictionary(dictionary);

                if (value is IEnumerable enumerable && value is not string)
                    return SerializeEnumerable(enumerable);

                return SerializeAttributedObject(value, runtimeType);
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonSerializationException($"Failed to serialize value of type '{declaredType.FullName}'.", ex);
            }
        }

        public object? DeserializeValue(BJsonValue value, Type targetType)
        {
            try
            {
                if (targetType == typeof(BJsonValue))
                    return value;

                var nullableType = Nullable.GetUnderlyingType(targetType);
                var effectiveTargetType = nullableType ?? targetType;

                if (value.IsNull)
                {
                    if (!effectiveTargetType.IsValueType || nullableType is not null)
                        return null;

                    return Activator.CreateInstance(effectiveTargetType);
                }

                effectiveTargetType = ResolvePolymorphicTargetType(value, effectiveTargetType);

                value = ApplyPreprocessorPipeline(value, effectiveTargetType);

                if (TryGetConverter(effectiveTargetType, out var converter))
                    return converter.Deserialize(value, _context);

                if (typeof(IBJsonDeserializable).IsAssignableFrom(effectiveTargetType))
                {
                    if (Activator.CreateInstance(effectiveTargetType) is not IBJsonDeserializable deserializable)
                        throw new BJsonDeserializationException($"Type '{effectiveTargetType.FullName}' must have a public parameterless constructor to implement {nameof(IBJsonDeserializable)}.");

                    var deserializationContext = new BJsonDeserializationContext(this, _options, effectiveTargetType);
                    deserializable.Deserialize(value, deserializationContext);
                    return deserializable;
                }

                if (TryDeserializePrimitive(value, effectiveTargetType, out var primitive))
                    return primitive;

                if (TryDeserializeDictionary(value, effectiveTargetType, out var dictionary))
                    return dictionary;

                if (TryDeserializeEnumerable(value, effectiveTargetType, out var enumerable))
                    return enumerable;

                return DeserializeAttributedObject(value, effectiveTargetType);
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonDeserializationException($"Failed to deserialize value to type '{targetType.FullName}'.", ex);
            }
        }

        private BJsonValue ApplyPreprocessorPipeline(BJsonValue value, Type targetType)
        {
            var preprocessorAttribute = targetType.GetCustomAttribute<BJsonPreprocessorAttribute>();
            if (preprocessorAttribute is null)
                return value;

            var context = _options.PreprocessorContext as BJsonPreprocessorContext ?? new BJsonPreprocessorContext();
            if (_options.PreprocessorContext is null)
                _options.PreprocessorContext = context;

            var preprocessor = CreatePreprocessor(preprocessorAttribute);
            object processed;
            try
            {
                processed = preprocessor.Process(value, context);
            }
            catch (Exception ex)
            {
                throw new BJsonDeserializationException(
                    $"Preprocessor pipeline failed for type '{targetType.FullName}'.",
                    errorCode: BJsonErrorCode.PreprocessorPipelineError,
                    innerException: ex);
            }
            if (processed is BJsonValue processedValue)
                value = processedValue;
            else if (processed is BJsonObject processedObject)
                value = BJsonValue.Create(processedObject);
            else if (processed is BJsonArray processedArray)
                value = BJsonValue.Create(processedArray);
            else if (processed is string processedString)
                value = BJsonValue.Create(processedString);

            if (value.IsObject)
            {
                var metadata = GetTypeMetadata(targetType);
                ApplyPreprocessorRules(value.ObjectValue, targetType, metadata, context);
            }

            return value;
        }

        private IBJsonPreprocessor CreatePreprocessor(BJsonPreprocessorAttribute attribute)
        {
            if (attribute.PreprocessorType is null)
                return new BuiltInPreprocessor();

            if (Activator.CreateInstance(attribute.PreprocessorType) is not IBJsonPreprocessor preprocessor)
                throw new BJsonDeserializationException($"Preprocessor type '{attribute.PreprocessorType.FullName}' must implement {nameof(IBJsonPreprocessor)} and expose a parameterless constructor.");

            return preprocessor;
        }

        private void ApplyPreprocessorRules(BJsonObject obj, Type targetType, TypeMetadata metadata, BJsonPreprocessorContext context)
        {
            var memberContexts = BuildPreprocessorMemberContexts(targetType, metadata);

            foreach (var memberContext in memberContexts)
            {
                var anchorAttribute = memberContext.AnchorAttribute;
                if (anchorAttribute is null)
                    continue;

                foreach (var key in memberContext.CandidatePropertyNames)
                {
                    if (obj.TryGetValue(key, out var memberValue))
                    {
                        context.RegisterAnchor(anchorAttribute.AnchorName, memberValue);
                        break;
                    }
                }
            }

            var properties = obj.ToArray();
            foreach (var property in properties)
            {
                if (property.Value.IsObject && property.Value.ObjectValue.TryGetValue("$ref", out var refToken) && refToken.TryGetString(out var anchorName))
                {
                    if (context.TryGetAnchor(anchorName, out var anchoredValue))
                        obj[property.Key] = anchoredValue;
                }
            }

            foreach (var memberContext in memberContexts)
            {
                var externalReferenceAttribute = memberContext.ExternalReferenceAttribute;
                if (externalReferenceAttribute is null)
                    continue;

                foreach (var key in memberContext.CandidatePropertyNames)
                {
                    if (!obj.TryGetValue(key, out var memberValue))
                        continue;

                    var path = ResolveExternalReferencePath(externalReferenceAttribute, memberValue, context);
                    if (path is null)
                        continue;

                    if (!File.Exists(path))
                    {
                        if (externalReferenceAttribute.Optional)
                        {
                            obj[key] = BJsonValue.Null;
                        }
                        else
                        {
                            throw new BJsonDeserializationException(
                                $"External reference file '{path}' was not found.",
                                errorCode: BJsonErrorCode.ExternalReferenceReadError);
                        }

                        break;
                    }

                    try
                    {
                        // Keep the external payload as BJson and let the regular member
                        // assignment path deserialize it exactly once.
                        obj[key] = LoadExternalReferenceValue(path, context);
                    }
                    catch (Exception ex)
                    {
                        if (externalReferenceAttribute.Optional)
                        {
                            obj[key] = BJsonValue.Null;
                            break;
                        }

                        throw new BJsonDeserializationException(
                            $"Failed to load external reference file '{path}'.",
                            errorCode: BJsonErrorCode.ExternalReferenceReadError,
                            innerException: ex);
                    }
                    break;
                }
            }
        }

        private static BJsonValue LoadExternalReferenceValue(string path, BJsonPreprocessorContext context)
        {
            var lastWriteTicks = File.GetLastWriteTimeUtc(path).Ticks;
            var fileLength = new FileInfo(path).Length;

            if (context.TryGetExternalReference(path, lastWriteTicks, fileLength, out var cached))
                return cached;

            var loaded = BJson.DeserializeFromFile(path);
            context.SetExternalReference(path, lastWriteTicks, fileLength, loaded);
            return loaded;
        }

        private static PreprocessorMemberContext[] BuildPreprocessorMemberContexts(Type targetType, TypeMetadata metadata)
        {
            var members = metadata.Members.ToArray();
            var contexts = new PreprocessorMemberContext[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                var memberInfo = ResolveMemberInfo(targetType, member.MemberName);
                var candidateNames = GetCandidatePropertyNames(member).Distinct(StringComparer.Ordinal).ToArray();

                contexts[i] = new PreprocessorMemberContext(
                    candidateNames,
                    memberInfo?.GetCustomAttribute<BJsonAnchorAttribute>(),
                    memberInfo?.GetCustomAttribute<BJsonExternalRefAttribute>());
            }

            return contexts;
        }

        private static IEnumerable<string> GetCandidatePropertyNames(MemberMetadata member)
        {
            yield return member.JsonName;
            yield return member.MemberName;
            if (!string.IsNullOrWhiteSpace(member.LegacyJsonName))
                yield return member.LegacyJsonName!;
            foreach (var alias in member.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    yield return alias;
            }
        }

        private static MemberInfo? ResolveMemberInfo(Type targetType, string memberName)
        {
            return targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? (MemberInfo?)targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private string? ResolveExternalReferencePath(BJsonExternalRefAttribute attribute, BJsonValue memberValue, BJsonPreprocessorContext context)
        {
            if (!string.IsNullOrWhiteSpace(attribute.FixedPath))
                return ResolvePath(attribute.FixedPath, context);

            if (memberValue.TryGetString(out var stringPath) && !string.IsNullOrWhiteSpace(stringPath))
                return ResolvePath(stringPath, context);

            return null;
        }

        private string ResolvePath(string path, BJsonPreprocessorContext context)
        {
            if (Path.IsPathRooted(path))
                return EnsureExternalReferencePathPolicy(path, context);

            var basePath = context.BasePath;
            if (string.IsNullOrWhiteSpace(basePath))
                basePath = Environment.CurrentDirectory;

            var fullPath = Path.GetFullPath(Path.Combine(basePath!, path));
            return EnsureExternalReferencePathPolicy(fullPath, context);
        }

        private string EnsureExternalReferencePathPolicy(string path, BJsonPreprocessorContext context)
        {
            var fullPath = Path.GetFullPath(path);
            if (_options.ExternalReferencePathPolicy == ExternalReferencePathPolicy.AllowAny)
                return fullPath;

            var basePath = context.BasePath;
            if (string.IsNullOrWhiteSpace(basePath))
                return fullPath;

            var baseFullPath = Path.GetFullPath(basePath!);
            if (!IsSameOrSubPath(baseFullPath, fullPath))
            {
                throw new BJsonDeserializationException(
                    $"External reference path '{fullPath}' is outside the allowed base path '{baseFullPath}'.",
                    errorCode: BJsonErrorCode.ExternalReferenceSecurityViolation);
            }

            return fullPath;
        }

        private static bool IsSameOrSubPath(string basePath, string candidate)
        {
            var normalizedBase = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedCandidate = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(normalizedBase, normalizedCandidate, StringComparison.OrdinalIgnoreCase))
                return true;

            return normalizedCandidate.StartsWith(normalizedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || normalizedCandidate.StartsWith(normalizedBase + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private readonly struct PreprocessorMemberContext
        {
            public PreprocessorMemberContext(
                string[] candidatePropertyNames,
                BJsonAnchorAttribute? anchorAttribute,
                BJsonExternalRefAttribute? externalReferenceAttribute)
            {
                CandidatePropertyNames = candidatePropertyNames;
                AnchorAttribute = anchorAttribute;
                ExternalReferenceAttribute = externalReferenceAttribute;
            }

            public string[] CandidatePropertyNames { get; }

            public BJsonAnchorAttribute? AnchorAttribute { get; }

            public BJsonExternalRefAttribute? ExternalReferenceAttribute { get; }
        }

        private bool TryGetConverter(Type type, out IBJsonConverter converter)
        {
            if (_converterCache.TryGetValue(type, out var cachedConverter))
            {
                converter = cachedConverter!;
                return converter is not null;
            }

            converter = CreateConverter(type)!;
            _converterCache[type] = converter;
            return converter is not null;
        }

        private IBJsonConverter? CreateConverter(Type type)
        {
            var attr = type.GetCustomAttribute<BJsonConverterAttribute>();
            if (attr is not null)
                return InstantiateConverter(attr.ConverterType, type);

            var factoryAttribute = type.GetCustomAttribute<BJsonConverterFactoryAttribute>();
            if (factoryAttribute is not null)
            {
                var factory = InstantiateConverterFactory(factoryAttribute.FactoryType);
                if (factory != null && factory.CanConvert(type))
                    return factory.CreateConverter(type);
            }

            if (_options.TryGetConverter(type, out var converter))
                return converter;

            var generatedConverter = TryCreateGeneratedConverter(type);
            if (generatedConverter is not null)
                return generatedConverter;

            if (type.GetCustomAttribute<BJsonSerializableAttribute>() is not null)
            {
                var converterType = typeof(AttributeBasedConverter<>).MakeGenericType(type);
                return (IBJsonConverter?)Activator.CreateInstance(converterType, this);
            }

            return null;
        }

        private IBJsonConverter? TryCreateGeneratedConverter(Type type)
        {
            var serializerTypeName = type.FullName + "_BJsonSerializer";
            var generatedType = type.Assembly.GetType(serializerTypeName, throwOnError: false, ignoreCase: false);
            if (generatedType is null)
                return null;

            if (!typeof(IBJsonConverter).IsAssignableFrom(generatedType))
                throw new BJsonConverterException($"Generated serializer '{generatedType.FullName}' must implement {nameof(IBJsonConverter)}.");

            return (IBJsonConverter?)Activator.CreateInstance(generatedType);
        }

        private static IBJsonConverter InstantiateConverter(Type converterType, Type targetType)
        {
            if (!typeof(IBJsonConverter).IsAssignableFrom(converterType))
                throw new BJsonConverterException($"Converter '{converterType.FullName}' must implement {nameof(IBJsonConverter)}.");

            if (Activator.CreateInstance(converterType) is not IBJsonConverter converter)
                throw new BJsonConverterException($"Converter '{converterType.FullName}' could not be instantiated.");

            if (!converter.Type.IsAssignableFrom(targetType) && !targetType.IsAssignableFrom(converter.Type))
                throw new BJsonConverterException($"Converter '{converterType.FullName}' is not compatible with type '{targetType.FullName}'.");

            return converter;
        }

        private static bool TrySerializePrimitive(object value, Type runtimeType, out BJsonValue result)
        {
            if (runtimeType == typeof(string))
            {
                result = BJsonValue.Create((string)value);
                return true;
            }

            if (runtimeType == typeof(bool))
            {
                result = BJsonValue.Create((bool)value);
                return true;
            }

            if (runtimeType.IsEnum)
            {
                result = BJsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)!);
                return true;
            }

            switch (Type.GetTypeCode(runtimeType))
            {
                case TypeCode.SByte: result = BJsonValue.Create((sbyte)value); return true;
                case TypeCode.Int16: result = BJsonValue.Create((short)value); return true;
                case TypeCode.Int32: result = BJsonValue.Create((int)value); return true;
                case TypeCode.Int64: result = BJsonValue.Create((long)value); return true;
                case TypeCode.Byte: result = BJsonValue.Create((byte)value); return true;
                case TypeCode.UInt16: result = BJsonValue.Create((ushort)value); return true;
                case TypeCode.UInt32: result = BJsonValue.Create((uint)value); return true;
                case TypeCode.UInt64: result = BJsonValue.Create((ulong)value); return true;
                case TypeCode.Single: result = BJsonValue.Create((float)value); return true;
                case TypeCode.Double: result = BJsonValue.Create((double)value); return true;
                case TypeCode.Decimal: result = BJsonValue.Create((double)(decimal)value); return true;
                case TypeCode.Char: result = BJsonValue.Create(value.ToString()); return true;
                case TypeCode.DateTime: result = BJsonValue.Create(((DateTime)value).ToString("O", CultureInfo.InvariantCulture)); return true;
            }

            result = default;
            return false;
        }

        private static bool TryDeserializePrimitive(BJsonValue value, Type type, out object? result)
        {
            if (type == typeof(string))
            {
                if (value.TryGetString(out var stringValue))
                {
                    result = stringValue;
                    return true;
                }

                result = null;
                return false;
            }

            if (type == typeof(bool))
            {
                if (value.TryGetBool(out var boolValue))
                {
                    result = boolValue;
                    return true;
                }

                result = null;
                return false;
            }

            if (type.IsEnum)
            {
                if (value.TryGetString(out var enumString))
                {
                    result = Enum.Parse(type, enumString, ignoreCase: true);
                    return true;
                }

                if (value.TryGetNumberAsLong(out var enumLong))
                {
                    result = Enum.ToObject(type, enumLong);
                    return true;
                }

                result = null;
                return false;
            }

            if (type == typeof(DateTime))
            {
                if (value.TryGetString(out var dateString) && DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
                {
                    result = dateTime;
                    return true;
                }

                result = null;
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    if (value.TryGetSByte(out var sbyteValue)) { result = sbyteValue; return true; }
                    break;
                case TypeCode.Int16:
                    if (value.TryGetShort(out var shortValue)) { result = shortValue; return true; }
                    break;
                case TypeCode.Int32:
                    if (value.TryGetInt(out var intValue)) { result = intValue; return true; }
                    break;
                case TypeCode.Int64:
                    if (value.TryGetLong(out var longValue)) { result = longValue; return true; }
                    break;
                case TypeCode.Byte:
                    if (value.TryGetByte(out var byteValue)) { result = byteValue; return true; }
                    break;
                case TypeCode.UInt16:
                    if (value.TryGetUShort(out var ushortValue)) { result = ushortValue; return true; }
                    break;
                case TypeCode.UInt32:
                    if (value.TryGetUInt(out var uintValue)) { result = uintValue; return true; }
                    break;
                case TypeCode.UInt64:
                    if (value.TryGetULong(out var ulongValue)) { result = ulongValue; return true; }
                    break;
                case TypeCode.Single:
                    if (value.TryGetFloat(out var floatValue)) { result = floatValue; return true; }
                    if (value.TryGetDouble(out var doubleToFloat)) { result = (float)doubleToFloat; return true; }
                    break;
                case TypeCode.Double:
                    if (value.TryGetDouble(out var doubleValue)) { result = doubleValue; return true; }
                    if (value.TryGetLong(out var longToDouble)) { result = (double)longToDouble; return true; }
                    break;
                case TypeCode.Decimal:
                    if (value.TryGetDouble(out var decimalValue)) { result = (decimal)decimalValue; return true; }
                    break;
                case TypeCode.Char:
                    if (value.TryGetString(out var charString) && charString.Length == 1) { result = charString[0]; return true; }
                    break;
            }

            result = null;
            return false;
        }

        private BJsonValue SerializeDictionary(IDictionary dictionary)
        {
            var obj = new BJsonObject(dictionary.Count);

            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                var serializedValue = SerializeValue(entry.Value, entry.Value?.GetType() ?? typeof(object));

                if (_options.IgnoreNullValues && serializedValue.IsNull)
                    continue;

                obj[key] = serializedValue;
            }

            return BJsonValue.Create(obj);
        }

        private BJsonValue SerializeEnumerable(IEnumerable enumerable)
        {
            var array = new BJsonArray();
            foreach (var item in enumerable)
                array.Add(SerializeValue(item, item?.GetType() ?? typeof(object)));

            return BJsonValue.Create(array);
        }

        internal BJsonValue SerializeAttributedObject(object value, Type type)
        {
            string? referenceId = null;
            bool writeReferenceOnly = false;
            if (_context.ReferenceResolver?.PreserveReferences == true)
            {
                referenceId = _context.ReferenceResolver.GetOrAddReference(value, out var alreadyExists);
                writeReferenceOnly = alreadyExists;
            }

            if (writeReferenceOnly)
            {
                var refObject = new BJsonObject(1)
                {
                    ["$ref"] = BJsonValue.Create(referenceId)
                };
                return BJsonValue.Create(refObject);
            }

            _context.PushObject(value);
            try
            {
                InvokeOnSerializingHooks(value, type);

                var metadata = GetTypeMetadata(type);
                var members = metadata.Members;
                var discriminatorName = GetTypeDiscriminatorPropertyName(type);
                var includeTypeDiscriminator = ShouldWriteTypeDiscriminator(type);
                var extraMetadataCount = (referenceId is null ? 0 : 1) + (includeTypeDiscriminator ? 1 : 0);
                var obj = new BJsonObject(members.Length + extraMetadataCount);
                if (referenceId is not null)
                    obj["$id"] = BJsonValue.Create(referenceId);
                if (includeTypeDiscriminator)
                    obj[discriminatorName] = BJsonValue.Create(GetDiscriminatorValue(type));

                    var activeVersion = _options.Version ?? metadata.VersionContext;

                    foreach (var member in members)
                    {
                        var memberValue = member.Getter(value);

                        if (member.IsExtensionData)
                        {
                            if (memberValue is IEnumerable<KeyValuePair<string, BJsonValue>> extensionPairs)
                            {
                                foreach (var pair in extensionPairs)
                                {
                                    if (!obj.ContainsKey(pair.Key))
                                        obj[pair.Key] = pair.Value;
                                }
                            }
                            continue;
                        }

                        // Version range guard
                        if (!IsInVersionRange(activeVersion, metadata.VersionIntroducedIn, metadata.VersionRemovedIn)
                            || !member.IsInVersionRange(activeVersion))
                            continue;

                        // Static predicate check
                        if (member.IgnoreWhenPredicate != null)
                        {
                            var shouldIgnore = member.IgnoreWhenPredicateDelegate != null
                                ? member.IgnoreWhenPredicateDelegate(memberValue, member.JsonName, activeVersion)
                                : member.IgnoreWhenPredicate.Invoke(null, new object?[] { memberValue, member.JsonName, activeVersion });

                            if (shouldIgnore is true)
                                continue;
                        }

                        var memberInfo = ResolveMemberInfo(type, member.MemberName);
                        var externalRefAttribute = memberInfo?.GetCustomAttribute<BJsonExternalRefAttribute>();

                        if (externalRefAttribute != null)
                        {
                            var preprocessorContext = _options.PreprocessorContext as BJsonPreprocessorContext ?? new BJsonPreprocessorContext();
                            if (_options.PreprocessorContext is null)
                                _options.PreprocessorContext = preprocessorContext;

                            var externalReferenceToken = SerializeExternalReferenceMember(member, memberValue, externalRefAttribute, preprocessorContext);
                            obj[member.JsonName] = externalReferenceToken;
                            continue;
                        }

                        BJsonValue serializedValue;

                        if (member.Converter is not null)
                            serializedValue = member.Converter.Serialize(memberValue, _context);
                        else
                            serializedValue = SerializeValue(memberValue, member.MemberType);

                        if (_options.IgnoreNullValues && serializedValue.IsNull)
                            continue;

                        if (member.IgnoreCondition == BJsonIgnoreCondition.WhenWritingNull && serializedValue.IsNull)
                            continue;

                        if (member.IgnoreCondition == BJsonIgnoreCondition.WhenWritingDefault && IsDefaultValue(memberValue, member.MemberType))
                            continue;

                        if (member.IgnoreCondition == BJsonIgnoreCondition.WhenWritingCustomDefault && IsCustomDefaultValue(member, memberValue, activeVersion))
                            continue;

                        if (member.IgnoreCondition == BJsonIgnoreCondition.WhenWriting)
                            continue;

                        // Value mapper (write direction)
                        serializedValue = ApplyValueMapper(member, serializedValue, activeVersion, isReading: false);

                        serializedValue = ApplyNumberHandlingOnWrite(member, memberValue, serializedValue);

                        obj[member.JsonName] = serializedValue;
                    }

                    return BJsonValue.Create(obj);
                }
                finally
                {
                    _context.PopObject();
                }
            }

        private bool TryDeserializeDictionary(BJsonValue value, Type targetType, out object? result)
        {
            if (!value.TryGetObject(out var obj))
            {
                result = null;
                return false;
            }

            if (targetType == typeof(Dictionary<string, BJsonValue>))
            {
                var direct = new Dictionary<string, BJsonValue>(obj.Count);
                foreach (var item in obj)
                    direct[item.Key] = item.Value;

                result = direct;
                return true;
            }

            Type keyType;
            Type valueType;

            if (TryGetDictionaryTypes(targetType, out keyType, out valueType) && keyType == typeof(string))
            {
                var concreteType = targetType;
                if (targetType.IsInterface || targetType.IsAbstract)
                    concreteType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

                if (Activator.CreateInstance(concreteType) is not IDictionary dictionary)
                {
                    result = null;
                    return false;
                }

                foreach (var item in obj)
                    dictionary[item.Key] = DeserializeValue(item.Value, valueType);

                result = dictionary;
                return true;
            }

            if (typeof(IDictionary).IsAssignableFrom(targetType))
            {
                if (Activator.CreateInstance(targetType) is not IDictionary dictionary)
                {
                    result = null;
                    return false;
                }

                foreach (var item in obj)
                    dictionary[item.Key] = item.Value;

                result = dictionary;
                return true;
            }

            result = null;
            return false;
        }

        private BJsonValue SerializeExternalReferenceMember(
            MemberMetadata member,
            object? memberValue,
            BJsonExternalRefAttribute externalReferenceAttribute,
            BJsonPreprocessorContext context)
        {
            var path = ResolveExternalReferencePathForSerialization(externalReferenceAttribute, memberValue, context);

            if (path is null)
            {
                if (externalReferenceAttribute.Optional)
                    return BJsonValue.Null;

                throw new BJsonSerializationException(
                    $"Member '{member.MemberName}' marked with [BJsonExternalRef] requires a FixedPath or a string member value path for serialization.",
                    errorCode: BJsonErrorCode.ExternalReferencePathError);
            }

            if (memberValue is null)
                return BJsonValue.Create(path);

            if (member.MemberType == typeof(string) && memberValue is string)
                return BJsonValue.Create(path);

            try
            {
                BJson.SerializeToFile(path, memberValue, member.MemberType, _options);
            }
            catch (Exception ex)
            {
                if (externalReferenceAttribute.Optional)
                    return BJsonValue.Null;

                throw new BJsonSerializationException(
                    $"Failed to write external reference file '{path}' for member '{member.MemberName}'.",
                    errorCode: BJsonErrorCode.ExternalReferenceWriteError,
                    innerException: ex);
            }

            return BJsonValue.Create(path);
        }

        private string? ResolveExternalReferencePathForSerialization(BJsonExternalRefAttribute attribute, object? memberValue, BJsonPreprocessorContext context)
        {
            if (!string.IsNullOrWhiteSpace(attribute.FixedPath))
                return ResolvePath(attribute.FixedPath, context);

            if (memberValue is string stringPath && !string.IsNullOrWhiteSpace(stringPath))
                return ResolvePath(stringPath, context);

            return null;
        }

        private bool TryDeserializeEnumerable(BJsonValue value, Type targetType, out object? result)
        {
            if (!value.TryGetArray(out var array))
            {
                result = null;
                return false;
            }

            if (targetType.IsArray)
            {
                var itemType = targetType.GetElementType()!;
                var typedArray = Array.CreateInstance(itemType, array.Count);
                for (int i = 0; i < array.Count; i++)
                    typedArray.SetValue(DeserializeValue(array[i], itemType), i);

                result = typedArray;
                return true;
            }

            var elementType = GetEnumerableElementType(targetType);
            if (elementType is null)
            {
                result = null;
                return false;
            }

            var concreteType = targetType;
            if (targetType.IsInterface || targetType.IsAbstract)
                concreteType = typeof(List<>).MakeGenericType(elementType);

            if (Activator.CreateInstance(concreteType) is not IList list)
            {
                result = null;
                return false;
            }

            foreach (var item in array)
                list.Add(DeserializeValue(item, elementType));

            result = list;
            return true;
        }

        internal object DeserializeAttributedObject(BJsonValue value, Type targetType)
        {
            if (!value.TryGetObject(out var obj))
                throw new BJsonDeserializationException($"Cannot deserialize value '{value.Type}' to '{targetType.FullName}'.");

            var metadata = GetTypeMetadata(targetType);
            var caseInsensitiveIndex = CreateCaseInsensitiveIndex(obj);
            var instance = CreateObjectInstance(targetType, metadata, obj, caseInsensitiveIndex);
            var consumedNames = new HashSet<string>(StringComparer.Ordinal);

            var activeVersion = _options.Version ?? metadata.VersionContext;

            foreach (var member in metadata.Members)
            {
                if (member.IsExtensionData)
                    continue;

                if (member.IgnoreCondition == BJsonIgnoreCondition.WhenReading)
                    continue;

                // Version range guard
                if (!IsInVersionRange(activeVersion, metadata.VersionIntroducedIn, metadata.VersionRemovedIn)
                    || !member.IsInVersionRange(activeVersion))
                    continue;

                var isRequired = member.Required || IsConditionallyRequired(member, activeVersion);

                // Try primary key, then legacy keys (RenamedFrom + aliases)
                if (!TryGetMemberValue(obj, member, caseInsensitiveIndex, out var jsonMemberValue, out var consumedName))
                {
                    if (_options.StrictMode && isRequired)
                        throw new BJsonDeserializationException($"Required member '{member.JsonName}' was not found while deserializing '{targetType.FullName}'.");

                    // Apply default value if key is absent
                    ApplyDefaultValue(member, instance, activeVersion);
                    continue;
                }

                consumedNames.Add(consumedName);

                // Apply configured defaults for explicit null tokens on non-nullable value members.
                if (jsonMemberValue.IsNull
                    && member.MemberType.IsValueType
                    && Nullable.GetUnderlyingType(member.MemberType) is null
                    && HasConfiguredDefault(member))
                {
                    ApplyDefaultValue(member, instance, activeVersion);
                    continue;
                }

                // Static predicate check
                if (member.IgnoreWhenPredicate != null)
                {
                    var rawForPredicate = member.Getter(instance);
                    var shouldIgnore = member.IgnoreWhenPredicateDelegate != null
                        ? member.IgnoreWhenPredicateDelegate(rawForPredicate, member.JsonName, activeVersion)
                        : member.IgnoreWhenPredicate.Invoke(null, new object?[] { rawForPredicate, member.JsonName, activeVersion });

                    if (shouldIgnore is true)
                        continue;
                }

                // Apply value mapper (read direction) before deserialization
                jsonMemberValue = ApplyValueMapper(member, jsonMemberValue, activeVersion, isReading: true);

                object? deserialized;
                if (member.Converter is not null)
                    deserialized = member.Converter.Deserialize(jsonMemberValue, _context);
                else
                    deserialized = DeserializeMemberWithNumberHandling(member, jsonMemberValue);

                member.Setter(instance, deserialized);
            }

            if (metadata.ExtensionDataMember is not null)
            {
                var extras = new Dictionary<string, BJsonValue>(StringComparer.Ordinal);
                var discriminatorName = GetTypeDiscriminatorPropertyName(targetType);
                foreach (var pair in obj)
                {
                    if (consumedNames.Contains(pair.Key))
                        continue;
                    if (pair.Key == "$id" || pair.Key == "$ref" || pair.Key == discriminatorName)
                        continue;

                    extras[pair.Key] = pair.Value;
                }

                if (extras.Count > 0 || metadata.ExtensionDataMember.MemberType == typeof(Dictionary<string, BJsonValue>))
                    metadata.ExtensionDataMember.Setter(instance, extras);
            }

            InvokeOnDeserializedHooks(instance, targetType);

            return instance;
        }

        private object CreateObjectInstance(Type targetType, TypeMetadata metadata, BJsonObject obj, Dictionary<string, BJsonValue>? caseInsensitiveIndex)
        {
            // Factory method takes precedence over constructor
            if (metadata.FactoryMethod != null)
            {
                var factoryParams = metadata.FactoryMethod.GetParameters();
                var factoryArgs = new object?[factoryParams.Length];
                for (int i = 0; i < factoryParams.Length; i++)
                {
                    var parameter = factoryParams[i];
                    var parameterJsonName = ResolveParameterJsonName(parameter, metadata, useFactoryMapping: true);
                    if (TryGetObjectValue(obj, parameterJsonName, out var parameterValue, caseInsensitiveIndex))
                    {
                        factoryArgs[i] = DeserializeValue(parameterValue, parameter.ParameterType);
                    }
                    else if (parameter.HasDefaultValue)
                    {
                        factoryArgs[i] = parameter.DefaultValue;
                    }
                    else if (parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) is null)
                    {
                        factoryArgs[i] = Activator.CreateInstance(parameter.ParameterType);
                    }
                    else
                    {
                        factoryArgs[i] = null;
                    }
                }

                return metadata.FactoryMethod.Invoke(null, factoryArgs)
                    ?? throw new BJsonDeserializationException($"Factory method on '{targetType.FullName}' returned null.");
            }

            var constructorMetadata = metadata.Constructor;
            if (constructorMetadata is null)
                throw new BJsonDeserializationException($"Type '{targetType.FullName}' has no usable constructor for deserialization.");

            if (constructorMetadata.Parameters.Length == 0)
            {
                return Activator.CreateInstance(targetType)
                    ?? throw new BJsonDeserializationException($"Type '{targetType.FullName}' must have a usable constructor.");
            }

            var args = new object?[constructorMetadata.Parameters.Length];
            for (int i = 0; i < constructorMetadata.Parameters.Length; i++)
            {
                var parameter = constructorMetadata.Parameters[i];
                var parameterJsonName = ResolveParameterJsonName(parameter, metadata, useFactoryMapping: false);
                if (TryGetObjectValue(obj, parameterJsonName, out var parameterValue, caseInsensitiveIndex))
                {
                    args[i] = DeserializeValue(parameterValue, parameter.ParameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                }
                else if (parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) is null)
                {
                    args[i] = Activator.CreateInstance(parameter.ParameterType);
                }
                else
                {
                    args[i] = null;
                }
            }

            return constructorMetadata.Constructor.Invoke(args);
        }

        private static string ResolveParameterJsonName(ParameterInfo parameter, TypeMetadata metadata, bool useFactoryMapping)
        {
            var parameterName = parameter.Name ?? string.Empty;

            if (useFactoryMapping
                && metadata.FactoryParameterMapping != null
                && metadata.FactoryParameterMapping.TryGetValue(parameterName, out var mappedJsonName)
                && !string.IsNullOrWhiteSpace(mappedJsonName))
            {
                return mappedJsonName;
            }

            var member = metadata.Members.FirstOrDefault(m => string.Equals(m.MemberName, parameterName, StringComparison.OrdinalIgnoreCase));
            if (member != null)
                return member.JsonName;

            return parameterName;
        }

        private BJsonValue ApplyValueMapper(MemberMetadata member, BJsonValue value, IComparable? activeVersion, bool isReading)
        {
            if (member.ValueMapperFullSignature != null)
            {
                var result = member.ValueMapperFullDelegate != null
                    ? member.ValueMapperFullDelegate(value, member.JsonName, activeVersion, isReading)
                    : member.ValueMapperFullSignature.Invoke(null, new object?[] { value, member.JsonName, activeVersion, isReading });

                return result is BJsonValue bv ? bv : value;
            }

            if (member.ValueMapperShortSignature != null)
            {
                var result = member.ValueMapperShortDelegate != null
                    ? member.ValueMapperShortDelegate(value)
                    : member.ValueMapperShortSignature.Invoke(null, new object?[] { value });

                return result is BJsonValue bv2 ? bv2 : value;
            }

            return value;
        }

        private void ApplyDefaultValue(MemberMetadata member, object instance, IComparable? activeVersion)
        {
            if (member.DefaultProviderMethod != null)
            {
                var defaultVal = member.DefaultProviderDelegate != null
                    ? member.DefaultProviderDelegate(activeVersion)
                    : InvokeDefaultProvider(member.DefaultProviderMethod, activeVersion);

                member.Setter(instance, defaultVal);
                return;
            }

            if (member.HasDefaultConstant)
            {
                try
                {
                    var converted = member.DefaultConstantValue == null
                        ? null
                        : Convert.ChangeType(member.DefaultConstantValue, member.MemberType, System.Globalization.CultureInfo.InvariantCulture);
                    member.Setter(instance, converted);
                }
                catch
                {
                    // If conversion fails, leave the existing member value.
                }
            }
        }

        private Dictionary<string, BJsonValue>? CreateCaseInsensitiveIndex(BJsonObject obj)
        {
            if (!_options.PropertyNameCaseInsensitive)
                return null;

            var index = new Dictionary<string, BJsonValue>(obj.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in obj)
            {
                if (!index.ContainsKey(pair.Key))
                    index[pair.Key] = pair.Value;
            }

            return index;
        }

        private bool TryGetObjectValue(BJsonObject obj, string propertyName, out BJsonValue value, Dictionary<string, BJsonValue>? caseInsensitiveIndex = null)
        {
            if (obj.TryGetValue(propertyName, out value))
                return true;

            if (caseInsensitiveIndex is null)
                return false;

            return caseInsensitiveIndex.TryGetValue(propertyName, out value);
        }

        private bool TryGetMemberValue(
            BJsonObject obj,
            MemberMetadata member,
            Dictionary<string, BJsonValue>? caseInsensitiveIndex,
            out BJsonValue value,
            out string consumedName)
        {
            if (TryGetObjectValue(obj, member.JsonName, out value, caseInsensitiveIndex))
            {
                consumedName = member.JsonName;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(member.LegacyJsonName)
                && TryGetObjectValue(obj, member.LegacyJsonName!, out value, caseInsensitiveIndex))
            {
                consumedName = member.LegacyJsonName!;
                return true;
            }

            foreach (var alias in member.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    continue;

                if (TryGetObjectValue(obj, alias, out value, caseInsensitiveIndex))
                {
                    consumedName = alias;
                    return true;
                }
            }

            consumedName = member.JsonName;
            value = default;
            return false;
        }

        private static bool IsConditionallyRequired(MemberMetadata member, IComparable? activeVersion)
        {
            if (member.RequiredWhenMethod == null)
                return false;

            if (member.RequiredWhenDelegate != null)
                return member.RequiredWhenDelegate(member.JsonName, activeVersion);

            var parameters = member.RequiredWhenMethod.GetParameters();
            object? result;
            if (parameters.Length == 0)
            {
                result = member.RequiredWhenMethod.Invoke(null, null);
            }
            else if (parameters.Length == 1)
            {
                result = member.RequiredWhenMethod.Invoke(null, new object?[] { activeVersion });
            }
            else
            {
                result = member.RequiredWhenMethod.Invoke(null, new object?[] { member.JsonName, activeVersion });
            }

            return result is bool b && b;
        }

        private object? DeserializeMemberWithNumberHandling(MemberMetadata member, BJsonValue value)
        {
            var handling = member.NumberHandling;
            if (handling == BJsonNumberHandling.Strict || !IsNumericType(member.MemberType))
                return DeserializeValue(value, member.MemberType);

            if (!TryDeserializeNumericMember(value, member.MemberType, handling, out var parsed))
                return DeserializeValue(value, member.MemberType);

            return parsed;
        }

        private static bool TryDeserializeNumericMember(BJsonValue value, Type memberType, BJsonNumberHandling handling, out object? parsed)
        {
            var nullableType = Nullable.GetUnderlyingType(memberType);
            var targetType = nullableType ?? memberType;

            if (value.IsString)
            {
                if ((handling & (BJsonNumberHandling.AllowReadingFromString | BJsonNumberHandling.Lossless)) == 0)
                {
                    parsed = null;
                    return false;
                }

                if (!value.TryGetString(out var raw))
                {
                    parsed = null;
                    return false;
                }

                return TryParseNumericString(raw, targetType, out parsed);
            }

            parsed = null;
            return false;
        }

        private static bool TryParseNumericString(string raw, Type targetType, out object? parsed)
        {
            parsed = null;
            if (targetType == typeof(byte) && byte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b)) { parsed = b; return true; }
            if (targetType == typeof(sbyte) && sbyte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb)) { parsed = sb; return true; }
            if (targetType == typeof(short) && short.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)) { parsed = s; return true; }
            if (targetType == typeof(ushort) && ushort.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var us)) { parsed = us; return true; }
            if (targetType == typeof(int) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) { parsed = i; return true; }
            if (targetType == typeof(uint) && uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui)) { parsed = ui; return true; }
            if (targetType == typeof(long) && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) { parsed = l; return true; }
            if (targetType == typeof(ulong) && ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ul)) { parsed = ul; return true; }
            if (targetType == typeof(float) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) { parsed = f; return true; }
            if (targetType == typeof(double) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { parsed = d; return true; }
            if (targetType == typeof(decimal) && decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) { parsed = m; return true; }
            return false;
        }

        private static bool IsNumericType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying.IsEnum)
                return false;

            switch (Type.GetTypeCode(underlying))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private BJsonValue ApplyNumberHandlingOnWrite(MemberMetadata member, object? memberValue, BJsonValue serializedValue)
        {
            if (memberValue is null || !IsNumericType(member.MemberType))
                return serializedValue;

            var handling = member.NumberHandling;
            var writeAsString = (handling & BJsonNumberHandling.WriteAsString) != 0
                || (handling & BJsonNumberHandling.Lossless) != 0;

            if (!writeAsString)
                return serializedValue;

            var raw = FormatNumericForWrite(memberValue, member.MemberType);
            return BJsonValue.Create(raw);
        }

        private static string FormatNumericForWrite(object value, Type memberType)
        {
            var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;
            if (targetType == typeof(float))
                return ((float)value).ToString("R", CultureInfo.InvariantCulture);
            if (targetType == typeof(double))
                return ((double)value).ToString("R", CultureInfo.InvariantCulture);
            if (targetType == typeof(decimal))
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
        }

        private static object? InvokeDefaultProvider(MethodInfo method, IComparable? activeVersion)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return method.Invoke(null, null);

            return method.Invoke(null, new object?[] { activeVersion });
        }

        private void InvokeOnSerializingHooks(object instance, Type type)
        {
            var hooks = GetLifecycleHooks(type);
            foreach (var hook in hooks.OnSerializing)
                InvokeLifecycleHook(instance, hook, _context);
        }

        private void InvokeOnDeserializedHooks(object instance, Type type)
        {
            var hooks = GetLifecycleHooks(type);
            var context = new BJsonDeserializationContext(this, _options, type);
            foreach (var hook in hooks.OnDeserialized)
                InvokeLifecycleHook(instance, hook, context);
        }

        private static void InvokeLifecycleHook(object instance, MethodInfo method, object context)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                method.Invoke(instance, null);
                return;
            }

            method.Invoke(instance, new[] { context });
        }

        private static LifecycleHooks GetLifecycleHooks(Type type)
        {
            return LifecycleHookCache.GetOrAdd(type, static candidate =>
            {
                var onSerializing = new List<MethodInfo>();
                var onDeserialized = new List<MethodInfo>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
                {
                    foreach (var method in current.GetMethods(flags))
                    {
                        if (method.GetCustomAttribute<BJsonOnSerializingAttribute>() != null && IsValidLifecycleHook(method, typeof(BJsonSerializationContext)))
                            onSerializing.Add(method);
                        if (method.GetCustomAttribute<BJsonOnDeserializedAttribute>() != null && IsValidLifecycleHook(method, typeof(BJsonDeserializationContext)))
                            onDeserialized.Add(method);
                    }
                }

                return new LifecycleHooks(onSerializing.ToArray(), onDeserialized.ToArray());
            });
        }

        private static bool IsValidLifecycleHook(MethodInfo method, Type contextType)
        {
            if (method.ReturnType != typeof(void) || method.IsStatic)
                return false;

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return true;

            return parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(contextType);
        }

        private readonly struct LifecycleHooks
        {
            public LifecycleHooks(MethodInfo[] onSerializing, MethodInfo[] onDeserialized)
            {
                OnSerializing = onSerializing;
                OnDeserialized = onDeserialized;
            }

            public MethodInfo[] OnSerializing { get; }

            public MethodInfo[] OnDeserialized { get; }
        }

        private MemberMetadata[] GetSerializableMembers(Type type)
        {
            return GetTypeMetadata(type).Members;
        }

        private TypeMetadata GetTypeMetadata(Type type)
        {
            if (_metadataCache.TryGet(type, out var cached))
                return cached;

            cached = ReflectionAnalyzer.Analyze(type, _options, ResolveMemberConverter);
            _metadataCache.Set(type, cached);
            return cached;
        }

        private IBJsonConverter? ResolveMemberConverter(Type memberType, BJsonConverterAttribute? attribute)
        {
            if (attribute is not null)
                return InstantiateConverter(attribute.ConverterType, memberType);

            if (memberType.GetCustomAttribute<BJsonConverterAttribute>() is BJsonConverterAttribute typeAttribute)
                return InstantiateConverter(typeAttribute.ConverterType, memberType);

            var memberTypeFactoryAttribute = memberType.GetCustomAttribute<BJsonConverterFactoryAttribute>();
            if (memberTypeFactoryAttribute is not null)
            {
                var factory = InstantiateConverterFactory(memberTypeFactoryAttribute.FactoryType);
                if (factory != null && factory.CanConvert(memberType))
                    return factory.CreateConverter(memberType);
            }

            if (_options.TryGetConverter(memberType, out var converter))
                return converter;

            return null;
        }

        private static IBJsonConverterFactory? InstantiateConverterFactory(Type factoryType)
        {
            return ConverterFactoryInstanceCache.GetOrAdd(factoryType, static type =>
            {
                if (!typeof(IBJsonConverterFactory).IsAssignableFrom(type))
                    throw new BJsonConverterException($"Converter factory '{type.FullName}' must implement {nameof(IBJsonConverterFactory)}.");

                if (Activator.CreateInstance(type) is not IBJsonConverterFactory factory)
                    throw new BJsonConverterException($"Converter factory '{type.FullName}' could not be instantiated.");

                return factory;
            });
        }

        private static bool IsDefaultValue(object? value, Type type)
        {
            if (value is null)
                return true;

            if (!type.IsValueType)
                return false;

            var defaultValue = DefaultValueCache.GetOrAdd(type, static t => Activator.CreateInstance(t));
            return value.Equals(defaultValue);
        }

        private static bool IsInVersionRange(IComparable? activeVersion, IComparable? introducedIn, IComparable? removedIn)
        {
            if (activeVersion == null)
                return true;

            if (introducedIn != null && activeVersion.CompareTo(introducedIn) < 0)
                return false;

            if (removedIn != null && activeVersion.CompareTo(removedIn) >= 0)
                return false;

            return true;
        }

        private static bool HasConfiguredDefault(MemberMetadata member)
        {
            return member.DefaultProviderMethod != null || member.HasDefaultConstant;
        }

        private bool IsCustomDefaultValue(MemberMetadata member, object? memberValue, IComparable? activeVersion)
        {
            if (member.DefaultProviderMethod == null)
                return IsDefaultValue(memberValue, member.MemberType);

            var providerValue = member.DefaultProviderDelegate != null
                ? member.DefaultProviderDelegate(activeVersion)
                : InvokeDefaultProvider(member.DefaultProviderMethod, activeVersion);

            if (memberValue is null && providerValue is null)
                return true;

            return Equals(memberValue, providerValue);
        }

        private sealed class BuiltInPreprocessor : IBJsonPreprocessor
        {
            public object Process(object node, IBJsonPreprocessorContext context)
            {
                if (node is BJsonValue value)
                    return ProcessValue(value, context);

                return node;
            }

            private static BJsonValue ProcessValue(BJsonValue value, IBJsonPreprocessorContext context)
            {
                if (value.IsString)
                    return BJsonValue.Create(ReplaceVariables(value.StringValue, context));

                if (value.IsArray)
                {
                    var array = new BJsonArray(value.ArrayValue.Count);
                    foreach (var item in value.ArrayValue)
                        array.Add(ProcessValue(item, context));
                    return BJsonValue.Create(array);
                }

                if (!value.IsObject)
                    return value;

                var obj = value.ObjectValue;
                if (TryGetBranch(obj, context, out var branch))
                    return ProcessValue(branch, context);

                var processed = new BJsonObject(obj.Count);
                foreach (var pair in obj)
                {
                    if (IsBranchMarker(pair.Key))
                        continue;

                    processed[pair.Key] = ProcessValue(pair.Value, context);
                }

                return BJsonValue.Create(processed);
            }

            private static bool TryGetBranch(BJsonObject obj, IBJsonPreprocessorContext context, out BJsonValue branch)
            {
                if (obj.TryGetValue("$branches", out var branchesValue) && branchesValue.TryGetArray(out var branches))
                {
                    var matched = false;
                    foreach (var branchValue in branches)
                    {
                        if (!branchValue.IsObject)
                            continue;

                        var branchObject = branchValue.ObjectValue;
                        if (branchObject.TryGetValue("$if", out var branchConditionValue))
                        {
                            var branchIsMatch = EvaluateCondition(branchConditionValue, context);
                            if (branchIsMatch)
                            {
                                if (branchObject.TryGetValue("$then", out var branchThenValue))
                                {
                                    branch = branchThenValue;
                                    return true;
                                }

                                matched = false;
                                continue;
                            }

                            if (branchObject.TryGetValue("$else", out var branchElseValue))
                            {
                                branch = branchElseValue;
                                return true;
                            }

                            matched = false;
                            continue;
                        }

                        if (matched)
                            continue;

                        if (branchObject.TryGetValue("$else", out var fallbackValue))
                        {
                            branch = fallbackValue;
                            return true;
                        }
                    }

                    branch = BJsonValue.Null;
                    return false;
                }

                if (!obj.TryGetValue("$if", out var conditionValue))
                {
                    branch = BJsonValue.Null;
                    return false;
                }

                var isMatch = EvaluateCondition(conditionValue, context);
                if (isMatch && obj.TryGetValue("$then", out var thenValue))
                {
                    branch = thenValue;
                    return true;
                }

                if (isMatch == false && obj.TryGetValue("$else", out var elseValue))
                {
                    branch = elseValue;
                    return true;
                }

                branch = BJsonValue.Null;
                return false;
            }

            private static bool EvaluateCondition(BJsonValue conditionValue, IBJsonPreprocessorContext context)
            {
                if (!conditionValue.IsObject)
                    return conditionValue.BoolValue;

                var conditionObject = conditionValue.ObjectValue;
                if (!conditionObject.TryGetValue("$var", out var nameValue) || !nameValue.TryGetString(out var variableName))
                    return false;

                var actualValue = context.GetVariable(variableName) ?? string.Empty;
                if (conditionObject.TryGetValue("$eq", out var expectedValue) && expectedValue.TryGetString(out var expectedString))
                    return string.Equals(actualValue, expectedString, StringComparison.OrdinalIgnoreCase);

                return string.Equals(actualValue, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            private static string ReplaceVariables(string input, IBJsonPreprocessorContext context)
            {
                if (string.IsNullOrEmpty(input))
                    return input;

                var firstStart = input.IndexOf("{{", StringComparison.Ordinal);
                if (firstStart < 0)
                    return input;

                var builder = new StringBuilder(input.Length + 16);
                var cursor = 0;
                var start = firstStart;
                while (start >= 0)
                {
                    builder.Append(input, cursor, start - cursor);

                    var end = input.IndexOf("}}", start + 2, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        builder.Append(input, start, input.Length - start);
                        return builder.ToString();
                    }

                    var name = input.Substring(start + 2, end - start - 2);
                    var replacement = context.GetVariable(name);
                    if (replacement is not null)
                        builder.Append(replacement);

                    cursor = end + 2;
                    start = input.IndexOf("{{", cursor, StringComparison.Ordinal);
                }

                builder.Append(input, cursor, input.Length - cursor);
                return builder.ToString();
            }

            private static bool IsBranchMarker(string key) => string.Equals(key, "$if", StringComparison.Ordinal)
                || string.Equals(key, "$then", StringComparison.Ordinal)
                || string.Equals(key, "$elif", StringComparison.Ordinal)
                || string.Equals(key, "$else", StringComparison.Ordinal);
        }

        private static Type? GetEnumerableElementType(Type type)
        {
            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();
                if (arguments.Length == 1 && typeof(IEnumerable<>).MakeGenericType(arguments[0]).IsAssignableFrom(type))
                    return arguments[0];
            }

            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerableInterface?.GetGenericArguments()[0];
        }

        private Type ResolvePolymorphicTargetType(BJsonValue value, Type declaredType)
        {
            if (!value.TryGetObject(out var obj))
                return declaredType;

            var discriminatorName = GetTypeDiscriminatorPropertyName(declaredType);
            if (!TryGetObjectValue(obj, discriminatorName, out var discriminatorValue) || !discriminatorValue.TryGetString(out var discriminator))
                return declaredType;

            var derivedAttributes = declaredType.GetCustomAttributes<BJsonDerivedTypeAttribute>();
            foreach (var attribute in derivedAttributes)
            {
                var mappedDiscriminator = attribute.TypeDiscriminator
                    ?? attribute.DerivedType.GetCustomAttribute<BJsonDiscriminatorValueAttribute>()?.Value
                    ?? attribute.DerivedType.FullName;

                if (string.Equals(mappedDiscriminator, discriminator, StringComparison.Ordinal)
                    || string.Equals(attribute.DerivedType.FullName, discriminator, StringComparison.Ordinal))
                    return attribute.DerivedType;
            }

            var cachedDiscriminators = PolymorphicDiscriminatorCache.GetOrAdd(declaredType, BuildDiscriminatorMap);
            if (cachedDiscriminators.TryGetValue(discriminator, out var mappedType))
                return mappedType;

            if (string.Equals(declaredType.FullName, discriminator, StringComparison.Ordinal))
                return declaredType;

            var resolved = declaredType.Assembly.GetType(discriminator, throwOnError: false, ignoreCase: false);
            return resolved is not null && declaredType.IsAssignableFrom(resolved) ? resolved : declaredType;
        }

        private static Type? ResolvePolymorphicRuntimeType(Type declaredType, Type runtimeType)
        {
            if (declaredType == runtimeType)
                return runtimeType;

            if (declaredType.GetCustomAttribute<BJsonPolymorphicAttribute>() is not null && declaredType.IsAssignableFrom(runtimeType))
                return runtimeType;

            foreach (var attribute in declaredType.GetCustomAttributes<BJsonDerivedTypeAttribute>())
            {
                if (attribute.DerivedType == runtimeType)
                    return runtimeType;
            }

            if (runtimeType.GetCustomAttribute<BJsonDiscriminatorValueAttribute>() != null && declaredType.IsAssignableFrom(runtimeType))
                return runtimeType;

            return runtimeType;
        }

        private static bool ShouldWriteTypeDiscriminator(Type type)
        {
            return type.GetCustomAttribute<BJsonPolymorphicAttribute>() is not null
                || type.BaseType?.GetCustomAttribute<BJsonPolymorphicAttribute>() is not null;
        }

        private static string GetTypeDiscriminatorPropertyName(Type type)
        {
            return type.GetCustomAttribute<BJsonPolymorphicAttribute>()?.TypeDiscriminatorPropertyName
                ?? type.BaseType?.GetCustomAttribute<BJsonPolymorphicAttribute>()?.TypeDiscriminatorPropertyName
                ?? "$type";
        }

        private static string GetDiscriminatorValue(Type type)
        {
            return type.GetCustomAttribute<BJsonDiscriminatorValueAttribute>()?.Value
                ?? type.FullName
                ?? type.Name;
        }

        private static Dictionary<string, Type> BuildDiscriminatorMap(Type declaredType)
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var candidate in declaredType.Assembly.GetTypes())
            {
                if (candidate == declaredType || !declaredType.IsAssignableFrom(candidate))
                    continue;

                var discriminator = candidate.GetCustomAttribute<BJsonDiscriminatorValueAttribute>()?.Value;
                if (string.IsNullOrWhiteSpace(discriminator))
                    continue;

                map[discriminator] = candidate;
            }

            return map;
        }

        private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
        {
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(IDictionary<,>) || definition == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    return true;
                }
            }

            var dictInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictInterface is not null)
            {
                var args = dictInterface.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }

            keyType = null!;
            valueType = null!;
            return false;
        }

    }
}
