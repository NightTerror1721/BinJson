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

        [Fact]
        public void GetValueOrDefault_ReturnsFallback_WhenMissing()
        {
            var obj = new BJsonObject();
            obj.Add("x", 10);

            var fallback = BJsonValue.Create("missing");
            Assert.Equal(10, obj.GetIntOrDefault("x", -1));
            Assert.Equal(-1, obj.GetIntOrDefault("y", -1));
            Assert.Equal(fallback, obj.GetValueOrDefault("y", fallback));
        }

        [Fact]
        public void Remove_KeyValuePair_RequiresMatchingValue()
        {
            var obj = new BJsonObject();
            obj.Add("id", 1);

            Assert.False(obj.Remove(new System.Collections.Generic.KeyValuePair<string, BJsonValue>("id", BJsonValue.Create(2))));
            Assert.True(obj.ContainsKey("id"));
            Assert.True(obj.Remove(new System.Collections.Generic.KeyValuePair<string, BJsonValue>("id", BJsonValue.Create(1))));
            Assert.False(obj.ContainsKey("id"));
        }

        [Fact]
        public void TryAdd_And_AddOrUpdate_WorkAsExpected()
        {
            var obj = new BJsonObject();

            Assert.True(obj.TryAdd("id", 1));
            Assert.False(obj.TryAdd("id", 2));

            obj.AddOrUpdate("id", 3);
            Assert.Equal(3, obj["id"].IntValue);
        }

        [Fact]
        public void Merge_Update_RenameKey_WorkAsExpected()
        {
            var left = new BJsonObject { ["a"] = 1, ["b"] = 2 };
            var right = new BJsonObject { ["b"] = 9, ["c"] = 3 };

            left.Merge(right, overwrite: false);
            Assert.Equal(2, left["b"].IntValue);
            Assert.Equal(3, left["c"].IntValue);

            left.Merge(right, overwrite: true);
            Assert.Equal(9, left["b"].IntValue);

            Assert.True(left.Update("b", v => BJsonValue.Create(v.IntValue + 1)));
            Assert.Equal(10, left["b"].IntValue);
            Assert.False(left.Update("missing", v => v));

            Assert.True(left.RenameKey("c", "renamed"));
            Assert.False(left.ContainsKey("c"));
            Assert.True(left.ContainsKey("renamed"));
        }

        [Fact]
        public void CloneAndDeepClone_WorkAsExpected()
        {
            var nested = new BJsonObject { ["x"] = 7 };
            var obj = new BJsonObject { ["nested"] = nested };

            var clone = obj.Clone();
            var deep = obj.DeepClone();

            Assert.Equal(obj, clone);
            Assert.Equal(obj, deep);
            Assert.Same(obj["nested"].ObjectValue, clone["nested"].ObjectValue);
            Assert.NotSame(obj["nested"].ObjectValue, deep["nested"].ObjectValue);
        }

        [Fact]
        public void GetKeysByType_ReturnsFilteredKeys()
        {
            var obj = new BJsonObject
            {
                ["id"] = 1,
                ["name"] = "alice",
                ["active"] = true,
                ["score"] = 3.2
            };

            var keys = new System.Collections.Generic.List<string>(obj.GetKeysByType(BJsonValueType.Integer));

            Assert.Single(keys);
            Assert.Equal("id", keys[0]);
        }

        [Fact]
        public void Merge_WithStrategy_RespectsOverwriteAndKeepExisting()
        {
            var baseObject = new BJsonObject
            {
                ["id"] = 1,
                ["name"] = "base"
            };

            var incoming = new BJsonObject
            {
                ["name"] = "incoming",
                ["enabled"] = true
            };

            var keep = baseObject.Clone();
            keep.Merge(incoming, BJsonMergeStrategy.KeepExisting);
            Assert.Equal("base", keep["name"].StringValue);
            Assert.True(keep["enabled"].BoolValue);

            var overwrite = baseObject.Clone();
            overwrite.Merge(incoming, BJsonMergeStrategy.Overwrite);
            Assert.Equal("incoming", overwrite["name"].StringValue);
            Assert.True(overwrite["enabled"].BoolValue);
        }

        [Fact]
        public void Merge_DeepMerge_MergesNestedObjects()
        {
            var left = new BJsonObject
            {
                ["meta"] = new BJsonObject
                {
                    ["count"] = 1,
                    ["stable"] = true
                },
                ["name"] = "left"
            };

            var right = new BJsonObject
            {
                ["meta"] = new BJsonObject
                {
                    ["count"] = 5,
                    ["newField"] = "x"
                },
                ["name"] = "right"
            };

            left.Merge(right, BJsonMergeStrategy.DeepMerge);

            var meta = left["meta"].ObjectValue;
            Assert.Equal(5, meta["count"].IntValue);
            Assert.True(meta["stable"].BoolValue);
            Assert.Equal("x", meta["newField"].StringValue);
            Assert.Equal("right", left["name"].StringValue);
        }
    }
}
