#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks an instance method to be invoked immediately before BinJson serializes an object.
    /// <para>
    /// Supported method signatures are:
    /// <code>
    /// void MethodName()
    /// void MethodName(BJsonSerializationContext context)
    /// </code>
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonOnSerializing]
    /// internal void Prepare()
    /// {
    ///     UpdatedAt = DateTime.UtcNow;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class BJsonOnSerializingAttribute : Attribute
    {
    }
}