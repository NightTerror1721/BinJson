#nullable enable

namespace Krampus.BinJson.Serialization.References
{
    internal sealed class IgnoreReferenceHandler : ReferenceHandler
    {
        public override bool SupportsPreserveReferences => false;

        public override ReferenceResolver CreateResolver()
        {
            return new ReferenceResolver(preserveReferences: false);
        }
    }
}
