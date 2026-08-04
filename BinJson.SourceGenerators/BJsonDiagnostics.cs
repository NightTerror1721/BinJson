#nullable enable

using Microsoft.CodeAnalysis;

namespace Krampus.BinJson.SourceGenerators
{
    /// <summary>
    /// Diagnostic descriptors for BJson source generator
    /// </summary>
    internal static class BJsonDiagnostics
    {
        private const string Category = "BinJson.SourceGenerator";

        // BJSON001: Invalid ExtensionData type
        public static readonly DiagnosticDescriptor InvalidExtensionDataType = new DiagnosticDescriptor(
            id: "BJSON001",
            title: "Invalid ExtensionData member type",
            messageFormat: "Member '{0}' marked with [BJsonExtensionData] must be of type IDictionary<string, BJsonValue>",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON002: Multiple constructors marked with [BJsonConstructor]
        public static readonly DiagnosticDescriptor MultipleConstructorAttributes = new DiagnosticDescriptor(
            id: "BJSON002",
            title: "Multiple constructors marked with [BJsonConstructor]",
            messageFormat: "Type '{0}' has multiple constructors marked with [BJsonConstructor]. Only one is allowed.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // BJSON003: Multiple members marked with [BJsonExtensionData]
        public static readonly DiagnosticDescriptor MultipleExtensionDataMembers = new DiagnosticDescriptor(
            id: "BJSON003",
            title: "Multiple members marked with [BJsonExtensionData]",
            messageFormat: "Type '{0}' has multiple members marked with [BJsonExtensionData]. Only one is allowed.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // BJSON004: Custom converter type not found
        public static readonly DiagnosticDescriptor ConverterTypeNotFound = new DiagnosticDescriptor(
            id: "BJSON004",
            title: "Custom converter type not found",
            messageFormat: "Custom converter type '{0}' specified in [BJsonConverter] could not be found",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON005: Conflicting JSON property names
        public static readonly DiagnosticDescriptor ConflictingPropertyNames = new DiagnosticDescriptor(
            id: "BJSON005",
            title: "Conflicting JSON property names",
            messageFormat: "Multiple members in type '{0}' map to the same JSON property name '{1}'. This may cause serialization issues.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON006: Constructor parameter cannot be matched to member
        public static readonly DiagnosticDescriptor UnmatchedConstructorParameter = new DiagnosticDescriptor(
            id: "BJSON006",
            title: "Constructor parameter cannot be matched to member",
            messageFormat: "Constructor parameter '{0}' in type '{1}' could not be matched to any property or field. Deserialization may fail.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON007: Referenced method not found
        public static readonly DiagnosticDescriptor ReferencedMethodNotFound = new DiagnosticDescriptor(
            id: "BJSON007",
            title: "Referenced attribute method not found",
            messageFormat: "Method '{0}' referenced by attribute '{1}' was not found on type '{2}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON008: Referenced method is not accessible to generated serializer
        public static readonly DiagnosticDescriptor ReferencedMethodInaccessible = new DiagnosticDescriptor(
            id: "BJSON008",
            title: "Referenced attribute method is not accessible",
            messageFormat: "Method '{0}' referenced by attribute '{1}' on type '{2}' is not accessible from generated code. Use public/internal/protected internal.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON009: Referenced method has invalid signature
        public static readonly DiagnosticDescriptor ReferencedMethodInvalidSignature = new DiagnosticDescriptor(
            id: "BJSON009",
            title: "Referenced attribute method has invalid signature",
            messageFormat: "Method '{0}' referenced by attribute '{1}' on type '{2}' has an invalid signature. Expected: {3}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // BJSON010: Unsupported type shape
        public static readonly DiagnosticDescriptor UnsupportedTypeShape = new DiagnosticDescriptor(
            id: "BJSON010",
            title: "Unsupported type shape for source generation",
            messageFormat: "Type '{0}' is not supported by BJson source generation yet: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
