#nullable enable

using Krampus.BinJson.Serialization;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonSpecializedFacadesTests
    {
        [Fact]
        public void BinaryFacade_RoundTripsDomValue()
        {
            var input = BJsonValue.Create(new BJsonObject
            {
                ["name"] = BJsonValue.Create("hero"),
                ["level"] = BJsonValue.Create(5)
            });

            var bytes = BJsonBinaryFacade.SerializeToBytes(input);
            var roundTrip = BJsonBinaryFacade.Deserialize(bytes);

            Assert.True(roundTrip.TryGetObject(out var obj));
            Assert.Equal("hero", obj["name"].StringValue);
            Assert.Equal(5, obj["level"].IntValue);
        }

        [Fact]
        public void TextFacade_RoundTripsDomValue()
        {
            var input = BJsonValue.Create(new BJsonObject
            {
                ["active"] = BJsonValue.True,
                ["count"] = BJsonValue.Create(2)
            });

            var json = BJsonTextFacade.Stringify(input);
            var roundTrip = BJsonTextFacade.Parse(json);

            Assert.True(roundTrip.TryGetObject(out var obj));
            Assert.True(obj["active"].BoolValue);
            Assert.Equal(2, obj["count"].IntValue);
        }

        [Fact]
        public void TypedFacade_SerializesAndDeserializesClrObject()
        {
            var model = new FacadeTypedModel { Name = "mage", Level = 12 };

            var value = BJsonTypedFacade.Serialize(model);
            var roundTrip = BJsonTypedFacade.Deserialize<FacadeTypedModel>(value);

            Assert.NotNull(roundTrip);
            Assert.Equal("mage", roundTrip!.Name);
            Assert.Equal(12, roundTrip.Level);
        }

        [Fact]
        public void DomFacade_TransformsTree()
        {
            var input = BJsonValue.Create(new BJsonObject
            {
                ["hp"] = BJsonValue.Create(10),
                ["values"] = BJsonValue.Create(new BJsonArray { 1, 2 })
            });

            var transformed = BJsonDomFacade.Transform(input, v =>
            {
                if (v.IsInteger)
                    return BJsonValue.Create(v.IntValue + 1);
                return v;
            });

            Assert.True(transformed.TryGetObject(out var obj));
            Assert.Equal(11, obj["hp"].IntValue);
            var arr = obj["values"].ArrayValue;
            Assert.Equal(2, arr.Count);
            Assert.Equal(2, arr[0].IntValue);
            Assert.Equal(3, arr[1].IntValue);
        }

        [BJsonSerializable]
        private sealed class FacadeTypedModel
        {
            public string Name { get; set; } = string.Empty;

            public int Level { get; set; }
        }
    }
}
