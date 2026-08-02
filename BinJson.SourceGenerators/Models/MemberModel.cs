#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Base class for property and field models
    /// </summary>
    internal abstract class MemberModel
    {
        protected MemberModel(
            string memberName,
            string memberType,
            bool isNullable,
            bool isValueType,
            bool isPublic,
            bool isStatic,
            bool isReadOnly)
        {
            MemberName = memberName;
            MemberType = memberType;
            IsNullable = isNullable;
            IsValueType = isValueType;
            IsPublic = isPublic;
            IsStatic = isStatic;
            IsReadOnly = isReadOnly;
        }

        /// <summary>CLR member name (e.g., "UserName")</summary>
        public string MemberName { get; }

        /// <summary>Fully qualified type name (e.g., "System.String")</summary>
        public string MemberType { get; }

        /// <summary>True if the type is nullable (T? or reference type with nullable annotation)</summary>
        public bool IsNullable { get; }

        /// <summary>True if the member type is a value type (struct/enum)</summary>
        public bool IsValueType { get; }

        /// <summary>True if the member is public</summary>
        public bool IsPublic { get; }

        /// <summary>True if the member is static</summary>
        public bool IsStatic { get; }

        /// <summary>True if the member is readonly</summary>
        public bool IsReadOnly { get; }

        /// <summary>JSON serialized name (respects NamingPolicy and attributes)</summary>
        public string? JsonName { get; set; }

        /// <summary>Order for serialization (from [BJsonProperty])</summary>
        public int Order { get; set; }

        /// <summary>True if marked with [BJsonRequired] or BJsonProperty.Required=true</summary>
        public bool IsRequired { get; set; }

        /// <summary>Ignore condition (from [BJsonIgnore])</summary>
        public IgnoreCondition IgnoreCondition { get; set; } = IgnoreCondition.Never;

        /// <summary>Custom converter type (from [BJsonConverter])</summary>
        public string? CustomConverterType { get; set; }

        /// <summary>True if marked with [BJsonExtensionData]</summary>
        public bool IsExtensionData { get; set; }

        /// <summary>True if explicitly included with [BJsonInclude]</summary>
        public bool HasIncludeAttribute { get; set; }

        /// <summary>Version range metadata from [BJsonVersion]. Null if no version constraint.</summary>
        public VersionInfo? Version { get; set; }

        /// <summary>Name of the static predicate method from [BJsonIgnoreWhen]. Null if not present.</summary>
        public string? IgnoreWhenMethod { get; set; }

        /// <summary>Name of the static mapper method from [BJsonValueMapper]. Null if not present.</summary>
        public string? ValueMapperMethod { get; set; }

        /// <summary>True if marked with [BJsonExternalRef].</summary>
        public bool IsExternalRef { get; set; }

        /// <summary>Fixed path from [BJsonExternalRef]. Null means path comes from the JSON value.</summary>
        public string? ExternalRefFixedPath { get; set; }

        /// <summary>True if [BJsonExternalRef] has Optional = true.</summary>
        public bool IsExternalRefOptional { get; set; }

        /// <summary>Anchor name from [BJsonAnchor]. Null if not present.</summary>
        public string? AnchorName { get; set; }

        /// <summary>Default value info from [BJsonDefaultValue] and/or [BJsonDefaultProvider]. Null if not present.</summary>
        public DefaultValueInfo? DefaultValue { get; set; }

        public abstract MemberKind Kind { get; }
    }

    internal enum MemberKind
    {
        Property,
        Field
    }

    internal enum IgnoreCondition
    {
        Never                = 0,
        Always               = 1,
        WhenWritingNull      = 2,
        WhenWritingDefault   = 3,
        WhenWritingCustomDefault = 4,
        WhenWriting          = 5,
        WhenReading          = 6,
    }
}
