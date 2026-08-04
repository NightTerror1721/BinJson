#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Krampus.BinJson.SourceGenerators.Utilities
{
    /// <summary>
    /// Registry for identifying type categories and determining serialization strategies
    /// </summary>
    internal static class TypeRegistry
    {
        // Primitive types that BJsonValue.Create supports directly
        private static readonly HashSet<string> PrimitiveTypes = new()
        {
            "System.Boolean",
            "System.Byte",
            "System.SByte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Single",
            "System.Double",
            "System.Decimal",
            "System.String",
            "System.Char",
            "bool",
            "byte",
            "sbyte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "float",
            "double",
            "decimal",
            "string",
            "char"
        };

        // Special BinJson types
        private static readonly HashSet<string> BinJsonTypes = new()
        {
            "Krampus.BinJson.BJsonValue",
            "Krampus.BinJson.BJsonObject",
            "Krampus.BinJson.BJsonArray",
            "Krampus.BinJson.BJsonNull",
            "Krampus.BinJson.BJsonBinary"
        };

        /// <summary>
        /// Check if a type is a primitive that can be directly converted to BJsonValue
        /// </summary>
        public static bool IsPrimitive(ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Boolean or
                SpecialType.System_Byte or
                SpecialType.System_SByte or
                SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal or
                SpecialType.System_String or
                SpecialType.System_Char => true,
                _ => PrimitiveTypes.Contains(type.ToDisplayString())
            };
        }

        /// <summary>
        /// Check if a type is already a BJsonValue or derived type
        /// </summary>
        public static bool IsBinJsonType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType)
            {
                string? ns = namedType.ContainingNamespace?.ToDisplayString();
                if (string.Equals(ns, "Krampus.BinJson", StringComparison.Ordinal))
                {
                    return namedType.Name is "BJsonValue" or "BJsonObject" or "BJsonArray" or "BJsonNull" or "BJsonBinary";
                }
            }

            return BinJsonTypes.Contains(type.ToDisplayString());
        }

        /// <summary>
        /// Check if a type is an enum
        /// </summary>
        public static bool IsEnum(ITypeSymbol type)
        {
            return type.TypeKind == TypeKind.Enum;
        }

        /// <summary>
        /// Check if a type is nullable (T? for value types or nullable reference type)
        /// </summary>
        public static bool IsNullable(ITypeSymbol type)
        {
            // Nullable value type: Nullable<T>
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return true;

            // Nullable reference type (requires nullable annotation context)
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
                return true;

            return false;
        }

        /// <summary>
        /// Get the underlying type of a nullable type
        /// </summary>
        public static ITypeSymbol? GetNullableUnderlyingType(ITypeSymbol type)
        {
            // Nullable<T> case
            if (type is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return namedType.TypeArguments.FirstOrDefault();
            }

            // For nullable reference types, return the type itself
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
                return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

            return null;
        }

        /// <summary>
        /// Check if type is a collection (array, List, IEnumerable, etc.)
        /// </summary>
        public static bool IsCollection(ITypeSymbol type, out ITypeSymbol? elementType)
        {
            elementType = null;

            // Array type: T[]
            if (type is IArrayTypeSymbol arrayType)
            {
                elementType = arrayType.ElementType;
                return true;
            }

            // Generic collection types
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var originalDefinition = namedType.OriginalDefinition.ToDisplayString();

                // List<T>, IList<T>, ICollection<T>, IEnumerable<T>, HashSet<T>, etc.
                if (originalDefinition.StartsWith("System.Collections.Generic.List<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.IList<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.ICollection<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.IEnumerable<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.HashSet<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.ISet<"))
                {
                    elementType = namedType.TypeArguments.FirstOrDefault();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if type is a dictionary
        /// </summary>
        public static bool IsDictionary(ITypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? valueType)
        {
            keyType = null;
            valueType = null;

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var originalDefinition = namedType.OriginalDefinition.ToDisplayString();

                // Dictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
                if (originalDefinition.StartsWith("System.Collections.Generic.Dictionary<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.IDictionary<") ||
                    originalDefinition.StartsWith("System.Collections.Generic.IReadOnlyDictionary<"))
                {
                    if (namedType.TypeArguments.Length >= 2)
                    {
                        keyType = namedType.TypeArguments[0];
                        valueType = namedType.TypeArguments[1];
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if dictionary key type is string (can serialize as BJsonObject)
        /// </summary>
        public static bool IsStringDictionary(ITypeSymbol keyType)
        {
            return keyType.SpecialType == SpecialType.System_String;
        }

        /// <summary>
        /// Check if type is an extension data dictionary (IDictionary&lt;string, BJsonValue&gt;)
        /// </summary>
        public static bool IsExtensionDataDictionary(ITypeSymbol type)
        {
            if (IsDictionary(type, out var keyType, out var valueType))
            {
                if (keyType != null && valueType != null)
                {
                    return IsStringDictionary(keyType) &&
                           valueType.ToDisplayString() == "Krampus.BinJson.BJsonValue";
                }
            }

            return false;
        }

        /// <summary>
        /// Get a simplified type name for code generation (without namespace for common types)
        /// </summary>
        public static string GetSimplifiedTypeName(ITypeSymbol type)
        {
            var fullName = type.ToDisplayString();

            // Map common types to C# keywords
            return fullName switch
            {
                "System.Boolean" => "bool",
                "System.Byte" => "byte",
                "System.SByte" => "sbyte",
                "System.Int16" => "short",
                "System.UInt16" => "ushort",
                "System.Int32" => "int",
                "System.UInt32" => "uint",
                "System.Int64" => "long",
                "System.UInt64" => "ulong",
                "System.Single" => "float",
                "System.Double" => "double",
                "System.Decimal" => "decimal",
                "System.String" => "string",
                "System.Char" => "char",
                "System.Object" => "object",
                _ => fullName
            };
        }

        /// <summary>
        /// Determine serialization strategy for a type
        /// </summary>
        public static SerializationStrategy GetStrategy(ITypeSymbol type)
        {
            if (IsPrimitive(type))
                return SerializationStrategy.Primitive;

            if (IsBinJsonType(type))
                return SerializationStrategy.BinJsonValue;

            if (IsEnum(type))
                return SerializationStrategy.Enum;

            if (IsNullable(type))
                return SerializationStrategy.Nullable;

            if (IsDictionary(type, out var keyType, out _))
            {
                if (keyType != null && IsStringDictionary(keyType))
                    return SerializationStrategy.StringDictionary;
                return SerializationStrategy.Dictionary;
            }

            if (IsCollection(type, out _))
                return SerializationStrategy.Collection;

            return SerializationStrategy.ComplexObject;
        }
    }

    /// <summary>
    /// Strategy for serializing a type
    /// </summary>
    internal enum SerializationStrategy
    {
        Primitive,          // Direct BJsonValue.Create(value)
        BinJsonValue,       // Already BJsonValue, use as-is
        Enum,               // ToString() or numeric
        Nullable,           // Check null, serialize underlying
        Collection,         // BJsonArray with element serialization
        StringDictionary,   // BJsonObject (key as property name)
        Dictionary,         // Array of key-value objects
        ComplexObject       // context.Serialize or generated converter
    }
}
