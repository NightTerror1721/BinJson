#nullable enable

using System;
using System.Collections.Generic;
using Krampus.BinJson.Serialization.References;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonSerializationContext
    {
        private readonly BJsonObjectSerializer _serializer;
        private readonly Stack<object> _objectStack;

        internal BJsonSerializationContext(BJsonObjectSerializer serializer, BJsonSerializerOptions options)
        {
            _serializer = serializer;
            Options = options;
            _objectStack = new Stack<object>();
            ReferenceResolver = options.ReferenceHandler?.CreateResolver();
        }

        public BJsonSerializerOptions Options { get; }

        public ReferenceResolver? ReferenceResolver { get; }

        public int CurrentDepth => _objectStack.Count;

        public BJsonValue Serialize(object? value)
        {
            return _serializer.SerializeValue(value, value?.GetType() ?? typeof(object));
        }

        public BJsonValue Serialize(object? value, Type type)
        {
            return _serializer.SerializeValue(value, type ?? throw new ArgumentNullException(nameof(type)));
        }

        public T? Deserialize<T>(BJsonValue value)
        {
            return (T?)_serializer.DeserializeValue(value, typeof(T));
        }

        public object? Deserialize(BJsonValue value, Type type)
        {
            return _serializer.DeserializeValue(value, type ?? throw new ArgumentNullException(nameof(type)));
        }

        internal BJsonValue SerializeAttributed(object value, Type type)
        {
            return _serializer.SerializeAttributedObject(value, type);
        }

        internal object? DeserializeAttributed(BJsonValue value, Type type)
        {
            return _serializer.DeserializeAttributedObject(value, type);
        }

        public void PushObject(object obj)
        {
            if (obj == null)
                return;

            if (_objectStack.Count >= Options.MaxDepth)
                throw new InvalidOperationException($"Maximum serialization depth of {Options.MaxDepth} exceeded.");

            _objectStack.Push(obj);
        }

        public void PopObject()
        {
            if (_objectStack.Count > 0)
                _objectStack.Pop();
        }
    }
}
