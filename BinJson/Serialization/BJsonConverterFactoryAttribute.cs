#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Applies a converter factory to a member or type.
    /// <para>
    /// Use this attribute when conversion depends on the closed runtime type,
    /// for example for open generic wrappers such as <c>Wrapped&lt;T&gt;</c>.
    /// </para>
    /// <para>
    /// The referenced factory must implement <see cref="IBJsonConverterFactory"/> and be instantiable.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable]
    /// public sealed class Envelope
    /// {
    ///     [BJsonConverterFactory(typeof(WrappedConverterFactory))]
    ///     public Wrapped&lt;int&gt; Count { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonConverterFactoryAttribute : Attribute
    {
        public BJsonConverterFactoryAttribute(Type factoryType)
        {
            FactoryType = factoryType ?? throw new BJsonValidationException("Parameter 'factoryType' cannot be null.");
        }

        /// <summary>
        /// Factory type responsible for creating an <see cref="IBJsonConverter"/> for the target type.
        /// </summary>
        public Type FactoryType { get; }
    }
}