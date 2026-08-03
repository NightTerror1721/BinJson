#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member to be conditionally ignored based on a custom static predicate method
    /// declared on the same type.
    /// <para>
    /// The referenced method must match the following signature:
    /// <code>
    /// static bool MethodName(object? value, string propertyName, IComparable? version)
    /// </code>
    /// When the method returns <c>true</c>, the member is ignored for that operation.
    /// </para>
    /// <para>
    /// This attribute is orthogonal to <see cref="BJsonIgnoreAttribute"/> — both can be applied
    /// to the same member. <see cref="BJsonIgnoreAttribute"/> is evaluated first; if it causes
    /// the member to be ignored, the predicate is not invoked.
    /// </para>
    /// <para>
    /// The <c>version</c> parameter receives the current document version set by
    /// <see cref="BJsonVersionContextAttribute"/>, or <c>null</c> if no version context is active.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonIgnoreWhen(nameof(ShouldIgnoreScore))]
    /// public int Score { get; set; }
    ///
    /// private static bool ShouldIgnoreScore(object? value, string propertyName, IComparable? version)
    ///     => version != null &amp;&amp; version.CompareTo(new Version("2.0")) &lt; 0 &amp;&amp; (int)value! == 0;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonIgnoreWhenAttribute : Attribute
    {
        /// <param name="methodName">
        /// Name of the static predicate method on the declaring type.
        /// Use <c>nameof(...)</c> to avoid magic strings.
        /// </param>
        public BJsonIgnoreWhenAttribute(string methodName)
        {
            MethodName = methodName ?? throw new BJsonValidationException("Parameter 'methodName' cannot be null.");
        }

        /// <summary>
        /// Name of the static predicate method on the declaring type with signature
        /// <c>static bool MethodName(object? value, string propertyName, IComparable? version)</c>.
        /// </summary>
        public string MethodName { get; }
    }
}
