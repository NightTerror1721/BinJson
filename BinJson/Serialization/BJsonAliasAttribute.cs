#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Declares additional legacy JSON names accepted for a member during deserialization.
    /// <para>
    /// BinJson first tries the current member name, then <see cref="BJsonVersionAttribute.RenamedFrom"/>
    /// when present, and finally any aliases declared with this attribute.
    /// </para>
    /// <para>
    /// This attribute affects read-time compatibility only. Serialization still emits the current configured name.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonAlias("legacy_count")]
    /// [BJsonAlias("legacy_count_v2")]
    /// public int Count { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class BJsonAliasAttribute : Attribute
    {
        public BJsonAliasAttribute(string name)
        {
            Name = name ?? throw new BJsonValidationException("Parameter 'name' cannot be null.");
        }

        public string Name { get; }
    }
}