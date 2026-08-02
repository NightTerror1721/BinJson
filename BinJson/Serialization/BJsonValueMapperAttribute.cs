#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member to have its value transformed by a custom static mapper method
    /// declared on the same type, both during serialization (writing) and deserialization (reading).
    /// <para>
    /// The referenced method must match one of the following signatures (preferred first):
    /// <code>
    /// // Full signature — receives direction flag
    /// static object? MethodName(object? value, string propertyName, IComparable? version, bool isReading)
    ///
    /// // Fallback signature — no direction flag
    /// static object? MethodName(object? value, string propertyName, IComparable? version)
    /// </code>
    /// The method receives the raw value before serialization or after deserialization and must
    /// return the transformed value. Returning <c>null</c> is valid.
    /// </para>
    /// <para>
    /// The <c>version</c> parameter receives the current document version set by
    /// <see cref="BJsonVersionContextAttribute"/>, or <c>null</c> if no version context is active.
    /// </para>
    /// <para>
    /// The <c>isReading</c> parameter is <c>true</c> during deserialization and <c>false</c>
    /// during serialization, allowing a single method to handle both directions.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonValueMapper(nameof(MapScore))]
    /// public int Score { get; set; }
    ///
    /// private static object? MapScore(object? value, string propertyName, IComparable? version, bool isReading)
    /// {
    ///     if (isReading &amp;&amp; version != null &amp;&amp; version.CompareTo(new Version("2.0")) &lt; 0)
    ///         return (int)value! * 10; // legacy scale factor
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
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }

        /// <summary>
        /// Name of the static mapper method on the declaring type.
        /// The method must match the full or fallback signature documented on this attribute.
        /// </summary>
        public string MethodName { get; }
    }
}
