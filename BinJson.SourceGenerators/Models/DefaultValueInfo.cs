#nullable enable

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Holds the default value information parsed from [BJsonDefaultValue] and [BJsonDefaultProvider].
    /// When both attributes are present on the same member, ProviderMethod takes priority.
    /// </summary>
    internal sealed class DefaultValueInfo
    {
        private DefaultValueInfo(bool hasConstant, object? constantValue, string? providerMethod)
        {
            HasConstant = hasConstant;
            ConstantValue = constantValue;
            ProviderMethod = providerMethod;
        }

        /// <summary>True when a [BJsonDefaultValue] constant was parsed.</summary>
        public bool HasConstant { get; }

        /// <summary>The constant default value from [BJsonDefaultValue]. Valid only when HasConstant is true.</summary>
        public object? ConstantValue { get; }

        /// <summary>
        /// Name of the static provider method from [BJsonDefaultProvider].
        /// When non-null, takes priority over <see cref="ConstantValue"/>.
        /// </summary>
        public string? ProviderMethod { get; }

        /// <summary>True when a [BJsonDefaultProvider] method name was parsed.</summary>
        public bool HasProviderMethod => ProviderMethod != null;

        /// <summary>Creates a DefaultValueInfo for a compile-time constant default.</summary>
        public static DefaultValueInfo FromConstant(object? value) =>
            new DefaultValueInfo(hasConstant: true, constantValue: value, providerMethod: null);

        /// <summary>Creates a DefaultValueInfo for a static provider method default.</summary>
        public static DefaultValueInfo FromProvider(string methodName) =>
            new DefaultValueInfo(hasConstant: false, constantValue: null, providerMethod: methodName);

        /// <summary>
        /// Creates a DefaultValueInfo combining both a constant and a provider method.
        /// The provider takes priority at code-generation time; a warning is emitted.
        /// </summary>
        public static DefaultValueInfo FromBoth(object? constantValue, string providerMethod) =>
            new DefaultValueInfo(hasConstant: true, constantValue: constantValue, providerMethod: providerMethod);
    }
}
