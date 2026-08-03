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

        /// <summary>
        /// Parse [BJsonSerializable] attribute from a type symbol
        /// </summary>
        public static TypeConfiguration? ParseTypeConfiguration(INamedTypeSymbol typeSymbol)
        {
            var attribute = GetAttribute(typeSymbol, BJsonSerializableAttributeName);
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
            var converterAttr = GetAttribute(typeSymbol, BJsonConverterAttributeName);
            if (converterAttr != null)
            {
                config.CustomConverterType = GetConstructorArgument<INamedTypeSymbol>(converterAttr, 0)?.ToDisplayString();
            }

            // Check for [BJsonPolymorphic]
            var polymorphicAttr = GetAttribute(typeSymbol, BJsonPolymorphicAttributeName);
            if (polymorphicAttr != null)
            {
                config.IsPolymorphic = true;
                config.TypeDiscriminatorPropertyName = GetNamedArgument<string>(polymorphicAttr, "TypeDiscriminatorPropertyName", "$type") ?? "$type";
            }

            // Parse [BJsonDerivedType] attributes (multiple)
            var derivedTypeAttrs = typeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == BJsonDerivedTypeAttributeName);

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
            var versionContextAttr = GetAttribute(typeSymbol, BJsonVersionContextAttributeName);
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
            var preprocessorAttr = GetAttribute(typeSymbol, BJsonPreprocessorAttributeName);
            if (preprocessorAttr != null)
            {
                config.HasPreprocessor = true;
                var preprocessorType = GetConstructorArgument<INamedTypeSymbol>(preprocessorAttr, 0);
                config.PreprocessorType = preprocessorType?.ToDisplayString()
                    ?? GetNamedArgumentTypeSymbol(preprocessorAttr, "PreprocessorType")?.ToDisplayString();
            }

            // Check for [BJsonFactoryMethod] on any static method
            var factoryMethod = FindFactoryMethod(typeSymbol);
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
        public static void ParseMemberAttributes(ISymbol memberSymbol, MemberModel model)
        {
            // [BJsonProperty]
            var propertyAttr = GetAttribute(memberSymbol, BJsonPropertyAttributeName);
            if (propertyAttr != null)
            {
                model.JsonName = GetNamedArgument<string>(propertyAttr, "Name", null);
                model.Order = GetNamedArgument<int>(propertyAttr, "Order", 0);
                model.IsRequired = GetNamedArgument<bool>(propertyAttr, "Required", false);
            }

            // [BJsonPropertyName] - takes precedence over BJsonProperty.Name
            var propertyNameAttr = GetAttribute(memberSymbol, BJsonPropertyNameAttributeName);
            if (propertyNameAttr != null)
            {
                var name = GetConstructorArgument<string>(propertyNameAttr, 0);
                if (name != null)
                    model.JsonName = name;
            }

            // [BJsonRequired]
            if (HasAttribute(memberSymbol, BJsonRequiredAttributeName))
            {
                model.IsRequired = true;
            }

            // [BJsonIgnore]
            var ignoreAttr = GetAttribute(memberSymbol, BJsonIgnoreAttributeName);
            if (ignoreAttr != null)
            {
                var condition = GetNamedArgument<int>(ignoreAttr, "Condition", 0);
                model.IgnoreCondition = (IgnoreCondition)condition;
            }

            // [BJsonInclude]
            if (HasAttribute(memberSymbol, BJsonIncludeAttributeName))
            {
                model.HasIncludeAttribute = true;
            }

            // [BJsonExtensionData]
            if (HasAttribute(memberSymbol, BJsonExtensionDataAttributeName))
            {
                model.IsExtensionData = true;
            }

            // [BJsonConverter]
            var converterAttr = GetAttribute(memberSymbol, BJsonConverterAttributeName);
            if (converterAttr != null)
            {
                var converterType = GetConstructorArgument<INamedTypeSymbol>(converterAttr, 0);
                if (converterType != null)
                    model.CustomConverterType = converterType.ToDisplayString();
            }

            // [BJsonIgnoreWhen]
            var ignoreWhenAttr = GetAttribute(memberSymbol, BJsonIgnoreWhenAttributeName);
            if (ignoreWhenAttr != null)
            {
                model.IgnoreWhenMethod = GetConstructorArgument<string>(ignoreWhenAttr, 0);
            }

            // [BJsonValueMapper]
            var valueMapperAttr = GetAttribute(memberSymbol, BJsonValueMapperAttributeName);
            if (valueMapperAttr != null)
            {
                model.ValueMapperMethod = GetConstructorArgument<string>(valueMapperAttr, 0);
            }

            // [BJsonVersion]
            var versionAttr = GetAttribute(memberSymbol, BJsonVersionAttributeName);
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
            var externalRefAttr = GetAttribute(memberSymbol, BJsonExternalRefAttributeName);
            if (externalRefAttr != null)
            {
                model.IsExternalRef = true;
                model.ExternalRefFixedPath = GetNamedArgument<string>(externalRefAttr, "FixedPath", null);
                model.IsExternalRefOptional = GetNamedArgument<bool>(externalRefAttr, "Optional", false);
            }

            // [BJsonAnchor]
            var anchorAttr = GetAttribute(memberSymbol, BJsonAnchorAttributeName);
            if (anchorAttr != null)
            {
                model.AnchorName = GetConstructorArgument<string>(anchorAttr, 0);
            }

            // [BJsonDefaultValue] and [BJsonDefaultProvider]
            var defaultValueAttr = GetAttribute(memberSymbol, BJsonDefaultValueAttributeName);
            var defaultProviderAttr = GetAttribute(memberSymbol, BJsonDefaultProviderAttributeName);

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
        public static bool HasConstructorAttribute(IMethodSymbol constructorSymbol)
        {
            return HasAttribute(constructorSymbol, BJsonConstructorAttributeName);
        }

        /// <summary>
        /// Returns the static factory method marked with [BJsonFactoryMethod] on the type, if any.
        /// </summary>
        public static IMethodSymbol? FindFactoryMethod(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.IsStatic
                                     && m.MethodKind == MethodKind.Ordinary
                                     && HasAttribute(m, BJsonFactoryMethodAttributeName));
        }

        /// <summary>
        /// Get an attribute from a symbol by full name
        /// </summary>
        private static AttributeData? GetAttribute(ISymbol symbol, string attributeFullName)
        {
            return symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
        }

        /// <summary>
        /// Check if a symbol has an attribute
        /// </summary>
        private static bool HasAttribute(ISymbol symbol, string attributeFullName)
        {
            return symbol.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
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
