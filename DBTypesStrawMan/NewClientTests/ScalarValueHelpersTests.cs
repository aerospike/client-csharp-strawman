using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewClient.Tests
{
	[TestClass()]
	public class ScalarValueHelpersTests
	{
		[TestMethod()]
		public void ToAerospikeValueTest()
		{
			var intValue = 123.ToAerospikeValue();
			var strValue = "abc".ToAerospikeValue();
			var strNumValue = "123".ToAerospikeValue();
			var dblValue = 123.456D.ToAerospikeValue();
			var lngValue = 123L.ToAerospikeValue();
			var fltValue = 123.456F.ToAerospikeValue();
			var decValue = 123.456M.ToAerospikeValue();
			var decValue2 = 123.456M.ToAerospikeValue();

			Assert.AreEqual(123, intValue.Value);
			Assert.AreEqual("abc", strValue.Value);
			Assert.AreEqual("123", strNumValue.Value);
			Assert.AreEqual(123.456D, dblValue.Value);
			Assert.AreEqual(123L, lngValue.Value);
			Assert.AreEqual(123.456F, fltValue.Value);
			Assert.AreEqual(123.456M, decValue.Value);
			Assert.AreEqual(123.456M, decValue2.Value);

			Assert.AreEqual(123, (int) intValue);

			Assert.IsTrue(dblValue.TryConvert(out int intCValue));
			Assert.AreEqual(123, intCValue);
			Assert.IsFalse(strNumValue.TryConvert(out int nstrCValue));
			Assert.AreEqual(0, nstrCValue);
			Assert.IsTrue(strValue.TryConvert(out string? strCValue));
			Assert.AreEqual("abc", strCValue);

			Assert.IsTrue(decValue.Equals(123.456F));
			Assert.IsTrue((fltValue == 123.456F));
			Assert.IsTrue(decValue.Equals(123.456M));
			Assert.IsTrue(decValue.Equals(123.456D));
			Assert.IsTrue(decValue.Equals(fltValue));
			Assert.IsTrue(decValue.Equals(decValue));
			Assert.IsTrue(decValue.Equals(dblValue));
			Assert.IsTrue(decValue.Equals(dblValue.Value));

			Assert.IsTrue((decValue == decValue2));
			Assert.IsFalse(decValue.Equals("abc"));

			Assert.IsFalse(strValue.Equals(intValue));
			Assert.IsFalse(strValue.Equals(123));
			Assert.IsFalse(strNumValue.Equals(123));
			Assert.IsFalse(fltValue.Equals(intValue));

			Assert.AreEqual(123, ((IValue) intValue).ToValue<int>());

			Assert.IsTrue(intValue.TryConvert(out long lngCValue));
			Assert.AreEqual(123L, lngCValue);

			Assert.IsTrue(intValue.TryConvert(out ScalarValue<long> lngSCValue));
			Assert.AreEqual(123L, lngSCValue.Value);

			Assert.IsTrue(intValue.TryConvert(out IValue<long>? lngICValue));
			Assert.IsNotNull(lngICValue);
			Assert.AreEqual(123L, lngICValue.Value);

			Assert.IsTrue(intValue.TryConvert(typeof(long), out object? lngOCValue));
			Assert.IsNotNull(lngOCValue);
			Assert.IsInstanceOfType<long>(lngOCValue);
			Assert.AreEqual(123L, (long) lngOCValue);
			Assert.IsTrue(intValue.Equals(lngOCValue));
			Assert.IsFalse(strNumValue.Equals(lngOCValue));

			Assert.AreEqual(0, intValue.CompareTo(lngOCValue));
			Assert.AreEqual(0, intValue.CompareTo(lngValue));
			Assert.AreEqual(0, intValue.CompareTo(123));
			Assert.AreEqual(1, intValue.CompareTo(0));
			Assert.AreEqual(-1, intValue.CompareTo(567));

		}

		[TestMethod()]
		public void ExplicitTest()
		{
			var intValue = (ScalarValue<int>) 123;
			var strValue = (ScalarValue<string>) "abc";
			var strNumValue = (ScalarValue<string>) "123";
			var dblValue = (ScalarValue<double>) 123.456D;
			var lngValue = (ScalarValue<long>) 123L;
			var lngValue2 = (ScalarValue<long>) 456L;
			var fltValue = (ScalarValue<float>) 123.456F;
			var decValue = (ScalarValue<decimal>) 123.456M;
			var decValue2 = (ScalarValue<decimal>) 123.456M;

			Assert.AreEqual(123, intValue.Value);
			Assert.AreEqual("abc", strValue.Value);
			Assert.AreEqual("123", strNumValue.Value);
			Assert.AreEqual(123.456D, dblValue.Value);
			Assert.AreEqual(123L, lngValue.Value);
			Assert.AreEqual(123.456F, fltValue.Value);
			Assert.AreEqual(123.456M, decValue.Value);
			Assert.AreEqual(123.456M, decValue2.Value);

			Assert.AreNotEqual(lngValue, lngValue2);
			
			Assert.AreEqual(123, (int) intValue);

			Assert.IsTrue(dblValue.TryConvert(out int intCValue));
			Assert.AreEqual(123, intCValue);
			Assert.IsFalse(strNumValue.TryConvert(out int nstrCValue));
			Assert.AreEqual(0, nstrCValue);
			Assert.IsTrue(strValue.TryConvert(out string? strCValue));
			Assert.AreEqual("abc", strCValue);

			Assert.IsTrue(decValue.Equals(123.456F));
			Assert.IsTrue((fltValue == 123.456F));
			Assert.IsTrue(decValue.Equals(123.456M));
			Assert.IsTrue(decValue.Equals(123.456D));
			Assert.IsTrue(decValue.Equals(fltValue));
			Assert.IsTrue(decValue.Equals(decValue));
			Assert.IsTrue(decValue.Equals(dblValue));
			Assert.IsTrue(decValue.Equals(dblValue.Value));

			Assert.IsTrue((decValue == decValue2));
			Assert.IsFalse(decValue.Equals("abc"));

			Assert.IsFalse(strValue.Equals(intValue));
			Assert.IsFalse(strValue.Equals(123));
			Assert.IsFalse(strNumValue.Equals(123));
			Assert.IsFalse(fltValue.Equals(intValue));

			Assert.AreEqual(123, ((IValue) intValue).ToValue<int>());

			Assert.IsTrue(intValue.TryConvert(out long lngCValue));
			Assert.AreEqual(123L, lngCValue);

			Assert.IsTrue(intValue.TryConvert(out ScalarValue<long> lngSCValue));
			Assert.AreEqual(123L, lngSCValue.Value);

			Assert.IsTrue(intValue.TryConvert(out IValue<long>? lngICValue));
			Assert.IsNotNull(lngICValue);
			Assert.AreEqual(123L, lngICValue.Value);

			Assert.IsTrue(intValue.TryConvert(typeof(long), out object? lngOCValue));
			Assert.IsNotNull(lngOCValue);
			Assert.IsInstanceOfType<long>(lngOCValue);
			Assert.AreEqual(123L, (long) lngOCValue);
			Assert.IsTrue(intValue.Equals(lngOCValue));
			Assert.IsFalse(strNumValue.Equals(lngOCValue));

			Assert.AreEqual(0, intValue.CompareTo(lngOCValue));
			Assert.AreEqual(0, intValue.CompareTo(lngValue));
			Assert.AreEqual(0, intValue.CompareTo(123));
			Assert.AreEqual(1, intValue.CompareTo(0));
			Assert.AreEqual(-1, intValue.CompareTo(567));

		}

		[TestMethod]
		public void CompareTest()
		{
			var int1Value = 1.ToAerospikeValue();
			var int2Value = 2.ToAerospikeValue();
			
			var strAValue = "a".ToAerospikeValue();
			var strBValue = "b".ToAerospikeValue();

			var dblValue = 123.456D.ToAerospikeValue();
			var lngValue = 123L.ToAerospikeValue();
			var fltValue = 123.456F.ToAerospikeValue();
			
			Assert.AreEqual(0, int1Value.CompareTo(int1Value));
			Assert.AreEqual(0, int1Value.CompareTo(1));
			Assert.AreEqual(0, int1Value.CompareTo(1L));

			Assert.AreEqual(-1, int1Value.CompareTo(int2Value));
			Assert.AreEqual(-1, int1Value.CompareTo(2));
			Assert.AreEqual(-1, int1Value.CompareTo(2L));

			Assert.AreEqual(-1, int1Value.CompareTo(dblValue));
			Assert.AreEqual(-1, int1Value.CompareTo(lngValue));
			Assert.AreEqual(-1, int1Value.CompareTo(fltValue));

			Assert.AreEqual(1, int2Value.CompareTo(int1Value));
			Assert.AreEqual(1, int1Value.CompareTo(0));
			Assert.AreEqual(1, int1Value.CompareTo(0L));

			Assert.AreEqual(-1, int1Value.CompareTo(strAValue));
			Assert.AreEqual(-1, int1Value.CompareTo("a"));

			Assert.AreEqual(0, strAValue.CompareTo(strAValue));
			Assert.AreEqual(0, strAValue.CompareTo("a"));
			
			Assert.AreEqual(-1, strAValue.CompareTo(strBValue));
			Assert.AreEqual(-1, strAValue.CompareTo("b"));
			
			Assert.AreEqual(1, strBValue.CompareTo(strAValue));
			Assert.AreEqual(1, strBValue.CompareTo("A"));

			Assert.AreEqual(1, strAValue.CompareTo(int1Value));
			Assert.AreEqual(1, strAValue.CompareTo(1));
		}
	}
}