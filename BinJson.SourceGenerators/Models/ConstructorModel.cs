#nullable enable

using System.Collections.Generic;

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Represents a constructor to use for deserialization
    /// </summary>
    internal sealed class ConstructorModel
    {
        public ConstructorModel(List<ConstructorParameterModel> parameters, bool isParameterless)
        {
            Parameters = parameters;
            IsParameterless = isParameterless;
        }

        /// <summary>Constructor parameters in order</summary>
        public List<ConstructorParameterModel> Parameters { get; }

        /// <summary>True if this is a parameterless constructor</summary>
        public bool IsParameterless { get; }

        /// <summary>True if constructor is marked with [BJsonConstructor]</summary>
        public bool HasAttribute { get; set; }
    }

    /// <summary>
    /// Represents a constructor parameter
    /// </summary>
    internal sealed class ConstructorParameterModel
    {
        public ConstructorParameterModel(
            string parameterName,
            string parameterType,
            bool isNullable,
            bool isValueType)
        {
            ParameterName = parameterName;
            ParameterType = parameterType;
            IsNullable = isNullable;
            IsValueType = isValueType;
        }

        /// <summary>Parameter name as declared in constructor</summary>
        public string ParameterName { get; }

        /// <summary>Fully qualified type name</summary>
        public string ParameterType { get; }

        /// <summary>True if parameter type is nullable</summary>
        public bool IsNullable { get; }

        /// <summary>True if parameter type is a value type</summary>
        public bool IsValueType { get; }

        /// <summary>Corresponding JSON property name</summary>
        public string? JsonName { get; set; }

        /// <summary>Corresponding member that matches this parameter</summary>
        public MemberModel? MatchingMember { get; set; }
    }
}
