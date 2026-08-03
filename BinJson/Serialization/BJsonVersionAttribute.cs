#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Controls the version range in which a member or type participates in
    /// BJson serialization and deserialization.
    /// <para>
    /// The <paramref name="versionType"/> must implement <see cref="IComparable"/> and expose
    /// a public static <c>Parse(string)</c> method (e.g. <see cref="Version"/>,
    /// <c>SemanticVersion</c>, or any custom comparable type).
    /// </para>
    /// <para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>introducedIn</b>: The member is included only when the document version is greater
    ///     than or equal to this value. <c>null</c> means the member has always existed.
    ///   </description></item>
    ///   <item><description>
    ///     <b>removedIn</b>: The member is excluded when the document version is greater than
    ///     or equal to this value (exclusive upper bound). <c>null</c> means the member is
    ///     never removed.
    ///   </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The active document version is provided by <see cref="BJsonVersionContextAttribute"/>
    /// on the containing type, or via <c>BJsonSerializerOptions.Version</c> at runtime.
    /// If no version context is active, version constraints are ignored and the member
    /// participates unconditionally.
    /// </para>
    /// <para>
    /// Use <see cref="RenamedFrom"/> to handle legacy JSON key names when a member was
    /// renamed in a newer version. During deserialization, the engine tries both the
    /// current name and the legacy name.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // Member introduced in v1.5, still present
    /// [BJsonVersion(typeof(Version), introducedIn: "1.5.0")]
    /// public int Level { get; set; }
    ///
    /// // Member removed in v2.0 (only read/written for documents older than 2.0)
    /// [BJsonVersion(typeof(Version), introducedIn: "1.0.0", removedIn: "2.0.0")]
    /// public int OldScore { get; set; }
    ///
    /// // Member renamed from "score" to "totalScore" in v2.0
    /// [BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
    /// public int TotalScore { get; set; }
    ///
    /// // Composing with default value for legacy documents
    /// [BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
    /// [BJsonDefaultValue(0)]
    /// public int NewField { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false)]
    public sealed class BJsonVersionAttribute : Attribute
    {
        /// <param name="versionType">
        /// The concrete type used to represent version values (e.g. <c>typeof(Version)</c>).
        /// Must implement <see cref="IComparable"/> and expose a static <c>Parse(string)</c> method.
        /// </param>
        /// <param name="introducedIn">
        /// String representation of the version at which this member was introduced.
        /// <c>null</c> means the member has always existed.
        /// </param>
        /// <param name="removedIn">
        /// String representation of the version at which this member was removed (exclusive).
        /// <c>null</c> means the member is never removed.
        /// </param>
        public BJsonVersionAttribute(Type versionType, string? introducedIn = null, string? removedIn = null)
        {
            VersionType = versionType ?? throw new BJsonValidationException("Parameter 'versionType' cannot be null.");
            IntroducedIn = introducedIn;
            RemovedIn = removedIn;
        }

        /// <summary>
        /// The concrete type used to represent versions.
        /// Must implement <see cref="IComparable"/> and expose a static <c>Parse(string)</c>.
        /// </summary>
        public Type VersionType { get; }

        /// <summary>
        /// String representation of the version at which this member was first introduced.
        /// <c>null</c> means the member has always existed.
        /// </summary>
        public string? IntroducedIn { get; }

        /// <summary>
        /// String representation of the version at which this member was removed (exclusive upper bound).
        /// <c>null</c> means the member is never removed.
        /// </summary>
        public string? RemovedIn { get; }

        /// <summary>
        /// Optional legacy JSON key name. When set, the engine tries to read this name in addition
        /// to the current JSON name during deserialization, enabling smooth migration when a member
        /// is renamed across versions.
        /// </summary>
        public string? RenamedFrom { get; set; }
    }
}
