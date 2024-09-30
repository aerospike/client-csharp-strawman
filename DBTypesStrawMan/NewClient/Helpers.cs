using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace NewClient
{
	public static class Helpers
	{

		public static IValue ToAerospikeValue(this object value)
					=> value switch
					{
						null => new ScalarValue<string>(),
						IValue iValue => iValue,
						string strValue => new ScalarValue<string>(strValue),
						//IDictionary dictValur => ToAerospikeIValue(dictValur),
						IEnumerable collectionValue => ListValueHelpers.ToAerospikeList(collectionValue
																							.Cast<object>()
																							.Select(i => ToAerospikeValue(i))),
						Int16 valueI16 => new ScalarValue<Int16>(valueI16),
						Int32 valueI32 => new ScalarValue<Int32>(valueI32),
						Int64 valueI64 => new ScalarValue<Int64>(valueI64),
						UInt16 valueUI16 => new ScalarValue<UInt16>(valueUI16),
						UInt32 valueUI32 => new ScalarValue<UInt32>(valueUI32),
						UInt64 valueUI64 => new ScalarValue<UInt64>(valueUI64),
						decimal valueDec => new ScalarValue<decimal>(valueDec),
						float valueFloat => new ScalarValue<float>(valueFloat),
						double valueDouble => new ScalarValue<double>(valueDouble),
						_ => throw new NotSupportedException($"Cannot convert value \"{value}\" ({value.GetType().Name}) to Aerospike DB Type")
					};

		public static N ToValue<N>(this IValue iValue)
			where N : unmanaged
				=> iValue.TryConvert(out N nValue)
						? nValue
						: throw new InvalidCastException($"Cannot convert Value \"{iValue}\" ({iValue.GetType().Name}) to type {typeof(N).Name}");

		public static string? ToValue(this IValue? iValue)
				=> iValue is null 
						? null
						: (iValue.TryConvert(out string? nValue)
							? nValue
							: throw new InvalidCastException($"Cannot convert Value \"{iValue}\" ({iValue.GetType().Name}) to type string"));

		public static int DBTypeComparer(AerospikeDBTypes x, AerospikeDBTypes y)
		{
			switch(x)
			{
				case AerospikeDBTypes.Null:
					return y == AerospikeDBTypes.Null ? 0 : -1;
				case AerospikeDBTypes.Interger:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
							return 1;
						case AerospikeDBTypes.Interger:
							break;
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.Double:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
							return 1;
						case AerospikeDBTypes.Double:
							break;
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.Boolean:
					switch(y)
					{
						case AerospikeDBTypes.Null:
							return 1;
						case AerospikeDBTypes.Boolean:
							break;
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.String:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
							return 1;
						case AerospikeDBTypes.String:
							break;
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.List:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
							return 1;
						case AerospikeDBTypes.List:
							break;
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.Map:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
							return 1;
						case AerospikeDBTypes.Map:
							break;
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.Blob:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
							return 1;
						case AerospikeDBTypes.Blob:
							break;
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.GeoJSON:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
							return 1;
						case AerospikeDBTypes.GeoJSON:
							break;
						case AerospikeDBTypes.HyperLogLog:
							return -1;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				case AerospikeDBTypes.HyperLogLog:
					switch(y)
					{
						case AerospikeDBTypes.Null:
						case AerospikeDBTypes.Boolean:
						case AerospikeDBTypes.Interger:
						case AerospikeDBTypes.String:
						case AerospikeDBTypes.List:
						case AerospikeDBTypes.Map:
						case AerospikeDBTypes.Blob:
						case AerospikeDBTypes.Double:
						case AerospikeDBTypes.GeoJSON:
							return 1;
						case AerospikeDBTypes.HyperLogLog:
							break;
						default:
							throw new InvalidDataException($"DBType {y} is invalid");
					}
					break;
				default:
					throw new InvalidDataException($"DBType {y} is invalid");
			}
			return 0;
		}
	}
}
