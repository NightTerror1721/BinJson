#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a CLR type as participating in BinJson attribute-driven serialization.
    /// <para>
    /// The attribute is used by both the reflection-based serializer and the source generator.
    /// It also lets you declare per-type contract settings such as field inclusion,
    /// private-member inclusion, and naming policy.
    /// </para>
    /// <para>
    /// Apply this attribute to DTO-style classes or structs whose payload should be inferred
    /// from members plus additional BinJson attributes.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
    /// public sealed class PlayerProfile
    /// {
    ///     public string PlayerName { get; set; } = string.Empty;
    ///     public int Level { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class BJsonSerializableAttribute : Attribute
    {
        /// <summary>
        /// When <c>true</c>, public fields are considered serializable members in addition to properties.
        /// Default is <c>false</c> for attribute declarations.
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// When <c>true</c>, non-public members may participate in serialization.
        /// Members can also be included explicitly with <see cref="BJsonIncludeAttribute"/>.
        /// </summary>
        public bool IncludePrivateMembers { get; set; }

        /// <summary>
        /// Naming policy applied when a member does not declare an explicit JSON name.
        /// </summary>
        public NamingPolicy NamingPolicy { get; set; } = NamingPolicy.Default;
    }
}