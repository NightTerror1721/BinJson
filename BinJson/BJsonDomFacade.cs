#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson
{
    /// <summary>
    /// Specialized facade for DOM tree operations.
    /// </summary>
    public static class BJsonDomFacade
    {
        public static BJsonValue Transform(BJsonValue value, Func<BJsonValue, BJsonValue> transformer, int maxDepth = 256)
        {
            if (transformer is null)
                throw new BJsonValidationException("Parameter 'transformer' cannot be null.");
            if (maxDepth < 0)
                throw new BJsonValidationException("Parameter 'maxDepth' cannot be negative.");

            return TransformCore(value, transformer, maxDepth);
        }

        private static BJsonValue TransformCore(BJsonValue value, Func<BJsonValue, BJsonValue> transformer, int depth)
        {
            if (depth <= 0)
                throw new BJsonValidationException("Maximum transform depth exceeded.");

            var transformed = transformer(value);

            if (transformed.TryGetArray(out var array))
            {
                var copy = new BJsonArray(array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    copy.Add(TransformCore(array[i], transformer, depth - 1));
                }
                return BJsonValue.Create(copy);
            }

            if (transformed.TryGetObject(out var obj))
            {
                var copy = new BJsonObject(obj.Count);
                foreach (var kvp in obj)
                {
                    copy.Add(kvp.Key, TransformCore(kvp.Value, transformer, depth - 1));
                }
                return BJsonValue.Create(copy);
            }

            return transformed;
        }
    }
}
