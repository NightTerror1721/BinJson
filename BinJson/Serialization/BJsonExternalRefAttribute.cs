#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member as a reference to an external BJson file.
    /// <para>
    /// During <b>deserialization</b>, the member value is loaded from the referenced path
    /// instead of being read inline from the current document.
    /// </para>
    /// <para>
    /// During <b>serialization</b>, the member value is written to a separate file and the
    /// current document stores the reference file path as a string token in its place.
    /// </para>
    /// <para>
    /// When <see cref="FixedPath"/> is <c>null</c>, the path is read from the JSON value of
    /// the member itself (which must be a string token containing the file path).
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // Path resolved from the JSON value at runtime
    /// [BJsonExternalRef]
    /// public LevelData? Level { get; set; }
    ///
    /// // Fixed path relative to the document root
    /// [BJsonExternalRef(FixedPath = "data/inventory.bjson")]
    /// public Inventory? Inventory { get; set; }
    ///
    /// // Optional — missing file produces default(T) instead of throwing
    /// [BJsonExternalRef(Optional = true)]
    /// public Settings? Settings { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonExternalRefAttribute : Attribute
    {
        /// <summary>
        /// Fixed relative path to the referenced file.
        /// When <c>null</c>, the path is read from the JSON string value of the member itself.
        /// </summary>
        public string? FixedPath { get; set; }

        /// <summary>
        /// When <c>true</c>, resolving the reference is optional: a missing or unreadable file
        /// produces <c>default(T)</c> instead of throwing an exception.
        /// Default is <c>false</c>.
        /// </summary>
        public bool Optional { get; set; }
    }
}
