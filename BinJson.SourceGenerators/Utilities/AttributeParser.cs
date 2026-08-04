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
                BJsonVersionAttribute = compilation.GetTypeByMetadataName(BJsonVersionAttributeName);
                BJsonVersionContextAttribute = compilation.GetTypeByMetadataName(BJsonVersionContextAttributeName);
                BJsonExternalRefAttribute = compilation.GetTypeByMetadataName(BJsonExternalRefAttributeName);
                BJsonAnchorAttribute = compilation.GetTypeByMetadataName(BJsonAnchorAttributeName);
                BJsonPreprocessorAttribute = compilation.GetTypeByMetadataName(BJsonPreprocessorAttributeName);
                BJsonFactoryMethodAttribute = compilation.GetTypeByMetadataName(BJsonFactoryMethodAttributeName);
                BJsonValueType = compilation.GetTypeByMetadataName("Krampus.BinJson.BJsonValue");
                IComparableType = compilation.GetTypeByMetadataName("System.IComparable");
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
            public INamedTypeSymbol? BJsonVersionAttribute { get; }
            public INamedTypeSymbol? BJsonVersionContextAttribute { get; }
            public INamedTypeSymbol? BJsonExternalRefAttribute { get; }
            public INamedTypeSymbol? BJsonAnchorAttribute { get; }
            public INamedTypeSymbol? BJsonPreprocessorAttribute { get; }
            public INamedTypeSymbol? BJsonFactoryMethodAttribute { get; }
            public INamedTypeSymbol? BJsonValueType { get; }
            public INamedTypeSymbol? IComparableType { get; }
        }

        /// <summary>
        /// Parse [BJsonSerializable] attribute from a type symbol
        /// </summary>
        public static TypeConfiguration? ParseTypeConfiguration(INamedTypeSymbol typeSymbol, AttributeSymbols symbols)
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
                config.CustomConverterType = GetConstructorArgument<INamedTypeSymbol>(converterAttr, 0)?.ToDisplayString();
            }

            // Check for [BJsonPolymorphic]
            var polymorphicAttr = GetAttribute(typeSymbol, symbols.BJsonPolymorphicAttribute, BJsonPolymorphicAttributeName);
            if (polymorphicAttr != null)
            {
                config.IsPolymorphic = true;
                config.TypeDiscriminatorPropertyName = GetNamedArgument<string>(polymorphicAttr, "TypeDiscriminatorPropertyName", "$type") ?? "$type";
            }

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

            // Check for [BJsonPreprocessor]
            var preprocessorAttr = GetAttribute(typeSymbol, symbols.BJsonPreprocessorAttribute, BJsonPreprocessorAttributeName);
            if (preprocessorAttr != null)
            {
                config.HasPreprocessor = true;
                var preprocessorType = GetConstructorArgument<INamedTypeSymbol>(preprocessorAttr, 0);
                config.PreprocessorType = preprocessorType?.ToDisplayString()
                    ?? GetNamedArgumentTypeSymbol(preprocessorAttr, "PreprocessorType")?.ToDisplayString();
            }

            // Check for [BJsonFactoryMethod] on any static method
            var factoryMethod = FindFactoryMethod(typeSymbol, symbols);
            if (factoryMethod != null)
            {
                config.FactoryMethodName = factoryMethod.Name;

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
                var condition = GetNamedArgument<int>(ignoreAttr, "Condition", 0);
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
                if (converterType != null)
                    model.CustomConverterType = converterType.ToDisplayString();
            }

            // [BJsonIgnoreWhen]
            var ignoreWhenAttr = GetAttribute(memberSymbol, symbols.BJsonIgnoreWhenAttribute, BJsonIgnoreWhenAttributeName);
            if (ignoreWhenAttr != null)
            {
                model.IgnoreWhenMethod = GetConstructorArgument<string>(ignoreWhenAttr, 0);
                ValidateIgnoreWhenMethod(memberSymbol, model.IgnoreWhenMethod, diagnostics, symbols);
            }

            // [BJsonValueMapper]
            var valueMapperAttr = GetAttribute(memberSymbol, symbols.BJsonValueMapperAttribute, BJsonValueMapperAttributeName);
            if (valueMapperAttr != null)
            {
                model.ValueMapperMethod = GetConstructorArgument<string>(valueMapperAttr, 0);
                ValidateValueMapperMethod(memberSymbol, model.ValueMapperMethod, diagnostics, symbols);
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
                        model.DefaultValue = DefaultValueInfo.FromBoth(constantVal, providerMethod);
                    }
                    else
                    {
                        model.DefaultValue = DefaultValueInfo.FromProvider(providerMethod);
                    }

                    ValidateDefaultProviderMethod(memberSymbol, providerMethod, diagnostics);
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
        public static IMethodSymbol? FindFactoryMethod(INamedTypeSymbol typeSymbol, AttributeSymbols symbols)
        {
            return typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.IsStatic
                                     && m.MethodKind == MethodKind.Ordinary
                                     && HasAttribute(m, symbols.BJsonFactoryMethodAttribute, BJsonFactoryMethodAttributeName));
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

        private static void ValidateDefaultProviderMethod(ISymbol memberSymbol, string? methodName, List<Diagnostic> diagnostics)
        {
            ValidateReferencedMethod(
                memberSymbol,
                methodName,
                "BJsonDefaultProvider",
                diagnostics,
                expectedSignature: "static T Method()",
                isValidSignature: method => method.IsStatic && method.Parameters.Length == 0 && method.ReturnsVoid == false);
        }

        private static void ValidateReferencedMethod(
            ISymbol memberSymbol,
            string? methodName,
            string attributeName,
            List<Diagnostic> diagnostics,
            string expectedSignature,
            Func<IMethodSymbol, bool> isValidSignature)
        {
            if (string.IsNullOrWhiteSpace(methodName) || memberSymbol.ContainingType is null)
                return;

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
                return;
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
                return;
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
