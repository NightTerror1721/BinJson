#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Krampus.BinJson.Serialization.References
{
    public sealed class ReferenceResolver
    {
        private readonly Dictionary<object, string> _objectToId;
        private readonly Dictionary<string, object> _idToObject;
        private readonly bool _preserveReferences;
        private int _nextId;

        internal ReferenceResolver(bool preserveReferences)
        {
            _preserveReferences = preserveReferences;
            _objectToId = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
            _idToObject = new Dictionary<string, object>(StringComparer.Ordinal);
            _nextId = 1;
        }

        public bool PreserveReferences => _preserveReferences;

        public bool TryGetReference(object value, out string referenceId)
        {
            if (value is null)
            {
                referenceId = string.Empty;
                return false;
            }

            return _objectToId.TryGetValue(value, out referenceId!);
        }

        public string GetOrAddReference(object value, out bool alreadyExists)
        {
            if (_objectToId.TryGetValue(value, out var existing))
            {
                alreadyExists = true;
                return existing;
            }

            var id = _nextId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _nextId++;
            _objectToId[value] = id;
            alreadyExists = false;
            return id;
        }

        public void AddReference(string id, object value)
        {
            _idToObject[id] = value;
        }

        public bool TryResolveReference(string id, out object? value)
        {
            if (_idToObject.TryGetValue(id, out var resolved))
            {
                value = resolved;
                return true;
            }

            value = null;
            return false;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

            public new bool Equals(object? x, object? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
