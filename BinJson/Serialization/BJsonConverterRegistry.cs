#nullable enable

using System;
using System.Collections.Generic;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonConverterRegistry
    {
        private readonly Dictionary<Type, IBJsonConverter> _converters;
        private readonly Dictionary<Type, IBJsonConverter?> _resolutionCache;

        public BJsonConverterRegistry()
        {
            _converters = new Dictionary<Type, IBJsonConverter>();
            _resolutionCache = new Dictionary<Type, IBJsonConverter?>();
        }

        public void Add(IBJsonConverter converter)
        {
            if (converter is null)
                throw new BJsonValidationException("Parameter 'converter' cannot be null.");

            _converters[converter.Type] = converter;
            _resolutionCache.Clear();
        }

        public void AddRange(IEnumerable<IBJsonConverter> converters)
        {
            if (converters is null)
                throw new BJsonValidationException("Parameter 'converters' cannot be null.");

            foreach (var converter in converters)
                Add(converter);
        }

        public bool TryGetConverter(Type type, out IBJsonConverter converter)
        {
            if (type is null)
                throw new BJsonValidationException("Parameter 'type' cannot be null.");

            if (_resolutionCache.TryGetValue(type, out IBJsonConverter? cached))
            {
                converter = cached!;
                return converter is not null;
            }

            if (_converters.TryGetValue(type, out converter!))
            {
                _resolutionCache[type] = converter;
                return true;
            }

            foreach (var kvp in _converters)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    converter = kvp.Value;
                    _resolutionCache[type] = converter;
                    return true;
                }
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (_converters.TryGetValue(interfaceType, out converter!))
                {
                    _resolutionCache[type] = converter;
                    return true;
                }
            }

            converter = null!;
            _resolutionCache[type] = null;
            return false;
        }
    }
}
