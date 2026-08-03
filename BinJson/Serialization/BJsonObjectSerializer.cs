#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization.Metadata;

namespace Krampus.BinJson.Serialization
{
    internal sealed class BJsonObjectSerializer
    {
        private readonly BJsonSerializerOptions _options;
        private readonly BJsonSerializationContext _context;
        private readonly MetadataCache _metadataCache;
        private readonly Dictionary<Type, IBJsonConverter?> _converterCache;

        public BJsonObjectSerializer(BJsonSerializerOptions? options)
        {
            _options = options ?? new BJsonSerializerOptions();
            _context = new BJsonSerializationContext(this, _options);
            _metadataCache = new MetadataCache();
            _converterCache = new Dictionary<Type, IBJsonConverter?>();
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
                var metadata = GetTypeMetadata(type);
                var members = metadata.Members;
                var discriminatorName = GetTypeDiscriminatorPropertyName(type);
                var includeTypeDiscriminator = ShouldWriteTypeDiscriminator(type);
                var extraMetadataCount = (referenceId is null ? 0 : 1) + (includeTypeDiscriminator ? 1 : 0);
                var obj = new BJsonObject(members.Length + extraMetadataCount);
                if (referenceId is not null)
                    obj["$id"] = BJsonValue.Create(referenceId);
                if (includeTypeDiscriminator)
                    obj[discriminatorName] = BJsonValue.Create(type.FullName);

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
                        if (!member.IsInVersionRange(activeVersion))
                            continue;

                        // Static predicate check
                        if (member.IgnoreWhenPredicate != null)
                        {
                            var shouldIgnore = member.IgnoreWhenPredicate.Invoke(null, new object?[] { memberValue, member.JsonName, activeVersion });
                            if (shouldIgnore is true)
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

                        if (member.IgnoreCondition == BJsonIgnoreCondition.WhenWritingCustomDefault && IsDefaultValue(memberValue, member.MemberType))
                            continue;

                        if (member.IgnoreCondition == BJsonIgnoreCondition.WhenWriting)
                            continue;

                        // Value mapper (write direction)
                        serializedValue = ApplyValueMapper(member, serializedValue, activeVersion, isReading: false);

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
            var instance = CreateObjectInstance(targetType, metadata, obj);
            var consumedNames = new HashSet<string>(StringComparer.Ordinal);

            var activeVersion = _options.Version ?? metadata.VersionContext;

            foreach (var member in metadata.Members)
            {
                if (member.IsExtensionData)
                    continue;

                if (member.IgnoreCondition == BJsonIgnoreCondition.WhenReading)
                    continue;

                // Version range guard
                if (!member.IsInVersionRange(activeVersion))
                    continue;

                // Try primary key, then legacy key (RenamedFrom)
                BJsonValue jsonMemberValue;
                if (!TryGetObjectValue(obj, member.JsonName, out jsonMemberValue))
                {
                    if (member.LegacyJsonName != null && TryGetObjectValue(obj, member.LegacyJsonName, out jsonMemberValue))
                    {
                        consumedNames.Add(member.LegacyJsonName);
                    }
                    else
                    {
                        if (_options.StrictMode && member.Required)
                            throw new BJsonDeserializationException($"Required member '{member.JsonName}' was not found while deserializing '{targetType.FullName}'.");

                        // Apply default value if key is absent
                        ApplyDefaultValue(member, instance);
                        continue;
                    }
                }
                else
                {
                    consumedNames.Add(member.JsonName);
                }

                // Static predicate check
                if (member.IgnoreWhenPredicate != null)
                {
                    var rawForPredicate = member.Getter(instance);
                    var shouldIgnore = member.IgnoreWhenPredicate.Invoke(null, new object?[] { rawForPredicate, member.JsonName, activeVersion });
                    if (shouldIgnore is true)
                        continue;
                }

                // Apply value mapper (read direction) before deserialization
                jsonMemberValue = ApplyValueMapper(member, jsonMemberValue, activeVersion, isReading: true);

                object? deserialized;
                if (member.Converter is not null)
                    deserialized = member.Converter.Deserialize(jsonMemberValue, _context);
                else
                    deserialized = DeserializeValue(jsonMemberValue, member.MemberType);

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

            return instance;
        }

        private object CreateObjectInstance(Type targetType, TypeMetadata metadata, BJsonObject obj)
        {
            // Factory method takes precedence over constructor
            if (metadata.FactoryMethod != null)
            {
                var factoryParams = metadata.FactoryMethod.GetParameters();
                var factoryArgs = new object?[factoryParams.Length];
                for (int i = 0; i < factoryParams.Length; i++)
                {
                    var parameter = factoryParams[i];
                    if (TryGetObjectValue(obj, parameter.Name ?? string.Empty, out var parameterValue))
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
                if (TryGetObjectValue(obj, parameter.Name ?? string.Empty, out var parameterValue))
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

        private BJsonValue ApplyValueMapper(MemberMetadata member, BJsonValue value, IComparable? activeVersion, bool isReading)
        {
            if (member.ValueMapperFullSignature != null)
            {
                var result = member.ValueMapperFullSignature.Invoke(null, new object?[] { value, member.JsonName, activeVersion, isReading });
                return result is BJsonValue bv ? bv : value;
            }

            if (member.ValueMapperShortSignature != null)
            {
                var result = member.ValueMapperShortSignature.Invoke(null, new object?[] { value });
                return result is BJsonValue bv2 ? bv2 : value;
            }

            return value;
        }

        private void ApplyDefaultValue(MemberMetadata member, object instance)
        {
            if (member.DefaultProviderMethod != null)
            {
                var defaultVal = member.DefaultProviderMethod.Invoke(null, null);
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

        private bool TryGetObjectValue(BJsonObject obj, string propertyName, out BJsonValue value)
        {
            if (obj.TryGetValue(propertyName, out value))
                return true;

            if (!_options.PropertyNameCaseInsensitive)
                return false;

            foreach (var pair in obj)
            {
                if (string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            return false;
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

            if (_options.TryGetConverter(memberType, out var converter))
                return converter;

            return null;
        }

        private static bool IsDefaultValue(object? value, Type type)
        {
            if (value is null)
                return true;

            if (!type.IsValueType)
                return false;

            return value.Equals(Activator.CreateInstance(type));
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
                if (string.Equals(attribute.TypeDiscriminator ?? attribute.DerivedType.FullName, discriminator, StringComparison.Ordinal))
                    return attribute.DerivedType;
            }

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
