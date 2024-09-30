using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NewClient
{
	public interface IScalarValue : IValue, IConvertible
	{
		bool TryConvert<R>([NotNull] out ScalarValue<R> value)
			where R : unmanaged, IEquatable<R>, IComparable<R>, IConvertible;
		bool TryConvert(out ScalarValue<string>? value);

		/// <summary>
		/// Tries to convert to <paramref name="type"/>, if possible.
		/// </summary>
		/// <param name="type">
		/// The type the instance will be converted too.
		/// </param>
		/// <param name="value">
		/// The converted value.
		/// </param>
		/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
		bool TryConvert([DisallowNull] Type type, [AllowNull] out object value);
	}

	[System.Diagnostics.DebuggerDisplay("{DebuggerString()}")]
	public readonly struct ScalarValue<T> : IScalarValue, IValue<T>, IEquatable<T>, IEqualityComparer<T>, IComparable<T>,
											IEquatable<ScalarValue<T>>, IEqualityComparer<ScalarValue<T>>, IComparable<ScalarValue<T>>,
											IEquatable<IValue<T>>, IEqualityComparer<IValue<T>>,
											IComparable<IValue<T>>, IComparer<IValue<T>>

		where T : IEquatable<T>, IComparable<T>, IConvertible
	{
		#region Constructors

		public ScalarValue()
		{
			this.DBType = AerospikeDBTypes.Null;
			this.Value = default;
		}

		public ScalarValue([DisallowNull] T value, [DisallowNull] AerospikeDBTypes dbType)
		{
			this.Value = value;
			this.DBType = dbType;
		}

		public ScalarValue([DisallowNull] T value)
		{
			this.Value = value;
			this.DBType = ScalarValueHelpers.DetermineDBType(value);
		}

		public ScalarValue<T> CloneInstance() => new(this.Value, this.DBType);
		public IValue Clone() => this.CloneInstance();

		public ScalarValue<R> Clone<R>()
			where R : IEquatable<R>, IComparable<R>, IConvertible
				=> new((R) this.ToType(typeof(R), null));

		#endregion

		[AllowNull]
		public readonly T Value { get; }

		public readonly object Object => this.Value;

		public readonly AerospikeDBTypes DBType { get; }

		[DisallowNull]
		public readonly Type UnderlyingType => typeof(T);

		#region Type Checks

		public readonly bool IsCDT => false;

		public readonly bool IsList => false;

		public readonly bool IsMap => false;

		public readonly bool IsDouble => this.DBType == AerospikeDBTypes.Double;

		public readonly bool IsInteger => this.DBType == AerospikeDBTypes.Interger;

		public readonly bool IsBoolean => this.DBType == AerospikeDBTypes.Boolean;

		public readonly bool IsString => this.DBType == AerospikeDBTypes.String;

		public readonly bool IsBlob => false;

		public readonly bool IsGeoSpatial => false;

		public readonly bool IsHyperLogLog => false;

		public readonly bool IsNull => this.DBType == AerospikeDBTypes.Null;

		public readonly bool IsNumeric => this.IsInteger || this.IsDouble;

		#endregion

		#region Conversions

		public readonly TypeCode GetTypeCode() => this.IsNull ? TypeCode.DBNull : this.Value.GetTypeCode();

		public readonly bool ToBoolean(IFormatProvider? provider) => this.Value.ToBoolean(provider);

		public readonly byte ToByte(IFormatProvider? provider) => this.Value.ToByte(provider);

		public readonly char ToChar(IFormatProvider? provider) => this.Value.ToChar(provider);

		public readonly DateTime ToDateTime(IFormatProvider? provider) => this.Value.ToDateTime(provider);

		public readonly decimal ToDecimal(IFormatProvider? provider) => this.Value.ToDecimal(provider);

		public readonly double ToDouble(IFormatProvider? provider) => this.Value.ToDouble(provider);

		public readonly short ToInt16(IFormatProvider? provider) => this.Value.ToInt16(provider);

		public readonly int ToInt32(IFormatProvider? provider) => this.Value.ToInt32(provider);

		public readonly long ToInt64(IFormatProvider? provider) => this.Value.ToInt64(provider);

		public readonly sbyte ToSByte(IFormatProvider? provider) => this.Value.ToSByte(provider);

		public readonly float ToSingle(IFormatProvider? provider) => this.Value.ToSingle(provider);

		public readonly string ToString(IFormatProvider? provider) => this.Value.ToString(provider);

		public readonly object ToType(Type conversionType, IFormatProvider? provider) => this.Value.ToType(conversionType, provider);

		public readonly ushort ToUInt16(IFormatProvider? provider) => this.Value.ToUInt16(provider);

		public readonly uint ToUInt32(IFormatProvider? provider) => this.Value.ToUInt32(provider);

		public readonly ulong ToUInt64(IFormatProvider? provider) => this.Value.ToUInt64(provider);

		public bool TryConvert([DisallowNull] Type type, [AllowNull] out object value)
		{
			if(this.IsNull)
			{
				#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
				value = default;
				#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
				return false;
			}

			if(this.UnderlyingType == type)
			{
				value = this.Value;
				return true;
			}

			if(type == typeof(object))
			{
				value = this.Value;
				return true;
			}

			try
			{
				//This is much faster than using ToType and even the emit pattern... Same memory overhead as box/unbox...
				switch(Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						value = this.Value.ToBoolean(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Byte:
						value = this.Value.ToByte(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Char:
						value = this.Value.ToChar(CultureInfo.CurrentCulture);
						break;
					case TypeCode.DateTime:
						value = this.Value.ToDateTime(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Decimal:
						value = this.Value.ToDecimal(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Double:
						value = this.Value.ToDouble(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Int16:
						value = this.Value.ToInt16(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Int32:
						value = this.Value.ToInt32(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Int64:
						value = this.Value.ToInt64(CultureInfo.CurrentCulture);
						break;
					case TypeCode.SByte:
						value = this.Value.ToSByte(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Single:
						value = this.Value.ToSingle(CultureInfo.CurrentCulture);
						break;
					case TypeCode.UInt16:
						value = this.Value.ToUInt16(CultureInfo.CurrentCulture);
						break;
					case TypeCode.UInt32:
						value = this.Value.ToUInt32(CultureInfo.CurrentCulture);
						break;
					case TypeCode.UInt64:
						value = this.Value.ToUInt64(CultureInfo.CurrentCulture);
						break;
					case TypeCode.String:
						value = this.Value.ToString(CultureInfo.CurrentCulture);
						break;
					//case TypeCode.DBNull:							
					//case TypeCode.Empty: i.e., Null Reference
					//case TypeCode.Object:
					default:
						#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
						value = default;
						#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
						return false;
				}
			}
			catch(SystemException ex) when (ex is FormatException || ex is OverflowException)
			{
				#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
				value = default;
				#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
				return false;
			}

			return true;
		}

		/// <summary>
		/// Will try to convert the value to a value of type "R".
		/// </summary>
		/// <typeparam name="R">
		/// The .net type the value will be converted to.
		/// This can be a .net native type or collection.
		/// If a collection, each element in the bin's CDT will be converted to the target collection's element type. If not possible, the CDT element will be ignored.
		/// </typeparam>
		/// <param name="value">
		/// If successful, the converted value of type "R".
		/// If not successful, the default value of type "R".
		/// </param>
		/// <returns>
		/// True if the value was converted.
		/// </returns>	
		/// <remarks>
		/// This should be re-factored with Expressions.
		/// </remarks>
		public readonly bool TryConvert<R>(out R value)
			where R : unmanaged
		{
			if(this.IsNull || this.IsString)
			{
				value = default;
				return false;
			}

			if(this.Value is R rValue)
			{
				value = rValue;
				return true;
			}

			try
			{
				//This is much faster than using ToType and even the emit pattern... Same memory overhead as box/unbox...
				switch(Type.GetTypeCode(typeof(R)))
				{
					case TypeCode.Boolean:
						value = (R) (object) this.Value.ToBoolean(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Byte:
						value = (R) (object) this.Value.ToByte(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Char:
						value = (R) (object) this.Value.ToChar(CultureInfo.CurrentCulture);
						break;
					case TypeCode.DateTime:
						value = (R) (object) this.Value.ToDateTime(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Decimal:
						value = (R) (object) this.Value.ToDecimal(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Double:
						value = (R) (object) this.Value.ToDouble(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Int16:
						value = (R) (object) this.Value.ToInt16(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Int32:
						value = (R) (object) this.Value.ToInt32(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Int64:
						value = (R) (object) this.Value.ToInt64(CultureInfo.CurrentCulture);
						break;
					case TypeCode.SByte:
						value = (R) (object) this.Value.ToSByte(CultureInfo.CurrentCulture);
						break;
					case TypeCode.Single:
						value = (R) (object) this.Value.ToSingle(CultureInfo.CurrentCulture);
						break;
					case TypeCode.UInt16:
						value = (R) (object) this.Value.ToUInt16(CultureInfo.CurrentCulture);
						break;
					case TypeCode.UInt32:
						value = (R) (object) this.Value.ToUInt32(CultureInfo.CurrentCulture);
						break;
					case TypeCode.UInt64:
						value = (R) (object) this.Value.ToUInt64(CultureInfo.CurrentCulture);
						break;					
					//case TypeCode.DBNull:				
					//case TypeCode.String:
					//case TypeCode.Empty: i.e., Null Reference
					//case TypeCode.Object:
					default:
						value = default;
						return false;
				}
			}
			catch(SystemException ex) when(ex is FormatException || ex is OverflowException)
			{
				value = default;
				return false;
			}

			return true;
		}

		public readonly bool TryConvert(out string? value)
		{
			if(this.IsNull)
			{
				value = default;
				return true;
			}

			value = this.Value.ToString();

			return true;
		}
		
		public readonly bool TryConvert<R>(out IList<R>? value)
		{
			value = default;
			return false;
		}
		public readonly bool TryConvert<K, V>(out IDictionary<K, V>? value)
		{
			value = default;
			return false;
		}

		public readonly bool TryConvert<R>([NotNull] out ScalarValue<R> value)
			where R : unmanaged, IEquatable<R>, IComparable<R>, IConvertible
		{
			if(this.IsNull || this.IsString)
			{
				value = default;
				return false;
			}

			var rTypeCode = Type.GetTypeCode(typeof(R));

			if(this.GetTypeCode() == rTypeCode)
			{
				value = (ScalarValue<R>) (IScalarValue) this;
				return true;
			}

			try
			{
				switch(rTypeCode)
				{
					case TypeCode.Boolean:
						value = new ScalarValue<R>((R) (object) this.Value.ToBoolean(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Byte:
						value = new ScalarValue<R>((R) (object) this.Value.ToByte(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Char:
						value = new ScalarValue<R>((R) (object) this.Value.ToChar(CultureInfo.CurrentCulture));
						break;
					case TypeCode.DateTime:
						value = new ScalarValue<R>((R) (object) this.Value.ToDateTime(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Decimal:
						value = new ScalarValue<R>((R) (object) this.Value.ToDecimal(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Double:
						value = new ScalarValue<R>((R) (object) this.Value.ToDouble(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Int16:
						value = new ScalarValue<R>((R) (object) this.Value.ToInt16(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Int32:
						value = new ScalarValue<R>((R) (object) this.Value.ToInt32(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Int64:
						value = new ScalarValue<R>((R) (object) this.Value.ToInt64(CultureInfo.CurrentCulture));
						break;
					case TypeCode.SByte:
						value = new ScalarValue<R>((R) (object) this.Value.ToSByte(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Single:
						value = new ScalarValue<R>((R) (object) this.Value.ToSingle(CultureInfo.CurrentCulture));
						break;
					case TypeCode.UInt16:
						value = new ScalarValue<R>((R) (object) this.Value.ToUInt16(CultureInfo.CurrentCulture));
						break;
					case TypeCode.UInt32:
						value = new ScalarValue<R>((R) (object) this.Value.ToUInt32(CultureInfo.CurrentCulture));
						break;
					case TypeCode.UInt64:
						value = new ScalarValue<R>((R) (object) this.Value.ToUInt64(CultureInfo.CurrentCulture));
						break;
					//case TypeCode.DBNull:				
					//case TypeCode.String:
					//case TypeCode.Empty: i.e., Null Reference
					//case TypeCode.Object:
					default:
						value = default;
						return false;
				}
			}
			catch(SystemException e) when(e is FormatException || e is OverflowException || e is InvalidCastException)
			{
				value = default;
				return false;
			}

			return true;
		}

		public readonly bool TryConvert(out ScalarValue<string>? value)
		{
			if(this.IsNull)
			{
				value = default;
				return true;
			}

			if(this.IsString)
				value = (ScalarValue<string>) (IScalarValue) this;
			else
				value = new ScalarValue<string>(this.ToString(null));

			return true;
		}

		public readonly bool TryConvert<R>(out IValue<R>? value)
			where R : IComparable
		{
			if(this.IsNull)
			{
				value = default;
				return false;
			}

			var rTypeCode = Type.GetTypeCode(typeof(R));

			if(this.GetTypeCode() == rTypeCode)
			{
				value = (IValue<R>) (IValue) this;
				return true;
			}

			try
			{
				switch(rTypeCode)
				{
					case TypeCode.Boolean:
						value = (IValue<R>) (IValue) new ScalarValue<bool>(this.Value.ToBoolean(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Byte:
						value = (IValue<R>) (IValue) new ScalarValue<Byte>(this.Value.ToByte(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Char:
						value = (IValue<R>) (IValue) new ScalarValue<Char>(this.Value.ToChar(CultureInfo.CurrentCulture));
						break;
					case TypeCode.DateTime:
						value = (IValue<R>) (IValue) new ScalarValue<DateTime>(this.Value.ToDateTime(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Decimal:
						value = (IValue<R>) (IValue) new ScalarValue<Decimal>(this.Value.ToDecimal(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Double:
						value = (IValue<R>) (IValue) new ScalarValue<Double>(this.Value.ToDouble(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Int16:
						value = (IValue<R>) (IValue) new ScalarValue<Int16>(this.Value.ToInt16(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Int32:
						value = (IValue<R>) (IValue) new ScalarValue<Int32>(this.Value.ToInt32(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Int64:
						value = (IValue<R>) (IValue) new ScalarValue<Int64>(this.Value.ToInt64(CultureInfo.CurrentCulture));
						break;
					case TypeCode.SByte:
						value = (IValue<R>) (IValue) new ScalarValue<SByte>(this.Value.ToSByte(CultureInfo.CurrentCulture));
						break;
					case TypeCode.Single:
						value = (IValue<R>) (IValue) new ScalarValue<Single>(this.Value.ToSingle(CultureInfo.CurrentCulture));
						break;
					case TypeCode.UInt16:
						value = (IValue<R>) (IValue) new ScalarValue<UInt16>(this.Value.ToUInt16(CultureInfo.CurrentCulture));
						break;
					case TypeCode.UInt32:
						value = (IValue<R>) (IValue) new ScalarValue<UInt32>(this.Value.ToUInt32(CultureInfo.CurrentCulture));
						break;
					case TypeCode.UInt64:
						value = (IValue<R>) (IValue) new ScalarValue<UInt64>(this.Value.ToUInt64(CultureInfo.CurrentCulture));
						break;
					//case TypeCode.DBNull:				
					//case TypeCode.String:
					//case TypeCode.Empty: i.e., Null Reference
					//case TypeCode.Object:
					default:
						value = default;
						return false;
				}
			}
			catch(SystemException e) when(e is FormatException || e is OverflowException || e is InvalidCastException)
			{
				value = default;
				return false;
			}

			return true;
		}		

		/// <summary>
		/// Generate unique server hash value from set name and value.  
		/// The hash function is RIPEMD-160 (a 160 bit hash).
		/// </summary>
		/// <param name="setName">optional set name, enter null for the Aerospike NULL set</param>
		/// <returns>unique server hash value</returns>
		/// <exception cref="AerospikeException">if digest computation fails</exception>
		public readonly byte[] ComputeDigest(string? setName = null)
		{
			//Need to add logic
			return [];
		}

		public ScalarValue<T> GetValue() => this;

		#endregion

		#region Equality (Object, IValue, T)

		public readonly bool Equals(IValue? other) => this.IsNull ? other is null : this.Equals(other?.Object);

		public readonly bool Equals([AllowNull] T other) => this.IsNull
																? other is null
																: other is not null && this.Value.Equals(other);

		public readonly bool Equals([AllowNull] T x, [AllowNull] T y) => x?.Equals(y) ?? false;

		public readonly int GetHashCode([DisallowNull] T obj) => obj?.GetHashCode() ?? 0;

		public readonly override int GetHashCode() => this.IsNull ? 0 : this.Value.GetHashCode();

		/// <summary>
		/// </summary>
		/// 
		public readonly override bool Equals(object? obj)
		{
			if(obj is null)
				return this.IsNull;
			if(this.IsNull)
				return false;

			switch(obj)
			{
				case T value:
					return this.Equals(value);
				case ScalarValue<T> scalarValue:
					return this.Equals(scalarValue.Value);
				case IValue generalValue:
					return this.Equals(generalValue);
			}

			if(this.DBType == ScalarValueHelpers.DetermineDBType(obj, false)
					&& this.TryConvert(obj.GetType(), out object? cValue))
				return obj.Equals(cValue);

			return this.Value.Equals(obj);
		}

		#endregion

		#region Equality (IValue<T>)

		public readonly bool Equals(IValue<T>? other) => (other is null || other.IsNull) 
															? this.IsNull
															: this.Value.Equals(other.Value);

		public readonly bool Equals(IValue<T>? x, IValue<T>? y) => x?.Equals(y) ?? (y?.Equals(x) ?? true);

		public readonly int GetHashCode([DisallowNull] IValue<T> obj) => obj?.GetHashCode() ?? 0;

		#endregion

		#region ToEnumerable
		/// <inheritdoc/>
		public IEnumerable<IValue> ToEnumerable()
		{
			yield return this;
		}

		/// <inheritdoc/>
		public IEnumerable<R> ToEnumerable<R>()
		{
			if(this.TryConvert(typeof(R), out object value))
			{
				yield return (R) value;
			}
			throw new InvalidCastException($"Cannot convert {this} to {typeof(R)}.");
		}
		#endregion

		#region Compare
		public readonly int CompareTo(IValue? other) => this.CompareTo(other?.Object);

		public readonly int CompareTo(object? obj)
		{
			if(obj is null)
				return this.IsNull ? 0 : 1;
			if(this.IsNull)
				return -1;

			switch(obj)
			{
				case T tvalue:
					return this.CompareTo(tvalue);
				case ScalarValue<T> isValue:
					return this.CompareTo(isValue.Value);
				case IValue generalValue:
					return this.CompareTo(generalValue);
				case IComparable compareValue:
					{
						var dbType = ScalarValueHelpers.DetermineDBType(obj, false);

						if((this.IsNumeric || this.IsBoolean)
								&& (dbType == AerospikeDBTypes.Interger
										|| dbType == AerospikeDBTypes.Boolean
										|| dbType == AerospikeDBTypes.Double
										|| dbType == AerospikeDBTypes.String))
							return compareValue.CompareTo(this.ToType(obj.GetType(), null)) * -1;

						if(this.DBType == dbType)
							return compareValue.CompareTo(this.Value) * -1;

						return Helpers.DBTypeComparer(this.DBType, dbType);
					}
			}

			var objDBType = ScalarValueHelpers.DetermineDBType(obj, false);
			if(objDBType != AerospikeDBTypes.Null)
			{
				return Helpers.DBTypeComparer(this.DBType, objDBType);
			}

			return 1;
		}

		public readonly int CompareTo([AllowNull] T other) => this.IsNull ? (other is null ? 0 : -1) : this.Value.CompareTo(other);

		public readonly int CompareTo(IValue<T>? other) => (this.IsNull && (other is null || other.IsNull))
																? 0
																: (other is null
																		? -1
																		: this.CompareTo(other.Value));

		public readonly int Compare(T? x, T? y) => x?.CompareTo(y)
													?? (y is null ? 0 : -1);

		public readonly int Compare(IValue<T>? x, IValue<T>? y) 
		{
			if(x is null || x.Value is null)
				return y is null || y.Value is null ? 0 : -1;
			if(y is null || y.Value is null)
				return 1;
			
			return x.Value.CompareTo(y.Value);
		}
		#endregion

		#region ScalarValue<T>

		public readonly bool Equals(ScalarValue<T> other)
		{
			if(this.IsNull) return other.IsNull;
			if(other.IsNull) return false;
			return this.Value.Equals(other.Value);
		}

		public readonly bool Equals(ScalarValue<T> x, ScalarValue<T> y)
		{
			if(x.IsNull) return y.IsNull;
			if(y.IsNull) return false;
			return x.Value.Equals(y.Value);
		}

		public int GetHashCode(ScalarValue<T> obj) => obj.GetHashCode();

		public int CompareTo(ScalarValue<T> other)
		{
			if(this.IsNull)
				return other.IsNull ? 0 : -1;
			if(other.IsNull) return 1;
			return this.Value.CompareTo(other.Value);
		}


		#endregion

		/// <inheritdoc/>
		public byte[] DBSerializer() => [];

		public readonly override string? ToString() => this.IsNull ? null : this.Value.ToString();

		public readonly string DebuggerString()
		{
			var strValue = this.ToString() ?? "DBNull";

			if(this.IsString)
				strValue = '"' + strValue + '"';

			return $"ScalarValue<{typeof(T).Name}>({strValue})";
		}

		#region Operators

		public static bool operator ==(ScalarValue<T> left, ScalarValue<T> right) => left.Equals(right);
		public static bool operator !=(ScalarValue<T> left, ScalarValue<T> right) => !left.Equals(right);

		public static bool operator ==(ScalarValue<T> left, T right) => left.Equals(right);
		public static bool operator !=(ScalarValue<T> left, T right) => !left.Equals(right);

		public static bool operator ==(T left, ScalarValue<T> right) => right.Equals(left);
		public static bool operator !=(T left, ScalarValue<T> right) => !right.Equals(left);

		public static explicit operator T(ScalarValue<T> binValue) => binValue.Value;

		public static explicit operator ScalarValue<T>(T value) => new(value);

		public static bool operator <(ScalarValue<T> left, ScalarValue<T> right) => left.CompareTo(right) < 0;
		public static bool operator <=(ScalarValue<T> left, ScalarValue<T> right) => left.CompareTo(right.Value) <= 0;
		public static bool operator >(ScalarValue<T> left, ScalarValue<T> right) => left.CompareTo(right) > 0;
		public static bool operator >=(ScalarValue<T> left, ScalarValue<T> right) => left.CompareTo(right) >= 0;

		public static bool operator <(T left, ScalarValue<T> right) => right.CompareTo(left) > 0;
		public static bool operator <=(T left, ScalarValue<T> right) => right.CompareTo(left) >= 0;
		public static bool operator >(T left, ScalarValue<T> right) => right.CompareTo(left) < 0;
		public static bool operator >=(T left, ScalarValue<T> right) => right.CompareTo(left) <= 0;

		public static bool operator <(ScalarValue<T> left, T right) => left.CompareTo(right) < 0;
		public static bool operator <=(ScalarValue<T> left, T right) => left.CompareTo(right) <= 0;
		public static bool operator >(ScalarValue<T> left, T right) => left.CompareTo(right) > 0;
		public static bool operator >=(ScalarValue<T> left, T right) => left.CompareTo(right) >= 0;

		#endregion

	}

	public static class ScalarValueHelpers
	{
		public static ScalarValue<V> ToAerospikeValue<V>(this V value)
					where V : unmanaged, IEquatable<V>, IComparable<V>, IConvertible
				=> new(value);

		public static ScalarValue<string> ToAerospikeValue(this string? value)
				=> value is null ? new() : new(value);

		public static AerospikeDBTypes DetermineDBType<R>([AllowNull] R value, bool throwIfCannotBeDetermined = true)
		{
			switch(value)
			{
				case string:
					return AerospikeDBTypes.String;
				case Int16:
				case Int32:
				case Int64:
				case UInt16:
				case UInt32:
				case UInt64:
					return AerospikeDBTypes.Interger;
				case Single:
				case Double:
				case Decimal:
					return AerospikeDBTypes.Double;
				case bool:
					return AerospikeDBTypes.Boolean;
				case IDictionary:
					return AerospikeDBTypes.Map;
				case IEnumerable:
					return AerospikeDBTypes.List;
				default:
					if(value is null)
					{
						return AerospikeDBTypes.Null;
					}
					else if(throwIfCannotBeDetermined)
					{
						throw new NotSupportedException($"Value \"{value}\" ({typeof(R).Name}) could not be converted to an Aerospike DB Type.");
					}
					break;
			}

			return AerospikeDBTypes.Null;
		}

	}
}
