#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a static method as the factory used to create instances of the declaring type
    /// during BJson deserialization, instead of a constructor.
    /// <para>
    /// The marked method must satisfy all of the following:
    /// <list type="bullet">
    ///   <item><description>Be <c>static</c>.</description></item>
    ///   <item><description>Return the declaring type (or a subtype).</description></item>
    ///   <item><description>Have parameters whose names match JSON property names (case-insensitive,
    ///   respecting the active <c>NamingPolicy</c>).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This attribute takes priority over <see cref="BJsonConstructorAttribute"/> when both
    /// are present in the same type. Only one method per type may carry this attribute;
    /// the source generator or runtime will emit a diagnostic otherwise.
    /// </para>
    /// <para>
    /// Use <see cref="ParameterMapping"/> to explicitly map parameter names to JSON property names
    /// when the parameter names do not match after applying the NamingPolicy.
    /// The format is an alternating array of pairs: <c>["paramName", "jsonKey", ...]</c>.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// public sealed class Money
    /// {
    ///     private Money(decimal amount, string currency)
    ///     {
    ///         Amount = amount;
    ///         Currency = currency;
    ///     }
    ///
    ///     public decimal Amount { get; }
    ///     public string Currency { get; }
    ///
    ///     [BJsonFactoryMethod]
    ///     public static Money Create(decimal amount, string currency) => new(amount, currency);
    /// }
    ///
    /// // With explicit parameter mapping
    /// public sealed class Point
    /// {
    ///     [BJsonFactoryMethod(ParameterMapping = new[] { "x", "coord_x", "y", "coord_y" })]
    ///     public static Point FromCoords(double x, double y) => new() { X = x, Y = y };
    ///
    ///     public double X { get; init; }
    ///     public double Y { get; init; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BJsonFactoryMethodAttribute : Attribute
    {
        /// <summary>
        /// Optional explicit parameter-to-JSON-key mapping.
        /// Specify as alternating pairs: <c>["paramName", "jsonKey", "paramName2", "jsonKey2", ...]</c>.
        /// When <c>null</c> or empty, parameter names are matched against JSON keys using
        /// the active <c>NamingPolicy</c>.
        /// </summary>
        public string[]? ParameterMapping { get; set; }
    }
}
