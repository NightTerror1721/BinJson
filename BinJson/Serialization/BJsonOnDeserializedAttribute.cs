#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks an instance method to be invoked immediately after BinJson has finished deserializing an object.
    /// <para>
    /// Supported method signatures are:
    /// <code>
    /// void MethodName()
    /// void MethodName(BJsonDeserializationContext context)
    /// </code>
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonOnDeserialized]
    /// internal void Normalize()
    /// {
    ///     CacheKey = Name.ToLowerInvariant();
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class BJsonOnDeserializedAttribute : Attribute
    {
    }
}