#nullable enable

using System.Reflection;

namespace Krampus.BinJson.Serialization.Metadata
{
    internal sealed class ConstructorMetadata
    {
        public ConstructorMetadata(ConstructorInfo constructor, bool isPreferred)
        {
            Constructor = constructor;
            IsPreferred = isPreferred;
            Parameters = constructor.GetParameters();
        }

        public ConstructorInfo Constructor { get; }

        public ParameterInfo[] Parameters { get; }

        public bool IsPreferred { get; }
    }
}
