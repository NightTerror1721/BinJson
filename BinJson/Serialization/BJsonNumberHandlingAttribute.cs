#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Configures how a numeric member is read from or written to the payload.
    /// <para>
    /// This attribute is useful for interoperability scenarios where numbers may appear as strings,
    /// or when a decimal/text representation should be preserved on write.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString | BJsonNumberHandling.Lossless)]
    /// public decimal Amount { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonNumberHandlingAttribute : Attribute
    {
        public BJsonNumberHandlingAttribute(BJsonNumberHandling handling)
        {
            Handling = handling;
        }

        /// <summary>
        /// Numeric handling flags that control read/write behavior for the member.
        /// </summary>
        public BJsonNumberHandling Handling { get; }
    }
}