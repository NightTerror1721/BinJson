#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Marks a member as conditionally required at read time.
    /// <para>
    /// The referenced static method can decide whether the member is required based on the active version
    /// or on the member name itself.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonRequiredWhen(nameof(IsNameRequired))]
    /// public string? Name { get; set; }
    ///
    /// internal static bool IsNameRequired(string memberName, IComparable? version)
    ///     => version != null &amp;&amp; version.CompareTo(new Version("2.0.0")) &gt;= 0;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BJsonRequiredWhenAttribute : Attribute
    {
        public BJsonRequiredWhenAttribute(string methodName)
        {
            MethodName = methodName ?? throw new BJsonValidationException("Parameter 'methodName' cannot be null.");
        }

        /// <summary>
        /// Name of a static method on the declaring type. Supported signatures:
        /// <c>bool Method(string memberName, IComparable? version)</c>
        /// <c>bool Method(IComparable? version)</c>
        /// <c>bool Method()</c>
        /// </summary>
        public string MethodName { get; }
    }
}