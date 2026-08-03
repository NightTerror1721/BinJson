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
        private int _position;

        private JsonTextParser(string json, bool allowComments)
        {
            _json = json;
            _allowComments = allowComments;
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
                throw new BJsonParseException($"Unexpected character at position {parser._position}: expected end of JSON.");

            return value;
        }

        private BJsonValue ParseValue()
        {
            SkipWhitespace();

            if (_position >= _json.Length)
                throw new BJsonParseException("Unexpected end of JSON.");

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

            throw new BJsonParseException($"Unexpected character at position {_position}: '{c}'");
        }

        private BJsonValue ParseNull()
        {
            if (!TryConsume("null"))
                throw new BJsonParseException($"Invalid null literal at position {_position}.");
            return BJsonValue.Null;
        }

        private BJsonValue ParseBoolean()
        {
            if (TryConsume("true"))
                return BJsonValue.True;
            if (TryConsume("false"))
                return BJsonValue.False;

            throw new BJsonParseException($"Invalid boolean literal at position {_position}.");
        }

        private BJsonValue ParseString()
        {
            if (_json[_position] != '"')
                throw new BJsonParseException($"Expected '\"' at position {_position}.");

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
                        throw new BJsonParseException("Unexpected end of JSON in string escape.");

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
                            throw new BJsonParseException($"Invalid escape sequence '\\{escaped}' at position {_position - 1}.");
                    }
                }
                else if (c < ' ')
                {
                    throw new BJsonParseException($"Unescaped control character (U+{((int)c):X4}) at position {_position}.");
                }
                else
                {
                    sb.Append(c);
                    _position++;
                }
            }

            throw new BJsonParseException("Unterminated string.");
        }

        private char ParseUnicodeEscape()
        {
            if (_position + 4 > _json.Length)
                throw new BJsonParseException("Incomplete Unicode escape sequence.");

            string hex = _json.Substring(_position, 4);
            _position += 4;

            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort codePoint))
                throw new BJsonParseException($"Invalid Unicode escape sequence '\\u{hex}'.");

            return (char)codePoint;
        }

        private BJsonValue ParseNumber()
        {
            int start = _position;

            if (_json[_position] == '-')
                _position++;

            if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                throw new BJsonParseException($"Invalid number at position {start}.");

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
                    throw new BJsonParseException($"Invalid number: expected digit after '.' at position {_position}.");

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
                    throw new BJsonParseException($"Invalid number: expected digit in exponent at position {_position}.");

                while (_position < _json.Length && char.IsDigit(_json[_position]))
                    _position++;
            }

            string numberText = _json.Substring(start, _position - start);

            if (isFloat)
            {
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw new BJsonParseException($"Invalid float number '{numberText}' at position {start}.");
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

                throw new BJsonParseException($"Number '{numberText}' is out of range at position {start}.");
            }
        }

        private BJsonValue ParseArray()
        {
            if (_json[_position] != '[')
                throw new BJsonParseException($"Expected '[' at position {_position}.");

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
                array.Add(ParseValue());
                SkipWhitespace();

                if (_position >= _json.Length)
                    throw new BJsonParseException("Unexpected end of JSON in array.");

                if (_json[_position] == ']')
                {
                    _position++;
                    return BJsonValue.Create(array);
                }

                if (_json[_position] != ',')
                    throw new BJsonParseException($"Expected ',' or ']' at position {_position}.");

                _position++;
                SkipWhitespace();
            }
        }

        private BJsonValue ParseObject()
        {
            if (_json[_position] != '{')
                throw new BJsonParseException($"Expected '{{' at position {_position}.");

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
                    throw new BJsonParseException($"Expected property name (string) at position {_position}.");

                string key = ParseString().StringValue;

                SkipWhitespace();

                if (_position >= _json.Length || _json[_position] != ':')
                    throw new BJsonParseException($"Expected ':' after property name at position {_position}.");

                _position++;

                BJsonValue value = ParseValue();

                if (obj.ContainsKey(key))
                    throw new BJsonParseException($"Duplicate key '{key}' in object at position {_position}.");

                obj.Add(key, value);

                SkipWhitespace();

                if (_position >= _json.Length)
                    throw new BJsonParseException("Unexpected end of JSON in object.");

                if (_json[_position] == '}')
                {
                    _position++;
                    return BJsonValue.Create(obj);
                }

                if (_json[_position] != ',')
                    throw new BJsonParseException($"Expected ',' or '}}' at position {_position}.");

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

                        throw new BJsonParseException("Unterminated block comment.");
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
    }
}
