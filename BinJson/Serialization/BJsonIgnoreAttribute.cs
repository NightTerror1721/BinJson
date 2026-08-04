#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Excludes a member from serialization and/or deserialization according to a static condition.
    /// <para>
    /// This is the first layer of ignore processing. If it excludes the member, dynamic predicates such as
    /// <see cref="BJsonIgnoreWhenAttribute"/> are not evaluated for that operation.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonIgnore]
    /// public string DebugInfo { get; set; } = string.Empty;
    ///
    /// [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingNull)]
    /// public string? Alias { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonIgnoreAttribute : Attribute
    {
        /// <summary>
        /// Static ignore condition. Default is <see cref="BJsonIgnoreCondition.Always"/>.
        /// </summary>
        public BJsonIgnoreCondition Condition { get; set; } = BJsonIgnoreCondition.Always;
    }
}