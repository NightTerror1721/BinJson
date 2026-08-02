#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member as a named anchor that can be referenced elsewhere in the same BJson
    /// document using a <c>{ "$ref": "anchorName" }</c> token (YAML-style anchors).
    /// <para>
    /// During serialization, the member value is written normally and additionally registered
    /// under <see cref="AnchorName"/> in the document's anchor table.
    /// </para>
    /// <para>
    /// During deserialization, any node in the document containing <c>{ "$ref": "anchorName" }</c>
    /// is replaced with the value of this member before typed deserialization occurs.
    /// The BJson DOM pre-processor must be enabled (via <see cref="BJsonPreprocessorAttribute"/>)
    /// for anchor resolution to take effect.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable]
    /// [BJsonPreprocessor]
    /// public class Config
    /// {
    ///     [BJsonAnchor("defaultColor")]
    ///     public string PrimaryColor { get; set; } = "#FFFFFF";
    ///
    ///     // In the JSON document, other nodes can reference this value:
    ///     // { "$ref": "defaultColor" }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonAnchorAttribute : Attribute
    {
        /// <param name="anchorName">
        /// The unique name by which this member's value is registered as an anchor.
        /// Must be unique within the document.
        /// </param>
        public BJsonAnchorAttribute(string anchorName)
        {
            AnchorName = anchorName ?? throw new ArgumentNullException(nameof(anchorName));
        }

        /// <summary>The unique anchor name used in <c>{ "$ref": "anchorName" }</c> tokens.</summary>
        public string AnchorName { get; }
    }
}
