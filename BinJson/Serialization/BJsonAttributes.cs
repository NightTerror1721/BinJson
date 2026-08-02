#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class BJsonSerializableAttribute : Attribute
    {
        public bool IncludeFields { get; set; }

        public bool IncludePrivateMembers { get; set; }

        public NamingPolicy NamingPolicy { get; set; } = NamingPolicy.Default;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonConverterAttribute : Attribute
    {
        public BJsonConverterAttribute(Type converterType)
        {
            ConverterType = converterType ?? throw new ArgumentNullException(nameof(converterType));
        }

        public Type ConverterType { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonPropertyAttribute : Attribute
    {
        public string? Name { get; set; }

        public int Order { get; set; }

        public bool Required { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonPropertyNameAttribute : Attribute
    {
        public BJsonPropertyNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonIncludeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonRequiredAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonExtensionDataAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class BJsonConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonIgnoreAttribute : Attribute
    {
        public BJsonIgnoreCondition Condition { get; set; } = BJsonIgnoreCondition.Always;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class BJsonPolymorphicAttribute : Attribute
    {
        public string TypeDiscriminatorPropertyName { get; set; } = "$type";
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class BJsonDerivedTypeAttribute : Attribute
    {
        public BJsonDerivedTypeAttribute(Type derivedType)
        {
            DerivedType = derivedType ?? throw new ArgumentNullException(nameof(derivedType));
        }

        public Type DerivedType { get; }

        public string? TypeDiscriminator { get; set; }
    }
}
