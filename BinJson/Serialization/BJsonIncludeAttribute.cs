#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Forces a member to participate in serialization even when it would otherwise be excluded
    /// by visibility rules.
    /// <para>
    /// This is most commonly used for non-public properties or fields that belong to the persisted contract.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable]
    /// public sealed class SessionState
    /// {
    ///     [BJsonInclude]
    ///     internal string Token { get; set; } = string.Empty;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonIncludeAttribute : Attribute
    {
    }
}