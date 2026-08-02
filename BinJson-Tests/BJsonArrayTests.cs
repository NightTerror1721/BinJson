using Krampus.BinJson;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonArrayTests
    {
        [Fact]
        public void NewArray_IsEmpty()
        {
            var array = new BJsonArray();

            Assert.Empty(array);
            Assert.False(array.IsReadOnly);
        }

        [Fact]
        public void Add_MultiplePrimitiveValues_PreservesOrderAndTypes()
        {
            var array = new BJsonArray();

            array.Add(42);
            array.Add("hello");
            array.Add(true);
            array.AddNull();

            Assert.Equal(4, array.Count);
            Assert.True(array[0].IsInteger);
            Assert.Equal(42, array[0].IntValue);
            Assert.True(array[1].IsString);
            Assert.Equal("hello", array[1].StringValue);
            Assert.True(array[2].IsBoolean);
            Assert.True(array[2].BoolValue);
            Assert.True(array[3].IsNull);
        }

        [Fact]
        public void TryGetValue_InvalidIndex_ReturnsFalseAndNull()
        {
            var array = new BJsonArray();
            array.Add(1);

            var result = array.TryGetValue(5, out var value);

            Assert.False(result);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void TryGetInt_UsesContainedValueConversionRules()
        {
            var array = new BJsonArray();
            array.Add(123L);
            array.Add(1000L);

            Assert.True(array.TryGetInt(0, out var intValue));
            Assert.Equal(123, intValue);
            Assert.False(array.TryGetByte(1, out _));
        }

        [Fact]
        public void AddRange_AppendsValuesInSequence()
        {
            var array = new BJsonArray();

            array.AddRange(1, 2, 3);
            array.AddRange(new[] { "a", "b" });

            Assert.Equal(5, array.Count);
            Assert.Equal(1, array[0].IntValue);
            Assert.Equal(2, array[1].IntValue);
            Assert.Equal(3, array[2].IntValue);
            Assert.Equal("a", array[3].StringValue);
            Assert.Equal("b", array[4].StringValue);
        }

        [Fact]
        public void Equality_IsStructuralAndOrderSensitive()
        {
            var a = new BJsonArray();
            a.Add(1);
            a.Add("two");

            var b = new BJsonArray();
            b.Add(1L);
            b.Add("two");

            var c = new BJsonArray();
            c.Add("two");
            c.Add(1);

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.NotEqual(a, c);
        }

        [Fact]
        public void Equality_EmptyArrays_Match()
        {
            var a = new BJsonArray();
            var b = new BJsonArray();

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
    }
}
