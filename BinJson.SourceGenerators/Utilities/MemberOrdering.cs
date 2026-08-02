#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Krampus.BinJson.SourceGenerators.Models;

namespace Krampus.BinJson.SourceGenerators.Utilities
{
    /// <summary>
    /// Utilities for ordering members for deterministic code generation
    /// </summary>
    internal static class MemberOrdering
    {
        /// <summary>
        /// Sort members by Order attribute (ascending), then by name (alphabetically) for determinism
        /// </summary>
        /// <param name="members">Members to sort</param>
        /// <returns>Sorted list of members</returns>
        public static List<MemberModel> SortMembers(IEnumerable<MemberModel> members)
        {
            return members
                .OrderBy(m => m.Order) // Primary: by Order attribute
                .ThenBy(m => m.MemberName, StringComparer.Ordinal) // Secondary: alphabetically
                .ToList();
        }

        /// <summary>
        /// Sort members for serialization (respecting IgnoreCondition)
        /// </summary>
        /// <param name="members">Members to sort</param>
        /// <returns>Sorted list of serializable members (excluding Always ignored)</returns>
        public static List<MemberModel> GetSerializableMembers(IEnumerable<MemberModel> members)
        {
            return members
                .Where(m => m.IgnoreCondition != IgnoreCondition.Always) // Exclude always-ignored
                .Where(m => CanSerialize(m)) // Must be serializable
                .OrderBy(m => m.Order)
                .ThenBy(m => m.MemberName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Sort members for deserialization
        /// </summary>
        /// <param name="members">Members to sort</param>
        /// <returns>Sorted list of deserializable members (excluding Always ignored)</returns>
        public static List<MemberModel> GetDeserializableMembers(IEnumerable<MemberModel> members)
        {
            return members
                .Where(m => m.IgnoreCondition != IgnoreCondition.Always) // Exclude always-ignored
                .Where(m => CanDeserialize(m)) // Must be deserializable
                .OrderBy(m => m.Order)
                .ThenBy(m => m.MemberName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Check if a member can be serialized
        /// </summary>
        private static bool CanSerialize(MemberModel member)
        {
            return member.Kind switch
            {
                MemberKind.Property => ((PropertyModel)member).CanSerialize,
                MemberKind.Field => ((FieldModel)member).CanSerialize,
                _ => false
            };
        }

        /// <summary>
        /// Check if a member can be deserialized
        /// </summary>
        private static bool CanDeserialize(MemberModel member)
        {
            return member.Kind switch
            {
                MemberKind.Property => ((PropertyModel)member).CanDeserialize,
                MemberKind.Field => ((FieldModel)member).CanDeserialize,
                _ => false
            };
        }

        /// <summary>
        /// Sort constructor parameters by position (natural order)
        /// </summary>
        public static List<ConstructorParameterModel> SortConstructorParameters(IEnumerable<ConstructorParameterModel> parameters)
        {
            // Constructor parameters are already in order, just return as list
            return parameters.ToList();
        }
    }
}
