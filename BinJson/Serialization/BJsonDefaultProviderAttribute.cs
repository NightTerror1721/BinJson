#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Specifies a static factory method on the declaring type that provides a default value
    /// during deserialization when the JSON key is absent or its value is <c>null</c>
    /// on a non-nullable member.
    /// <para>
    /// The referenced method must match one of the following signatures:
    /// <code>
    /// static T       MethodName()                 // strongly typed — preferred
    /// static object? MethodName()                // loosely typed — fallback
    /// static T       MethodName(IComparable?)    // version-aware
    /// static object? MethodName(IComparable?)    // version-aware fallback
    /// </code>
    /// </para>
    /// <para>
    /// This attribute takes priority over <see cref="BJsonDefaultValueAttribute"/> when both
    /// are present on the same member. A source-generator or runtime warning is emitted
    /// when both attributes coexist.
    /// </para>
    /// <para>
    /// Composes naturally with <see cref="BJsonVersionAttribute"/>: members introduced in a
    /// newer version will receive the provider-supplied default when deserializing documents
    /// from older versions that do not contain the key.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonDefaultProvider(nameof(GetDefaultStats))]
    /// public Stats PlayerStats { get; set; }
    ///
    /// internal static Stats GetDefaultStats() => new Stats { Level = 1, Hp = 100 };
    ///
    /// // Composing with versioning:
    /// [BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
    /// [BJsonDefaultProvider(nameof(GetDefaultProfile))]
    /// public Profile Profile { get; set; }
    ///
    /// internal static Profile GetDefaultProfile(IComparable? version)
    ///     => version != null &amp;&amp; version.CompareTo(new Version("3.0.0")) &gt;= 0
    ///        ? new Profile { Mode = "modern" }
    ///        : new Profile { Mode = "legacy" };
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonDefaultProviderAttribute : Attribute
    {
        /// <param name="methodName">
        /// Name of the static factory method on the declaring type.
        /// Use <c>nameof(...)</c> to avoid magic strings.
        /// </param>
        public BJsonDefaultProviderAttribute(string methodName)
        {
            MethodName = methodName ?? throw new BJsonValidationException("Parameter 'methodName' cannot be null.");
        }

        /// <summary>
        /// Name of the static method on the declaring type that returns the default value.
        /// The method may be parameterless or version-aware as documented on this attribute.
        /// </summary>
        public string MethodName { get; }
    }
}
