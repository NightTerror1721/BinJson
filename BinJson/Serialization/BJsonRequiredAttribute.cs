#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member as required during deserialization.
    /// <para>
    /// In strict mode, BinJson throws if the JSON document does not contain the member.
    /// Combine with <see cref="BJsonVersionAttribute"/> or <see cref="BJsonRequiredWhenAttribute"/>
    /// when requiredness depends on version or context.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonRequired]
    /// public string UserId { get; set; } = string.Empty;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonRequiredAttribute : Attribute
    {
    }
}