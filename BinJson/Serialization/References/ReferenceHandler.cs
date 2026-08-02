#nullable enable

namespace Krampus.BinJson.Serialization.References
{
    public abstract class ReferenceHandler
    {
        public static ReferenceHandler Ignore { get; } = new IgnoreReferenceHandler();

        public static ReferenceHandler Preserve { get; } = new PreserveReferenceHandler();

        public abstract bool SupportsPreserveReferences { get; }

        public abstract ReferenceResolver CreateResolver();
    }
}
