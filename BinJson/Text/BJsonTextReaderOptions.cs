#nullable enable

namespace Krampus.BinJson.Text
{
    /// <summary>
    /// Configuration options for BJsonTextReader parsing.
    /// </summary>
    public sealed class BJsonTextReaderOptions
    {
        /// <summary>
        /// Gets or sets the maximum depth of nested arrays/objects allowed during parsing.
        /// Default is 64. Set to 0 for unlimited depth (not recommended).
        /// </summary>
        public int MaxDepth { get; set; } = 64;

        /// <summary>
        /// Gets or sets whether JavaScript-style comments (// and /* */) are allowed in JSON input.
        /// Default is false (standard JSON, no comments).
        /// </summary>
        public bool AllowComments { get; set; }

        /// <summary>
        /// Gets or sets whether strict JSON parsing is enforced.
        /// When true, trailing commas and unquoted property names are rejected.
        /// Default is true.
        /// </summary>
        public bool StrictMode { get; set; } = true;

        /// <summary>
        /// Creates a new instance with default settings.
        /// </summary>
        public BJsonTextReaderOptions()
        {
        }

        /// <summary>
        /// Gets a default configuration instance (strict, no comments, depth 64).
        /// </summary>
        public static BJsonTextReaderOptions Default { get; } = new BJsonTextReaderOptions();
    }
}
