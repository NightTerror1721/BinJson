using Krampus.BinJson;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonObjectTests
    {
        [Fact]
        public void NewObject_IsEmpty()
        {
            var obj = new BJsonObject();

            Assert.Empty(obj);
            Assert.False(obj.IsReadOnly);
        }

        [Fact]
        public void Add_PrimitiveValues_StoresByKey()
        {
            var obj = new BJsonObject();

            obj.Add("id", 42);
            obj.Add("name", "alice");
            obj.Add("active", true);
            obj.AddNull("missing");

            Assert.Equal(4, obj.Count);
            Assert.True(obj.TryGetInt("id", out var id));
            Assert.Equal(42, id);
            Assert.True(obj.TryGetString("name", out var name));
            Assert.Equal("alice", name);
            Assert.True(obj.TryGetBool("active", out var active));
            Assert.True(active);
            Assert.True(obj.TryGetValue("missing", out var missing));
            Assert.True(missing.IsNull);
        }

        [Fact]
        public void TryGetValue_MissingKey_ReturnsFalse()
        {
            var obj = new BJsonObject();

            var result = obj.TryGetValue("unknown", out var value);

            Assert.False(result);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void TryGetNumberAsDouble_UsesContainedValueConversionRules()
        {
            var obj = new BJsonObject();
            obj.Add("count", 5);
            obj.Add("ratio", 2.5);

            Assert.True(obj.TryGetNumberAsDouble("count", out var count));
            Assert.Equal(5.0, count);
            Assert.True(obj.TryGetNumberAsInt("ratio", out var ratio));
            Assert.Equal(2, ratio);
        }

        [Fact]
        public void Equality_IsStructuralAndKeyOrderIndependent()
        {
            var a = new BJsonObject();
            a.Add("id", 1);
            a.Add("name", "alice");

            var b = new BJsonObject();
            b.Add("name", "alice");
            b.Add("id", 1L);

            var c = new BJsonObject();
            c.Add("id", 2);
            c.Add("name", "alice");

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.False(a.Equals(c));
        }

        [Fact]
        public void Remove_ExistingKey_UpdatesCount()
        {
            var obj = new BJsonObject();
            obj.Add("id", 1);
            obj.Add("name", "alice");

            var removed = obj.Remove("id");

            Assert.True(removed);
            Assert.Single(obj);
            Assert.False(obj.ContainsKey("id"));
        }

        [Fact]
        public void Equality_EmptyObjects_Match()
        {
            var a = new BJsonObject();
            var b = new BJsonObject();

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
    }
}
