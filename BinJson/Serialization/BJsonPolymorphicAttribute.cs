#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Enables polymorphic serialization and deserialization for a base class or interface.
    /// <para>
    /// BinJson writes a discriminator property to identify the concrete runtime type and uses that discriminator
    /// to resolve derived instances during deserialization.
    /// </para>
    /// <para>
    /// Pair this attribute with one or more <see cref="BJsonDerivedTypeAttribute"/> declarations,
    /// and optionally with <see cref="BJsonDiscriminatorValueAttribute"/> on derived types.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    /// [BJsonDerivedType(typeof(Mage), TypeDiscriminator = "mage")]
    /// public abstract class Actor
    /// {
    ///     public string Name { get; set; } = string.Empty;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class BJsonPolymorphicAttribute : Attribute
    {
        /// <summary>
        /// JSON property name used to store the type discriminator. Default is <c>$type</c>.
        /// </summary>
        public string TypeDiscriminatorPropertyName { get; set; } = "$type";
    }
}