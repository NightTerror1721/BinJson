#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Declares the current version of the document format for a serializable type.
    /// This version is used to evaluate <see cref="BJsonVersionAttribute"/> range constraints
    /// on the type's members, and is passed as the <c>version</c> parameter to methods
    /// referenced by <see cref="BJsonIgnoreWhenAttribute"/>, <see cref="BJsonValueMapperAttribute"/>,
    /// and <see cref="BJsonDefaultProviderAttribute"/>.
    /// <para>
    /// The <paramref name="versionType"/> must implement <see cref="IComparable"/> and expose
    /// a public static <c>Parse(string)</c> method.
    /// </para>
    /// <para>
    /// The version declared here can be overridden at runtime by setting
    /// <c>BJsonSerializerOptions.Version</c>.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable]
    /// [BJsonVersionContext(typeof(Version), "2.1.0")]
    /// public class PlayerSave
    /// {
    ///     public string Name { get; set; } = string.Empty;
    ///
    ///     [BJsonVersion(typeof(Version), introducedIn: "1.5.0")]
    ///     public int Level { get; set; }
    ///
    ///     [BJsonVersion(typeof(Version), introducedIn: "1.0.0", removedIn: "2.0.0")]
    ///     public int LegacyScore { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class BJsonVersionContextAttribute : Attribute
    {
        /// <param name="versionType">
        /// The concrete type used to represent the version (e.g. <c>typeof(Version)</c>).
        /// Must implement <see cref="IComparable"/> and expose a static <c>Parse(string)</c> method.
        /// </param>
        /// <param name="currentVersion">
        /// String representation of the current document version (e.g. <c>"2.1.0"</c>).
        /// </param>
        public BJsonVersionContextAttribute(Type versionType, string currentVersion)
        {
            VersionType = versionType ?? throw new BJsonValidationException("Parameter 'versionType' cannot be null.");
            CurrentVersion = currentVersion ?? throw new BJsonValidationException("Parameter 'currentVersion' cannot be null.");
        }

        /// <summary>The concrete type used to represent versions.</summary>
        public Type VersionType { get; }

        /// <summary>String representation of the current document version.</summary>
        public string CurrentVersion { get; }
    }
}
