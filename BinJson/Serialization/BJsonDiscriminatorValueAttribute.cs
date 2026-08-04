#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Declares the discriminator token that represents a concrete polymorphic type in serialized payloads.
    /// <para>
    /// This attribute is typically applied to derived types participating in a contract defined by
    /// <see cref="BJsonPolymorphicAttribute"/> and <see cref="BJsonDerivedTypeAttribute"/>.
    /// It is useful when the desired wire value should not be the CLR full type name.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonDiscriminatorValue("mage")]
    /// public sealed class Mage : Actor
    /// {
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class BJsonDiscriminatorValueAttribute : Attribute
    {
        public BJsonDiscriminatorValueAttribute(string value)
        {
            Value = value ?? throw new BJsonValidationException("Parameter 'value' cannot be null.");
        }

        /// <summary>
        /// Discriminator token written to or matched from the payload.
        /// </summary>
        public string Value { get; }
    }
}