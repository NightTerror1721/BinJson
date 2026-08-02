#nullable enable

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Represents a property to be serialized/deserialized
    /// </summary>
    internal sealed class PropertyModel : MemberModel
    {
        public PropertyModel(
            string memberName,
            string memberType,
            bool isNullable,
            bool isValueType,
            bool isPublic,
            bool isStatic,
            bool isReadOnly,
            bool hasGetter,
            bool hasSetter)
            : base(memberName, memberType, isNullable, isValueType, isPublic, isStatic, isReadOnly)
        {
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }

        /// <summary>True if property has a getter</summary>
        public bool HasGetter { get; }

        /// <summary>True if property has a setter (public or init)</summary>
        public bool HasSetter { get; }

        public override MemberKind Kind => MemberKind.Property;

        /// <summary>True if this property can be serialized</summary>
        public bool CanSerialize => HasGetter && !IsStatic;

        /// <summary>True if this property can be deserialized</summary>
        public bool CanDeserialize => HasSetter && !IsStatic;
    }
}
