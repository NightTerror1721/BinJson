#nullable enable

using System;
using System.Collections.Generic;
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
            MethodInfo? factoryMethod = null,
            IReadOnlyDictionary<string, string>? factoryParameterMapping = null,
            IComparable? versionIntroducedIn = null,
            IComparable? versionRemovedIn = null)
        {
            Type = type;
            Members = members;
            Constructor = constructor;
            ExtensionDataMember = extensionDataMember;
            VersionContext = versionContext;
            FactoryMethod = factoryMethod;
            FactoryParameterMapping = factoryParameterMapping;
            VersionIntroducedIn = versionIntroducedIn;
            VersionRemovedIn = versionRemovedIn;
        }

        public Type Type { get; }

        public MemberMetadata[] Members { get; }

        public ConstructorMetadata? Constructor { get; }

        public MemberMetadata? ExtensionDataMember { get; }

        /// <summary>Version declared by [BJsonVersionContext] on the type. Can be overridden by BJsonSerializerOptions.Version.</summary>
        public IComparable? VersionContext { get; }

        /// <summary>Static factory method decorated with [BJsonFactoryMethod], if any.</summary>
        public MethodInfo? FactoryMethod { get; }

        /// <summary>Optional explicit parameter-to-JSON-key mapping from [BJsonFactoryMethod(ParameterMapping=...)]</summary>
        public IReadOnlyDictionary<string, string>? FactoryParameterMapping { get; }

        /// <summary>Lower bound from type-level [BJsonVersion]. Null means always active.</summary>
        public IComparable? VersionIntroducedIn { get; }

        /// <summary>Exclusive upper bound from type-level [BJsonVersion]. Null means no upper bound.</summary>
        public IComparable? VersionRemovedIn { get; }

        public bool HasTypeVersionRange => VersionIntroducedIn != null || VersionRemovedIn != null;
    }
}
