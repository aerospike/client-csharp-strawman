<Query Kind="Program">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

void Main()
{
	var x = new Bin<string>("binname", "aa");
	string xv;
	
	x.TryGetValue(out xv).Dump();
	xv.Dump();
	
	var intV = 1;
	var x1 = new Bin<int>("binname", intV);
	bool x1b;
	decimal x1d;
	
	x1.TryGetValue(out x1b).Dump();
	x1b.Dump();
	x1.TryGetValue(out x1d).Dump();
	x1d.Dump();
	
	var x2 = new Bin<string>("binname", DateTimeOffset.Now.ToString());
	DateTimeOffset x2d;
	
	x2.TryGetValue(out x2d).Dump();
	x2d.Dump();

	//Use Extension Methods
	
	var e1 = "bb".ToBin("binname");
	string ev;

	e1.TryGetValue(out ev).Dump();
	ev.Dump();

	var e2 = DateTimeOffset.Now.ToBin("binname");
	DateTimeOffset e2d;
	
	e2.TryGetValue(out e2d).Dump();
	e2d.Dump();

	var e3 = DateTime.Now.ToBin("binname");
	DateTime e3d;

	e3.TryGetValue(out e3d).Dump();
	e3d.Dump();

	var e4 = intV.ToBin("binname");
	int e4i;

	e4.TryGetValue(out e4i).Dump();
	e4i.Dump();

	e4.Equals(intV).Dump();
	e4.Equals(2).Dump();
	e4.Equals(x1).Dump();

}


public static class BinHelpers
{
	public static Bin<DateTimeOffset> ToBin(this DateTimeOffset value, string binName) => new Bin<DateTimeOffset>(binName, value);
	public static Bin<DateTime> ToBin(this DateTime value, string binName) => new Bin<DateTime>(binName, value);
	public static Bin<string> ToBin(this string value, string binName) => new Bin<string>(binName, value);
	public static Bin<int> ToBin(this int value, string binName) => new Bin<int>(binName, value);
	//All the different Bin types we can support Can use T4 here!
}


public interface IBin : IConvertible, IComparable, IFormattable, IComparable<IBin>, IEquatable<IBin>
{
	public string Name {get;}
	
	public bool TryGetValue<V>(out V outValue) where V : IConvertible;
	public bool TryGetValue(out DateTimeOffset outValue);
	
	public object ToObject();
	
}

[System.Diagnostics.DebuggerDisplay("{ToString()}")]
public class Bin<T> : IBin, IComparable<Bin<T>>, IEquatable<Bin<T>>
{
	public Bin([NotNull] string name, T value)
	{
		this.Name = name;
		this.Value = value;
	}
	
	public string Name { get; }

	public T Value { get; }	
	
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
				outValue = new DateTimeOffset(ticks, TimeSpan.Zero); //Need to also implement NanoEpochToDateTime
				return true;
			}
		}
		
		outValue = default(DateTimeOffset);
		return false;
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

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string ToString()
	{
		return $"{{{this.Name} : {this.Value} }}";
	}
}


