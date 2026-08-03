using Krampus.BinJson;
using Krampus.BinJson.Text;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonFacadeTests
    {
        [Fact]
        public void TryParse_ReturnsTrue_ForValidJson()
        {
            var ok = BJson.TryParse("{\"x\":1}", out var value);

            Assert.True(ok);
            Assert.True(value.IsObject);
            Assert.Equal(1, value.ObjectValue["x"].IntValue);
        }

        [Fact]
        public void TryParse_ReturnsFalse_ForInvalidJson()
        {
            var ok = BJson.TryParse("{\"x\":", out var value);

            Assert.False(ok);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void TryParse_WithOptions_AllowsComments()
        {
            var ok = BJson.TryParse("{/*c*/\"x\":2}", new BJsonTextReaderOptions { AllowComments = true }, out var value);

            Assert.True(ok);
            Assert.True(value.IsObject);
            Assert.Equal(2, value.ObjectValue["x"].IntValue);
        }

        [Fact]
        public void TryDeserialize_ReturnsFalse_ForInvalidPayload()
        {
            var ok = BJson.TryDeserialize(new byte[] { 0x7F }, out var value);

            Assert.False(ok);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void Transform_AppliesRecursively()
        {
            var obj = new BJsonObject
            {
                ["a"] = 1,
                ["nested"] = new BJsonArray { 2, 3 }
            };

            var transformed = BJson.Transform(BJsonValue.Create(obj), v =>
            {
                if (v.IsInteger)
                    return BJsonValue.Create(v.IntValue + 10);
                return v;
            });

            Assert.Equal(11, transformed.ObjectValue["a"].IntValue);
            Assert.Equal(12, transformed.ObjectValue["nested"].ArrayValue[0].IntValue);
            Assert.Equal(13, transformed.ObjectValue["nested"].ArrayValue[1].IntValue);
        }
    }
}
