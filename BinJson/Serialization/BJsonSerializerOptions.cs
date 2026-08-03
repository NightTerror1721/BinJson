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
}
