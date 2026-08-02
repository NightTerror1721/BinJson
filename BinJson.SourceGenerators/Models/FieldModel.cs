#nullable enable

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Represents a field to be serialized/deserialized
    /// </summary>
    internal sealed class FieldModel : MemberModel
    {
        public FieldModel(
            string memberName,
            string memberType,
            bool isNullable,
            bool isValueType,
            bool isPublic,
            bool isStatic,
            bool isReadOnly)
            : base(memberName, memberType, isNullable, isValueType, isPublic, isStatic, isReadOnly)
        {
        }

        public override MemberKind Kind => MemberKind.Field;

        /// <summary>True if this field can be serialized</summary>
        public bool CanSerialize => !IsStatic;

        /// <summary>True if this field can be deserialized</summary>
        public bool CanDeserialize => !IsReadOnly && !IsStatic;
    }
}
