#nullable enable

namespace Krampus.BinJson.Text
{
    /// <summary>
    /// Configuration options for BJsonTextWriter serialization.
    /// </summary>
    public sealed class BJsonTextWriterOptions
    {
        /// <summary>
        /// Gets or sets whether the output should be indented (pretty-printed).
        /// Default is false (compact output).
        /// </summary>
        public bool Indented { get; set; }

        /// <summary>
        /// Gets or sets the number of spaces per indentation level when Indented is true.
        /// Default is 2. Ignored if Indented is false.
        /// </summary>
        public int IndentSize { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether binary values should be allowed in JSON text output as base64 strings.
        /// Default is false (binary values will throw InvalidOperationException).
        /// </summary>
        public bool AllowBinaryAsBase64 { get; set; }

        /// <summary>
        /// Gets or sets whether to skip validation of numeric values (NaN/Infinity).
        /// Default is false (NaN and Infinity will throw InvalidOperationException).
        /// Use with caution: enabling this may produce invalid JSON.
        /// </summary>
        public bool SkipValidation { get; set; }

        /// <summary>
        /// Creates a new instance with default settings.
        /// </summary>
        public BJsonTextWriterOptions()
        {
        }

        /// <summary>
        /// Gets a default configuration instance (compact, no binary, strict validation).
        /// </summary>
        public static BJsonTextWriterOptions Default { get; } = new BJsonTextWriterOptions();

        /// <summary>
        /// Gets a pretty-print configuration instance with default indent size.
        /// </summary>
        public static BJsonTextWriterOptions PrettyPrint { get; } = new BJsonTextWriterOptions { Indented = true };
    }
}
