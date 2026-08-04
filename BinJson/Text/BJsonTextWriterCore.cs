#nullable enable

using System;
using System.Globalization;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    internal sealed class BJsonTextWriterCore
    {
        private const string HexDigits = "0123456789abcdef";

        private readonly TextWriter _writer;
        private readonly BJsonTextWriterOptions _options;
        private int _indentLevel;

        public BJsonTextWriterCore(TextWriter writer, BJsonTextWriterOptions options)
        {
            _writer = writer ?? throw new BJsonValidationException("Parameter 'writer' cannot be null.");
            _options = options ?? BJsonTextWriterOptions.Default;
            _indentLevel = 0;
        }

        public void Write(BJsonValue value)
        {
            WriteValue(value);
        }

        public void Flush()
        {
            _writer.Flush();
        }

        public static string SerializeToString(BJsonValue value, BJsonTextWriterOptions? options)
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            var core = new BJsonTextWriterCore(writer, options ?? BJsonTextWriterOptions.Default);
            core.Write(value);
            core.Flush();
            return writer.ToString();
        }

        private void WriteValue(BJsonValue value)
        {
            switch (value.Type)
            {
                case BJsonValueType.Null:
                    _writer.Write("null");
                    return;
                case BJsonValueType.Boolean:
                    _writer.Write(value.BoolValue ? "true" : "false");
                    return;
                case BJsonValueType.Integer:
                    _writer.Write(unchecked((long)value.ULongValue).ToString(CultureInfo.InvariantCulture));
                    return;
                case BJsonValueType.Float:
                    WriteFloat(value.DoubleValue);
                    return;
                case BJsonValueType.String:
                    WriteString(value.StringValue);
                    return;
                case BJsonValueType.Array:
                    WriteArray(value.ArrayValue);
                    return;
                case BJsonValueType.Object:
                    WriteObject(value.ObjectValue);
                    return;
                case BJsonValueType.Binary:
                    WriteBinary(value.BinaryValue);
                    return;
                default:
                    throw new BJsonSerializationException($"Unsupported BJsonValueType: {value.Type}");
            }
        }

        private void WriteFloat(double value)
        {
            if (!_options.SkipValidation && (double.IsNaN(value) || double.IsInfinity(value)))
                throw new BJsonSerializationException("JSON text cannot represent NaN or Infinity.");

            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0)
                text += ".0";

            _writer.Write(text);
        }

        private void WriteString(string value)
        {
            _writer.Write('"');
            ReadOnlySpan<char> source = value.AsSpan();
            int segmentStart = 0;

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];

                if (!NeedsEscaping(c))
                    continue;

                if (i > segmentStart)
                    _writer.Write(source.Slice(segmentStart, i - segmentStart));

                switch (c)
                {
                    case '"':
                        _writer.Write("\\\"");
                        break;
                    case '\\':
                        _writer.Write("\\\\");
                        break;
                    case '\b':
                        _writer.Write("\\b");
                        break;
                    case '\f':
                        _writer.Write("\\f");
                        break;
                    case '\n':
                        _writer.Write("\\n");
                        break;
                    case '\r':
                        _writer.Write("\\r");
                        break;
                    case '\t':
                        _writer.Write("\\t");
                        break;
                    default:
                        WriteHexEscape(c);
                        break;
                }

                segmentStart = i + 1;
            }

            if (segmentStart < source.Length)
                _writer.Write(source.Slice(segmentStart));

            _writer.Write('"');
        }

        private static bool NeedsEscaping(char c)
        {
            return c < ' ' || c == '"' || c == '\\';
        }

        private void WriteHexEscape(char c)
        {
            Span<char> escape = stackalloc char[6];
            escape[0] = '\\';
            escape[1] = 'u';
            escape[2] = '0';
            escape[3] = '0';
            escape[4] = HexDigits[(c >> 4) & 0x0F];
            escape[5] = HexDigits[c & 0x0F];
            _writer.Write(escape);
        }

        private void WriteArray(BJsonArray array)
        {
            _writer.Write('[');
            if (array.Count == 0)
            {
                _writer.Write(']');
                return;
            }

            if (_options.Indented)
            {
                _indentLevel++;
                for (int i = 0; i < array.Count; i++)
                {
                    _writer.WriteLine();
                    WriteIndent();
                    WriteValue(array[i]);
                    if (i < array.Count - 1)
                        _writer.Write(',');
                }
                _writer.WriteLine();
                _indentLevel--;
                WriteIndent();
            }
            else
            {
                for (int i = 0; i < array.Count; i++)
                {
                    if (i > 0)
                        _writer.Write(',');
                    WriteValue(array[i]);
                }
            }
            _writer.Write(']');
        }

        private void WriteObject(BJsonObject obj)
        {
            _writer.Write('{');
            if (obj.Count == 0)
            {
                _writer.Write('}');
                return;
            }

            if (_options.Indented)
            {
                _indentLevel++;
                int index = 0;
                foreach (var pair in obj)
                {
                    _writer.WriteLine();
                    WriteIndent();
                    WriteString(pair.Key);
                    _writer.Write(": ");
                    WriteValue(pair.Value);
                    if (index < obj.Count - 1)
                        _writer.Write(',');
                    index++;
                }
                _writer.WriteLine();
                _indentLevel--;
                WriteIndent();
            }
            else
            {
                bool first = true;
                foreach (var pair in obj)
                {
                    if (!first)
                        _writer.Write(',');

                    first = false;
                    WriteString(pair.Key);
                    _writer.Write(':');
                    WriteValue(pair.Value);
                }
            }
            _writer.Write('}');
        }

        private void WriteIndent()
        {
            int spaces = _indentLevel * _options.IndentSize;
            for (int i = 0; i < spaces; i++)
                _writer.Write(' ');
        }

        private void WriteBinary(BJsonBinary value)
        {
            if (!_options.AllowBinaryAsBase64)
                throw new BJsonSerializationException("Binary values are not allowed in JSON text output. Set BJsonTextWriterOptions.AllowBinaryAsBase64 to true to serialize as base64 strings.");

            WriteString(Convert.ToBase64String(value.AsSpan()));
        }
    }
}
