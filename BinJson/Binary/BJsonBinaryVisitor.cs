#nullable enable

using System;

namespace Krampus.BinJson.Binary
{
    /// <summary>
    /// Visitor for binary payload traversal without materializing a DOM tree.
    /// </summary>
    /// <remarks>
    /// <para>Use this API when you need to inspect or stream-process a binary payload and want to avoid allocating <see cref="BJsonArray"/>, <see cref="BJsonObject"/>, and nested <see cref="BJsonValue"/> structures.</para>
    /// <para>The <paramref name="data"/> span passed to <see cref="OnBinary(ReadOnlySpan{byte})"/> is only valid for the duration of that callback.</para>
    /// </remarks>
    public abstract class BJsonBinaryVisitor
    {
        public virtual void OnDocumentStart() { }

        public virtual void OnDocumentEnd() { }

        public virtual void OnNull() { }

        public virtual void OnBoolean(bool value) { }

        public virtual void OnSignedInteger(long value) { }

        public virtual void OnUnsignedInteger(ulong value) { }

        public virtual void OnFloat(double value) { }

        public virtual void OnString(string value) { }

        public virtual void OnBinary(ReadOnlySpan<byte> data) { }

        public virtual void OnArrayStart(int count, bool isPacked) { }

        public virtual void OnArrayEnd(int count, bool isPacked) { }

        public virtual void OnObjectStart(int count) { }

        public virtual void OnObjectProperty(string propertyName, int index) { }

        public virtual void OnObjectEnd(int count) { }
    }
}