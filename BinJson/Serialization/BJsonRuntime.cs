#nullable enable

using System;
using System.Collections.Generic;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization.Metadata;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Reusable runtime for CLR object serialization/deserialization.
    /// Keeps serializer metadata and converter resolution caches warm across operations.
    /// </summary>
    public sealed class BJsonRuntime
    {
        private readonly BJsonSerializerOptions _options;
        private readonly MetadataCache _metadataCache;
        private readonly Dictionary<Type, IBJsonConverter?> _converterCache;
        private readonly object _sync;

        public BJsonRuntime()
            : this(new BJsonSerializerOptions())
        {
        }

        public BJsonRuntime(BJsonSerializerOptions options)
        {
            _options = options ?? throw new BJsonValidationException("Parameter 'options' cannot be null.");
            _metadataCache = new MetadataCache();
            _converterCache = new Dictionary<Type, IBJsonConverter?>();
            _sync = new object();
        }

        public BJsonSerializerOptions Options => _options;

        public BJsonValue Serialize<T>(T? value)
        {
            lock (_sync)
            {
                var serializer = CreateSerializer();
                return serializer.SerializeValue(value, typeof(T));
            }
        }

        public BJsonValue Serialize(object? value, Type declaredType)
        {
            if (declaredType is null)
                throw new BJsonValidationException("Parameter 'declaredType' cannot be null.");

            lock (_sync)
            {
                var serializer = CreateSerializer();
                return serializer.SerializeValue(value, declaredType);
            }
        }

        public T? Deserialize<T>(BJsonValue value)
        {
            lock (_sync)
            {
                var serializer = CreateSerializer();
                return (T?)serializer.DeserializeValue(value, typeof(T));
            }
        }

        public object? Deserialize(BJsonValue value, Type targetType)
        {
            if (targetType is null)
                throw new BJsonValidationException("Parameter 'targetType' cannot be null.");

            lock (_sync)
            {
                var serializer = CreateSerializer();
                return serializer.DeserializeValue(value, targetType);
            }
        }

        private BJsonObjectSerializer CreateSerializer()
        {
            return new BJsonObjectSerializer(_options, _metadataCache, _converterCache);
        }
    }
}
