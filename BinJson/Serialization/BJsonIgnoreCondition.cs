#nullable enable

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Specifies the condition under which a member is ignored during BJson serialization or deserialization.
    /// </summary>
    public enum BJsonIgnoreCondition
    {
        /// <summary>Never ignore the member. This is the default.</summary>
        Never = 0,

        /// <summary>Always ignore the member, both when writing and when reading.</summary>
        Always = 1,

        /// <summary>Ignore the member when its value is <c>null</c> during serialization.</summary>
        WhenWritingNull = 2,

        /// <summary>
        /// Ignore the member when its value equals the CLR default for its type
        /// (<c>null</c> for reference types, <c>0</c> / <c>false</c> for value types) during serialization.
        /// </summary>
        WhenWritingDefault = 3,

        /// <summary>
        /// Ignore the member when its value equals a custom default defined by
        /// <see cref="IBJsonDefaultProvider{T}"/> associated via <c>[BJsonDefaultProvider]</c>.
        /// Falls back to <see cref="WhenWritingDefault"/> behaviour if no provider is found.
        /// </summary>
        WhenWritingCustomDefault = 4,

        /// <summary>Ignore the member only during serialization (writing). The member is still read during deserialization.</summary>
        WhenWriting = 5,

        /// <summary>Ignore the member only during deserialization (reading). The member is still written during serialization.</summary>
        WhenReading = 6,
    }
}
