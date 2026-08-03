using Krampus.BinJson;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonValueTests
    {
        #region Null Tests

        [Fact]
        public void Null_IsNull_ReturnsTrue()
        {
            var value = BJsonValue.Null;
            Assert.True(value.IsNull);
            Assert.Equal(BJsonValueType.Null, value.Type);
        }

        [Fact]
        public void CreateNull_IsNull()
        {
            var value = BJsonValue.CreateNull();
            Assert.True(value.IsNull);
        }

        [Fact]
        public void Null_EqualsNull()
        {
            var a = BJsonValue.Null;
            var b = BJsonValue.CreateNull();
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        #endregion

        #region Boolean Tests

        [Fact]
        public void BoolTrue_IsBoolean()
        {
            var value = BJsonValue.True;
            Assert.True(value.IsBoolean);
            Assert.True(value.BoolValue);
            Assert.Equal(BJsonValueType.Boolean, value.Type);
        }

        [Fact]
        public void BoolFalse_IsBoolean()
        {
            var value = BJsonValue.False;
            Assert.True(value.IsBoolean);
            Assert.False(value.BoolValue);
            Assert.Equal(BJsonValueType.Boolean, value.Type);
        }

        [Fact]
        public void Bool_Equality()
        {
            Assert.Equal(BJsonValue.True, BJsonValue.Create(true));
            Assert.Equal(BJsonValue.False, BJsonValue.Create(false));
            Assert.NotEqual(BJsonValue.True, BJsonValue.False);
        }

        [Fact]
        public void Bool_HashCode()
        {
            Assert.Equal(BJsonValue.True.GetHashCode(), BJsonValue.Create(true).GetHashCode());
            Assert.Equal(BJsonValue.False.GetHashCode(), BJsonValue.Create(false).GetHashCode());
        }

        #endregion

        #region Integer Tests

        [Fact]
        public void Int32_Creation()
        {
            var value = BJsonValue.Create(42);
            Assert.True(value.IsInteger);
            Assert.Equal(42, value.IntValue);
            Assert.Equal(BJsonValueType.Integer, value.Type);
        }

        [Fact]
        public void Int32_MinMax()
        {
            var min = BJsonValue.Create(int.MinValue);
            var max = BJsonValue.Create(int.MaxValue);

            Assert.Equal(int.MinValue, min.IntValue);
            Assert.Equal(int.MaxValue, max.IntValue);
        }

        [Fact]
        public void Int64_MinMax()
        {
            var min = BJsonValue.Create(long.MinValue);
            var max = BJsonValue.Create(long.MaxValue);

            Assert.Equal(long.MinValue, min.LongValue);
            Assert.Equal(long.MaxValue, max.LongValue);
        }

        [Fact]
        public void UInt64_Max()
        {
            var max = BJsonValue.Create(ulong.MaxValue);
            Assert.Equal(ulong.MaxValue, max.ULongValue);
        }

        [Fact]
        public void Integer_TryGet_WithRangeChecking()
        {
            var largeValue = BJsonValue.Create(1000L);

            // Should succeed for int
            Assert.True(largeValue.TryGetInt(out int intVal));
            Assert.Equal(1000, intVal);

            // Should succeed for short
            Assert.True(largeValue.TryGetShort(out short shortVal));
            Assert.Equal(1000, shortVal);

            // Should fail for byte (max 255)
            Assert.False(largeValue.TryGetByte(out _));
        }

        [Fact]
        public void Integer_TryGet_OutOfRange_ReturnsFalse()
        {
            var tooLarge = BJsonValue.Create(300L); // Too large for byte (max 255)
            Assert.False(tooLarge.TryGetByte(out _));

            var tooLargeForShort = BJsonValue.Create(100000L);
            Assert.False(tooLargeForShort.TryGetShort(out _));
        }

        [Fact]
        public void Integer_Equality()
        {
            var a = BJsonValue.Create(42);
            var b = BJsonValue.Create(42L);
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        #endregion

        #region Float Tests

        [Fact]
        public void Float_Creation()
        {
            var value = BJsonValue.Create(3.14);
            Assert.True(value.IsFloat);
            Assert.Equal(3.14, value.DoubleValue);
            Assert.Equal(BJsonValueType.Float, value.Type);
        }

        [Fact]
        public void Float_NaN()
        {
            var value = BJsonValue.Create(double.NaN);
            Assert.True(value.IsFloat);
            Assert.True(double.IsNaN(value.DoubleValue));
        }

        [Fact]
        public void Float_PositiveInfinity()
        {
            var value = BJsonValue.Create(double.PositiveInfinity);
            Assert.True(value.IsFloat);
            Assert.True(double.IsPositiveInfinity(value.DoubleValue));
        }

        [Fact]
        public void Float_NegativeInfinity()
        {
            var value = BJsonValue.Create(double.NegativeInfinity);
            Assert.True(value.IsFloat);
            Assert.True(double.IsNegativeInfinity(value.DoubleValue));
        }

        [Fact]
        public void Float_NegativeZero()
        {
            var value = BJsonValue.Create(-0.0);
            Assert.True(value.IsFloat);
            Assert.Equal(-0.0, value.DoubleValue);
        }

        [Fact]
        public void Float_Equality()
        {
            var a = BJsonValue.Create(3.14);
            var b = BJsonValue.Create(3.14);
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        #endregion

        #region Numeric Cross-Type Tests

        [Fact]
        public void Numeric_IntegerAndFloat_Equality()
        {
            var intOne = BJsonValue.Create(1);
            var floatOne = BJsonValue.Create(1.0);

            Assert.Equal(intOne, floatOne);
            Assert.Equal(intOne.GetHashCode(), floatOne.GetHashCode());
        }

        [Fact]
        public void Numeric_IntegerAndFloat_TryGetNumberAs()
        {
            var intValue = BJsonValue.Create(42);
            Assert.True(intValue.TryGetNumberAsDouble(out double d));
            Assert.Equal(42.0, d);

            var floatValue = BJsonValue.Create(42.5);
            Assert.True(floatValue.TryGetNumberAsInt(out int i));
            Assert.Equal(42, i);
        }

        #endregion

        #region String Tests

        [Fact]
        public void String_Creation()
        {
            var value = BJsonValue.Create("hello");
            Assert.True(value.IsString);
            Assert.Equal("hello", value.StringValue);
            Assert.Equal(BJsonValueType.String, value.Type);
        }

        [Fact]
        public void String_Empty()
        {
            var value = BJsonValue.Create("");
            Assert.True(value.IsString);
            Assert.Equal("", value.StringValue);
        }

        [Fact]
        public void String_Null_ReturnsNullValue()
        {
            var value = BJsonValue.Create((string?)null);
            Assert.True(value.IsNull);
            Assert.False(value.IsString);
        }

        [Fact]
        public void String_UTF8_WithAccents()
        {
            var value = BJsonValue.Create("Héllo Wörld");
            Assert.Equal("Héllo Wörld", value.StringValue);
        }

        [Fact]
        public void String_UTF8_WithEmoji()
        {
            var value = BJsonValue.Create("Hello 👋 World 🌍");
            Assert.Equal("Hello 👋 World 🌍", value.StringValue);
        }

        [Fact]
        public void String_Equality()
        {
            var a = BJsonValue.Create("test");
            var b = BJsonValue.Create("test");
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        #endregion

        #region Comparison Tests

        [Fact]
        public void CompareTo_SameType_Numeric()
        {
            var a = BJsonValue.Create(1);
            var b = BJsonValue.Create(2);
            var c = BJsonValue.Create(1);

            Assert.True(a.CompareTo(b) < 0);
            Assert.True(b.CompareTo(a) > 0);
            Assert.Equal(0, a.CompareTo(c));
        }

        [Fact]
        public void CompareTo_SameType_String()
        {
            var a = BJsonValue.Create("apple");
            var b = BJsonValue.Create("banana");
            var c = BJsonValue.Create("apple");

            Assert.True(a.CompareTo(b) < 0);
            Assert.True(b.CompareTo(a) > 0);
            Assert.Equal(0, a.CompareTo(c));
        }

        [Fact]
        public void CompareTo_DifferentTypes_ByTypeOrder()
        {
            var nullVal = BJsonValue.Null;
            var boolVal = BJsonValue.True;
            var strVal = BJsonValue.Create("test");

            // Null < Boolean < String based on enum values
            Assert.True(nullVal.CompareTo(boolVal) < 0);
            Assert.True(boolVal.CompareTo(strVal) < 0);
            Assert.True(nullVal.CompareTo(strVal) < 0);
        }

        [Fact]
        public void CompareTo_Numeric_CrossType()
        {
            // Integer and Float compare as numbers, not by type
            var intSmall = BJsonValue.Create(1);
            var floatLarge = BJsonValue.Create(100.5);
            var intLarge = BJsonValue.Create(200);
            var floatSmall = BJsonValue.Create(0.5);

            Assert.True(intSmall.CompareTo(floatLarge) < 0);  // 1 < 100.5
            Assert.True(floatSmall.CompareTo(intSmall) < 0);  // 0.5 < 1
            Assert.True(floatLarge.CompareTo(intLarge) < 0);  // 100.5 < 200
        }

        [Fact]
        public void RelationalOperators_UseCompareToSemantics()
        {
            BJsonValue a = 1;
            BJsonValue b = 2.0;

            Assert.True(a < b);
            Assert.True(a <= b);
            Assert.True(b > a);
            Assert.True(b >= a);
        }

        #endregion

        #region Conversion Operator Tests

        [Fact]
        public void ImplicitOperator_Primitives_CreatesExpectedTypes()
        {
            BJsonValue intValue = 42;
            BJsonValue strValue = "hello";
            BJsonValue boolValue = true;

            Assert.True(intValue.IsInteger);
            Assert.Equal(42, intValue.IntValue);
            Assert.True(strValue.IsString);
            Assert.Equal("hello", strValue.StringValue);
            Assert.True(boolValue.IsBoolean);
            Assert.True(boolValue.BoolValue);
        }

        [Fact]
        public void ExplicitOperator_ExtractsTypedValues()
        {
            BJsonValue value = 123;
            int intValue = (int)value;

            Assert.Equal(123, intValue);
            Assert.Throws<Krampus.BinJson.Error.BJsonValidationException>(() =>
            {
                var _ = (string)value;
            });
        }

        #endregion

        #region Coercion Tests

        [Fact]
        public void AsInt_AsLong_AsDouble_WorkForNumericValues()
        {
            var fromInt = BJsonValue.Create(42);
            var fromDouble = BJsonValue.Create(42.8);

            Assert.Equal(42, fromInt.AsInt());
            Assert.Equal(42L, fromInt.AsLong());
            Assert.Equal(42.0, fromInt.AsDouble());
            Assert.Equal(42, fromDouble.AsInt());
        }

        [Fact]
        public void AsInt_ThrowsForInvalidNumeric()
        {
            var nan = BJsonValue.Create(double.NaN);
            Assert.Throws<Krampus.BinJson.Error.BJsonValidationException>(() => nan.AsInt());
        }

        #endregion

        #region Clone And ToString Tests

        [Fact]
        public void DeepClone_NestedObject_IsIndependent()
        {
            var nested = new BJsonObject { ["x"] = 5 };
            var obj = new BJsonObject { ["nested"] = nested };
            var value = BJsonValue.Create(obj);

            var clone = value.DeepClone();

            Assert.True(clone.IsObject);
            Assert.NotSame(value.ObjectValue, clone.ObjectValue);
            Assert.True(clone.ObjectValue.TryGetObject("nested", out var nestedClone));
            Assert.NotSame(nested, nestedClone);
            Assert.Equal(5, nestedClone["x"].IntValue);
        }

        [Fact]
        public void ToString_UsesReadableFormatting()
        {
            Assert.Equal("null", BJsonValue.Null.ToString());
            Assert.Equal("true", BJsonValue.True.ToString());
            Assert.Equal("5", BJsonValue.Create(5).ToString());
            Assert.Equal("\"a\\n\"", BJsonValue.Create("a\n").ToString());
        }

        #endregion

        #region Type Checking Tests

        [Fact]
        public void IsNumber_ReturnsTrue_ForIntegerAndFloat()
        {
            Assert.True(BJsonValue.Create(42).IsNumber);
            Assert.True(BJsonValue.Create(3.14).IsNumber);
            Assert.False(BJsonValue.Create("42").IsNumber);
            Assert.False(BJsonValue.Null.IsNumber);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Zero_IntegerVsFloat()
        {
            var intZero = BJsonValue.Create(0);
            var floatZero = BJsonValue.Create(0.0);

            Assert.Equal(intZero, floatZero);
            Assert.Equal(intZero.GetHashCode(), floatZero.GetHashCode());
        }

        [Fact]
        public void NegativeZero_Float()
        {
            var negZero = BJsonValue.Create(-0.0);
            var posZero = BJsonValue.Create(0.0);

            // IEEE 754 distinguishes -0.0 and 0.0 in binary representation
            // Our implementation uses bitwise comparison for same-type floats
            // so they are NOT equal (this preserves exact representation)
            Assert.NotEqual(negZero, posZero);

            // But values are preserved correctly
            Assert.Equal(-0.0, negZero.DoubleValue);
            Assert.Equal(0.0, posZero.DoubleValue);
        }

        #endregion
    }
}
