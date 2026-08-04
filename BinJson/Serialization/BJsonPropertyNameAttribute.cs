#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Sets the exact JSON property name for a member.
    /// <para>
    /// This attribute takes precedence over inferred names and over <see cref="BJsonPropertyAttribute.Name"/>.
    /// Use it when wire compatibility requires a stable or legacy key that differs from the CLR member name.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonPropertyName("player_level")]
    /// public int Level { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonPropertyNameAttribute : Attribute
    {
        /// <param name="name">Exact JSON key to use for the member.</param>
        public BJsonPropertyNameAttribute(string name)
        {
            Name = name ?? throw new BJsonValidationException("Parameter 'name' cannot be null.");
        }

        /// <summary>
        /// Exact JSON property name.
        /// </summary>
        public string Name { get; }
    }
}