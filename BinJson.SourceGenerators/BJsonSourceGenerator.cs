#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Krampus.BinJson.SourceGenerators.Models;
using Krampus.BinJson.SourceGenerators.Utilities;

namespace Krampus.BinJson.SourceGenerators
{
    [Generator]
    public sealed class BJsonSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "Krampus.BinJson.Serialization.BJsonSerializableAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax || node is StructDeclarationSyntax,
                transform: static (generatorContext, _) => AnalyzeType(generatorContext.TargetSymbol as INamedTypeSymbol, generatorContext.SemanticModel.Compilation))
                .Where(static result => result != null);

            context.RegisterSourceOutput(candidates, static (productionContext, result) =>
            {
                if (result is null)
                    return;

                // Report all diagnostics
                foreach (var diagnostic in result.Diagnostics)
                {
                    productionContext.ReportDiagnostic(diagnostic);
                }

                // Generate source only if model is valid
                if (result.Model != null)
                {
                    var source = CodeEmitter.EmitSerializer(result.Model);
                    var hintName = string.IsNullOrEmpty(result.Model.HintName)
                        ? $"{result.Model.TypeName}.BJson.g.cs"
                        : $"{result.Model.HintName}.BJson.g.cs";
                    productionContext.AddSource(hintName, SourceText.From(source, System.Text.Encoding.UTF8));
                }
            });
        }

        /// <summary>
        /// Analyze a type declaration and build complete GeneratedTypeModel
        /// </summary>
        private static AnalysisResult? AnalyzeType(INamedTypeSymbol? symbol, Compilation compilation)
        {
            var diagnostics = new List<Diagnostic>();

            if (symbol == null)
                return null;

            if (symbol.IsGenericType)
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.UnsupportedTypeShape,
                    symbol.Locations.FirstOrDefault() ?? Location.None,
                    symbol.Name,
                    "Open generic types are not supported in generated serializers."));
                return new AnalysisResult(model: null, diagnostics);
            }

            if (symbol.ContainingType != null)
            {
                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.UnsupportedTypeShape,
                    symbol.Locations.FirstOrDefault() ?? Location.None,
                    symbol.Name,
                    "Nested types are not supported in generated serializers."));
                return new AnalysisResult(model: null, diagnostics);
            }

            var attributeSymbols = new AttributeParser.AttributeSymbols(compilation);

            // Parse [BJsonSerializable] attribute
            var configuration = AttributeParser.ParseTypeConfiguration(symbol, attributeSymbols, diagnostics);
            if (configuration == null)
                return null;

            // If type has a custom converter, we don't generate code
            if (configuration.CustomConverterType != null)
                return null;

            // Create model
            var namespaceName = symbol.ContainingNamespace is null || symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString();

            var model = new GeneratedTypeModel(
                namespaceName,
                symbol.Name,
                symbol.IsValueType,
                symbol.IsAbstract,
                configuration);

            model.HintName = BuildHintName(symbol);

            // Analyze properties
            foreach (var member in GetAllPropertySymbols(symbol))
            {
                if (!ShouldIncludeProperty(member, configuration, attributeSymbols))
                    continue;

                var property = CreatePropertyModel(member, configuration, attributeSymbols, diagnostics);
                if (property != null)
                    model.Properties.Add(property);
            }

            // Analyze fields (if IncludeFields = true)
            if (configuration.IncludeFields)
            {
                foreach (var member in GetAllFieldSymbols(symbol))
                {
                    // Skip compiler-generated backing fields
                    if (member.IsImplicitlyDeclared)
                        continue;

                    if (!ShouldIncludeField(member, configuration, attributeSymbols))
                        continue;

                    var field = CreateFieldModel(member, configuration, attributeSymbols, diagnostics);
                    if (field != null)
                        model.Fields.Add(field);
                }
            }

            // Find extension data member
            var extensionDataMembers = model.AllMembers.Where(m => m.IsExtensionData).ToList();

            // Validate: only one extension data member allowed
            if (extensionDataMembers.Count > 1)
            {
                foreach (var member in extensionDataMembers.Skip(1))
                {
                    var location = GetMemberLocation(symbol, member);
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.MultipleExtensionDataMembers,
                        location,
                        symbol.Name));
                }
                model.ExtensionDataMember = extensionDataMembers.First();
            }
            else if (extensionDataMembers.Count == 1)
            {
                model.ExtensionDataMember = extensionDataMembers.First();
            }

            // Validate extension data member type
            if (model.ExtensionDataMember != null)
            {
                var memberSymbol = GetMemberSymbol(symbol, model.ExtensionDataMember);
                if (memberSymbol != null && !TypeRegistry.IsExtensionDataDictionary(memberSymbol.GetMemberType()))
                {
                    // Invalid extension data type
                    var location = GetMemberLocation(symbol, model.ExtensionDataMember);
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.InvalidExtensionDataType,
                        location,
                        model.ExtensionDataMember.MemberName));
                    model.ExtensionDataMember = null;
                }
            }

            // Find constructor
            model.Constructor = FindConstructor(symbol, diagnostics, attributeSymbols);

            // Match constructor parameters to members (for JSON name resolution)
            if (model.Constructor != null)
            {
                MatchConstructorParametersToMembers(model.Constructor, model, symbol, diagnostics);
            }

            // Match factory method parameters to members (if factory method is present)
            if (model.Configuration.FactoryMethodParameters != null && model.Configuration.FactoryMethodParameters.Count > 0)
            {
                MatchFactoryParametersToMembers(model.Configuration.FactoryMethodParameters, model, symbol, diagnostics);
            }

            // Validate conflicting JSON names
            ValidateJsonNames(model, symbol, diagnostics);

            return new AnalysisResult(model, diagnostics);
        }

        private static IEnumerable<IPropertySymbol> GetAllPropertySymbols(INamedTypeSymbol symbol)
        {
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            var current = symbol;

            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (property.IsImplicitlyDeclared)
                        continue;

                    if (!seenNames.Add(property.Name))
                        continue;

                    yield return property;
                }

                current = current.BaseType;
            }
        }

        private static IEnumerable<IFieldSymbol> GetAllFieldSymbols(INamedTypeSymbol symbol)
        {
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            var current = symbol;

            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var field in current.GetMembers().OfType<IFieldSymbol>())
                {
                    if (field.IsImplicitlyDeclared)
                        continue;

                    if (!seenNames.Add(field.Name))
                        continue;

                    yield return field;
                }

                current = current.BaseType;
            }
        }

        /// <summary>
        /// Determine if a property should be included in serialization
        /// </summary>
        private static bool ShouldIncludeProperty(IPropertySymbol property, TypeConfiguration config, AttributeParser.AttributeSymbols symbols)
        {
            // Skip static properties
            if (property.IsStatic)
                return false;

            // Skip indexers
            if (property.IsIndexer)
                return false;

            // Check for [BJsonIgnore] with Always condition
            var attrs = property.GetAttributes();
            var ignoreAttr = attrs.FirstOrDefault(a =>
                symbols.BJsonIgnoreAttribute != null
                    ? SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.BJsonIgnoreAttribute)
                    : string.Equals(a.AttributeClass?.ToDisplayString(), "Krampus.BinJson.Serialization.BJsonIgnoreAttribute", StringComparison.Ordinal));

            if (ignoreAttr != null)
            {
                var condition = AttributeParser.GetNamedArgument<int>(ignoreAttr, "Condition", 1);
                if (condition == 1) // Always
                    return false;
            }

            // Check for [BJsonInclude] - forces inclusion even if private
            var hasInclude = attrs.Any(a =>
                symbols.BJsonIncludeAttribute != null
                    ? SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.BJsonIncludeAttribute)
                    : string.Equals(a.AttributeClass?.ToDisplayString(), "Krampus.BinJson.Serialization.BJsonIncludeAttribute", StringComparison.Ordinal));

            if (hasInclude)
                return true;

            // Include public properties by default
            if (property.DeclaredAccessibility == Accessibility.Public)
                return true;

            // Include private/protected if IncludePrivateMembers = true
            if (config.IncludePrivateMembers)
                return true;

            return false;
        }

        /// <summary>
        /// Determine if a field should be included in serialization
        /// </summary>
        private static bool ShouldIncludeField(IFieldSymbol field, TypeConfiguration config, AttributeParser.AttributeSymbols symbols)
        {
            // Skip static fields
            if (field.IsStatic)
                return false;

            // Skip constants
            if (field.IsConst)
                return false;

            // Check for [BJsonIgnore] with Always condition
            var attrs = field.GetAttributes();
            var ignoreAttr = attrs.FirstOrDefault(a =>
                symbols.BJsonIgnoreAttribute != null
                    ? SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.BJsonIgnoreAttribute)
                    : string.Equals(a.AttributeClass?.ToDisplayString(), "Krampus.BinJson.Serialization.BJsonIgnoreAttribute", StringComparison.Ordinal));

            if (ignoreAttr != null)
            {
                var condition = AttributeParser.GetNamedArgument<int>(ignoreAttr, "Condition", 1);
                if (condition == 1) // Always
                    return false;
            }

            // Check for [BJsonInclude] - forces inclusion even if private
            var hasInclude = attrs.Any(a =>
                symbols.BJsonIncludeAttribute != null
                    ? SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.BJsonIncludeAttribute)
                    : string.Equals(a.AttributeClass?.ToDisplayString(), "Krampus.BinJson.Serialization.BJsonIncludeAttribute", StringComparison.Ordinal));

            if (hasInclude)
                return true;

            // Include public fields by default
            if (field.DeclaredAccessibility == Accessibility.Public)
                return true;

            // Include private/protected if IncludePrivateMembers = true
            if (config.IncludePrivateMembers)
                return true;

            return false;
        }

        /// <summary>
        /// Create PropertyModel from IPropertySymbol
        /// </summary>
        private static PropertyModel? CreatePropertyModel(IPropertySymbol property, TypeConfiguration config, AttributeParser.AttributeSymbols symbols, List<Diagnostic> diagnostics)
        {
            var model = new PropertyModel(
                property.Name,
                property.Type.ToDisplayString(),
                TypeRegistry.IsNullable(property.Type),
                property.Type.IsValueType,
                property.DeclaredAccessibility == Accessibility.Public,
                property.IsStatic,
                property.SetMethod == null || property.SetMethod.IsInitOnly,
                property.GetMethod != null,
                property.SetMethod != null);

            // Parse attributes
            AttributeParser.ParseMemberAttributes(property, model, symbols, diagnostics);

            // Determine JSON name
            if (model.JsonName == null)
            {
                model.JsonName = NamingPolicyTransformer.Transform(property.Name, config.NamingPolicy);
            }

            return model;
        }

        /// <summary>
        /// Create FieldModel from IFieldSymbol
        /// </summary>
        private static FieldModel? CreateFieldModel(IFieldSymbol field, TypeConfiguration config, AttributeParser.AttributeSymbols symbols, List<Diagnostic> diagnostics)
        {
            var model = new FieldModel(
                field.Name,
                field.Type.ToDisplayString(),
                TypeRegistry.IsNullable(field.Type),
                field.Type.IsValueType,
                field.DeclaredAccessibility == Accessibility.Public,
                field.IsStatic,
                field.IsReadOnly);

            // Parse attributes
            AttributeParser.ParseMemberAttributes(field, model, symbols, diagnostics);

            // Determine JSON name
            if (model.JsonName == null)
            {
                model.JsonName = NamingPolicyTransformer.Transform(field.Name, config.NamingPolicy);
            }

            return model;
        }

        /// <summary>
        /// Find appropriate constructor for deserialization
        /// </summary>
        private static ConstructorModel? FindConstructor(INamedTypeSymbol symbol, List<Diagnostic> diagnostics, AttributeParser.AttributeSymbols symbols)
        {
            var constructors = symbol.Constructors
                .Where(c => !c.IsStatic)
                .ToList();

            // Look for constructors with [BJsonConstructor]
            var markedConstructors = constructors
                .Where(c => AttributeParser.HasConstructorAttribute(c, symbols))
                .ToList();

            // Validate: only one constructor can be marked
            if (markedConstructors.Count > 1)
            {
                foreach (var ctor in markedConstructors.Skip(1))
                {
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.MultipleConstructorAttributes,
                        ctor.Locations.FirstOrDefault() ?? Location.None,
                        symbol.Name));
                }
            }

            if (markedConstructors.Count > 0)
                return CreateConstructorModel(markedConstructors.First(), hasAttribute: true, symbols);

            // Find parameterless constructor
            var parameterlessConstructor = constructors.FirstOrDefault(c => c.Parameters.Length == 0);
            if (parameterlessConstructor != null)
                return CreateConstructorModel(parameterlessConstructor, hasAttribute: false, symbols);

            // For value types, use default constructor
            if (symbol.IsValueType)
                return new ConstructorModel(new List<ConstructorParameterModel>(), isParameterless: true);

            // If no suitable constructor found, return null (will generate error later)
            return null;
        }

        /// <summary>
        /// Create ConstructorModel from IMethodSymbol
        /// </summary>
        private static ConstructorModel CreateConstructorModel(IMethodSymbol constructor, bool hasAttribute, AttributeParser.AttributeSymbols symbols)
        {
            var parameters = new List<ConstructorParameterModel>();

            foreach (var param in constructor.Parameters)
            {
                var paramModel = new ConstructorParameterModel(
                    param.Name,
                    param.Type.ToDisplayString(),
                    TypeRegistry.IsNullable(param.Type),
                    param.Type.IsValueType);

                // Check for [BJsonPropertyName] on the parameter itself
                var propertyNameAttr = param.GetAttributes()
                    .FirstOrDefault(a => symbols.BJsonPropertyNameAttribute != null
                        ? SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.BJsonPropertyNameAttribute)
                        : string.Equals(a.AttributeClass?.Name, "BJsonPropertyNameAttribute", StringComparison.Ordinal));

                if (propertyNameAttr != null)
                {
                    paramModel.JsonName = AttributeParser.GetNamedArgument<string?>(propertyNameAttr, "Name", null)
                        ?? propertyNameAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                }

                parameters.Add(paramModel);
            }

            var model = new ConstructorModel(parameters, constructor.Parameters.Length == 0)
            {
                HasAttribute = hasAttribute
            };

            return model;
        }

        /// <summary>
        /// Match constructor parameters to members and set JsonName/MatchingMember
        /// </summary>
        private static void MatchConstructorParametersToMembers(
            ConstructorModel constructor, 
            GeneratedTypeModel model, 
            INamedTypeSymbol symbol,
            List<Diagnostic> diagnostics)
        {
            if (constructor.IsParameterless)
                return;

            var allMembers = model.AllMembers;
            var membersByJsonName = allMembers
                .GroupBy(m => m.JsonName ?? m.MemberName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var membersByName = allMembers
                .GroupBy(m => m.MemberName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var param in constructor.Parameters)
            {
                // If parameter already has an explicit JsonName from [BJsonPropertyName], use it
                if (param.JsonName != null)
                {
                    // Try to find member with matching JSON name
                    membersByJsonName.TryGetValue(param.JsonName, out var memberByJsonName);

                    param.MatchingMember = memberByJsonName;

                    if (memberByJsonName == null)
                    {
                        // Warning: parameter has explicit name but no matching member
                        diagnostics.Add(Diagnostic.Create(
                            BJsonDiagnostics.UnmatchedConstructorParameter,
                            Location.None,
                            param.ParameterName,
                            symbol.Name));
                    }
                    continue;
                }

                // Try to match by parameter name (case-insensitive)
                membersByName.TryGetValue(param.ParameterName, out var memberByName);

                if (memberByName != null)
                {
                    param.MatchingMember = memberByName;
                    param.JsonName = memberByName.JsonName ?? memberByName.MemberName;
                }
                else
                {
                    // No matching member found - parameter will use its own name as JSON key
                    param.JsonName = param.ParameterName;

                    // Warning: parameter cannot be matched to any member
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.UnmatchedConstructorParameter,
                        Location.None,
                        param.ParameterName,
                        symbol.Name));
                }
            }
        }

        /// <summary>
        /// Match factory method parameters to type members
        /// </summary>
        private static void MatchFactoryParametersToMembers(
            List<ConstructorParameterModel> parameters,
            GeneratedTypeModel model,
            INamedTypeSymbol symbol,
            List<Diagnostic> diagnostics)
        {
            var allMembers = model.AllMembers;
            var membersByJsonName = allMembers
                .GroupBy(m => m.JsonName ?? m.MemberName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var membersByName = allMembers
                .GroupBy(m => m.MemberName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var param in parameters)
            {
                // If parameter already has an explicit JsonName from [BJsonPropertyName], use it
                if (param.JsonName != null)
                {
                    // Try to find member with matching JSON name
                    membersByJsonName.TryGetValue(param.JsonName, out var memberByJsonName);

                    param.MatchingMember = memberByJsonName;

                    if (memberByJsonName == null)
                    {
                        // Warning: parameter has explicit name but no matching member
                        diagnostics.Add(Diagnostic.Create(
                            BJsonDiagnostics.UnmatchedConstructorParameter,
                            Location.None,
                            param.ParameterName,
                            symbol.Name));
                    }
                    continue;
                }

                // Try to match by parameter name (case-insensitive)
                membersByName.TryGetValue(param.ParameterName, out var memberByName);

                if (memberByName != null)
                {
                    param.MatchingMember = memberByName;
                    param.JsonName = memberByName.JsonName ?? memberByName.MemberName;
                }
                else
                {
                    // No matching member found - parameter will use its own name as JSON key
                    param.JsonName = param.ParameterName;

                    // Warning: parameter cannot be matched to any member
                    diagnostics.Add(Diagnostic.Create(
                        BJsonDiagnostics.UnmatchedConstructorParameter,
                        Location.None,
                        param.ParameterName,
                        symbol.Name));
                }
            }
        }

        /// <summary>
        /// Validate JSON property names for conflicts
        /// </summary>
        private static void ValidateJsonNames(GeneratedTypeModel model, INamedTypeSymbol symbol, List<Diagnostic> diagnostics)
        {
            var jsonNameGroups = model.AllMembers
                .GroupBy(m => m.JsonName ?? m.MemberName)
                .Where(g => g.Count() > 1);

            foreach (var group in jsonNameGroups)
            {
                var firstMember = group.First();
                var location = GetMemberLocation(symbol, firstMember);

                diagnostics.Add(Diagnostic.Create(
                    BJsonDiagnostics.ConflictingPropertyNames,
                    location,
                    symbol.Name,
                    group.Key));
            }
        }

        /// <summary>
        /// Get location for a member in the source code
        /// </summary>
        private static Location GetMemberLocation(INamedTypeSymbol typeSymbol, MemberModel member)
        {
            var memberSymbol = GetMemberSymbol(typeSymbol, member);
            return memberSymbol?.Locations.FirstOrDefault() ?? Location.None;
        }

        /// <summary>
        /// Get ISymbol for a MemberModel
        /// </summary>
        private static ISymbol? GetMemberSymbol(INamedTypeSymbol typeSymbol, MemberModel member)
        {
            return typeSymbol.GetMembers(member.MemberName).FirstOrDefault();
        }

        private static string BuildHintName(INamedTypeSymbol symbol)
        {
            string fullName = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            var chars = fullName.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                    continue;

                chars[i] = '_';
            }

            return new string(chars);
        }
    }

    /// <summary>
    /// Extension methods for ISymbol
    /// </summary>
    internal static class SymbolExtensions
    {
        public static ITypeSymbol GetMemberType(this ISymbol symbol)
        {
            return symbol switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                _ => null!
            };
        }
    }
}

