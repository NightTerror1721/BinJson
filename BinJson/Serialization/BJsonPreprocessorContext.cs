#nullable enable

using System;
using System.Collections.Generic;
using Krampus.BinJson;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonPreprocessorContext : IBJsonPreprocessorContext
    {
        private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, BJsonValue> _anchors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ExternalReferenceCacheEntry> _externalReferenceCache = new(StringComparer.OrdinalIgnoreCase);

        public string? BasePath { get; set; }

        public string? GetVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return _variables.TryGetValue(name, out var value) ? value : null;
        }

        public void SetVariable(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            _variables[name] = value ?? string.Empty;
        }

        public void RegisterAnchor(string name, BJsonValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            _anchors[name] = value;
        }

        public bool TryGetAnchor(string name, out BJsonValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                value = BJsonValue.Null;
                return false;
            }

            return _anchors.TryGetValue(name, out value);
        }

        internal bool TryGetExternalReference(string path, long lastWriteUtcTicks, long length, out BJsonValue value)
        {
            if (_externalReferenceCache.TryGetValue(path, out var entry)
                && entry.LastWriteUtcTicks == lastWriteUtcTicks
                && entry.Length == length)
            {
                value = entry.Value;
                return true;
            }

            value = BJsonValue.Null;
            return false;
        }

        internal void SetExternalReference(string path, long lastWriteUtcTicks, long length, BJsonValue value)
        {
            _externalReferenceCache[path] = new ExternalReferenceCacheEntry(lastWriteUtcTicks, length, value);
        }

        private readonly struct ExternalReferenceCacheEntry
        {
            public ExternalReferenceCacheEntry(long lastWriteUtcTicks, long length, BJsonValue value)
            {
                LastWriteUtcTicks = lastWriteUtcTicks;
                Length = length;
                Value = value;
            }

            public long LastWriteUtcTicks { get; }

            public long Length { get; }

            public BJsonValue Value { get; }
        }
    }
}
