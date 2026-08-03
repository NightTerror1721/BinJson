namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryReaderOptions
    {
        public static BJsonBinaryReaderOptions Default { get; } = new BJsonBinaryReaderOptions();

        public BJsonInvalidStringRefPolicy InvalidStringRefPolicy { get; set; } = BJsonInvalidStringRefPolicy.Strict;
    }
}
