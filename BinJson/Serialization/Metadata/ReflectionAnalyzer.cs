#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            return new TypeMetadata(type, orderedMembers, constructor, extensionDataMember, versionContext, factoryMethod);
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
            var ignoreWhenAttr = memberInfo.GetCustomAttribute<BJsonIgnoreWhenAttribute>();
            if (ignoreWhenAttr != null)
                ignoreWhenPredicate = FindStaticMethod(declaringType, ignoreWhenAttr.MethodName);

            // ValueMapper
            MethodInfo? mapperFull = null;
            MethodInfo? mapperShort = null;
            var mapperAttr = memberInfo.GetCustomAttribute<BJsonValueMapperAttribute>();
            if (mapperAttr != null)
            {
                mapperFull = declaringType.GetMethod(mapperAttr.MethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(BJsonValue), typeof(string), typeof(IComparable), typeof(bool) },
                    null);
                if (mapperFull == null)
                {
                    mapperShort = declaringType.GetMethod(mapperAttr.MethodName,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(BJsonValue) },
                        null);
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
            var defaultProviderAttr = memberInfo.GetCustomAttribute<BJsonDefaultProviderAttribute>();
            if (defaultProviderAttr != null)
                defaultProviderMethod = FindStaticMethod(declaringType, defaultProviderAttr.MethodName);

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

            return new MemberMetadata(
                jsonName, memberType, getter, setter, converter, order, required,
                ignoreCondition, isExtensionData,
                ignoreWhenPredicate,
                mapperFull, mapperShort,
                hasDefaultConstant, defaultConstantValue,
                defaultProviderMethod,
                versionIntroducedIn, versionRemovedIn,
                legacyJsonName);
        }

        private static IComparable? ResolveVersionContext(Type type)
        {
            var attr = type.GetCustomAttribute<BJsonVersionContextAttribute>();
            if (attr == null)
                return null;
            return ParseVersion(attr.VersionType, attr.CurrentVersion);
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

        private static MethodInfo? SelectFactoryMethod(Type type, BJsonSerializerOptions options)
        {
            var staticFlags = BindingFlags.Static | BindingFlags.Public;
            if (options.IncludePrivateMembers)
                staticFlags |= BindingFlags.NonPublic;

            foreach (var method in type.GetMethods(staticFlags))
            {
                if (method.GetCustomAttribute<BJsonFactoryMethodAttribute>() != null)
                    return method;
            }
            return null;
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

        private static string ToSeparatedCase(string name, char separator)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var chars = new List<char>(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                    chars.Add(separator);

                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
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
