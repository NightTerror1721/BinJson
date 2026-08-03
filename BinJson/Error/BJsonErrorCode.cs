#nullable enable

namespace Krampus.BinJson.Error
{
    public enum BJsonErrorCode
    {
        Unknown = 0,

        ParseUnexpectedTrailingChar,
        ParseUnexpectedEof,
        ParseUnexpectedChar,
        ParseInvalidNullLiteral,
        ParseInvalidBoolLiteral,
        ParseExpectedStringStart,
        ParseUnexpectedEofInEscape,
        ParseInvalidEscape,
        ParseUnescapedControlChar,
        ParseUnterminatedString,
        ParseIncompleteUnicodeEscape,
        ParseInvalidUnicodeEscape,
        ParseInvalidNumber,
        ParseInvalidFraction,
        ParseInvalidExponent,
        ParseInvalidFloat,
        ParseNumberOutOfRange,
        ParseExpectedArrayStart,
        ParseUnexpectedEofInArray,
        ParseExpectedArraySeparator,
        ParseExpectedObjectStart,
        ParseExpectedPropertyName,
        ParseExpectedColon,
        ParseDuplicateKey,
        ParseUnexpectedEofInObject,
        ParseExpectedObjectSeparator,
        ParseUnterminatedBlockComment,

        TextReadParseError,
        TextSerializationError,

        BinaryFormatError,
        BinarySerializationError,
    }
}
