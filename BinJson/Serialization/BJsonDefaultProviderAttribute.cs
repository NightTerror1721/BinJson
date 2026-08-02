#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Specifies a static factory method on the declaring type that provides a default value
    /// during deserialization when the JSON key is absent or its value is <c>null</c>
    /// on a non-nullable member.
    /// <para>
    /// The referenced method must match one of the following signatures:
    /// <code>
    /// static T      MethodName()          // strongly typed — preferred
    /// static object? MethodName()         // loosely typed — fallback
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
    /// private static Stats GetDefaultStats() => new Stats { Level = 1, Hp = 100 };
    ///
    /// // Composing with versioning:
    /// [BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
    /// [BJsonDefaultProvider(nameof(GetDefaultProfile))]
    /// public Profile Profile { get; set; }
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
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }

        /// <summary>
        /// Name of the static method on the declaring type that returns the default value.
        /// The method must have a parameterless signature: <c>static T MethodName()</c>
        /// or <c>static object? MethodName()</c>.
        /// </summary>
        public string MethodName { get; }
    }
}
