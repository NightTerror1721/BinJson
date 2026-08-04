#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    internal sealed class JsonTextParser
    {
        private readonly string _json;
        private readonly bool _allowComments;
        private PathSegment[] _pathSegments;
        private int _pathDepth;
        private int _position;

        private JsonTextParser(string json, bool allowComments)
        {
            _json = json;
            _allowComments = allowComments;
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
            _position = 0;
        }

        public static BJsonValue Parse(string json, BJsonTextReaderOptions? options = null)
        {
            if (json == null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            options ??= BJsonTextReaderOptions.Default;

            var parser = new JsonTextParser(json, options.AllowComments);
            var value = parser.ParseValue();
            parser.EnsureEndOfJson();

            return value;
        }

        public static void Visit(string json, BJsonTextVisitor visitor, BJsonTextReaderOptions? options = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");
            if (visitor is null)
                throw new BJsonValidationException("Parameter 'visitor' cannot be null.");

            options ??= BJsonTextReaderOptions.Default;

            var parser = new JsonTextParser(json, options.AllowComments);
            visitor.OnDocumentStart();
            parser.VisitValue(visitor);
            parser.EnsureEndOfJson();
            visitor.OnDocumentEnd();
        }

        public static bool TryReadRootObjectProperty(string json, string propertyName, out BJsonValue value, BJsonTextReaderOptions? options = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");
            if (propertyName is null)
                throw new BJsonValidationException("Parameter 'propertyName' cannot be null.");

            options ??= BJsonTextReaderOptions.Default;

            var parser = new JsonTextParser(json, options.AllowComments);
            bool found = parser.TryReadRootObjectPropertyCore(propertyName, out value);
            parser.EnsureEndOfJson();
            return found;
        }

        public static BJsonObject ReadRootObjectProperties(string json, IReadOnlyList<string> propertyNames, BJsonTextReaderOptions? options = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");
            if (propertyNames is null)
                throw new BJsonValidationException("Parameter 'propertyNames' cannot be null.");

            options ??= BJsonTextReaderOptions.Default;

            var parser = new JsonTextParser(json, options.AllowComments);
            BJsonObject selected = parser.ReadRootObjectPropertiesCore(propertyNames);
            parser.EnsureEndOfJson();
            return selected;
        }

        private BJsonValue ParseValue()
        {
            SkipWhitespace();

            if (_position >= _json.Length)
                throw CreateParseException("Unexpected end of JSON.", _position, BJsonErrorCode.ParseUnexpectedEof);

            char c = _json[_position];

            if (c == 'n')
                return ParseNull();
            if (c == 't' || c == 'f')
                return ParseBoolean();
            if (c == '"')
                return ParseString();
            if (c == '[')
                return ParseArray();
            if (c == '{')
                return ParseObject();
            if (c == '-' || char.IsDigit(c))
                return ParseNumber();

            throw CreateParseException(
                $"Unexpected character at position {_position}: '{c}'",
                _position,
                BJsonErrorCode.ParseUnexpectedChar,
                new Dictionary<string, object?> { ["found"] = c.ToString() });
        }

        private void VisitValue(BJsonTextVisitor visitor)
        {
            SkipWhitespace();

            if (_position >= _json.Length)
                throw CreateParseException("Unexpected end of JSON.", _position, BJsonErrorCode.ParseUnexpectedEof);

            char c = _json[_position];
            if (c == 'n')
            {
                ParseNull();
                visitor.OnNull();
                return;
            }

            if (c == 't' || c == 'f')
            {
                BJsonValue value = ParseBoolean();
                visitor.OnBoolean(value.BoolValue);
                return;
            }

            if (c == '"')
            {
                visitor.OnString(ParseStringValue());
                return;
            }

            if (c == '[')
            {
                VisitArray(visitor);
                return;
            }

            if (c == '{')
            {
                VisitObject(visitor);
                return;
            }

            if (c == '-' || char.IsDigit(c))
            {
                EmitScalarNumber(visitor, ParseNumber());
                return;
            }

            throw CreateParseException(
                $"Unexpected character at position {_position}: '{c}'",
                _position,
                BJsonErrorCode.ParseUnexpectedChar,
                new Dictionary<string, object?> { ["found"] = c.ToString() });
        }

        private bool TryReadRootObjectPropertyCore(string propertyName, out BJsonValue value)
        {
            value = BJsonValue.Null;
            bool found = false;

            SkipWhitespace();
            if (_position >= _json.Length || _json[_position] != '{')
                throw CreateParseException($"Expected '{{' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectStart);

            _position++;
            SkipWhitespace();

            if (_position < _json.Length && _json[_position] == '}')
            {
                _position++;
                return false;
            }

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != '"')
                    throw CreateParseException($"Expected property name (string) at position {_position}.", _position, BJsonErrorCode.ParseExpectedPropertyName);

                string key = ParseStringValue();
                if (!seenKeys.Add(key))
                    throw CreateParseException($"Duplicate key '{key}' in object at position {_position}.", _position, BJsonErrorCode.ParseDuplicateKey, new Dictionary<string, object?> { ["key"] = key });

                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != ':')
                    throw CreateParseException($"Expected ':' after property name at position {_position}.", _position, BJsonErrorCode.ParseExpectedColon);

                _position++;

                PushPropertyPathSegment(key);
                try
                {
                    if (!found && string.Equals(key, propertyName, StringComparison.Ordinal))
                    {
                        value = ParseValue();
                        found = true;
                    }
                    else
                    {
                        SkipValue();
                    }
                }
                finally
                {
                    PopPathSegment();
                }

                SkipWhitespace();
                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in object.", _position, BJsonErrorCode.ParseUnexpectedEofInObject);

                if (_json[_position] == '}')
                {
                    _position++;
                    return found;
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or '}}' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectSeparator);

                _position++;
            }
        }

        private BJsonObject ReadRootObjectPropertiesCore(IReadOnlyList<string> propertyNames)
        {
            var selected = new BJsonObject(propertyNames.Count);
            if (propertyNames.Count == 0)
            {
                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != '{')
                    throw CreateParseException($"Expected '{{' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectStart);

                _position++;
                SkipObjectBody();
                return selected;
            }

            var wanted = new HashSet<string>(propertyNames, StringComparer.Ordinal);

            SkipWhitespace();
            if (_position >= _json.Length || _json[_position] != '{')
                throw CreateParseException($"Expected '{{' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectStart);

            _position++;
            SkipWhitespace();
            if (_position < _json.Length && _json[_position] == '}')
            {
                _position++;
                return selected;
            }

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != '"')
                    throw CreateParseException($"Expected property name (string) at position {_position}.", _position, BJsonErrorCode.ParseExpectedPropertyName);

                string key = ParseStringValue();
                if (!seenKeys.Add(key))
                    throw CreateParseException($"Duplicate key '{key}' in object at position {_position}.", _position, BJsonErrorCode.ParseDuplicateKey, new Dictionary<string, object?> { ["key"] = key });

                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != ':')
                    throw CreateParseException($"Expected ':' after property name at position {_position}.", _position, BJsonErrorCode.ParseExpectedColon);

                _position++;

                PushPropertyPathSegment(key);
                try
                {
                    if (wanted.Contains(key) && !selected.ContainsKey(key))
                        selected.Add(key, ParseValue());
                    else
                        SkipValue();
                }
                finally
                {
                    PopPathSegment();
                }

                SkipWhitespace();
                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in object.", _position, BJsonErrorCode.ParseUnexpectedEofInObject);

                if (_json[_position] == '}')
                {
                    _position++;
                    return selected;
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or '}}' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectSeparator);

                _position++;
            }
        }

        private BJsonValue ParseNull()
        {
            if (!TryConsume("null"))
                throw CreateParseException($"Invalid null literal at position {_position}.", _position, BJsonErrorCode.ParseInvalidNullLiteral);
            return BJsonValue.Null;
        }

        private BJsonValue ParseBoolean()
        {
            if (TryConsume("true"))
                return BJsonValue.True;
            if (TryConsume("false"))
                return BJsonValue.False;

            throw CreateParseException($"Invalid boolean literal at position {_position}.", _position, BJsonErrorCode.ParseInvalidBoolLiteral);
        }

        private BJsonValue ParseString()
        {
            return BJsonValue.Create(ParseStringValue());
        }

        private string ParseStringValue()
        {
            if (_json[_position] != '"')
                throw CreateParseException($"Expected '\"' at position {_position}.", _position, BJsonErrorCode.ParseExpectedStringStart);

            _position++;

            int rawStart = _position;
            while (_position < _json.Length)
            {
                char c = _json[_position];
                if (c == '"')
                {
                    string value = _json.Substring(rawStart, _position - rawStart);
                    _position++;
                    return value;
                }

                if (c == '\\' || c < ' ')
                    break;

                _position++;
            }

            var sb = new StringBuilder(Math.Min(256, _json.Length - rawStart));
            if (_position > rawStart)
                sb.Append(_json, rawStart, _position - rawStart);

            while (_position < _json.Length)
            {
                char c = _json[_position];

                if (c == '"')
                {
                    _position++;
                    return sb.ToString();
                }

                if (c == '\\')
                {
                    _position++;
                    if (_position >= _json.Length)
                        throw CreateParseException("Unexpected end of JSON in string escape.", _position, BJsonErrorCode.ParseUnexpectedEofInEscape);

                    char escaped = _json[_position];
                    _position++;

                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append(escaped);
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            sb.Append(ParseUnicodeEscape());
                            break;
                        default:
                            throw CreateParseException(
                                $"Invalid escape sequence '\\{escaped}' at position {_position - 1}.",
                                _position - 1,
                                BJsonErrorCode.ParseInvalidEscape,
                                new Dictionary<string, object?> { ["escape"] = escaped.ToString() });
                    }
                }
                else if (c < ' ')
                {
                    throw CreateParseException(
                        $"Unescaped control character (U+{((int)c):X4}) at position {_position}.",
                        _position,
                        BJsonErrorCode.ParseUnescapedControlChar,
                        new Dictionary<string, object?> { ["codePoint"] = $"U+{((int)c):X4}" });
                }
                else
                {
                    sb.Append(c);
                    _position++;
                }
            }

            throw CreateParseException("Unterminated string.", _position, BJsonErrorCode.ParseUnterminatedString);
        }

        private char ParseUnicodeEscape()
        {
            if (_position + 4 > _json.Length)
                throw CreateParseException("Incomplete Unicode escape sequence.", _position, BJsonErrorCode.ParseIncompleteUnicodeEscape);

            int start = _position;
            int codePoint = 0;
            for (int i = 0; i < 4; i++)
            {
                int value = ParseHexDigit(_json[_position + i]);
                if (value < 0)
                {
                    string hex = _json.Substring(start, 4);
                    throw CreateParseException($"Invalid Unicode escape sequence '\\u{hex}'.", start, BJsonErrorCode.ParseInvalidUnicodeEscape);
                }

                codePoint = (codePoint << 4) | value;
            }

            _position += 4;
            return (char)codePoint;
        }

        private BJsonValue ParseNumber()
        {
            ParseNumberToken(out int start, out int length, out bool isFloat);
            ReadOnlySpan<char> numberSpan = _json.AsSpan(start, length);

            if (isFloat)
            {
                if (!double.TryParse(numberSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw CreateParseException($"Invalid float number '{new string(numberSpan)}' at position {start}.", start, BJsonErrorCode.ParseInvalidFloat);
                return BJsonValue.Create(d);
            }
            else
            {
                if (long.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return BJsonValue.Create(l);

                if (ulong.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ul))
                    return BJsonValue.Create(ul);

                if (double.TryParse(numberSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return BJsonValue.Create(d);

                throw CreateParseException($"Number '{new string(numberSpan)}' is out of range at position {start}.", start, BJsonErrorCode.ParseNumberOutOfRange);
            }
        }

        private void ParseNumberToken(out int start, out int length, out bool isFloat)
        {
            start = _position;

            if (_json[_position] == '-')
                _position++;

            if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                throw CreateParseException($"Invalid number at position {start}.", start, BJsonErrorCode.ParseInvalidNumber);

            if (_json[_position] == '0')
            {
                _position++;
            }
            else
            {
                while (_position < _json.Length && char.IsDigit(_json[_position]))
                    _position++;
            }

            isFloat = false;

            if (_position < _json.Length && _json[_position] == '.')
            {
                isFloat = true;
                _position++;

                if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                    throw CreateParseException($"Invalid number: expected digit after '.' at position {_position}.", _position, BJsonErrorCode.ParseInvalidFraction);

                while (_position < _json.Length && char.IsDigit(_json[_position]))
                    _position++;
            }

            if (_position < _json.Length && (_json[_position] == 'e' || _json[_position] == 'E'))
            {
                isFloat = true;
                _position++;

                if (_position < _json.Length && (_json[_position] == '+' || _json[_position] == '-'))
                    _position++;

                if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                    throw CreateParseException($"Invalid number: expected digit in exponent at position {_position}.", _position, BJsonErrorCode.ParseInvalidExponent);

                while (_position < _json.Length && char.IsDigit(_json[_position]))
                    _position++;
            }

            length = _position - start;
        }

        private BJsonValue ParseArray()
        {
            if (_json[_position] != '[')
                throw CreateParseException($"Expected '[' at position {_position}.", _position, BJsonErrorCode.ParseExpectedArrayStart);

            _position++;
            SkipWhitespace();

            var array = new BJsonArray();

            if (_position < _json.Length && _json[_position] == ']')
            {
                _position++;
                return BJsonValue.Create(array);
            }

            while (true)
            {
                int index = array.Count;
                PushIndexPathSegment(index);
                try
                {
                    array.Add(ParseValue());
                }
                finally
                {
                    PopPathSegment();
                }
                SkipWhitespace();

                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in array.", _position, BJsonErrorCode.ParseUnexpectedEofInArray);

                if (_json[_position] == ']')
                {
                    _position++;
                    return BJsonValue.Create(array);
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or ']' at position {_position}.", _position, BJsonErrorCode.ParseExpectedArraySeparator);

                _position++;
                SkipWhitespace();
            }
        }

        private BJsonValue ParseObject()
        {
            if (_json[_position] != '{')
                throw CreateParseException($"Expected '{{' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectStart);

            _position++;
            SkipWhitespace();

            var obj = new BJsonObject();

            if (_position < _json.Length && _json[_position] == '}')
            {
                _position++;
                return BJsonValue.Create(obj);
            }

            while (true)
            {
                SkipWhitespace();

                if (_position >= _json.Length || _json[_position] != '"')
                    throw CreateParseException($"Expected property name (string) at position {_position}.", _position, BJsonErrorCode.ParseExpectedPropertyName);

                string key = ParseStringValue();

                SkipWhitespace();

                if (_position >= _json.Length || _json[_position] != ':')
                    throw CreateParseException($"Expected ':' after property name at position {_position}.", _position, BJsonErrorCode.ParseExpectedColon);

                _position++;

                BJsonValue value;
                PushPropertyPathSegment(key);
                try
                {
                    value = ParseValue();
                }
                finally
                {
                    PopPathSegment();
                }

                if (!obj.TryAdd(key, value))
                    throw CreateParseException(
                        $"Duplicate key '{key}' in object at position {_position}.",
                        _position,
                        BJsonErrorCode.ParseDuplicateKey,
                        new Dictionary<string, object?> { ["key"] = key });

                SkipWhitespace();

                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in object.", _position, BJsonErrorCode.ParseUnexpectedEofInObject);

                if (_json[_position] == '}')
                {
                    _position++;
                    return BJsonValue.Create(obj);
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or '}}' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectSeparator);

                _position++;
            }
        }

        private void VisitArray(BJsonTextVisitor visitor)
        {
            if (_json[_position] != '[')
                throw CreateParseException($"Expected '[' at position {_position}.", _position, BJsonErrorCode.ParseExpectedArrayStart);

            visitor.OnArrayStart();
            _position++;
            SkipWhitespace();

            if (_position < _json.Length && _json[_position] == ']')
            {
                _position++;
                visitor.OnArrayEnd();
                return;
            }

            int index = 0;
            while (true)
            {
                PushIndexPathSegment(index);
                try
                {
                    VisitValue(visitor);
                }
                finally
                {
                    PopPathSegment();
                }

                index++;
                SkipWhitespace();
                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in array.", _position, BJsonErrorCode.ParseUnexpectedEofInArray);

                if (_json[_position] == ']')
                {
                    _position++;
                    visitor.OnArrayEnd();
                    return;
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or ']' at position {_position}.", _position, BJsonErrorCode.ParseExpectedArraySeparator);

                _position++;
            }
        }

        private void VisitObject(BJsonTextVisitor visitor)
        {
            if (_json[_position] != '{')
                throw CreateParseException($"Expected '{{' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectStart);

            visitor.OnObjectStart();
            _position++;
            SkipWhitespace();

            if (_position < _json.Length && _json[_position] == '}')
            {
                _position++;
                visitor.OnObjectEnd();
                return;
            }

            int propertyIndex = 0;
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != '"')
                    throw CreateParseException($"Expected property name (string) at position {_position}.", _position, BJsonErrorCode.ParseExpectedPropertyName);

                string key = ParseStringValue();
                if (!seenKeys.Add(key))
                    throw CreateParseException($"Duplicate key '{key}' in object at position {_position}.", _position, BJsonErrorCode.ParseDuplicateKey, new Dictionary<string, object?> { ["key"] = key });

                visitor.OnObjectProperty(key, propertyIndex++);

                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != ':')
                    throw CreateParseException($"Expected ':' after property name at position {_position}.", _position, BJsonErrorCode.ParseExpectedColon);

                _position++;

                PushPropertyPathSegment(key);
                try
                {
                    VisitValue(visitor);
                }
                finally
                {
                    PopPathSegment();
                }

                SkipWhitespace();
                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in object.", _position, BJsonErrorCode.ParseUnexpectedEofInObject);

                if (_json[_position] == '}')
                {
                    _position++;
                    visitor.OnObjectEnd();
                    return;
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or '}}' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectSeparator);

                _position++;
            }
        }

        private void EmitScalarNumber(BJsonTextVisitor visitor, BJsonValue number)
        {
            if (number.Type == BJsonValueType.Float)
            {
                visitor.OnFloat(number.DoubleValue);
                return;
            }

            long signed = unchecked((long)number.ULongValue);
            if (signed < 0)
                visitor.OnSignedInteger(signed);
            else
                visitor.OnUnsignedInteger(number.ULongValue);
        }

        private void SkipValue()
        {
            SkipWhitespace();
            if (_position >= _json.Length)
                throw CreateParseException("Unexpected end of JSON.", _position, BJsonErrorCode.ParseUnexpectedEof);

            char c = _json[_position];
            if (c == 'n')
            {
                ParseNull();
                return;
            }

            if (c == 't' || c == 'f')
            {
                ParseBoolean();
                return;
            }

            if (c == '"')
            {
                SkipStringToken();
                return;
            }

            if (c == '[')
            {
                _position++;
                SkipArrayBody();
                return;
            }

            if (c == '{')
            {
                _position++;
                SkipObjectBody();
                return;
            }

            if (c == '-' || char.IsDigit(c))
            {
                ParseNumberToken(out _, out _, out _);
                return;
            }

            throw CreateParseException(
                $"Unexpected character at position {_position}: '{c}'",
                _position,
                BJsonErrorCode.ParseUnexpectedChar,
                new Dictionary<string, object?> { ["found"] = c.ToString() });
        }

        private void SkipArrayBody()
        {
            SkipWhitespace();
            if (_position < _json.Length && _json[_position] == ']')
            {
                _position++;
                return;
            }

            int index = 0;
            while (true)
            {
                PushIndexPathSegment(index);
                try
                {
                    SkipValue();
                }
                finally
                {
                    PopPathSegment();
                }

                index++;
                SkipWhitespace();
                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in array.", _position, BJsonErrorCode.ParseUnexpectedEofInArray);

                if (_json[_position] == ']')
                {
                    _position++;
                    return;
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or ']' at position {_position}.", _position, BJsonErrorCode.ParseExpectedArraySeparator);

                _position++;
            }
        }

        private void SkipObjectBody()
        {
            SkipWhitespace();
            if (_position < _json.Length && _json[_position] == '}')
            {
                _position++;
                return;
            }

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != '"')
                    throw CreateParseException($"Expected property name (string) at position {_position}.", _position, BJsonErrorCode.ParseExpectedPropertyName);

                string key = ParseStringValue();
                if (!seenKeys.Add(key))
                    throw CreateParseException($"Duplicate key '{key}' in object at position {_position}.", _position, BJsonErrorCode.ParseDuplicateKey, new Dictionary<string, object?> { ["key"] = key });

                SkipWhitespace();
                if (_position >= _json.Length || _json[_position] != ':')
                    throw CreateParseException($"Expected ':' after property name at position {_position}.", _position, BJsonErrorCode.ParseExpectedColon);

                _position++;
                PushPropertyPathSegment(key);
                try
                {
                    SkipValue();
                }
                finally
                {
                    PopPathSegment();
                }

                SkipWhitespace();
                if (_position >= _json.Length)
                    throw CreateParseException("Unexpected end of JSON in object.", _position, BJsonErrorCode.ParseUnexpectedEofInObject);

                if (_json[_position] == '}')
                {
                    _position++;
                    return;
                }

                if (_json[_position] != ',')
                    throw CreateParseException($"Expected ',' or '}}' at position {_position}.", _position, BJsonErrorCode.ParseExpectedObjectSeparator);

                _position++;
            }
        }

        private void SkipStringToken()
        {
            if (_json[_position] != '"')
                throw CreateParseException($"Expected '\"' at position {_position}.", _position, BJsonErrorCode.ParseExpectedStringStart);

            _position++;
            while (_position < _json.Length)
            {
                char c = _json[_position];
                if (c == '"')
                {
                    _position++;
                    return;
                }

                if (c == '\\')
                {
                    _position++;
                    if (_position >= _json.Length)
                        throw CreateParseException("Unexpected end of JSON in string escape.", _position, BJsonErrorCode.ParseUnexpectedEofInEscape);

                    char escaped = _json[_position++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                        case 'b':
                        case 'f':
                        case 'n':
                        case 'r':
                        case 't':
                            break;
                        case 'u':
                            _ = ParseUnicodeEscape();
                            break;
                        default:
                            throw CreateParseException($"Invalid escape sequence '\\{escaped}' at position {_position - 1}.", _position - 1, BJsonErrorCode.ParseInvalidEscape, new Dictionary<string, object?> { ["escape"] = escaped.ToString() });
                    }
                }
                else if (c < ' ')
                {
                    throw CreateParseException($"Unescaped control character (U+{((int)c):X4}) at position {_position}.", _position, BJsonErrorCode.ParseUnescapedControlChar, new Dictionary<string, object?> { ["codePoint"] = $"U+{((int)c):X4}" });
                }
                else
                {
                    _position++;
                }
            }

            throw CreateParseException("Unterminated string.", _position, BJsonErrorCode.ParseUnterminatedString);
        }

        private void SkipWhitespace()
        {
            while (_position < _json.Length)
            {
                char c = _json[_position];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    _position++;
                    continue;
                }

                if (_allowComments && c == '/' && _position + 1 < _json.Length)
                {
                    char next = _json[_position + 1];
                    if (next == '/')
                    {
                        _position += 2;
                        while (_position < _json.Length)
                        {
                            char commentChar = _json[_position];
                            if (commentChar == '\r' || commentChar == '\n')
                                break;

                            _position++;
                        }
                        continue;
                    }

                    if (next == '*')
                    {
                        _position += 2;
                        while (_position + 1 < _json.Length)
                        {
                            if (_json[_position] == '*' && _json[_position + 1] == '/')
                            {
                                _position += 2;
                                goto ContinueSkipping;
                            }

                            _position++;
                        }

                        throw CreateParseException("Unterminated block comment.", _position, BJsonErrorCode.ParseUnterminatedBlockComment);
                    }
                }

                break;

            ContinueSkipping:
                continue;
            }
        }

        private bool TryConsume(string literal)
        {
            if (_position + literal.Length > _json.Length)
                return false;

            for (int i = 0; i < literal.Length; i++)
            {
                if (_json[_position + i] != literal[i])
                    return false;
            }

            _position += literal.Length;
            return true;
        }

        private void EnsureEndOfJson()
        {
            SkipWhitespace();
            if (_position < _json.Length)
            {
                throw CreateParseException(
                    $"Unexpected character at position {_position}: expected end of JSON.",
                    _position,
                    errorCode: BJsonErrorCode.ParseUnexpectedTrailingChar,
                    details: new Dictionary<string, object?>
                    {
                        ["expected"] = "end of JSON",
                        ["found"] = _json[_position].ToString()
                    });
            }
        }

        private BJsonParseException CreateParseException(
            string message,
            int? position,
            BJsonErrorCode errorCode,
            IReadOnlyDictionary<string, object?>? details = null)
        {
            int? line = null;
            int? column = null;

            if (position.HasValue)
            {
                (line, column) = GetLineColumn(position.Value);
            }

            return new BJsonParseException(message, position, line, column, errorCode, CurrentPath, details: details);
        }

        private string CurrentPath
        {
            get
            {
                if (_pathDepth == 0)
                    return "$";

                var sb = new StringBuilder("$");
                for (int i = 0; i < _pathDepth; i++)
                {
                    var segment = _pathSegments[i];
                    if (segment.IsIndex)
                    {
                        sb.Append('[');
                        sb.Append(segment.Index);
                        sb.Append(']');
                    }
                    else
                    {
                        AppendPropertySegment(sb, segment.PropertyName!);
                    }
                }

                return sb.ToString();
            }
        }

        private void PushIndexPathSegment(int index)
        {
            EnsurePathCapacity(_pathDepth + 1);
            _pathSegments[_pathDepth++] = PathSegment.ForIndex(index);
        }

        private void PushPropertyPathSegment(string key)
        {
            EnsurePathCapacity(_pathDepth + 1);
            _pathSegments[_pathDepth++] = PathSegment.ForProperty(key);
        }

        private void PopPathSegment()
        {
            if (_pathDepth <= 0)
                return;

            _pathDepth--;
            _pathSegments[_pathDepth] = default;
        }

        private void EnsurePathCapacity(int requiredCapacity)
        {
            if (_pathSegments.Length >= requiredCapacity)
                return;

            int nextSize = _pathSegments.Length == 0 ? 8 : _pathSegments.Length * 2;
            while (nextSize < requiredCapacity)
                nextSize *= 2;

            Array.Resize(ref _pathSegments, nextSize);
        }

        private static void AppendPropertySegment(StringBuilder builder, string key)
        {
            if (IsSimpleIdentifier(key))
            {
                builder.Append('.');
                builder.Append(key);
                return;
            }

            builder.Append("['");
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c == '\\' || c == '\'')
                    builder.Append('\\');
                builder.Append(c);
            }
            builder.Append("']");
        }

        private static bool IsSimpleIdentifier(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (!(char.IsLetter(key[0]) || key[0] == '_'))
                return false;

            for (int i = 1; i < key.Length; i++)
            {
                char c = key[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return false;
            }

            return true;
        }

        private static int ParseHexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            return -1;
        }

        private readonly struct PathSegment
        {
            private PathSegment(bool isIndex, int index, string? propertyName)
            {
                IsIndex = isIndex;
                Index = index;
                PropertyName = propertyName;
            }

            public bool IsIndex { get; }

            public int Index { get; }

            public string? PropertyName { get; }

            public static PathSegment ForIndex(int index) => new PathSegment(true, index, null);

            public static PathSegment ForProperty(string propertyName) => new PathSegment(false, 0, propertyName);
        }

        private (int line, int column) GetLineColumn(int position)
        {
            int safePosition = Math.Max(0, Math.Min(position, _json.Length));

            int line = 1;
            int column = 1;

            for (int i = 0; i < safePosition; i++)
            {
                if (_json[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return (line, column);
        }
    }
}
