#nullable enable

using System;
using System.Reflection;

namespace Krampus.BinJson.Serialization.Metadata
{
    internal sealed class MemberMetadata
    {
        public MemberMetadata(
            string jsonName,
            Type memberType,
            Func<object, object?> getter,
            Action<object, object?> setter,
            IBJsonConverter? converter,
            int order,
            bool required,
            BJsonIgnoreCondition ignoreCondition,
            bool isExtensionData,
            MethodInfo? ignoreWhenPredicate = null,
            MethodInfo? valueMapperFullSignature = null,
            MethodInfo? valueMapperShortSignature = null,
            bool hasDefaultConstant = false,
            object? defaultConstantValue = null,
            MethodInfo? defaultProviderMethod = null,
            IComparable? versionIntroducedIn = null,
            IComparable? versionRemovedIn = null,
            string? legacyJsonName = null)
        {
            JsonName = jsonName;
            MemberType = memberType;
            Getter = getter;
            Setter = setter;
            Converter = converter;
            Order = order;
            Required = required;
            IgnoreCondition = ignoreCondition;
            IsExtensionData = isExtensionData;
            IgnoreWhenPredicate = ignoreWhenPredicate;
            ValueMapperFullSignature = valueMapperFullSignature;
            ValueMapperShortSignature = valueMapperShortSignature;
            HasDefaultConstant = hasDefaultConstant;
            DefaultConstantValue = defaultConstantValue;
            DefaultProviderMethod = defaultProviderMethod;
            VersionIntroducedIn = versionIntroducedIn;
            VersionRemovedIn = versionRemovedIn;
            LegacyJsonName = legacyJsonName;
        }

        public string JsonName { get; }

        public Type MemberType { get; }

        public Func<object, object?> Getter { get; }

        public Action<object, object?> Setter { get; }

        public IBJsonConverter? Converter { get; }

        public int Order { get; }

        public bool Required { get; }

        public BJsonIgnoreCondition IgnoreCondition { get; }

        public bool IsExtensionData { get; }

        /// <summary>Static predicate method (object? value, string name, IComparable? version) → bool</summary>
        public MethodInfo? IgnoreWhenPredicate { get; }

        /// <summary>Value mapper with full signature (BJsonValue value, string name, IComparable? version, bool isReading) → BJsonValue</summary>
        public MethodInfo? ValueMapperFullSignature { get; }

        /// <summary>Value mapper with short signature (BJsonValue value) → BJsonValue</summary>
        public MethodInfo? ValueMapperShortSignature { get; }

        public bool HasDefaultConstant { get; }

        public object? DefaultConstantValue { get; }

        public MethodInfo? DefaultProviderMethod { get; }

        public IComparable? VersionIntroducedIn { get; }

        public IComparable? VersionRemovedIn { get; }

        /// <summary>Legacy JSON key from [BJsonVersion(RenamedFrom="...")] for backwards-compatible deserialization.</summary>
        public string? LegacyJsonName { get; }

        public bool HasVersionRange => VersionIntroducedIn != null || VersionRemovedIn != null;

        /// <summary>Returns true if the member is in range for the given version (null version = always in range).</summary>
        public bool IsInVersionRange(IComparable? version)
        {
            if (version == null || !HasVersionRange)
                return true;

            if (VersionIntroducedIn != null && version.CompareTo(VersionIntroducedIn) < 0)
                return false;

            if (VersionRemovedIn != null && version.CompareTo(VersionRemovedIn) >= 0)
                return false;

            return true;
        }
    }
}
