using Krampus.BinJson;
using System.Collections.Generic;
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

        [Fact]
        public void GetOrDefault_ReturnsFallback_WhenIndexMissing()
        {
            var array = new BJsonArray();
            array.Add(10);

            var fallback = BJsonValue.Create("missing");
            var actual = array.GetOrDefault(5, fallback);

            Assert.Equal(fallback, actual);
            Assert.Equal(10, array.GetIntOrDefault(0, -1));
            Assert.Equal(-1, array.GetIntOrDefault(9, -1));
        }

        [Fact]
        public void CapacityHelpers_AdjustCapacity()
        {
            var array = new BJsonArray(1);
            var ensured = array.EnsureCapacity(16);

            Assert.True(ensured >= 16);
            Assert.True(array.Capacity >= 16);

            array.TrimExcess();
            Assert.True(array.Capacity >= array.Count);
        }

        [Fact]
        public void FindHelpers_ReturnExpectedValues()
        {
            var array = new BJsonArray();
            array.Add(1);
            array.Add(2);
            array.Add(3);
            array.Add(4);

            Assert.Equal(1, array.FindIndex(v => v.IntValue == 2));
            Assert.Equal(3, array.FindLastIndex(v => v.IntValue % 2 == 0));

            Assert.True(array.Find(v => v.IntValue == 3, out var found));
            Assert.Equal(3, found.IntValue);

            var evens = array.FindAll(v => v.IntValue % 2 == 0);
            Assert.Equal(2, evens.Count);
            Assert.Equal(2, evens[0].IntValue);
            Assert.Equal(4, evens[1].IntValue);
        }

        [Fact]
        public void CloneAndDeepClone_WorkAsExpected()
        {
            var nested = new BJsonObject { ["x"] = 7 };
            var original = new BJsonArray { 1, nested };

            var clone = original.Clone();
            var deep = original.DeepClone();

            Assert.Equal(original, clone);
            Assert.Equal(original, deep);

            Assert.Same(original[1].ObjectValue, clone[1].ObjectValue);
            Assert.NotSame(original[1].ObjectValue, deep[1].ObjectValue);
        }

        [Fact]
        public void TryFirstTryLast_RespectEmptyAndNonEmpty()
        {
            var array = new BJsonArray();
            Assert.False(array.TryFirst(out _));
            Assert.False(array.TryLast(out _));

            array.Add(11);
            array.Add(22);

            Assert.True(array.TryFirst(out var first));
            Assert.True(array.TryLast(out var last));
            Assert.Equal(11, first.IntValue);
            Assert.Equal(22, last.IntValue);
        }

        [Fact]
        public void QueryHelpers_First_Last_Where_Select_WorkAsExpected()
        {
            var array = new BJsonArray { 1, 2, 3, 4, 5 };

            Assert.True(array.First(v => v.IntValue > 2, out var first));
            Assert.Equal(3, first.IntValue);

            Assert.True(array.Last(v => v.IntValue % 2 == 0, out var last));
            Assert.Equal(4, last.IntValue);

            var odds = new List<BJsonValue>(array.Where(v => (v.IntValue % 2) != 0));
            Assert.Equal(3, odds.Count);
            Assert.Equal(1, odds[0].IntValue);
            Assert.Equal(3, odds[1].IntValue);
            Assert.Equal(5, odds[2].IntValue);

            var projected = new List<int>(array.Select(v => v.IntValue * 10));
            Assert.Equal(new[] { 10, 20, 30, 40, 50 }, projected);
        }
    }
}
