#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member to have its value transformed by a custom static mapper method
    /// declared on the same type, both during serialization (writing) and deserialization (reading).
    /// <para>
    /// The referenced method must match one of the following signatures (preferred first):
    /// <code>
    /// // Full signature — receives direction flag
    /// static BJsonValue MethodName(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    ///
    /// // Runtime-only fallback signature — no extra context
    /// static BJsonValue MethodName(BJsonValue value)
    /// </code>
    /// The method receives the member value in BJson form before serialization or after
    /// deserialization and must return the transformed <see cref="BJsonValue"/>.
    /// </para>
    /// <para>
    /// The <c>version</c> parameter receives the current document version set by
    /// <see cref="BJsonVersionContextAttribute"/>, or <c>null</c> if no version context is active.
    /// </para>
    /// <para>
    /// The <c>isReading</c> parameter is <c>true</c> during deserialization and <c>false</c>
    /// during serialization, allowing a single method to handle both directions.
    /// </para>
    /// <para>
    /// Source-generated serializers require the full 4-parameter signature.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonValueMapper(nameof(MapScore))]
    /// public int Score { get; set; }
    ///
    /// internal static BJsonValue MapScore(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    /// {
    ///     if (isReading &amp;&amp; version != null &amp;&amp; version.CompareTo(new Version("2.0")) &lt; 0)
    ///         return BJsonValue.Create(value.IntValue * 10); // legacy scale factor
    ///     return value;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonValueMapperAttribute : Attribute
    {
        /// <param name="methodName">
        /// Name of the static mapper method on the declaring type.
        /// Use <c>nameof(...)</c> to avoid magic strings.
        /// </param>
        public BJsonValueMapperAttribute(string methodName)
        {
            MethodName = methodName ?? throw new BJsonValidationException("Parameter 'methodName' cannot be null.");
        }

        /// <summary>
        /// Name of the static mapper method on the declaring type.
        /// The method must match the full or fallback signature documented on this attribute.
        /// </summary>
        public string MethodName { get; }
    }
}
