#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Shared runtime support for advanced attribute behavior used by the reflection and generated paths.
    /// </summary>
    public static class BJsonAttributeRuntimeSupport
    {
        private static readonly ConcurrentDictionary<Type, SerializableMemberInfo[]> SerializableMemberCache = new ConcurrentDictionary<Type, SerializableMemberInfo[]>();
        private static readonly ConcurrentDictionary<Type, LifecycleHooks> LifecycleHookCache = new ConcurrentDictionary<Type, LifecycleHooks>();

        public static BJsonValue ApplyPreprocessorPipeline(BJsonValue value, Type targetType, BJsonSerializationContext context)
        {
            var preprocessorAttribute = targetType.GetCustomAttribute<BJsonPreprocessorAttribute>();
            if (preprocessorAttribute is null)
                return value;

            var preprocessorContext = context.Options.PreprocessorContext as BJsonPreprocessorContext ?? new BJsonPreprocessorContext();
            if (context.Options.PreprocessorContext is null)
                context.Options.PreprocessorContext = preprocessorContext;

            var preprocessor = CreatePreprocessor(preprocessorAttribute);
            object processed;
            try
            {
                processed = preprocessor.Process(value, preprocessorContext);
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
                ApplyAttributeRules(value.ObjectValue, targetType, preprocessorContext, context);

            return value;
        }

        public static void InvokeOnSerializingHooks(object instance, BJsonSerializationContext context)
        {
            var hooks = GetLifecycleHooks(instance.GetType());
            foreach (var hook in hooks.OnSerializing)
                InvokeLifecycleHook(instance, hook, context);
        }

        public static void InvokeOnDeserializedHooks(object instance, BJsonSerializationContext context)
        {
            var hooks = GetLifecycleHooks(instance.GetType());
            var deserializationContext = new BJsonDeserializationContext(context.Serializer, context.Options, instance.GetType());
            foreach (var hook in hooks.OnDeserialized)
                InvokeLifecycleHook(instance, hook, deserializationContext);
        }

        public static BJsonValue ApplyNumberHandlingOnWrite(BJsonValue serializedValue, object? memberValue, int handling)
        {
            const int writeAsString = (int)BJsonNumberHandling.WriteAsString;
            const int lossless = (int)BJsonNumberHandling.Lossless;

            if (memberValue is null)
                return serializedValue;

            if ((handling & (writeAsString | lossless)) == 0)
                return serializedValue;

            if (!TryFormatNumericForWrite(memberValue, out var raw))
                return serializedValue;

            return BJsonValue.Create(raw);
        }

        public static bool TryDeserializeNumericMember(BJsonValue value, Type memberType, int handling, out object? parsed)
        {
            const int allowFromString = (int)BJsonNumberHandling.AllowReadingFromString;
            const int lossless = (int)BJsonNumberHandling.Lossless;

            parsed = null;
            if (!value.IsString)
                return false;

            if ((handling & (allowFromString | lossless)) == 0)
                return false;

            if (!value.TryGetString(out var raw) || raw is null)
                return false;

            return TryParseNumericString(raw, Nullable.GetUnderlyingType(memberType) ?? memberType, out parsed);
        }

        private static IBJsonPreprocessor CreatePreprocessor(BJsonPreprocessorAttribute attribute)
        {
            if (attribute.PreprocessorType is null)
                return new BuiltInPreprocessor();

            if (Activator.CreateInstance(attribute.PreprocessorType) is not IBJsonPreprocessor preprocessor)
                throw new BJsonDeserializationException($"Preprocessor type '{attribute.PreprocessorType.FullName}' must implement {nameof(IBJsonPreprocessor)} and expose a parameterless constructor.");

            return preprocessor;
        }

        private static bool TryFormatNumericForWrite(object value, out string raw)
        {
            switch (value)
            {
                case byte b:
                    raw = b.ToString(CultureInfo.InvariantCulture);
                    return true;
                case sbyte sb:
                    raw = sb.ToString(CultureInfo.InvariantCulture);
                    return true;
                case short s:
                    raw = s.ToString(CultureInfo.InvariantCulture);
                    return true;
                case ushort us:
                    raw = us.ToString(CultureInfo.InvariantCulture);
                    return true;
                case int i:
                    raw = i.ToString(CultureInfo.InvariantCulture);
                    return true;
                case uint ui:
                    raw = ui.ToString(CultureInfo.InvariantCulture);
                    return true;
                case long l:
                    raw = l.ToString(CultureInfo.InvariantCulture);
                    return true;
                case ulong ul:
                    raw = ul.ToString(CultureInfo.InvariantCulture);
                    return true;
                case float f:
                    raw = f.ToString("R", CultureInfo.InvariantCulture);
                    return true;
                case double d:
                    raw = d.ToString("R", CultureInfo.InvariantCulture);
                    return true;
                case decimal m:
                    raw = m.ToString(CultureInfo.InvariantCulture);
                    return true;
                default:
                    raw = string.Empty;
                    return false;
            }
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

        private static void ApplyAttributeRules(BJsonObject obj, Type targetType, BJsonPreprocessorContext context, BJsonSerializationContext serializationContext)
        {
            var members = GetCachedSerializableMembers(targetType);

            foreach (var member in members)
            {
                var anchorAttribute = member.AnchorAttribute;
                if (anchorAttribute is null)
                    continue;

                foreach (var key in member.CandidatePropertyNames)
                {
                    if (obj.TryGetValue(key, out var memberValue))
                    {
                        context.RegisterAnchor(anchorAttribute.AnchorName, memberValue);
                        break;
                    }
                }
            }

            foreach (var property in obj)
            {
                if (property.Value.IsObject && property.Value.ObjectValue.TryGetValue("$ref", out var refToken) && refToken.TryGetString(out var anchorName))
                {
                    if (context.TryGetAnchor(anchorName, out var anchoredValue))
                        obj[property.Key] = anchoredValue;
                }
            }

            foreach (var member in members)
            {
                var externalReferenceAttribute = member.ExternalReferenceAttribute;
                if (externalReferenceAttribute is null)
                    continue;

                foreach (var key in member.CandidatePropertyNames)
                {
                    if (!obj.TryGetValue(key, out var memberValue))
                        continue;

                    var path = ResolveExternalReferencePath(externalReferenceAttribute, memberValue, context, serializationContext.Options);
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
                        // Keep the external payload as BJson and let the generated or
                        // reflection binding path deserialize it once for the target member.
                        obj[key] = BJson.DeserializeFromFile(path);
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

        private static SerializableMemberInfo[] GetCachedSerializableMembers(Type targetType)
        {
            return SerializableMemberCache.GetOrAdd(targetType, static type =>
            {
                var members = new List<SerializableMemberInfo>();
                foreach (var member in GetSerializableMembers(type))
                {
                    var names = GetCandidatePropertyNames(member).Distinct(StringComparer.Ordinal).ToArray();
                    members.Add(new SerializableMemberInfo(
                        member,
                        names,
                        member.GetCustomAttribute<BJsonAnchorAttribute>(),
                        member.GetCustomAttribute<BJsonExternalRefAttribute>()));
                }

                return members.ToArray();
            });
        }

        private static IEnumerable<MemberInfo> GetSerializableMembers(Type targetType)
        {
            foreach (var property in targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.GetIndexParameters().Length != 0)
                    continue;

                yield return property;
            }

            foreach (var field in targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsStatic)
                    continue;

                yield return field;
            }
        }

        private static IEnumerable<string> GetCandidatePropertyNames(MemberInfo member)
        {
            var propertyNameAttribute = member.GetCustomAttribute<BJsonPropertyNameAttribute>();
            if (!string.IsNullOrWhiteSpace(propertyNameAttribute?.Name))
                yield return propertyNameAttribute.Name!;

            var propertyAttribute = member.GetCustomAttribute<BJsonPropertyAttribute>();
            if (!string.IsNullOrWhiteSpace(propertyAttribute?.Name))
                yield return propertyAttribute.Name!;

            foreach (var alias in member.GetCustomAttributes<BJsonAliasAttribute>())
            {
                if (!string.IsNullOrWhiteSpace(alias.Name))
                    yield return alias.Name;
            }

            yield return member.Name;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
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

        private static string? ResolveExternalReferencePath(BJsonExternalRefAttribute attribute, BJsonValue memberValue, BJsonPreprocessorContext context, BJsonSerializerOptions options)
        {
            if (!string.IsNullOrWhiteSpace(attribute.FixedPath))
                return ResolvePath(attribute.FixedPath, context, options);

            if (memberValue.TryGetString(out var stringPath) && !string.IsNullOrWhiteSpace(stringPath))
                return ResolvePath(stringPath, context, options);

            return null;
        }

        private static string ResolvePath(string path, BJsonPreprocessorContext context, BJsonSerializerOptions options)
        {
            if (Path.IsPathRooted(path))
                return EnsureExternalReferencePathPolicy(path, context, options);

            var basePath = context.BasePath;
            if (string.IsNullOrWhiteSpace(basePath))
                basePath = Environment.CurrentDirectory;

            return EnsureExternalReferencePathPolicy(Path.GetFullPath(Path.Combine(basePath!, path)), context, options);
        }

        private static string EnsureExternalReferencePathPolicy(string path, BJsonPreprocessorContext context, BJsonSerializerOptions options)
        {
            var fullPath = Path.GetFullPath(path);
            if (options.ExternalReferencePathPolicy == ExternalReferencePathPolicy.AllowAny)
                return fullPath;

            // Shared helper uses context only; policy is read from options in callers that own serialization settings.
            // For generated pipeline, keep the same default-safe behavior as runtime.
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

        private readonly struct SerializableMemberInfo
        {
            public SerializableMemberInfo(
                MemberInfo member,
                string[] candidatePropertyNames,
                BJsonAnchorAttribute? anchorAttribute,
                BJsonExternalRefAttribute? externalReferenceAttribute)
            {
                Member = member;
                CandidatePropertyNames = candidatePropertyNames;
                AnchorAttribute = anchorAttribute;
                ExternalReferenceAttribute = externalReferenceAttribute;
            }

            public MemberInfo Member { get; }

            public string[] CandidatePropertyNames { get; }

            public BJsonAnchorAttribute? AnchorAttribute { get; }

            public BJsonExternalRefAttribute? ExternalReferenceAttribute { get; }
        }
    }
}
