#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization.Metadata
{
    internal static class ReflectionAnalyzer
    {
        public static TypeMetadata Analyze(Type type, BJsonSerializerOptions options, Func<Type, BJsonConverterAttribute?, IBJsonConverter?> resolveConverter)
        {
            if (type is null)
                throw new BJsonMetadataException("Parameter 'type' cannot be null.");
            if (options is null)
                throw new BJsonMetadataException("Parameter 'options' cannot be null.");
            if (resolveConverter is null)
                throw new BJsonMetadataException("Parameter 'resolveConverter' cannot be null.");

            var members = new List<MemberMetadata>();
            MemberMetadata? extensionDataMember = null;
            var flags = BindingFlags.Instance | BindingFlags.Public;
            if (options.IncludePrivateMembers)
                flags |= BindingFlags.NonPublic;

            // Resolve type-level version context
            var versionContext = ResolveVersionContext(type);
            var (typeVersionIntroducedIn, typeVersionRemovedIn) = ResolveTypeVersionRange(type);

            foreach (var property in type.GetProperties(flags))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                if (property.GetIndexParameters().Length != 0)
                    continue;

                var propertyIgnore = property.GetCustomAttribute<BJsonIgnoreAttribute>();
                if (propertyIgnore is not null && propertyIgnore.Condition == BJsonIgnoreCondition.Always)
                    continue;

                var include = property.GetCustomAttribute<BJsonIncludeAttribute>() is not null;
                var getter = property.GetMethod;
                var setter = property.SetMethod;
                var getterIsPublic = getter?.IsPublic == true;
                var setterIsPublic = setter?.IsPublic == true;
                if (!options.IncludePrivateMembers && (!getterIsPublic || !setterIsPublic) && !include)
                    continue;

                var propertyAttribute = property.GetCustomAttribute<BJsonPropertyAttribute>();
                var jsonName = property.GetCustomAttribute<BJsonPropertyNameAttribute>()?.Name
                    ?? propertyAttribute?.Name
                    ?? ApplyNamingPolicy(property.Name, options.NamingPolicy);
                var converter = resolveConverter(property.PropertyType, property.GetCustomAttribute<BJsonConverterAttribute>());
                if (converter is null)
                    converter = ResolveMemberConverterFactory(property.PropertyType, property.GetCustomAttribute<BJsonConverterFactoryAttribute>());
                var required = property.GetCustomAttribute<BJsonRequiredAttribute>() is not null || propertyAttribute?.Required == true;
                var order = propertyAttribute?.Order ?? 0;
                var isExtensionData = property.GetCustomAttribute<BJsonExtensionDataAttribute>() is not null;

                var metadata = BuildMemberMetadata(
                    type, jsonName, property.PropertyType,
                    instance => property.GetValue(instance),
                    (instance, memberValue) => property.SetValue(instance, memberValue),
                    converter, order, required,
                    propertyIgnore?.Condition ?? BJsonIgnoreCondition.Never,
                    isExtensionData, property);

                members.Add(metadata);
                if (isExtensionData)
                    extensionDataMember = metadata;
            }

            if (options.IncludeFields)
            {
                foreach (var field in type.GetFields(flags))
                {
                    if (field.IsInitOnly)
                        continue;

                    var fieldIgnore = field.GetCustomAttribute<BJsonIgnoreAttribute>();
                    if (fieldIgnore is not null && fieldIgnore.Condition == BJsonIgnoreCondition.Always)
                        continue;

                    var include = field.GetCustomAttribute<BJsonIncludeAttribute>() is not null;
                    if (!options.IncludePrivateMembers && !field.IsPublic && !include)
                        continue;

                    var propertyAttribute = field.GetCustomAttribute<BJsonPropertyAttribute>();
                    var jsonName = field.GetCustomAttribute<BJsonPropertyNameAttribute>()?.Name
                        ?? propertyAttribute?.Name
                        ?? ApplyNamingPolicy(field.Name, options.NamingPolicy);
                    var converter = resolveConverter(field.FieldType, field.GetCustomAttribute<BJsonConverterAttribute>());
                    if (converter is null)
                        converter = ResolveMemberConverterFactory(field.FieldType, field.GetCustomAttribute<BJsonConverterFactoryAttribute>());
                    var required = field.GetCustomAttribute<BJsonRequiredAttribute>() is not null || propertyAttribute?.Required == true;
                    var order = propertyAttribute?.Order ?? 0;
                    var isExtensionData = field.GetCustomAttribute<BJsonExtensionDataAttribute>() is not null;

                    var metadata = BuildMemberMetadata(
                        type, jsonName, field.FieldType,
                        instance => field.GetValue(instance),
                        (instance, memberValue) => field.SetValue(instance, memberValue),
                        converter, order, required,
                        fieldIgnore?.Condition ?? BJsonIgnoreCondition.Never,
                        isExtensionData, field);

                    members.Add(metadata);
                    if (isExtensionData)
                        extensionDataMember = metadata;
                }
            }

            var orderedMembers = members
                .OrderBy(m => m.Order)
                .ThenBy(m => m.JsonName, StringComparer.Ordinal)
                .ToArray();

            var constructor = SelectConstructor(type, options);
            var factoryMethod = SelectFactoryMethod(type, options);
            var factoryParameterMapping = ParseFactoryParameterMapping(type, factoryMethod);
            return new TypeMetadata(
                type,
                orderedMembers,
                constructor,
                extensionDataMember,
                versionContext,
                factoryMethod,
                factoryParameterMapping,
                typeVersionIntroducedIn,
                typeVersionRemovedIn);
        }

        private static MemberMetadata BuildMemberMetadata(
            Type declaringType,
            string jsonName,
            Type memberType,
            Func<object, object?> getter,
            Action<object, object?> setter,
            IBJsonConverter? converter,
            int order,
            bool required,
            BJsonIgnoreCondition ignoreCondition,
            bool isExtensionData,
            MemberInfo memberInfo)
        {
            // IgnoreWhen predicate
            MethodInfo? ignoreWhenPredicate = null;
            Func<object?, string, IComparable?, bool>? ignoreWhenPredicateDelegate = null;
            var ignoreWhenAttr = memberInfo.GetCustomAttribute<BJsonIgnoreWhenAttribute>();
            if (ignoreWhenAttr != null)
            {
                ignoreWhenPredicate = FindStaticMethod(declaringType, ignoreWhenAttr.MethodName);
                ignoreWhenPredicateDelegate = CreateIgnoreWhenDelegate(ignoreWhenPredicate);
            }

            // ValueMapper
            MethodInfo? mapperFull = null;
            MethodInfo? mapperShort = null;
            Func<BJsonValue, string, IComparable?, bool, BJsonValue>? mapperFullDelegate = null;
            Func<BJsonValue, BJsonValue>? mapperShortDelegate = null;
            var mapperAttr = memberInfo.GetCustomAttribute<BJsonValueMapperAttribute>();
            if (mapperAttr != null)
            {
                mapperFull = declaringType.GetMethod(mapperAttr.MethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(BJsonValue), typeof(string), typeof(IComparable), typeof(bool) },
                    null);
                if (mapperFull != null)
                {
                    mapperFullDelegate = CreateValueMapperFullDelegate(mapperFull);
                }
                else
                {
                    mapperShort = declaringType.GetMethod(mapperAttr.MethodName,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(BJsonValue) },
                        null);
                    mapperShortDelegate = CreateValueMapperShortDelegate(mapperShort);
                }
            }

            // DefaultValue constant
            bool hasDefaultConstant = false;
            object? defaultConstantValue = null;
            var defaultValueAttr = memberInfo.GetCustomAttribute<BJsonDefaultValueAttribute>();
            if (defaultValueAttr != null)
            {
                hasDefaultConstant = true;
                defaultConstantValue = defaultValueAttr.Value;
            }

            // DefaultProvider method
            MethodInfo? defaultProviderMethod = null;
            Func<IComparable?, object?>? defaultProviderDelegate = null;
            var defaultProviderAttr = memberInfo.GetCustomAttribute<BJsonDefaultProviderAttribute>();
            if (defaultProviderAttr != null)
            {
                defaultProviderMethod = FindStaticMethod(declaringType, defaultProviderAttr.MethodName);
                defaultProviderDelegate = CreateDefaultProviderDelegate(defaultProviderMethod);
            }

            // RequiredWhen method
            MethodInfo? requiredWhenMethod = null;
            Func<string, IComparable?, bool>? requiredWhenDelegate = null;
            var requiredWhenAttr = memberInfo.GetCustomAttribute<BJsonRequiredWhenAttribute>();
            if (requiredWhenAttr != null)
            {
                requiredWhenMethod = FindStaticMethod(declaringType, requiredWhenAttr.MethodName);
                requiredWhenDelegate = CreateRequiredWhenDelegate(requiredWhenMethod);
            }

            // Version range
            IComparable? versionIntroducedIn = null;
            IComparable? versionRemovedIn = null;
            string? legacyJsonName = null;
            var versionAttr = memberInfo.GetCustomAttribute<BJsonVersionAttribute>();
            if (versionAttr != null)
            {
                versionIntroducedIn = ParseVersion(versionAttr.VersionType, versionAttr.IntroducedIn);
                versionRemovedIn = ParseVersion(versionAttr.VersionType, versionAttr.RemovedIn);
                legacyJsonName = versionAttr.RenamedFrom;
            }

            var aliases = memberInfo.GetCustomAttributes<BJsonAliasAttribute>()
                .Select(a => a.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var numberHandling = memberInfo.GetCustomAttribute<BJsonNumberHandlingAttribute>()?.Handling
                ?? BJsonNumberHandling.Strict;

            return new MemberMetadata(
                memberInfo.Name,
                jsonName, memberType, getter, setter, converter, order, required,
                ignoreCondition, isExtensionData,
                ignoreWhenPredicate,
                mapperFull, mapperShort,
                hasDefaultConstant, defaultConstantValue,
                defaultProviderMethod,
                requiredWhenMethod,
                ignoreWhenPredicateDelegate,
                mapperFullDelegate,
                mapperShortDelegate,
                defaultProviderDelegate,
                requiredWhenDelegate,
                versionIntroducedIn, versionRemovedIn,
                legacyJsonName,
                aliases,
                numberHandling);
        }

        private static IComparable? ResolveVersionContext(Type type)
        {
            var attr = type.GetCustomAttribute<BJsonVersionContextAttribute>();
            if (attr == null)
                return null;
            return ParseVersion(attr.VersionType, attr.CurrentVersion);
        }

        private static (IComparable? IntroducedIn, IComparable? RemovedIn) ResolveTypeVersionRange(Type type)
        {
            var attr = type.GetCustomAttribute<BJsonVersionAttribute>();
            if (attr == null)
                return (null, null);

            return (
                ParseVersion(attr.VersionType, attr.IntroducedIn),
                ParseVersion(attr.VersionType, attr.RemovedIn));
        }

        private static IComparable? ParseVersion(Type versionType, string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            try
            {
                var parseMethod = versionType.GetMethod("Parse",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(string) },
                    null);

                if (parseMethod == null)
                    return null;

                return parseMethod.Invoke(null, new object[] { raw! }) as IComparable;
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo? FindStaticMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static Func<object?, string, IComparable?, bool>? CreateIgnoreWhenDelegate(MethodInfo? method)
        {
            if (method == null)
                return null;

            try
            {
                return (Func<object?, string, IComparable?, bool>)method.CreateDelegate(typeof(Func<object?, string, IComparable?, bool>));
            }
            catch
            {
                return null;
            }
        }

        private static Func<BJsonValue, string, IComparable?, bool, BJsonValue>? CreateValueMapperFullDelegate(MethodInfo? method)
        {
            if (method == null)
                return null;

            try
            {
                return (Func<BJsonValue, string, IComparable?, bool, BJsonValue>)method.CreateDelegate(typeof(Func<BJsonValue, string, IComparable?, bool, BJsonValue>));
            }
            catch
            {
                return null;
            }
        }

        private static Func<BJsonValue, BJsonValue>? CreateValueMapperShortDelegate(MethodInfo? method)
        {
            if (method == null)
                return null;

            try
            {
                return (Func<BJsonValue, BJsonValue>)method.CreateDelegate(typeof(Func<BJsonValue, BJsonValue>));
            }
            catch
            {
                return null;
            }
        }

        private static Func<IComparable?, object?>? CreateDefaultProviderDelegate(MethodInfo? method)
        {
            if (method == null)
                return null;

            var parameters = method.GetParameters();

            try
            {
                if (parameters.Length == 0)
                {
                    var noArgsDelegate = (Func<object?>)method.CreateDelegate(typeof(Func<object?>));
                    return _ => noArgsDelegate();
                }

                if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(IComparable))
                {
                    return (Func<IComparable?, object?>)method.CreateDelegate(typeof(Func<IComparable?, object?>));
                }
            }
            catch
            {
            }

            return null;
        }

        private static Func<string, IComparable?, bool>? CreateRequiredWhenDelegate(MethodInfo? method)
        {
            if (method == null)
                return null;

            var parameters = method.GetParameters();
            if (!method.IsStatic || method.ReturnType != typeof(bool))
                return null;

            try
            {
                if (parameters.Length == 0)
                {
                    var noArgs = (Func<bool>)method.CreateDelegate(typeof(Func<bool>));
                    return (_, _) => noArgs();
                }

                if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(IComparable))
                {
                    var versionOnly = (Func<IComparable?, bool>)method.CreateDelegate(typeof(Func<IComparable?, bool>));
                    return (_, version) => versionOnly(version);
                }

                if (parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(IComparable))
                {
                    return (Func<string, IComparable?, bool>)method.CreateDelegate(typeof(Func<string, IComparable?, bool>));
                }
            }
            catch
            {
            }

            return null;
        }

        private static MethodInfo? SelectFactoryMethod(Type type, BJsonSerializerOptions options)
        {
            // Factory methods are explicit opt-in via attribute; include all access levels for parity
            // with generated serializers that run in the declaring partial type.
            var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var methods = type.GetMethods(staticFlags)
                .Where(m => m.GetCustomAttribute<BJsonFactoryMethodAttribute>() != null)
                .ToArray();

            if (methods.Length == 0)
                return null;

            if (methods.Length > 1)
            {
                throw new BJsonMetadataException(
                    $"Type '{type.FullName}' has multiple methods marked with [BJsonFactoryMethod]. Only one is allowed.");
            }

            var method = methods[0];
            if (!IsValidFactoryMethod(type, method))
            {
                throw new BJsonMetadataException(
                    $"Factory method '{method.Name}' on type '{type.FullName}' must be static, non-generic, return '{type.FullName}' (or derived), and avoid ref/out parameters.");
            }

            return method;
        }

        private static bool IsValidFactoryMethod(Type declaringType, MethodInfo method)
        {
            if (!method.IsStatic || method.IsGenericMethod)
                return false;

            if (!declaringType.IsAssignableFrom(method.ReturnType))
                return false;

            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType.IsByRef || parameter.IsOut)
                    return false;
            }

            return true;
        }

        private static IReadOnlyDictionary<string, string>? ParseFactoryParameterMapping(Type type, MethodInfo? factoryMethod)
        {
            if (factoryMethod == null)
                return null;

            var attribute = factoryMethod.GetCustomAttribute<BJsonFactoryMethodAttribute>();
            var raw = attribute?.ParameterMapping;
            if (raw == null || raw.Length == 0)
                return null;

            if ((raw.Length % 2) != 0)
            {
                throw new BJsonMetadataException(
                    $"Factory method '{factoryMethod.Name}' on type '{type.FullName}' has invalid ParameterMapping. Expected alternating ['paramName', 'jsonKey'] pairs.");
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parameterNames = new HashSet<string>(factoryMethod.GetParameters().Select(p => p.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            var seenParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenJsonKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < raw.Length; i += 2)
            {
                var parameterName = raw[i];
                var jsonKey = raw[i + 1];

                if (string.IsNullOrWhiteSpace(parameterName) || string.IsNullOrWhiteSpace(jsonKey))
                {
                    throw new BJsonMetadataException(
                        $"Factory method '{factoryMethod.Name}' on type '{type.FullName}' has invalid ParameterMapping entries. Parameter names and JSON keys must be non-empty.");
                }

                if (!parameterNames.Contains(parameterName)
                    || !seenParameters.Add(parameterName)
                    || !seenJsonKeys.Add(jsonKey))
                {
                    throw new BJsonMetadataException(
                        $"Factory method '{factoryMethod.Name}' on type '{type.FullName}' has invalid ParameterMapping target '{parameterName}'.");
                }

                map[parameterName] = jsonKey;
            }

            return map;
        }

        private static string ApplyNamingPolicy(string name, NamingPolicy namingPolicy)
        {
            return namingPolicy switch
            {
                NamingPolicy.CamelCase when name.Length > 0 => char.ToLowerInvariant(name[0]) + name.Substring(1),
                NamingPolicy.SnakeCase => ToSeparatedCase(name, '_'),
                NamingPolicy.KebabCase => ToSeparatedCase(name, '-'),
                _ => name
            };
        }

        private static IBJsonConverter? ResolveMemberConverterFactory(Type memberType, BJsonConverterFactoryAttribute? attribute)
        {
            if (attribute is null)
                return null;

            if (!typeof(IBJsonConverterFactory).IsAssignableFrom(attribute.FactoryType))
                throw new BJsonMetadataException($"Converter factory '{attribute.FactoryType.FullName}' must implement {nameof(IBJsonConverterFactory)}.");

            if (Activator.CreateInstance(attribute.FactoryType) is not IBJsonConverterFactory factory)
                throw new BJsonMetadataException($"Converter factory '{attribute.FactoryType.FullName}' could not be instantiated.");

            if (!factory.CanConvert(memberType))
                return null;

            return factory.CreateConverter(memberType);
        }

        private static string ToSeparatedCase(string name, char separator)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var builder = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                    builder.Append(separator);

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static ConstructorMetadata? SelectConstructor(Type type, BJsonSerializerOptions options)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | (options.IncludePrivateMembers ? BindingFlags.NonPublic : 0));
            if (constructors.Length == 0)
                return null;

            var attributed = constructors.FirstOrDefault(c => c.GetCustomAttribute<BJsonConstructorAttribute>() is not null);
            if (attributed is not null)
                return new ConstructorMetadata(attributed, isPreferred: true);

            var parameterless = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            if (parameterless is not null)
                return new ConstructorMetadata(parameterless, isPreferred: false);

            var longest = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            return new ConstructorMetadata(longest, isPreferred: false);
        }
    }
}
