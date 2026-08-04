#nullable enable

using System;
using System.Collections.Generic;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonConverterRegistry
    {
        private readonly Dictionary<Type, IBJsonConverter> _converters;
        private readonly List<IBJsonConverterFactory> _factories;
        private readonly Dictionary<Type, IBJsonConverter?> _resolutionCache;

        public BJsonConverterRegistry()
        {
            _converters = new Dictionary<Type, IBJsonConverter>();
            _factories = new List<IBJsonConverterFactory>();
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

        public void AddFactory(IBJsonConverterFactory factory)
        {
            if (factory is null)
                throw new BJsonValidationException("Parameter 'factory' cannot be null.");

            _factories.Add(factory);
            _resolutionCache.Clear();
        }

        public void AddFactories(IEnumerable<IBJsonConverterFactory> factories)
        {
            if (factories is null)
                throw new BJsonValidationException("Parameter 'factories' cannot be null.");

            foreach (var factory in factories)
                AddFactory(factory);
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

            foreach (var factory in _factories)
            {
                if (!factory.CanConvert(type))
                    continue;

                var created = factory.CreateConverter(type);
                if (created is null)
                    continue;

                _resolutionCache[type] = created;
                converter = created;
                return true;
            }

            converter = null!;
            _resolutionCache[type] = null;
            return false;
        }
    }
}
