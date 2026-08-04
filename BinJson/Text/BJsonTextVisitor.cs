#nullable enable

using System;

namespace Krampus.BinJson.Text
{
    /// <summary>
    /// Visitor for JSON text traversal without materializing a DOM tree.
    /// </summary>
    public abstract class BJsonTextVisitor
    {
        public virtual void OnDocumentStart() { }

        public virtual void OnDocumentEnd() { }

        public virtual void OnNull() { }

        public virtual void OnBoolean(bool value) { }

        public virtual void OnSignedInteger(long value) { }

        public virtual void OnUnsignedInteger(ulong value) { }

        public virtual void OnFloat(double value) { }

        public virtual void OnString(string value) { }

        public virtual void OnArrayStart() { }

        public virtual void OnArrayEnd() { }

        public virtual void OnObjectStart() { }

        public virtual void OnObjectProperty(string propertyName, int index) { }

        public virtual void OnObjectEnd() { }
    }
}
