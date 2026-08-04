#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Customizes the JSON contract of a property or field.
    /// <para>
    /// Use this attribute when you want to set several contract options together,
    /// such as a custom JSON key, deterministic ordering, or requiredness.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonProperty(Name = "player_name", Order = 0, Required = true)]
    /// public string Name { get; set; } = string.Empty;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonPropertyAttribute : Attribute
    {
        /// <summary>
        /// Optional explicit JSON key for the member.
        /// When omitted, the serializer uses <see cref="NamingPolicy"/> or the CLR member name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Relative serialization order. Lower values are emitted first.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Marks the member as required during deserialization.
        /// Equivalent in intent to <see cref="BJsonRequiredAttribute"/>.
        /// </summary>
        public bool Required { get; set; }
    }
}