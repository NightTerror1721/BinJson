#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Specifies a compile-time constant default value to be used during deserialization
    /// when the JSON key is absent or its value is <c>null</c> on a non-nullable member.
    /// <para>
    /// Only compile-time constants are accepted: <see cref="bool"/>, <see cref="byte"/>,
    /// <see cref="sbyte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>,
    /// <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>,
    /// <see cref="double"/>, <see cref="char"/>, <see cref="string"/>, and enum values
    /// (passed as their underlying integer).
    /// </para>
    /// <para>
    /// For complex or computed defaults, use <see cref="BJsonDefaultProviderAttribute"/> instead.
    /// If both attributes are present on the same member, <see cref="BJsonDefaultProviderAttribute"/>
    /// takes priority and this attribute is ignored.
    /// </para>
    /// <para>
    /// Composes naturally with <see cref="BJsonVersionAttribute"/>: members introduced in a newer
    /// version will receive this default value when deserializing documents from older versions
    /// that do not contain the key.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonDefaultValue(42)]
    /// public int Score { get; set; }
    ///
    /// [BJsonDefaultValue("unknown")]
    /// public string Tag { get; set; } = string.Empty;
    ///
    /// // Composing with versioning:
    /// [BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
    /// [BJsonDefaultValue(0)]
    /// public int NewField { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonDefaultValueAttribute : Attribute
    {
        public BJsonDefaultValueAttribute(object? value)
        {
            Value = value;
        }

        /// <summary>The constant default value applied when the JSON key is absent.</summary>
        public object? Value { get; }
    }
}
