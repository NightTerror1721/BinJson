#nullable enable

using System;
using System.Reflection;

namespace Krampus.BinJson.Serialization.Metadata
{
    internal sealed class MemberMetadata
    {
        public MemberMetadata(
            string memberName,
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
            MethodInfo? requiredWhenMethod = null,
            Func<object?, string, IComparable?, bool>? ignoreWhenPredicateDelegate = null,
            Func<BJsonValue, string, IComparable?, bool, BJsonValue>? valueMapperFullDelegate = null,
            Func<BJsonValue, BJsonValue>? valueMapperShortDelegate = null,
            Func<IComparable?, object?>? defaultProviderDelegate = null,
            Func<string, IComparable?, bool>? requiredWhenDelegate = null,
            IComparable? versionIntroducedIn = null,
            IComparable? versionRemovedIn = null,
            string? legacyJsonName = null,
            string[]? aliases = null,
            BJsonNumberHandling numberHandling = BJsonNumberHandling.Strict)
        {
            MemberName = memberName;
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
            RequiredWhenMethod = requiredWhenMethod;
            IgnoreWhenPredicateDelegate = ignoreWhenPredicateDelegate;
            ValueMapperFullDelegate = valueMapperFullDelegate;
            ValueMapperShortDelegate = valueMapperShortDelegate;
            DefaultProviderDelegate = defaultProviderDelegate;
            RequiredWhenDelegate = requiredWhenDelegate;
            VersionIntroducedIn = versionIntroducedIn;
            VersionRemovedIn = versionRemovedIn;
            LegacyJsonName = legacyJsonName;
            Aliases = aliases ?? Array.Empty<string>();
            NumberHandling = numberHandling;
        }

        public string MemberName { get; }

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

        public MethodInfo? RequiredWhenMethod { get; }

        public Func<object?, string, IComparable?, bool>? IgnoreWhenPredicateDelegate { get; }

        public Func<BJsonValue, string, IComparable?, bool, BJsonValue>? ValueMapperFullDelegate { get; }

        public Func<BJsonValue, BJsonValue>? ValueMapperShortDelegate { get; }

        public Func<IComparable?, object?>? DefaultProviderDelegate { get; }

        public Func<string, IComparable?, bool>? RequiredWhenDelegate { get; }

        public IComparable? VersionIntroducedIn { get; }

        public IComparable? VersionRemovedIn { get; }

        /// <summary>Legacy JSON key from [BJsonVersion(RenamedFrom="...")] for backwards-compatible deserialization.</summary>
        public string? LegacyJsonName { get; }

        /// <summary>Additional legacy JSON aliases from [BJsonAlias].</summary>
        public string[] Aliases { get; }

        public BJsonNumberHandling NumberHandling { get; }

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
