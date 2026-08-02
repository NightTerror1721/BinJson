#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Serialization.Metadata
{
    internal sealed class MetadataCache
    {
        private readonly Dictionary<Type, TypeMetadata> _cache;

        public MetadataCache()
        {
            _cache = new Dictionary<Type, TypeMetadata>();
        }

        public bool TryGet(Type type, out TypeMetadata metadata)
        {
            return _cache.TryGetValue(type, out metadata!);
        }

        public void Set(Type type, TypeMetadata metadata)
        {
            _cache[type] = metadata;
        }
    }
}
