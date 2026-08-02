#nullable enable

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Configuration from [BJsonSerializable] attribute
    /// </summary>
    internal sealed class TypeConfiguration
    {
        public TypeConfiguration(
            bool includeFields,
            bool includePrivateMembers,
            NamingPolicy namingPolicy)
        {
            IncludeFields = includeFields;
            IncludePrivateMembers = includePrivateMembers;
            NamingPolicy = namingPolicy;
        }

        /// <summary>True if fields should be included in serialization</summary>
        public bool IncludeFields { get; }

        /// <summary>True if private members should be included</summary>
        public bool IncludePrivateMembers { get; }

        /// <summary>Naming policy to apply to member names</summary>
        public NamingPolicy NamingPolicy { get; }

        /// <summary>Custom converter type for the entire type (from [BJsonConverter])</summary>
        public string? CustomConverterType { get; set; }

        /// <summary>True if type is marked with [BJsonPolymorphic]</summary>
        public bool IsPolymorphic { get; set; }

        /// <summary>Type discriminator property name (from [BJsonPolymorphic])</summary>
        public string TypeDiscriminatorPropertyName { get; set; } = "$type";

        /// <summary>Derived types (from [BJsonDerivedType])</summary>
        public System.Collections.Generic.List<DerivedTypeInfo> DerivedTypes { get; } = new();

        /// <summary>Version context from [BJsonVersionContext]. Null if not present.</summary>
        public VersionInfo? VersionContext { get; set; }

        /// <summary>True if the type is marked with [BJsonPreprocessor].</summary>
        public bool HasPreprocessor { get; set; }

        /// <summary>Custom preprocessor type name from [BJsonPreprocessor]. Null means use the built-in preprocessor.</summary>
        public string? PreprocessorType { get; set; }

        /// <summary>Name of the static factory method from [BJsonFactoryMethod]. Null if not present.</summary>
        public string? FactoryMethodName { get; set; }

        /// <summary>Parameters of the static factory method. Null or empty if factory is parameterless.</summary>
        public System.Collections.Generic.List<ConstructorParameterModel>? FactoryMethodParameters { get; set; }
    }

    internal enum NamingPolicy
    {
        Default,
        CamelCase,
        SnakeCase,
        KebabCase
    }

    /// <summary>
    /// Information about a derived type from [BJsonDerivedType]
    /// </summary>
    internal sealed class DerivedTypeInfo
    {
        public DerivedTypeInfo(string derivedType, string? typeDiscriminator)
        {
            DerivedType = derivedType;
            TypeDiscriminator = typeDiscriminator;
        }

        /// <summary>Fully qualified derived type name</summary>
        public string DerivedType { get; }

        /// <summary>Type discriminator value (or null to use type name)</summary>
        public string? TypeDiscriminator { get; }
    }
}
