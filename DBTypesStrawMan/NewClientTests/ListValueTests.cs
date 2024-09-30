using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewClient.Tests
{
	[TestClass()]
	public class ListValueTests
	{
		static private void AreEqualItems<L>(ICollection expected, ICollection<L> actual)
		{
			int i = 0;
			foreach (var item in expected)
			{
				if(item is ICDTValue aItem)
				{
					Assert.IsNotNull(actual.ElementAt(i));
					if(actual.ElementAt(i) is ICDTValue aaItem)
					{
						CollectionAssert.AreEqual(aItem.ToEnumerable<object>().ToList(), aaItem.ToEnumerable<object>().ToList());
					}
					else
					{
						Assert.IsInstanceOfType<ICollection>(actual.ElementAt(i));
						CollectionAssert.AreEqual(aItem.ToEnumerable<object>().ToList(), (ICollection) actual.ElementAt(i));
					}
				}
				else if(item is ICollection cItem)
				{
					Assert.IsNotNull(actual.ElementAt(i));

					if(actual.ElementAt(i) is ICDTValue aaItem)
					{
						CollectionAssert.AreEqual(cItem, aaItem.ToEnumerable<object>().ToList());
					}
					else
					{
						Assert.IsInstanceOfType<ICollection>(actual.ElementAt(i));
						CollectionAssert.AreEqual(cItem, (ICollection) actual.ElementAt(i));
					}					
				}
				else if(item is IValue vItem)
				{
					Assert.IsNotNull(actual.ElementAt(i));
					if(actual.ElementAt(i) is IValue avItem)
					{
						Assert.AreEqual(vItem.Object, avItem.Object);
					}
					else
					{
						Assert.AreEqual(vItem.Object, actual.ElementAt(i));
					}
				}
				else
				{
					if(actual.ElementAt(i) is IValue avItem)
					{
						Assert.AreEqual((object) item, avItem.Object);
					}
					else
					{
						Assert.AreEqual((object) item, actual.ElementAt(i));
					}					
				}
				i++;
			}
		}

		[TestMethod()]
		public void ToAerospikeValueTest()
		{
			{
				var tstList = new List<object>() { 0, 1, 2, "abc", new List<int>() { 1, 2, 3 }, 3, "dfg" };

				var valuelst = tstList.ToAerospikeValue();

				Assert.IsNotNull(valuelst);
				Assert.IsTrue(valuelst.IsList);
				Assert.IsTrue(valuelst.TryConvert(out IList<object>? orgLst));

				Assert.IsNotNull(orgLst);
				AreEqualItems(tstList, orgLst);

				Assert.IsFalse(valuelst.TryConvert(out IList<int>? failLst));
				Assert.IsNull(failLst);			
			}
			{
				var tstList = new List<object>() {new List<int>() { 1, 2, 3 }};

				var valuelst = tstList.ToAerospikeValue();

				Assert.IsNotNull(valuelst);
				Assert.IsTrue(valuelst.IsList);
				Assert.IsTrue(valuelst.TryConvert(out IList<object>? orgLst));

				Assert.IsNotNull(orgLst);
				AreEqualItems(tstList, orgLst);

				Assert.IsTrue(valuelst.TryConvert(out IList<List<int>>? intLst));
				Assert.IsNotNull(intLst);
				AreEqualItems((ICollection) intLst[0], (List<int>)tstList[0]);
			}

			{
				var tstList = new List<object>() { 0, 1, 2, "abc", new List<int>() { 1, 2, 3 }, 3, "dfg" };
				var intList = new List<int>() { 0, 1, 2, 3 };

				var valuelst = tstList.ToAerospikeList();

				Assert.IsNotNull(valuelst);
				Assert.IsTrue(valuelst.IsList);
				Assert.IsTrue(valuelst.TryConvert(out IList<object>? orgLst));

				Assert.IsNotNull(orgLst);
				AreEqualItems(tstList, orgLst);

				Assert.IsFalse(valuelst.TryConvert(out IList<int>? failLst));
				Assert.IsNull(failLst);

				var oftypeLst = valuelst.OfType<int>().ToList();

				Assert.IsNotNull(oftypeLst);
				AreEqualItems(intList, oftypeLst);

				var oftypeMLst = valuelst.OfType<decimal>().ToList();

				Assert.IsNotNull(oftypeMLst);
				
				var oftypeLLst = valuelst.OfType<List<int>>();

				Assert.IsNotNull(oftypeLLst);
				AreEqualItems((ICollection) tstList[4], oftypeLLst.First());

			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException),
		"Invalid Cast was expected")]
		public void ListInvalidCast()
		{
			var tstList = new List<object>() { 0, 1, 2, "abc", new List<int>() { 1, 2, 3 }, 3, "dfg" };
			
			var valuelst = tstList.ToAerospikeList();

			Assert.IsNotNull(valuelst);
			Assert.IsTrue(valuelst.IsList);
			Assert.IsTrue(valuelst.TryConvert(out IList<object>? orgLst));

			CollectionAssert.Equals(tstList, orgLst);

			Assert.IsFalse(valuelst.TryConvert(out IList<int>? failLst));
			Assert.IsNull(failLst);

			valuelst.Cast<int>().ToList();
		}

		[TestMethod]
		public void ComparerTest()
		{
			var tstList = new List<object>() { 123M, 0, 2, 2, 1, "abc", new List<int>() { 2, 1, 3 }, 3, "dfg" };

			var valuelst = tstList.ToAerospikeList();

			var valueOrder = valuelst.GetOrderedCollection().ToList();

			AreEqualItems(valuelst.ToList(), valueOrder);

			tstList = new List<object>() { 123M, 0, 2, 2, 1, "abc", new List<int>() { 2, 1, 3 }.ToAerospikeList(OrderActions.ValueOrdered), 3, "dfg" };

			valuelst = tstList.ToAerospikeList();

			var tstListOrd = new List<object>() { 123M, 0, 2, 2, 1, "abc", new List<int>() { 1, 2, 3 }, 3, "dfg" };

			valueOrder = valuelst.GetOrderedCollection().ToList();

			AreEqualItems(tstListOrd, valueOrder);

			tstListOrd = new List<object>() { 0, 1, 2, 2, 3, "abc", "dfg", new List<int>() { 1, 2, 3 }, 123M};

			valuelst.OrderAction = OrderActions.ValueOrdered;
			
			valueOrder = valuelst.GetOrderedCollection().ToList();

			AreEqualItems(tstListOrd, valueOrder);
		}

		[TestMethod]
		public void AddElementsTest()
		{
			var tstList = new List<object>() { 123M, 0, 2, 2, 1, "abc", new List<int>() { 2, 1, 3 }, 3, "dfg" };

			var valuelst = tstList.ToAerospikeList();

			AreEqualItems(tstList, valuelst.ToList());

			tstList.Add(12345);

			AreEqualItems(tstList, valuelst.ToList());

			tstList.RemoveAt(tstList.Count - 1);

			AreEqualItems(tstList, valuelst.ToList());

			tstList.Add(new List<string>() { "a","b","c"});

			AreEqualItems(tstList, valuelst.ToList());
		}
	}
}
