#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Applies a custom converter type to a member or an entire CLR type.
    /// <para>
    /// On a property or field, the converter overrides the default conversion logic for that member.
    /// On a class or struct, the converter becomes the canonical serialization strategy for the type.
    /// </para>
    /// <para>
    /// The referenced converter type must implement <see cref="IBJsonConverter"/>,
    /// typically by inheriting from <see cref="BJsonConverter{T}"/>.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// public sealed class DateOnlyStringConverter : BJsonConverter&lt;DateTime&gt;
    /// {
    ///     public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
    ///         =&gt; BJsonValue.Create(value.ToString("yyyy-MM-dd"));
    ///
    ///     public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
    ///         =&gt; DateTime.Parse(value.StringValue);
    /// }
    ///
    /// [BJsonSerializable]
    /// public sealed class AuditEntry
    /// {
    ///     [BJsonConverter(typeof(DateOnlyStringConverter))]
    ///     public DateTime CreatedAt { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonConverterAttribute : Attribute
    {
        /// <param name="converterType">Concrete converter type to instantiate.</param>
        public BJsonConverterAttribute(Type converterType)
        {
            ConverterType = converterType ?? throw new BJsonValidationException("Parameter 'converterType' cannot be null.");
        }

        /// <summary>
        /// Concrete converter type used for serialization and deserialization.
        /// </summary>
        public Type ConverterType { get; }
    }
}