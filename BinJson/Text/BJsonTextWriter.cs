#nullable enable

using System;
using System.Globalization;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public sealed class BJsonTextWriter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly bool _leaveOpen;
        private readonly BJsonTextWriterOptions _options;
        private int _indentLevel;

        public BJsonTextWriter(TextWriter writer, bool leaveOpen = false)
            : this(writer, BJsonTextWriterOptions.Default, leaveOpen)
        {
        }

        public BJsonTextWriter(TextWriter writer, BJsonTextWriterOptions? options, bool leaveOpen = false)
        {
            _writer = writer ?? throw new BJsonValidationException("Parameter 'writer' cannot be null.");
            _options = options ?? BJsonTextWriterOptions.Default;
            _leaveOpen = leaveOpen;
            _indentLevel = 0;
        }

        public void Write(BJsonValue value)
        {
            try
            {
                WriteValue(value);
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonSerializationException("Failed to serialize BinJson value to JSON text.", ex);
            }
        }

        public void Flush()
        {
            try
            {
                _writer.Flush();
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonSerializationException("Failed to flush JSON text writer.", ex);
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _writer.Dispose();
        }

        public static string Serialize(BJsonValue value, BJsonTextWriterOptions? options = null)
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            using var jsonWriter = new BJsonTextWriter(writer, options, leaveOpen: true);
            jsonWriter.Write(value);
            jsonWriter.Flush();
            return writer.ToString();
        }

        public static void Serialize(TextWriter writer, BJsonValue value, BJsonTextWriterOptions? options = null, bool leaveOpen = false)
        {
            using var jsonWriter = new BJsonTextWriter(writer, options, leaveOpen);
            jsonWriter.Write(value);
            jsonWriter.Flush();
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
            if (text.IndexOfAny(new[] { '.', 'e', 'E' }) < 0)
                text += ".0";

            _writer.Write(text);
        }

        private void WriteString(string value)
        {
            _writer.Write('"');
            foreach (char c in value)
            {
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
                        if (c < ' ')
                        {
                            _writer.Write("\\u");
                            _writer.Write(((int)c).ToString("x4"));
                        }
                        else
                        {
                            _writer.Write(c);
                        }
                        break;
                }
            }
            _writer.Write('"');
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
