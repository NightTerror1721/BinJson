#nullable enable

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Contract for custom BJson DOM pre-processors.
    /// A pre-processor transforms the raw BJson DOM node before typed deserialization occurs,
    /// enabling features such as conditional blocks (<c>$if/$then/$elif/$else</c>),
    /// anchor resolution (<c>$ref</c>), variable substitution, and external includes.
    /// </summary>
    /// <seealso cref="BJsonPreprocessorAttribute"/>
    public interface IBJsonPreprocessor
    {
        /// <summary>
        /// Transforms the raw BJson DOM node before it is deserialized into a typed object.
        /// </summary>
        /// <param name="node">
        /// The root DOM node of the document or sub-document. The concrete type depends on
        /// the BJson DOM implementation (e.g. <c>BJsonValue</c>, <c>BJsonObject</c>).
        /// </param>
        /// <param name="context">
        /// Contextual data available during pre-processing, including variable storage and
        /// environment queries.
        /// </param>
        /// <returns>
        /// The transformed DOM node. May be the same instance as <paramref name="node"/>
        /// (no transformation needed) or a new node.
        /// </returns>
        object Process(object node, IBJsonPreprocessorContext context);
    }

    /// <summary>
    /// Provides contextual data and services to an <see cref="IBJsonPreprocessor"/> during
    /// DOM pre-processing.
    /// </summary>
    public interface IBJsonPreprocessorContext
    {
        /// <summary>
        /// Retrieves the value of a named variable set during the current pre-processing session.
        /// Returns <c>null</c> if the variable has not been set.
        /// </summary>
        string? GetVariable(string name);

        /// <summary>
        /// Sets or updates a named variable in the current pre-processing session.
        /// Variables set here are available to subsequent calls within the same session.
        /// </summary>
        void SetVariable(string name, string value);
    }
}
