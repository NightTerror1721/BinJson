#nullable enable

using System;
using System.Reflection;

namespace Krampus.BinJson.Serialization.Metadata
{
    internal sealed class TypeMetadata
    {
        public TypeMetadata(
            Type type,
            MemberMetadata[] members,
            ConstructorMetadata? constructor,
            MemberMetadata? extensionDataMember,
            IComparable? versionContext = null,
            MethodInfo? factoryMethod = null)
        {
            Type = type;
            Members = members;
            Constructor = constructor;
            ExtensionDataMember = extensionDataMember;
            VersionContext = versionContext;
            FactoryMethod = factoryMethod;
        }

        public Type Type { get; }

        public MemberMetadata[] Members { get; }

        public ConstructorMetadata? Constructor { get; }

        public MemberMetadata? ExtensionDataMember { get; }

        /// <summary>Version declared by [BJsonVersionContext] on the type. Can be overridden by BJsonSerializerOptions.Version.</summary>
        public IComparable? VersionContext { get; }

        /// <summary>Static factory method decorated with [BJsonFactoryMethod], if any.</summary>
        public MethodInfo? FactoryMethod { get; }
    }
}
