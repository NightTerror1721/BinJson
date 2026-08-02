#nullable enable

namespace Krampus.BinJson.Serialization.References
{
    internal sealed class PreserveReferenceHandler : ReferenceHandler
    {
        public override bool SupportsPreserveReferences => true;

        public override ReferenceResolver CreateResolver()
        {
            return new ReferenceResolver(preserveReferences: true);
        }
    }
}
