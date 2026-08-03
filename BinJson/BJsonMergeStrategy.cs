#nullable enable

namespace Krampus.BinJson
{
    /// <summary>
    /// Defines how values are combined when merging one <see cref="BJsonObject"/> into another.
    /// </summary>
    public enum BJsonMergeStrategy
    {
        /// <summary>
        /// Incoming values replace existing keys.
        /// </summary>
        Overwrite = 0,

        /// <summary>
        /// Existing values are preserved; only missing keys are added.
        /// </summary>
        KeepExisting = 1,

        /// <summary>
        /// Objects are merged recursively; non-object values are overwritten.
        /// </summary>
        DeepMerge = 2,
    }
}
