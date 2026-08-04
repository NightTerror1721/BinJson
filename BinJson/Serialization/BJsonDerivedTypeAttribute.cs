#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Registers a concrete derived type for a polymorphic base contract.
    /// <para>
    /// Apply this attribute to the base type once per supported subtype.
    /// If <see cref="TypeDiscriminator"/> is omitted, BinJson falls back to the derived type's
    /// explicit <see cref="BJsonDiscriminatorValueAttribute"/>, full name, or type name depending on context.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    /// [BJsonDerivedType(typeof(SwordItem), TypeDiscriminator = "sword")]
    /// [BJsonDerivedType(typeof(BowItem), TypeDiscriminator = "bow")]
    /// public abstract class Item
    /// {
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class BJsonDerivedTypeAttribute : Attribute
    {
        /// <param name="derivedType">Concrete subtype allowed for the polymorphic contract.</param>
        public BJsonDerivedTypeAttribute(Type derivedType)
        {
            DerivedType = derivedType ?? throw new BJsonValidationException("Parameter 'derivedType' cannot be null.");
        }

        /// <summary>
        /// Concrete subtype allowed for the polymorphic contract.
        /// </summary>
        public Type DerivedType { get; }

        /// <summary>
        /// Optional explicit discriminator token written to the document for this subtype.
        /// </summary>
        public string? TypeDiscriminator { get; set; }
    }
}