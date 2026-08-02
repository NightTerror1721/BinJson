#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonConverterRegistry
    {
        private readonly Dictionary<Type, IBJsonConverter> _converters;

        public BJsonConverterRegistry()
        {
            _converters = new Dictionary<Type, IBJsonConverter>();
        }

        public void Add(IBJsonConverter converter)
        {
            if (converter is null)
                throw new ArgumentNullException(nameof(converter));

            _converters[converter.Type] = converter;
        }

        public void AddRange(IEnumerable<IBJsonConverter> converters)
        {
            if (converters is null)
                throw new ArgumentNullException(nameof(converters));

            foreach (var converter in converters)
                Add(converter);
        }

        public bool TryGetConverter(Type type, out IBJsonConverter converter)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (_converters.TryGetValue(type, out converter!))
                return true;

            foreach (var kvp in _converters)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    converter = kvp.Value;
                    return true;
                }
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (_converters.TryGetValue(interfaceType, out converter!))
                    return true;
            }

            converter = null!;
            return false;
        }
    }
}
