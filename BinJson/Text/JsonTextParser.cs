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
            parser.SkipWhitespace();

            if (parser._position < parser._json.Length)
                throw parser.CreateParseException(
                    $"Unexpected character at position {parser._position}: expected end of JSON.",
                    parser._position,
                    errorCode: BJsonErrorCode.ParseUnexpectedTrailingChar,
                    details: new Dictionary<string, object?>
                    {
                        ["expected"] = "end of JSON",
                        ["found"] = parser._json[parser._position].ToString()
                    });

            return value;
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
            if (_json[_position] != '"')
                throw CreateParseException($"Expected '\"' at position {_position}.", _position, BJsonErrorCode.ParseExpectedStringStart);

            _position++;
            var sb = new StringBuilder();

            while (_position < _json.Length)
            {
                char c = _json[_position];

                if (c == '"')
                {
                    _position++;
                    return BJsonValue.Create(sb.ToString());
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

            string hex = _json.Substring(_position, 4);
            _position += 4;

            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort codePoint))
                throw CreateParseException($"Invalid Unicode escape sequence '\\u{hex}'.", _position - 4, BJsonErrorCode.ParseInvalidUnicodeEscape);

            return (char)codePoint;
        }

        private BJsonValue ParseNumber()
        {
            int start = _position;

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

            bool isFloat = false;

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

            string numberText = _json.Substring(start, _position - start);

            if (isFloat)
            {
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw CreateParseException($"Invalid float number '{numberText}' at position {start}.", start, BJsonErrorCode.ParseInvalidFloat);
                return BJsonValue.Create(d);
            }
            else
            {
                if (long.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return BJsonValue.Create(l);

                if (ulong.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ul))
                    return BJsonValue.Create(ul);

                if (double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return BJsonValue.Create(d);

                throw CreateParseException($"Number '{numberText}' is out of range at position {start}.", start, BJsonErrorCode.ParseNumberOutOfRange);
            }
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

                string key = ParseString().StringValue;

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

                if (obj.ContainsKey(key))
                    throw CreateParseException(
                        $"Duplicate key '{key}' in object at position {_position}.",
                        _position,
                        BJsonErrorCode.ParseDuplicateKey,
                        new Dictionary<string, object?> { ["key"] = key });

                obj.Add(key, value);

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
