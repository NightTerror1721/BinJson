#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Identifies the constructor BinJson should use to materialize a type during deserialization.
    /// <para>
    /// Parameter names are matched against serialized member names, respecting explicit property-name overrides
    /// and the active naming policy.
    /// </para>
    /// <para>
    /// When a valid <see cref="BJsonFactoryMethodAttribute"/> is also present, the factory method takes precedence.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable]
    /// public sealed class Coordinate
    /// {
    ///     [BJsonConstructor]
    ///     public Coordinate(double x, double y)
    ///     {
    ///         X = x;
    ///         Y = y;
    ///     }
    ///
    ///     public double X { get; }
    ///     public double Y { get; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class BJsonConstructorAttribute : Attribute
    {
    }
}