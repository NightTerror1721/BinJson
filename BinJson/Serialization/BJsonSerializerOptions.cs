#nullable enable

using System;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization.BuiltIn;
using Krampus.BinJson.Serialization.References;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonSerializerOptions
    {
        private readonly BJsonConverterRegistry _converters;

        public BJsonSerializerOptions()
        {
            _converters = new BJsonConverterRegistry();
            RegisterBuiltInConverters();
        }

        public bool IgnoreNullValues { get; set; }

        public bool PropertyNameCaseInsensitive { get; set; }

        public int MaxDepth { get; set; } = 64;

        public ReferenceHandler? ReferenceHandler { get; set; }

        public IBJsonPreprocessorContext? PreprocessorContext { get; set; }

        /// <summary>
        /// Controls how paths provided to <see cref="BJsonExternalRefAttribute"/> are validated.
        /// Default keeps references inside the preprocessor base path (or current directory when not set).
        /// </summary>
        public ExternalReferencePathPolicy ExternalReferencePathPolicy { get; set; } = ExternalReferencePathPolicy.RestrictToBasePath;

        public bool IncludeFields { get; set; } = true;

        public bool IncludePrivateMembers { get; set; }

        public bool StrictMode { get; set; } = true;

        public NamingPolicy NamingPolicy { get; set; } = NamingPolicy.Default;

        /// <summary>
        /// Optional version value passed to predicates, mappers, and version-range guards at runtime.
        /// When set, overrides the version declared by <c>[BJsonVersionContext]</c> on the type.
        /// Must implement <see cref="System.IComparable"/>.
        /// </summary>
        public IComparable? Version { get; set; }

        public BJsonConverterRegistry Converters => _converters;

        public void AddConverter(IBJsonConverter converter)
        {
            if (converter is null)
                throw new BJsonValidationException("Parameter 'converter' cannot be null.");

            _converters.Add(converter);
        }

        public void AddConverterFactory(IBJsonConverterFactory factory)
        {
            if (factory is null)
                throw new BJsonValidationException("Parameter 'factory' cannot be null.");

            _converters.AddFactory(factory);
        }

        internal bool TryGetConverter(Type type, out IBJsonConverter converter)
        {
            return _converters.TryGetConverter(type, out converter!);
        }

        private void RegisterBuiltInConverters()
        {
            _converters.Add(new DateTimeConverter());
            _converters.Add(new GuidConverter());
            _converters.Add(new TimeSpanConverter());
            _converters.Add(new UriConverter());
        }
    }

    public enum NamingPolicy
    {
        Default,
        CamelCase,
        SnakeCase,
        KebabCase
    }

    public enum ExternalReferencePathPolicy
    {
        AllowAny,
        RestrictToBasePath
    }
}
