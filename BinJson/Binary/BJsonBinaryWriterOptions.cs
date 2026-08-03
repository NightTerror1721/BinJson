namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryWriterOptions
    {
        public static BJsonBinaryWriterOptions Default { get; } = new BJsonBinaryWriterOptions();

        public bool EnableStringTable { get; set; } = true;

        public bool EnablePackedArrays { get; set; } = true;
    }
}
