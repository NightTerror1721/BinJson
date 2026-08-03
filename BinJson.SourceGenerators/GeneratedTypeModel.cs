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

        /// <summary>All properties to serialize/deserialize</summary>
        public List<PropertyModel> Properties { get; } = new();

        /// <summary>All fields to serialize/deserialize (if IncludeFields=true)</summary>
        public List<FieldModel> Fields { get; } = new();

        /// <summary>Constructor to use for deserialization (null if parameterless default)</summary>
        public ConstructorModel? Constructor { get; set; }

        /// <summary>Member marked with [BJsonExtensionData] (must be IDictionary&lt;string, BJsonValue&gt;)</summary>
        public MemberModel? ExtensionDataMember { get; set; }

        /// <summary>All members (properties + fields) for easy iteration</summary>
        public IEnumerable<MemberModel> AllMembers
        {
            get
            {
                foreach (var prop in Properties)
                    yield return prop;
                foreach (var fd in Fields)
                    yield return fd;
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
