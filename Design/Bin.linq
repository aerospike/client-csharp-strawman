<Query Kind="Program">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Json</Namespace>
</Query>

#load "Record.linq"

void Main()
{
	/*var stringBin = new Bin<string>("binname", "aa");
	string stringValue;
	
	stringBin.TryGetValue(out stringValue).Dump();
	stringValue.Dump();
	
	var intValue = 1;
	var intBin = new Bin<int>("binname", intValue);
	bool boolValue;
	decimal decimalValue;
	
	intBin.TryGetValue(out boolValue).Dump();
	boolValue.Dump();
	intBin.TryGetValue(out decimalValue).Dump();
	decimalValue.Dump();
	
	var dateTimeOffsetBin = new Bin<string>("binname", DateTimeOffset.Now.ToString());
	DateTimeOffset dateTimeOffsetValue;
	
	dateTimeOffsetBin.TryGetValue(out dateTimeOffsetValue).Dump();
	dateTimeOffsetValue.Dump();

	//Use Extension Methods
	
	var stringBinE = "bb".ToBin("binname");
	string stringEValue;

	stringBinE.TryGetValue(out stringEValue).Dump();
	stringEValue.Dump();

	var dateTimeOffsetBinE = DateTimeOffset.Now.ToBin("binname");
	DateTimeOffset dateTimeOffsetBinEValue;
	
	dateTimeOffsetBinE.TryGetValue(out dateTimeOffsetBinEValue).Dump();
	dateTimeOffsetBinEValue.Dump();

	var dateTimeBinE = DateTime.Now.ToBin("binname");
	DateTime dateTimeBinEValue;

	dateTimeBinE.TryGetValue(out dateTimeBinEValue).Dump();
	dateTimeBinEValue.Dump();

	var intBinE = intValue.ToBin("binname");
	int intBinEValue;

	intBinE.TryGetValue(out intBinEValue).Dump();
	intBinEValue.Dump();

	intBinE.Equals(intValue).Dump();
	intBinE.Equals(2).Dump();
	intBinE.Equals(intBin).Dump();*/

	var geoJson = new GeoJSON("a place");
	var geoJsonBin = new Bin<GeoJSON>("binname", geoJson);
	geoJsonBin.Dump();
	geoJsonBin.Equals("a place").Dump();
	geoJsonBin.Value.Dump();
	geoJsonBin.Value.Value.Dump();
}

public interface IBin : IConvertible, IComparable, IFormattable, IComparable<IBin>, IEquatable<IBin>
{
	public string Name { get; }
	
	public bool TryGetValue<V>(out V outValue) where V : IConvertible;
	public bool TryGetValue(out DateTimeOffset outValue);
	
	public object ToObject();
	
	public DatabaseType DatabaseType { get; }
	
	[AllowNull]
	public IRecord Record { get; set; }
	
	public string Tag { get; set; }
}

public static class BinHelpers
{
	public static Bin<DateTimeOffset> ToBin(this DateTimeOffset value, string binName) => new Bin<DateTimeOffset>(binName, value);
	public static Bin<DateTime> ToBin(this DateTime value, string binName) => new Bin<DateTime>(binName, value);
	public static Bin<string> ToBin(this string value, string binName) => new Bin<string>(binName, value);
	public static Bin<T> ToBin<T>(this T value, string binName) where T : struct => new Bin<T>(binName, value);
	public static Bin<byte[]> ToBin(this byte[] value, string binName) => new Bin<byte[]>(binName, value);
	public static Bin<GeoJSON> ToBin(this GeoJSON value, string binName) => new Bin<GeoJSON>(binName, value);
	// TODO: Blob, ByteSegment, HLL, Inifinity, Wildcard, List, Map, Null, Array, Wildcard, any others?
	// IEnumerable pattern for collection types
	//All the different Bin types we can support Can use T4 here!
}

[System.Diagnostics.DebuggerDisplay("{ToString()}")]
public class Bin<T> : IBin, IComparable<Bin<T>>, IEquatable<Bin<T>>
{
	public Bin([NotNull] string name, T value)
	{
		this.Name = name;
		this.Value = value;
		this.DatabaseType = MapTypeToDatabaseType();
	}
	
	public string Name { get; }

	public T Value { get; }	
	
	public DatabaseType DatabaseType { get; }
		
	[AllowNull]
	public IRecord Record { get; set; }
	
	public string Tag { get; set; }
	
	public bool TryGetValue<V>(out V outValue) where V : IConvertible
	{
		outValue = (V)Convert.ChangeType(this.Value, typeof(V));
		return true;				
	}

	public bool TryGetValue(out DateTimeOffset outValue)
	{
		switch (this.Value)
		{
			case DateTimeOffset dto:
			{
				outValue = dto;
				return true;
			}
			case string strDTO:
			{
				return DateTimeOffset.TryParse(strDTO, out outValue);
			}
			case DateTime dt:
			{
				outValue = new DateTimeOffset(dt);
				return true;
			}
			case long ticks:
			{
				outValue = new DateTimeOffset(ticks, TimeSpan.Zero); // Need to also implement NanoEpochToDateTime
				return true;
			}
		}
		
		outValue = default(DateTimeOffset);
		return false;
	}
	
	private DatabaseType MapTypeToDatabaseType() {
		if (typeof(T) == typeof(string)) {
			return DatabaseType.STRING;
		}
		
		switch (this.Value)
		{
			case string s:
			{
				return DatabaseType.STRING;
			}
			case int i:
			case bool b:
			case byte byteValue:
			case long l:
			case short s:
			case sbyte sByte:
			case uint uInt:
			case ulong uLong:
			case ushort uShort:
			{
				return DatabaseType.INTEGER;
			}
			case double d:
			case float f:
			{
				return DatabaseType.DOUBLE;
			}
			case byte[] b:
			{
				return DatabaseType.BLOB;
			}
			case GeoJSON g:
			{
				return DatabaseType.GEOJSON;
			}
		}
		
		return DatabaseType.NULL;
		
		// TODO: DateTimeOffset, DateTime, Blob, ByteSegment, GeoJSON, HLL, Infinity, Wildcard, List, Map, Null, Array, Wildcard, any others?
	}

	public object ToObject() => this.Value;
	
	#region IConvertible

	public TypeCode GetTypeCode()
	{
		return Type.GetTypeCode(typeof(T));
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToBoolean(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Boolean.")

		};
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToByte(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Byte.")

		};
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToChar(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Char.")

		};
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToDateTime(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid DateTime.")

		};
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToDecimal(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Decimal.")

		};
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToDouble(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Double.")

		};
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToInt16(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Int16.")

		};
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToInt32(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Int32.")

		};
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToInt64(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Int64.")

		};
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToSByte(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid SByte.")

		};
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToSingle(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid Single.")

		};
	}

	string IConvertible.ToString(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToString(provider),
			_ => this.Value?.ToString()

		};
	}

	object IConvertible.ToType(Type conversionType, IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToType(conversionType, provider),
			DateTimeOffset dto => dto,
			TimeSpan ts => ts,
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid {conversionType.FullName}.")

		};
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToUInt16(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid UInt16.")

		};
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToUInt32(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid UInt32.")

		};
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return this.Value switch
		{
			IConvertible iconvt => iconvt.ToUInt64(provider),
			_ => throw new InvalidCastException($"{typeof(T).FullName} \"{this.Value}\" was not recongnized as a valid UInt64.")

		};
	}
#endregion

	public int CompareTo(object obj)
	{
		if(obj is null && this.Value is null) return 0;
		if(obj is Bin<T> value) return this.CompareTo(value);
		if(obj is IBin iValue) return this.CompareTo(iValue);
		if(this.Value is IComparable comparer) return comparer.CompareTo(obj);
		
		return this.Value.GetHashCode() < (obj?.GetHashCode() ?? 0) ? -1 : 1;
	}

	public int CompareTo(IBin other)
	{
		if(other is null) return 1;
		if(other is Bin<T> value) return this.CompareTo(value);
		
		var nameCompare = this.Name.CompareTo(other?.Name);
		
		if(nameCompare == 0)
		{
			if (this.Value is IComparable comparable)
				return comparable.CompareTo(other.ToObject());

			return (this.Value?.GetHashCode() ?? 0) < (other.ToObject()?.GetHashCode() ?? 0)
						? -1
						: 1;
		}

		return nameCompare;
	}

	public bool Equals(IBin other)
	{
		if(other is Bin<T> value) return this.Equals(value);
		return false;
	}

	public int CompareTo(Bin<T> other)
	{
		if(other is null) return 1;
		if(this.Equals(other)) return 0;
		if(this.Name == other.Name) 
		{
			if(this.Value is IComparable comparable) 
				return comparable.CompareTo(other.Value);
				
			return (this.Value?.GetHashCode() ?? 0) < (other.Value?.GetHashCode() ?? 0)
						? -1
						: 1;
		}
		return this.Name.CompareTo(other.Name);
	}

	public bool Equals(Bin<T> other) => this.Name == other.Name && (this.Value?.Equals(other.Value) ?? false);

	public string ToString(string format, IFormatProvider formatProvider)
	{
		if(this.Value is IFormattable formattable)
			return formattable.ToString(format, formatProvider);

		if (String.IsNullOrEmpty(format)) format = "G";
		if (formatProvider == null) formatProvider = System.Globalization.CultureInfo.CurrentCulture;

		
		return string.Format(formatProvider, format, this.Value, this.Name);
	}

	public override bool Equals(object obj)
	{
		if (obj is null && this.Value is null) return true;
		if (obj is Bin<T> value) return this.Equals(value);
		if (obj is IBin iValue) return this.Equals(iValue);
		
		return this.Value?.Equals(obj) ?? false;
	}
	
	public static explicit operator T(Bin<T> b) => b.Value;
	
	public static bool operator ==(Bin<T> o1, Bin<T> o2) => o1?.Equals(o2) ?? false;
	public static bool operator !=(Bin<T> o1, Bin<T> o2) => o1 == o2 ? false : true;

	public static bool operator ==(Bin<T> o1, T o2) => o1?.Equals(o2) ?? false;
	public static bool operator !=(Bin<T> o1, T o2) => o1 == o2 ? false : true;

	// TODO look up implementation of gethashcode and think about which one is best for us
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string ToString()
	{
		return $"{{{this.Name} : {this.Value} }}";
	}
}

public class GeoJSON : IEquatable<string>, IEquatable<GeoJSON>, IComparable<GeoJSON>, IComparable<string> {

	public GeoJSON([NotNull] string value)
	{
		this.Value = value;
	}
	
	// TODO J object version. which library should we use? microsoft version. Research this
	public GeoJSON([NotNull] JsonObject value)
	{
		this.Value = value.ToString();
	}
	
	public string Value { get; }
	
	public override bool Equals(object obj)
	{
		if (obj is string sValue) return Equals(sValue);
		if (obj is GeoJSON gValue) return Equals(gValue);

		return false;
	}
	
	public override string ToString() => Value;
	
	public object ToObject() => this.Value;
	
	public bool Equals(GeoJSON other) => other is null || other.Value is null ? false : Value.Equals(other.Value);

	public bool Equals(string other) => Value.Equals(other);
	
	public override int GetHashCode() => Value.GetHashCode();
	
	public static bool operator ==(GeoJSON o1, GeoJSON o2) => o1?.Equals(o2) ?? false;
	public static bool operator !=(GeoJSON o1, GeoJSON o2) => o1 == o2 ? false : true;
	
	public static bool operator ==(GeoJSON o1, string o2) => o1?.Equals(o2) ?? false;
	public static bool operator !=(GeoJSON o1, string o2) => o1 == o2 ? false : true;
	
	public int CompareTo(object obj)
	{
		if(obj is GeoJSON value) return this.CompareTo(value);
		if(obj is string sValue) return this.CompareTo(sValue);
		if(this.Value is IComparable comparer) return comparer.CompareTo(obj);
		
		return this.Value.GetHashCode() < (obj?.GetHashCode() ?? 0) ? -1 : 1;
	}

	public int CompareTo(GeoJSON other)
	{
		if(other is null) return 1;
		if(this.Equals(other)) return 0;
		
		if (this.Value is IComparable comparable)
				return comparable.CompareTo(other.Value);

		return (this.Value?.GetHashCode() ?? 0) < (other.ToObject()?.GetHashCode() ?? 0)
					? -1
					: 1;
	}
	
	public int CompareTo(string other)
	{
		if(other is null) return 1;
		if(this.Equals(other)) return 0;
		
		if (this.Value is IComparable comparable)
				return comparable.CompareTo(other);

		return (this.Value?.GetHashCode() ?? 0) < (other?.GetHashCode() ?? 0)
					? -1
					: 1;
	}
}

public enum DatabaseType : int
{
	// Server types.
	NULL = 0,
	INTEGER = 1,
	DOUBLE = 2,
	STRING = 3,
	BLOB = 4,
	CSHARP_BLOB = 8,
	BOOL = 17,
	HLL = 18,
	MAP = 19,
	LIST = 20,
	GEOJSON = 23
}

