#nullable enable

using System.Collections.Generic;
using Krampus.BinJson.SourceGenerators.Models;

namespace Krampus.BinJson.SourceGenerators
{
    /// <summary>
    /// Complete model of a type to generate serialization code for
    /// </summary>
    internal sealed class GeneratedTypeModel
    {
        public GeneratedTypeModel(
            string @namespace,
            string typeName,
            bool isValueType,
            TypeConfiguration configuration)
        {
            Namespace = @namespace;
            TypeName = typeName;
            IsValueType = isValueType;
            Configuration = configuration;
        }

        /// <summary>Namespace of the type (empty string if global)</summary>
        public string Namespace { get; }

        /// <summary>Simple type name (e.g., "SaveData")</summary>
        public string TypeName { get; }

        /// <summary>True if this is a struct (value type)</summary>
        public bool IsValueType { get; }

        /// <summary>Configuration from [BJsonSerializable] attribute</summary>
        public TypeConfiguration Configuration { get; }

        /// <summary>Unique hint-name-safe identifier for generated source files.</summary>
        public string HintName { get; set; } = string.Empty;

        /// <summary>All properties to serialize/deserialize</summary>
        public List<PropertyModel> Properties { get; } = new();

        /// <summary>All fields to serialize/deserialize (if IncludeFields=true)</summary>
        public List<FieldModel> Fields { get; } = new();

        /// <summary>Constructor to use for deserialization (null if parameterless default)</summary>
        public ConstructorModel? Constructor { get; set; }

        /// <summary>Member marked with [BJsonExtensionData] (must be IDictionary&lt;string, BJsonValue&gt;)</summary>
        public MemberModel? ExtensionDataMember { get; set; }

        /// <summary>All members (properties + fields) for easy iteration</summary>
        private List<MemberModel>? _allMembers;

        public IReadOnlyList<MemberModel> AllMembers
        {
            get
            {
                if (_allMembers is null)
                {
                    _allMembers = new List<MemberModel>(Properties.Count + Fields.Count);
                    _allMembers.AddRange(Properties);
                    _allMembers.AddRange(Fields);
                }

                return _allMembers;
            }
        }

        /// <summary>Fully qualified type name</summary>
        public string FullTypeName
        {
            get
            {
                if (string.IsNullOrEmpty(Namespace))
                    return TypeName;
                return $"{Namespace}.{TypeName}";
            }
        }
    }
}
