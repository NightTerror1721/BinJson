#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Krampus.BinJson.SourceGenerators.Models;

namespace Krampus.BinJson.SourceGenerators.Utilities
{
    /// <summary>
    /// Helper class to parse BJson attributes from Roslyn symbols
    /// </summary>
    internal static class AttributeParser
    {
        // Attribute full names
        private const string BJsonSerializableAttributeName = "Krampus.BinJson.Serialization.BJsonSerializableAttribute";
        private const string BJsonPropertyAttributeName = "Krampus.BinJson.Serialization.BJsonPropertyAttribute";
        private const string BJsonPropertyNameAttributeName = "Krampus.BinJson.Serialization.BJsonPropertyNameAttribute";
        private const string BJsonIncludeAttributeName = "Krampus.BinJson.Serialization.BJsonIncludeAttribute";
        private const string BJsonRequiredAttributeName = "Krampus.BinJson.Serialization.BJsonRequiredAttribute";
        private const string BJsonIgnoreAttributeName = "Krampus.BinJson.Serialization.BJsonIgnoreAttribute";
        private const string BJsonExtensionDataAttributeName = "Krampus.BinJson.Serialization.BJsonExtensionDataAttribute";
        private const string BJsonConstructorAttributeName = "Krampus.BinJson.Serialization.BJsonConstructorAttribute";
        private const string BJsonConverterAttributeName = "Krampus.BinJson.Serialization.BJsonConverterAttribute";
        private const string BJsonPolymorphicAttributeName = "Krampus.BinJson.Serialization.BJsonPolymorphicAttribute";
        private const string BJsonDerivedTypeAttributeName = "Krampus.BinJson.Serialization.BJsonDerivedTypeAttribute";
        private const string BJsonIgnoreWhenAttributeName = "Krampus.BinJson.Serialization.BJsonIgnoreWhenAttribute";
        private const string BJsonValueMapperAttributeName = "Krampus.BinJson.Serialization.BJsonValueMapperAttribute";
        private const string BJsonDefaultValueAttributeName = "Krampus.BinJson.Serialization.BJsonDefaultValueAttribute";
        private const string BJsonDefaultProviderAttributeName = "Krampus.BinJson.Serialization.BJsonDefaultProviderAttribute";
        private const string BJsonAliasAttributeName = "Krampus.BinJson.Serialization.BJsonAliasAttribute";
        private const string BJsonRequiredWhenAttributeName = "Krampus.BinJson.Serialization.BJsonRequiredWhenAttribute";
        private const string BJsonNumberHandlingAttributeName = "Krampus.BinJson.Serialization.BJsonNumberHandlingAttribute";
        private const string BJsonConverterFactoryAttributeName = "Krampus.BinJson.Serialization.BJsonConverterFactoryAttribute";
        private const string BJsonDiscriminatorValueAttributeName = "Krampus.BinJson.Serialization.BJsonDiscriminatorValueAttribute";
        private const string BJsonOnSerializingAttributeName = "Krampus.BinJson.Serialization.BJsonOnSerializingAttribute";
        private const string BJsonOnDeserializedAttributeName = "Krampus.BinJson.Serialization.BJsonOnDeserializedAttribute";
        private const string BJsonVersionAttributeName = "Krampus.BinJson.Serialization.BJsonVersionAttribute";
        private const string BJsonVersionContextAttributeName = "Krampus.BinJson.Serialization.BJsonVersionContextAttribute";
        private const string BJsonExternalRefAttributeName = "Krampus.BinJson.Serialization.BJsonExternalRefAttribute";
        private const string BJsonAnchorAttributeName = "Krampus.BinJson.Serialization.BJsonAnchorAttribute";
        private const string BJsonPreprocessorAttributeName = "Krampus.BinJson.Serialization.BJsonPreprocessorAttribute";
        private const string BJsonFactoryMethodAttributeName = "Krampus.BinJson.Serialization.BJsonFactoryMethodAttribute";

        internal sealed class AttributeSymbols
        {
            public AttributeSymbols(Compilation compilation)
            {
                BJsonSerializableAttribute = compilation.GetTypeByMetadataName(BJsonSerializableAttributeName);
                BJsonPropertyAttribute = compilation.GetTypeByMetadataName(BJsonPropertyAttributeName);
                BJsonPropertyNameAttribute = compilation.GetTypeByMetadataName(BJsonPropertyNameAttributeName);
                BJsonIncludeAttribute = compilation.GetTypeByMetadataName(BJsonIncludeAttributeName);
                BJsonRequiredAttribute = compilation.GetTypeByMetadataName(BJsonRequiredAttributeName);
                BJsonIgnoreAttribute = compilation.GetTypeByMetadataName(BJsonIgnoreAttributeName);
                BJsonExtensionDataAttribute = compilation.GetTypeByMetadataName(BJsonExtensionDataAttributeName);
                BJsonConstructorAttribute = compilation.GetTypeByMetadataName(BJsonConstructorAttributeName);
                BJsonConverterAttribute = compilation.GetTypeByMetadataName(BJsonConverterAttributeName);
                BJsonPolymorphicAttribute = compilation.GetTypeByMetadataName(BJsonPolymorphicAttributeName);
                BJsonDerivedTypeAttribute = compilation.GetTypeByMetadataName(BJsonDerivedTypeAttributeName);
                BJsonIgnoreWhenAttribute = compilation.GetTypeByMetadataName(BJsonIgnoreWhenAttributeName);
                BJsonValueMapperAttribute = compilation.GetTypeByMetadataName(BJsonValueMapperAttributeName);
                BJsonDefaultValueAttribute = compilation.GetTypeByMetadataName(BJsonDefaultValueAttributeName);
                BJsonDefaultProviderAttribute = compilation.GetTypeByMetadataName(BJsonDefaultProviderAttributeName);
                BJsonAliasAttribute = compilation.GetTypeByMetadataName(BJsonAliasAttributeName);
                BJsonRequiredWhenAttribute = compilation.GetTypeByMetadataName(BJsonRequiredWhenAttributeName);
                BJsonNumberHandlingAttribute = compilation.GetTypeByMetadataName(BJsonNumberHandlingAttributeName);
                BJsonConverterFactoryAttribute = compilation.GetTypeByMetadataName(BJsonConverterFactoryAttributeName);
                BJsonDiscriminatorValueAttribute = compilation.GetTypeByMetadataName(BJsonDiscriminatorValueAttributeName);
                BJsonOnSerializingAttribute = compilation.GetTypeByMetadataName(BJsonOnSerializingAttributeName);
                BJsonOnDeserializedAttribute = compilation.GetTypeByMetadataName(BJsonOnDeserializedAttributeName);
                BJsonVersionAttribute = compilation.GetTypeByMetadataName(BJsonVersionAttributeName);
                BJsonVersionContextAttribute = compilation.GetTypeByMetadataName(BJsonVersionContextAttributeName);
                BJsonExternalRefAttribute = compilation.GetTypeByMetadataName(BJsonExternalRefAttributeName);
                BJsonAnchorAttribute = compilation.GetTypeByMetadataName(BJsonAnchorAttributeName);
                BJsonPreprocessorAttribute = compilation.GetTypeByMetadataName(BJsonPreprocessorAttributeName);
                BJsonFactoryMethodAttribute = compilation.GetTypeByMetadataName(BJsonFactoryMethodAttributeName);
                BJsonValueType = compilation.GetTypeByMetadataName("Krampus.BinJson.BJsonValue");
                IComparableType = compilation.GetTypeByMetadataName("System.IComparable");
                BJsonSerializationContextType = compilation.GetTypeByMetadataName("Krampus.BinJson.Serialization.BJsonSerializationContext");
                BJsonDeserializationContextType = compilation.GetTypeByMetadataName("Krampus.BinJson.Serialization.BJsonDeserializationContext");
            }

            public INamedTypeSymbol? BJsonSerializableAttribute { get; }
            public INamedTypeSymbol? BJsonPropertyAttribute { get; }
            public INamedTypeSymbol? BJsonPropertyNameAttribute { get; }
            public INamedTypeSymbol? BJsonIncludeAttribute { get; }
            public INamedTypeSymbol? BJsonRequiredAttribute { get; }
            public INamedTypeSymbol? BJsonIgnoreAttribute { get; }
            public INamedTypeSymbol? BJsonExtensionDataAttribute { get; }
            public INamedTypeSymbol? BJsonConstructorAttribute { get; }
            public INamedTypeSymbol? BJsonConverterAttribute { get; }
            public INamedTypeSymbol? BJsonPolymorphicAttribute { get; }
            public INamedTypeSymbol? BJsonDerivedTypeAttribute { get; }
            public INamedTypeSymbol? BJsonIgnoreWhenAttribute { get; }
            public INamedTypeSymbol? BJsonValueMapperAttribute { get; }
            public INamedTypeSymbol? BJsonDefaultValueAttribute { get; }
            public INamedTypeSymbol? BJsonDefaultProviderAttribute { get; }
            public INamedTypeSymbol? BJsonAliasAttribute { get; }
            public INamedTypeSymbol? BJsonRequiredWhenAttribute { get; }
            public INamedTypeSymbol? BJsonNumberHandlingAttribute { get; }
            public INamedTypeSymbol? BJsonConverterFactoryAttribute { get; }
            public INamedTypeSymbol? BJsonDiscriminatorValueAttribute { get; }
            public INamedTypeSymbol? BJsonOnSerializingAttribute { get; }
            public INamedTypeSymbol? BJsonOnDeserializedAttribute { get; }
            public INamedTypeSymbol? BJsonVersionAttribute { get; }
            public INamedTypeSymbol? BJsonVersionContextAttribute { get; }
            public INamedTypeSymbol? BJsonExternalRefAttribute { get; }
            public INamedTypeSymbol? BJsonAnchorAttribute { get; }
            public INamedTypeSymbol? BJsonPreprocessorAttribute { get; }
            public INamedTypeSymbol? BJsonFactoryMethodAttribute { get; }
            public INamedTypeSymbol? BJsonValueType { get; }
            public INamedTypeSymbol? IComparableType { get; }
            public INamedTypeSymbol? BJsonSerializationContextType { get; }
            public INamedTypeSymbol? BJsonDeserializationContextType { get; }
        }

        /// <summary>
        /// Parse [BJsonSerializable] attribute from a type symbol
        /// </summary>
        public static TypeConfiguration? ParseTypeConfiguration(INamedTypeSymbol typeSymbol, AttributeSymbols symbols, List<Diagnostic> diagnostics)
        {
            var attribute = GetAttribute(typeSymbol, symbols.BJsonSerializableAttribute, BJsonSerializableAttributeName);
            if (attribute == null)
                return null;

            var includeFields = GetNamedArgument<bool>(attribute, "IncludeFields", false);
            var includePrivateMembers = GetNamedArgument<bool>(attribute, "IncludePrivateMembers", false);
            var namingPolicy = GetNamedArgument<int>(attribute, "NamingPolicy", 0);

            var config = new TypeConfiguration(
                includeFields,
                includePrivateMembers,
                (NamingPolicy)namingPolicy);

            // Check for [BJsonConverter] on type
            var converterAttr = GetAttribute(typeSymbol, symbols.BJsonConverterAttribute, BJsonConverterAttributeName);
            if (converterAttr != null)
            {
                var converterType = GetConstructorArgument<INamedTypeSymbol>(converterAttr, 0);
                if (converterType == null || converterType.TypeKind == TypeKind.Error)
                {
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.ConverterTypeNotFound,
                        typeSymbol.Locations.FirstOrDefault() ?? Location.None,
                        converterAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "<unknown>"));
                }
                else
                {
                    config.CustomConverterType = converterType.ToDisplayString();
                }
            }

            // Check for [BJsonPolymorphic]
            var polymorphicAttr = GetAttribute(typeSymbol, symbols.BJsonPolymorphicAttribute, BJsonPolymorphicAttributeName);
            if (polymorphicAttr != null)
            {
                config.IsPolymorphic = true;
                config.TypeDiscriminatorPropertyName = GetNamedArgument<string>(polymorphicAttr, "TypeDiscriminatorPropertyName", "$type") ?? "$type";
            }
            else if (typeSymbol.BaseType != null)
            {
                var basePolymorphicAttr = GetAttribute(typeSymbol.BaseType, symbols.BJsonPolymorphicAttribute, BJsonPolymorphicAttributeName);
                if (basePolymorphicAttr != null)
                {
                    config.InheritedPolymorphicDiscriminatorPropertyName =
                        GetNamedArgument<string>(basePolymorphicAttr, "TypeDiscriminatorPropertyName", "$type") ?? "$type";

                    var explicitDiscriminator = GetAttribute(typeSymbol, symbols.BJsonDiscriminatorValueAttribute, BJsonDiscriminatorValueAttributeName);
                    config.InheritedPolymorphicDiscriminatorValue = explicitDiscriminator != null
                        ? GetConstructorArgument<string>(explicitDiscriminator, 0)
                        : null;

                    foreach (var derivedAttr in typeSymbol.BaseType.GetAttributes()
                                 .Where(a => IsAttribute(a, symbols.BJsonDerivedTypeAttribute, BJsonDerivedTypeAttributeName)))
                    {
                        var derivedType = GetConstructorArgument<INamedTypeSymbol>(derivedAttr, 0);
                        if (derivedType == null)
                            continue;

                        if (!SymbolEqualityComparer.Default.Equals(derivedType, typeSymbol))
                            continue;

                        if (config.InheritedPolymorphicDiscriminatorValue == null)
                        {
                            config.InheritedPolymorphicDiscriminatorValue =
                                GetNamedArgument<string>(derivedAttr, "TypeDiscriminator", null)
                                ?? derivedType.ToDisplayString();
                        }
                        break;
                    }

                    if (config.InheritedPolymorphicDiscriminatorValue == null)
                        config.InheritedPolymorphicDiscriminatorValue = typeSymbol.ToDisplayString();
                }
            }

            config.HasOnSerializingHooks = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Any(m => IsAttributePresent(m, symbols.BJsonOnSerializingAttribute, BJsonOnSerializingAttributeName)
                    && IsValidLifecycleHook(m, symbols.BJsonSerializationContextType, "Krampus.BinJson.Serialization.BJsonSerializationContext"));

            config.HasOnDeserializedHooks = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Any(m => IsAttributePresent(m, symbols.BJsonOnDeserializedAttribute, BJsonOnDeserializedAttributeName)
                    && IsValidLifecycleHook(m, symbols.BJsonDeserializationContextType, "Krampus.BinJson.Serialization.BJsonDeserializationContext"));

            // Parse [BJsonDerivedType] attributes (multiple)
            var derivedTypeAttrs = typeSymbol.GetAttributes()
                .Where(a => IsAttribute(a, symbols.BJsonDerivedTypeAttribute, BJsonDerivedTypeAttributeName));

            foreach (var derivedAttr in derivedTypeAttrs)
            {
                var derivedType = GetConstructorArgument<INamedTypeSymbol>(derivedAttr, 0);
                var typeDiscriminator = GetNamedArgument<string>(derivedAttr, "TypeDiscriminator", null);

                if (derivedType != null)
                {
                    config.DerivedTypes.Add(new DerivedTypeInfo(
                        derivedType.ToDisplayString(),
                        typeDiscriminator));
                }
            }

            // Check for [BJsonVersionContext]
            var versionContextAttr = GetAttribute(typeSymbol, symbols.BJsonVersionContextAttribute, BJsonVersionContextAttributeName);
            if (versionContextAttr != null)
            {
                var versionType = GetConstructorArgument<INamedTypeSymbol>(versionContextAttr, 0);
                var currentVersion = GetConstructorArgument<string>(versionContextAttr, 1);
                if (versionType != null && currentVersion != null)
                {
                    config.VersionContext = new VersionInfo(
                        versionType.ToDisplayString(),
                        introducedIn: currentVersion,
                        removedIn: null,
                        renamedFrom: null);
                }
            }

            // Check for type-level [BJsonVersion]
            var typeVersionAttr = GetAttribute(typeSymbol, symbols.BJsonVersionAttribute, BJsonVersionAttributeName);
            if (typeVersionAttr != null)
            {
                var versionType = GetConstructorArgument<INamedTypeSymbol>(typeVersionAttr, 0);
                if (versionType != null)
                {
                    config.TypeVersionRange = new VersionInfo(
                        versionType.ToDisplayString(),
                        GetConstructorArgument<string>(typeVersionAttr, 1),
                        GetConstructorArgument<string>(typeVersionAttr, 2),
                        renamedFrom: null);
                }
            }

            // Check for [BJsonPreprocessor]
            var preprocessorAttr = GetAttribute(typeSymbol, symbols.BJsonPreprocessorAttribute, BJsonPreprocessorAttributeName);
            if (preprocessorAttr != null)
            {
                config.HasPreprocessor = true;
                var preprocessorType = GetConstructorArgument<INamedTypeSymbol>(preprocessorAttr, 0);
                config.PreprocessorType = preprocessorType?.ToDisplayString()
                    ?? GetNamedArgumentTypeSymbol(preprocessorAttr, "PreprocessorType")?.ToDisplayString();
            }

            // Check for [BJsonFactoryMethod]
            var factoryMethod = FindFactoryMethod(typeSymbol, symbols, diagnostics);
            if (factoryMethod != null)
            {
                config.FactoryMethodName = factoryMethod.Name;
                config.FactoryMethodParameterMapping = ParseFactoryParameterMapping(typeSymbol, factoryMethod, symbols, diagnostics);

                // Capture factory method parameters if any
                if (factoryMethod.Parameters.Length > 0)
                {
                    config.FactoryMethodParameters = new System.Collections.Generic.List<ConstructorParameterModel>();
                    foreach (var param in factoryMethod.Parameters)
                    {
                        var paramModel = new ConstructorParameterModel(
                            param.Name,
                            param.Type.ToDisplayString(),
                            param.Type.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated,
                            param.Type.IsValueType);

                        if (config.FactoryMethodParameterMapping != null
                            && config.FactoryMethodParameterMapping.TryGetValue(param.Name, out var mappedJsonName)
                            && !string.IsNullOrWhiteSpace(mappedJsonName))
                        {
                            paramModel.JsonName = mappedJsonName;
                        }

                        config.FactoryMethodParameters.Add(paramModel);
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Parse attributes from a property or field symbol and apply to MemberModel
        /// </summary>
        public static void ParseMemberAttributes(ISymbol memberSymbol, MemberModel model, AttributeSymbols symbols, List<Diagnostic> diagnostics)
        {
            // [BJsonProperty]
            var propertyAttr = GetAttribute(memberSymbol, symbols.BJsonPropertyAttribute, BJsonPropertyAttributeName);
            if (propertyAttr != null)
            {
                model.JsonName = GetNamedArgument<string>(propertyAttr, "Name", null);
                model.Order = GetNamedArgument<int>(propertyAttr, "Order", 0);
                model.IsRequired = GetNamedArgument<bool>(propertyAttr, "Required", false);
            }

            // [BJsonPropertyName] - takes precedence over BJsonProperty.Name
            var propertyNameAttr = GetAttribute(memberSymbol, symbols.BJsonPropertyNameAttribute, BJsonPropertyNameAttributeName);
            if (propertyNameAttr != null)
            {
                var name = GetConstructorArgument<string>(propertyNameAttr, 0);
                if (name != null)
                    model.JsonName = name;
            }

            // [BJsonRequired]
            if (HasAttribute(memberSymbol, symbols.BJsonRequiredAttribute, BJsonRequiredAttributeName))
            {
                model.IsRequired = true;
            }

            // [BJsonIgnore]
            var ignoreAttr = GetAttribute(memberSymbol, symbols.BJsonIgnoreAttribute, BJsonIgnoreAttributeName);
            if (ignoreAttr != null)
            {
                var condition = GetNamedArgument<int>(ignoreAttr, "Condition", 1);
                model.IgnoreCondition = (IgnoreCondition)condition;
            }

            // [BJsonInclude]
            if (HasAttribute(memberSymbol, symbols.BJsonIncludeAttribute, BJsonIncludeAttributeName))
            {
                model.HasIncludeAttribute = true;
            }

            // [BJsonExtensionData]
            if (HasAttribute(memberSymbol, symbols.BJsonExtensionDataAttribute, BJsonExtensionDataAttributeName))
            {
                model.IsExtensionData = true;
            }

            // [BJsonConverter]
            var converterAttr = GetAttribute(memberSymbol, symbols.BJsonConverterAttribute, BJsonConverterAttributeName);
            if (converterAttr != null)
            {
                var converterType = GetConstructorArgument<INamedTypeSymbol>(converterAttr, 0);
                if (converterType == null || converterType.TypeKind == TypeKind.Error)
                {
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.ConverterTypeNotFound,
                        memberSymbol.Locations.FirstOrDefault() ?? Location.None,
                        converterAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "<unknown>"));
                }
                else
                {
                    model.CustomConverterType = converterType.ToDisplayString();
                }
            }

            // [BJsonConverterFactory]
            var converterFactoryAttr = GetAttribute(memberSymbol, symbols.BJsonConverterFactoryAttribute, BJsonConverterFactoryAttributeName);
            if (converterFactoryAttr != null)
            {
                var factoryType = GetConstructorArgument<INamedTypeSymbol>(converterFactoryAttr, 0);
                if (factoryType == null || factoryType.TypeKind == TypeKind.Error)
                {
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.ConverterTypeNotFound,
                        memberSymbol.Locations.FirstOrDefault() ?? Location.None,
                        converterFactoryAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "<unknown>"));
                }
                else
                {
                    model.CustomConverterFactoryType = factoryType.ToDisplayString();
                }
            }

            // [BJsonIgnoreWhen]
            var ignoreWhenAttr = GetAttribute(memberSymbol, symbols.BJsonIgnoreWhenAttribute, BJsonIgnoreWhenAttributeName);
            if (ignoreWhenAttr != null)
            {
                model.IgnoreWhenMethod = GetConstructorArgument<string>(ignoreWhenAttr, 0);
                ValidateIgnoreWhenMethod(memberSymbol, model.IgnoreWhenMethod, diagnostics, symbols);
            }

            // [BJsonRequiredWhen]
            var requiredWhenAttr = GetAttribute(memberSymbol, symbols.BJsonRequiredWhenAttribute, BJsonRequiredWhenAttributeName);
            if (requiredWhenAttr != null)
            {
                model.RequiredWhenMethod = GetConstructorArgument<string>(requiredWhenAttr, 0);
                model.RequiredWhenParameterCount = ValidateRequiredWhenMethod(memberSymbol, model.RequiredWhenMethod, diagnostics, symbols);
            }

            // [BJsonValueMapper]
            var valueMapperAttr = GetAttribute(memberSymbol, symbols.BJsonValueMapperAttribute, BJsonValueMapperAttributeName);
            if (valueMapperAttr != null)
            {
                model.ValueMapperMethod = GetConstructorArgument<string>(valueMapperAttr, 0);
                ValidateValueMapperMethod(memberSymbol, model.ValueMapperMethod, diagnostics, symbols);
            }

            // [BJsonAlias] (multiple)
            foreach (var aliasAttr in memberSymbol.GetAttributes().Where(a => IsAttribute(a, symbols.BJsonAliasAttribute, BJsonAliasAttributeName)))
            {
                var alias = GetConstructorArgument<string>(aliasAttr, 0);
                if (!string.IsNullOrWhiteSpace(alias))
                    model.Aliases.Add(alias!);
            }

            // [BJsonNumberHandling]
            var numberHandlingAttr = GetAttribute(memberSymbol, symbols.BJsonNumberHandlingAttribute, BJsonNumberHandlingAttributeName);
            if (numberHandlingAttr != null
                && numberHandlingAttr.ConstructorArguments.Length > 0
                && numberHandlingAttr.ConstructorArguments[0].Value is int enumValue)
            {
                model.NumberHandling = enumValue;
            }

            // [BJsonVersion]
            var versionAttr = GetAttribute(memberSymbol, symbols.BJsonVersionAttribute, BJsonVersionAttributeName);
            if (versionAttr != null)
            {
                var versionType = GetConstructorArgument<INamedTypeSymbol>(versionAttr, 0);
                if (versionType != null)
                {
                    model.Version = new VersionInfo(
                        versionType.ToDisplayString(),
                        GetConstructorArgument<string>(versionAttr, 1),
                        GetConstructorArgument<string>(versionAttr, 2),
                        GetNamedArgument<string>(versionAttr, "RenamedFrom", null));
                }
            }

            // [BJsonExternalRef]
            var externalRefAttr = GetAttribute(memberSymbol, symbols.BJsonExternalRefAttribute, BJsonExternalRefAttributeName);
            if (externalRefAttr != null)
            {
                model.IsExternalRef = true;
                model.ExternalRefFixedPath = GetNamedArgument<string>(externalRefAttr, "FixedPath", null);
                model.IsExternalRefOptional = GetNamedArgument<bool>(externalRefAttr, "Optional", false);
            }

            // [BJsonAnchor]
            var anchorAttr = GetAttribute(memberSymbol, symbols.BJsonAnchorAttribute, BJsonAnchorAttributeName);
            if (anchorAttr != null)
            {
                model.AnchorName = GetConstructorArgument<string>(anchorAttr, 0);
            }

            // [BJsonDefaultValue] and [BJsonDefaultProvider]
            var defaultValueAttr = GetAttribute(memberSymbol, symbols.BJsonDefaultValueAttribute, BJsonDefaultValueAttributeName);
            var defaultProviderAttr = GetAttribute(memberSymbol, symbols.BJsonDefaultProviderAttribute, BJsonDefaultProviderAttributeName);

            if (defaultProviderAttr != null)
            {
                var providerMethod = GetConstructorArgument<string>(defaultProviderAttr, 0);
                if (providerMethod != null)
                {
                    if (defaultValueAttr != null)
                    {
                        // Both present: provider wins, constant preserved for diagnostic
                        var constantVal = defaultValueAttr.ConstructorArguments.Length > 0
                            ? defaultValueAttr.ConstructorArguments[0].Value
                            : null;
                        var providerAcceptsVersion = ValidateDefaultProviderMethod(memberSymbol, providerMethod, diagnostics, symbols);
                        model.DefaultValue = DefaultValueInfo.FromBoth(constantVal, providerMethod, providerAcceptsVersion);

                        diagnostics.Add(Diagnostic.Create(
                            BJsonDiagnostics.ConflictingDefaultAttributes,
                            memberSymbol.Locations.FirstOrDefault() ?? Location.None,
                            memberSymbol.Name,
                            memberSymbol.ContainingType?.Name ?? "<unknown>"));
                    }
                    else
                    {
                        var providerAcceptsVersion = ValidateDefaultProviderMethod(memberSymbol, providerMethod, diagnostics, symbols);
                        model.DefaultValue = DefaultValueInfo.FromProvider(providerMethod, providerAcceptsVersion);
                    }
                }
            }
            else if (defaultValueAttr != null)
            {
                var constantVal = defaultValueAttr.ConstructorArguments.Length > 0
                    ? defaultValueAttr.ConstructorArguments[0].Value
                    : null;
                model.DefaultValue = DefaultValueInfo.FromConstant(constantVal);
            }
        }

        /// <summary>
        /// Check if a constructor has [BJsonConstructor]
        /// </summary>
        public static bool HasConstructorAttribute(IMethodSymbol constructorSymbol, AttributeSymbols symbols)
        {
            return HasAttribute(constructorSymbol, symbols.BJsonConstructorAttribute, BJsonConstructorAttributeName);
        }

        /// <summary>
        /// Returns the static factory method marked with [BJsonFactoryMethod] on the type, if any.
        /// </summary>
        public static IMethodSymbol? FindFactoryMethod(INamedTypeSymbol typeSymbol, AttributeSymbols symbols, List<Diagnostic> diagnostics)
        {
            var methods = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary
                            && HasAttribute(m, symbols.BJsonFactoryMethodAttribute, BJsonFactoryMethodAttributeName))
                .ToList();

            if (methods.Count == 0)
                return null;

            if (methods.Count > 1)
            {
                foreach (var method in methods.Skip(1))
                {
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.MultipleFactoryMethods,
                        method.Locations.FirstOrDefault() ?? Location.None,
                        typeSymbol.Name));
                }
            }

            var selected = methods[0];
            if (!IsValidFactoryMethod(typeSymbol, selected))
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.InvalidFactoryMethodSignature,
                    selected.Locations.FirstOrDefault() ?? Location.None,
                    selected.Name,
                    typeSymbol.Name));
                return null;
            }

            return selected;
        }

        private static bool IsValidFactoryMethod(INamedTypeSymbol declaringType, IMethodSymbol method)
        {
            if (!method.IsStatic || method.IsGenericMethod)
                return false;

            if (method.ReturnType is not INamedTypeSymbol returnType)
                return false;

            if (!IsSameOrDerived(returnType, declaringType))
                return false;

            foreach (var parameter in method.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                    return false;
            }

            return true;
        }

        private static bool IsSameOrDerived(INamedTypeSymbol candidate, INamedTypeSymbol baseType)
        {
            var current = candidate;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private static Dictionary<string, string>? ParseFactoryParameterMapping(
            INamedTypeSymbol typeSymbol,
            IMethodSymbol factoryMethod,
            AttributeSymbols symbols,
            List<Diagnostic> diagnostics)
        {
            var attribute = GetAttribute(factoryMethod, symbols.BJsonFactoryMethodAttribute, BJsonFactoryMethodAttributeName);
            if (attribute == null)
                return null;

            var namedArg = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ParameterMapping");
            if (namedArg.Equals(default(KeyValuePair<string, TypedConstant>)))
                return null;

            var mappingArray = namedArg.Value;
            if (mappingArray.Kind != TypedConstantKind.Array)
                return null;

            var items = mappingArray.Values.Select(v => v.Value as string).ToList();
            if (items.Count == 0)
                return null;

            if ((items.Count % 2) != 0 || items.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.InvalidFactoryParameterMapping,
                    factoryMethod.Locations.FirstOrDefault() ?? Location.None,
                    factoryMethod.Name,
                    typeSymbol.Name));
                return null;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parameterNames = new HashSet<string>(factoryMethod.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var seenParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenJsonKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < items.Count; i += 2)
            {
                var parameterName = items[i]!;
                var jsonKey = items[i + 1]!;

                if (!parameterNames.Contains(parameterName)
                    || !seenParameters.Add(parameterName)
                    || !seenJsonKeys.Add(jsonKey))
                {
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.InvalidFactoryParameterReference,
                        factoryMethod.Locations.FirstOrDefault() ?? Location.None,
                        factoryMethod.Name,
                        typeSymbol.Name,
                        parameterName));
                    continue;
                }

                map[parameterName] = jsonKey;
            }

            return map.Count > 0 ? map : null;
        }

        private static void ValidateIgnoreWhenMethod(ISymbol memberSymbol, string? methodName, List<Diagnostic> diagnostics, AttributeSymbols symbols)
        {
            ValidateReferencedMethod(
                memberSymbol,
                methodName,
                "BJsonIgnoreWhen",
                diagnostics,
                expectedSignature: "static bool Method(object? value, string propertyName, IComparable? version)",
                isValidSignature: method =>
                {
                    if (!method.IsStatic || method.Parameters.Length != 3 || method.ReturnType.SpecialType != SpecialType.System_Boolean)
                        return false;

                    if (method.Parameters[0].Type.SpecialType != SpecialType.System_Object)
                        return false;
                    if (method.Parameters[1].Type.SpecialType != SpecialType.System_String)
                        return false;
                    return IsComparable(method.Parameters[2].Type, symbols.IComparableType);
                });
        }

        private static void ValidateValueMapperMethod(ISymbol memberSymbol, string? methodName, List<Diagnostic> diagnostics, AttributeSymbols symbols)
        {
            ValidateReferencedMethod(
                memberSymbol,
                methodName,
                "BJsonValueMapper",
                diagnostics,
                expectedSignature: "static BJsonValue Method(BJsonValue value, string propertyName, IComparable? version, bool isReading)",
                isValidSignature: method =>
                {
                    if (!method.IsStatic || method.Parameters.Length != 4)
                        return false;

                    if (!IsBJsonValue(method.ReturnType, symbols.BJsonValueType))
                        return false;

                    if (!IsBJsonValue(method.Parameters[0].Type, symbols.BJsonValueType))
                        return false;
                    if (method.Parameters[1].Type.SpecialType != SpecialType.System_String)
                        return false;
                    if (!IsComparable(method.Parameters[2].Type, symbols.IComparableType))
                        return false;
                    return method.Parameters[3].Type.SpecialType == SpecialType.System_Boolean;
                });
        }

        private static int ValidateRequiredWhenMethod(ISymbol memberSymbol, string? methodName, List<Diagnostic> diagnostics, AttributeSymbols symbols)
        {
            var method = ValidateReferencedMethod(
                memberSymbol,
                methodName,
                "BJsonRequiredWhen",
                diagnostics,
                expectedSignature: "static bool Method() or static bool Method(IComparable? version) or static bool Method(string memberName, IComparable? version)",
                isValidSignature: method =>
                {
                    if (!method.IsStatic || method.ReturnType.SpecialType != SpecialType.System_Boolean)
                        return false;

                    if (method.Parameters.Length == 0)
                        return true;

                    if (method.Parameters.Length == 1)
                        return IsComparable(method.Parameters[0].Type, symbols.IComparableType);

                    if (method.Parameters.Length == 2)
                    {
                        return method.Parameters[0].Type.SpecialType == SpecialType.System_String
                            && IsComparable(method.Parameters[1].Type, symbols.IComparableType);
                    }

                    return false;
                });

            return method?.Parameters.Length ?? 0;
        }

        private static bool ValidateDefaultProviderMethod(ISymbol memberSymbol, string? methodName, List<Diagnostic> diagnostics, AttributeSymbols symbols)
        {
            var method = ValidateReferencedMethod(
                memberSymbol,
                methodName,
                "BJsonDefaultProvider",
                diagnostics,
                expectedSignature: "static T Method() or static T Method(IComparable? version)",
                isValidSignature: method =>
                {
                    if (!method.IsStatic || method.ReturnsVoid)
                        return false;

                    if (method.Parameters.Length == 0)
                        return true;

                    if (method.Parameters.Length != 1)
                        return false;

                    return IsComparable(method.Parameters[0].Type, symbols.IComparableType);
                });

            return method != null && method.Parameters.Length == 1;
        }

        private static IMethodSymbol? ValidateReferencedMethod(
            ISymbol memberSymbol,
            string? methodName,
            string attributeName,
            List<Diagnostic> diagnostics,
            string expectedSignature,
            Func<IMethodSymbol, bool> isValidSignature)
        {
            if (string.IsNullOrWhiteSpace(methodName) || memberSymbol.ContainingType is null)
                return null;

            string mappedMethodName = methodName!;

            var methods = memberSymbol.ContainingType.GetMembers(mappedMethodName)
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary)
                .ToList();

            if (methods.Count == 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.ReferencedMethodNotFound,
                    memberSymbol.Locations.FirstOrDefault() ?? Location.None,
                    methodName,
                    attributeName,
                    memberSymbol.ContainingType.Name));
                return null;
            }

            var validMethod = methods.FirstOrDefault(isValidSignature);
            if (validMethod is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.ReferencedMethodInvalidSignature,
                    memberSymbol.Locations.FirstOrDefault() ?? Location.None,
                    methodName,
                    attributeName,
                    memberSymbol.ContainingType.Name,
                    expectedSignature));
                return null;
            }

            if (!IsAccessibleFromGeneratedCode(validMethod))
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.ReferencedMethodInaccessible,
                    memberSymbol.Locations.FirstOrDefault() ?? Location.None,
                    methodName,
                    attributeName,
                    memberSymbol.ContainingType.Name));
            }

            return validMethod;
        }

        private static bool IsAttributePresent(ISymbol symbol, INamedTypeSymbol? attributeSymbol, string attributeFullName)
        {
            return symbol.GetAttributes().Any(a => IsAttribute(a, attributeSymbol, attributeFullName));
        }

        private static bool IsValidLifecycleHook(IMethodSymbol method, INamedTypeSymbol? expectedContextType, string expectedContextTypeName)
        {
            if (method.ReturnsVoid is false || method.IsStatic)
                return false;

            if (method.Parameters.Length == 0)
                return true;

            if (method.Parameters.Length != 1)
                return false;

            var parameterType = method.Parameters[0].Type;
            if (expectedContextType != null)
                return SymbolEqualityComparer.Default.Equals(parameterType, expectedContextType);

            return string.Equals(parameterType.ToDisplayString(), expectedContextTypeName, StringComparison.Ordinal);
        }

        private static bool IsAccessibleFromGeneratedCode(IMethodSymbol method)
        {
            return method.DeclaredAccessibility == Accessibility.Public
                || method.DeclaredAccessibility == Accessibility.Internal
                || method.DeclaredAccessibility == Accessibility.ProtectedOrInternal;
        }

        private static bool IsComparable(ITypeSymbol type, INamedTypeSymbol? comparableType)
        {
            if (comparableType == null)
                return string.Equals(type.ToDisplayString(), "System.IComparable", StringComparison.Ordinal);

            if (SymbolEqualityComparer.Default.Equals(type, comparableType))
                return true;

            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, comparableType));
        }

        private static bool IsBJsonValue(ITypeSymbol type, INamedTypeSymbol? bjsonValueType)
        {
            if (bjsonValueType == null)
                return string.Equals(type.ToDisplayString(), "Krampus.BinJson.BJsonValue", StringComparison.Ordinal);

            return SymbolEqualityComparer.Default.Equals(type, bjsonValueType);
        }

        /// <summary>
        /// Get an attribute from a symbol by full name
        /// </summary>
        private static AttributeData? GetAttribute(ISymbol symbol, INamedTypeSymbol? attributeSymbol, string attributeFullName)
        {
            return symbol.GetAttributes()
            .FirstOrDefault(a => IsAttribute(a, attributeSymbol, attributeFullName));
        }

        /// <summary>
        /// Check if a symbol has an attribute
        /// </summary>
        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeSymbol, string attributeFullName)
        {
            return symbol.GetAttributes()
                .Any(a => IsAttribute(a, attributeSymbol, attributeFullName));
        }

        private static bool IsAttribute(AttributeData attributeData, INamedTypeSymbol? attributeSymbol, string attributeFullName)
        {
            if (attributeData.AttributeClass == null)
                return false;

            if (attributeSymbol != null)
                return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol);

            return string.Equals(attributeData.AttributeClass.ToDisplayString(), attributeFullName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Get a named argument from an attribute
        /// </summary>
        public static T? GetNamedArgument<T>(AttributeData attribute, string name, T? defaultValue)
        {
            var namedArg = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == name);
            if (namedArg.Equals(default(KeyValuePair<string, TypedConstant>)))
                return defaultValue;

            var value = namedArg.Value.Value;
            if (value == null)
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            return defaultValue;
        }

        /// <summary>
        /// Get a constructor argument from an attribute by position
        /// </summary>
        private static T? GetConstructorArgument<T>(AttributeData attribute, int index) where T : class
        {
            if (attribute.ConstructorArguments.Length <= index)
                return null;

            var arg = attribute.ConstructorArguments[index];
            return arg.Value as T;
        }

        /// <summary>
        /// Get a named argument whose value is a Type (INamedTypeSymbol) from an attribute
        /// </summary>
        private static INamedTypeSymbol? GetNamedArgumentTypeSymbol(AttributeData attribute, string name)
        {
            var namedArg = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == name);
            if (namedArg.Equals(default(KeyValuePair<string, TypedConstant>)))
                return null;

            return namedArg.Value.Value as INamedTypeSymbol;
        }
    }
}
