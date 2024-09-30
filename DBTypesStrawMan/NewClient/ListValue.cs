using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NewClient
{
	[System.Diagnostics.DebuggerDisplay("{DebuggerString()}")]
	public struct ListValue<T> : ICDTValue<T>, IReadOnlyList<T>, IComparable<ListValue<T>>
		where T : IValue
	{
		#region Constructors

		public ListValue()
		{
			this.UnderlyingType = typeof(T);
			this.Collection = null;
			this.DBType = AerospikeDBTypes.Null;
		}

		public ListValue([DisallowNull] IEnumerable<T> elements, OrderActions orderAction = OrderActions.UnOrdered)
		{
			this.UnderlyingType = typeof(T);
			this.Collection = elements;
			this.DBType = AerospikeDBTypes.List;
			this.OrderAction = orderAction;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ListValue{T}"/> struct as a shallow copy.
		/// </summary>
		/// <param name="clone">Used to make the clone</param>
		/// <param name="orderAction">
		/// If provided, this will override the cloned instance&apos;s value.
		/// </param>
		/// <seealso cref="Clone"/>
		public ListValue([DisallowNull] ListValue<T> clone, [AllowNull] OrderActions? orderAction = null)
		{
			this.Collection = clone.Collection;
			this.UnderlyingType = clone.UnderlyingType;
			this.DBType = AerospikeDBTypes.List;
			this.OrderAction = orderAction ?? clone.OrderAction;
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public readonly ListValue<T> CloneInstance() => this.Collection is null
												? new ListValue<T>()
												: new ListValue<T>(this.Collection.ToArray(),
																	this.OrderAction);

		public readonly IValue Clone() => this.CloneInstance();

		#endregion

		[AllowNull]
		public readonly T? Value => default;

		[AllowNull]
		public readonly IEnumerable<T>? Collection { get; }

		/// <summary>
		/// Returns the .net version of an list...
		/// </summary>
		/// <value>An IList instance or null</value>
		[AllowNull]
		public readonly object? Object
		{
			get
			{
				if(this.TryConvert(out IList<object>? value))
				{
					return value;
				}
				return null;
			}
		}

		public readonly Type UnderlyingType { get; }

		/// <inheritdoc />
		public readonly bool HasItems => this.Count > 0;

		#region Comparer

		/// <inheritdoc />
		public OrderActions OrderAction { get; set; }

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other based on <see cref="OrderAction"/>.
		/// If <see cref="OrderActions.UnOrdered"/> is selected, this will sort based on the sorting rules of the underlying IValue object.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// A signed integer that indicates the relative values of x and y, as shown in the following table.
		/// Value Meaning:
		///		Less than zero x is less than y.
		///		Zero x equals y.
		///		Greater than zero   x is greater than y
		/// </returns>
		/// <seealso cref="OrderAction"/>
		/// <exception cref="InvalidDataException">
		/// If database type is not valid
		/// </exception>
		/// <remarks>
		/// Elements with different types are ordered based on their type:
		///		NIL
		///		BOOLEAN
		///		INTEGER
		///		STRING
		///		LIST
		///		MAP
		///		BYTES
		///		DOUBLE
		///		GEOJSON
		///		INF
		/// </remarks>
		static int Comparer(T? x, T? y, OrderActions orderAction)
		{
			if(x is null)
				return y is null ? 0 : -1;
			if(y is null)
				return 1;

			if(orderAction != OrderActions.UnOrdered)
			{
				var result = Helpers.DBTypeComparer(x.DBType, y.DBType);
				if(result != 0)
					return result;
			}

			return x.CompareTo(y);
		}

		public readonly int Comparer(T? x, T? y) => Comparer(x, y, this.OrderAction);

		public readonly int CompareTo(ListValue<T> other) => 0;

		public readonly int CompareTo([AllowNull] IValue? other)
		{
			if(other is null)
				return 1;

			if(other is ListValue<T> lstValue)
				return this.CompareTo(lstValue);

			switch(other.DBType)
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
					throw new InvalidDataException($"DBType {other.DBType} is invalid");
			}
			return 0;
		}

		public readonly int CompareTo(T? other) => this.CompareTo((IValue?) other);

		public readonly int CompareTo([AllowNull] object obj)
		{
			if(obj is null)
				return 1;
			if(obj is IValue iValue)
				return this.CompareTo(iValue);

			var thisHC = this.GetHashCode();
			var otherHC = obj.GetHashCode();

			return thisHC == otherHC ? 0 : (thisHC > otherHC ? 1 : -1);
		}

		public readonly int Compare(T? x, T? y)
		{
			if(x is null)
				return y is null ? 0 : -1;
			if(y is null)
				return 1;

			return x.CompareTo(y);
		}

		/// <inheritdoc />
		public readonly IEnumerable<T> GetOrderedCollection()
		{
			if(this.Collection is null)
				yield break;

			foreach(var item in this.OrderAction == OrderActions.UnOrdered
										? this.Collection
										: this.Collection.Order())
			{
				if(item is ICDTValue<T> cdtItemt
						&& cdtItemt.HasItems
						&& cdtItemt.OrderAction != OrderActions.UnOrdered)
				{
					yield return (T) (IValue) new ListValue<T>(cdtItemt.GetOrderedCollection());							
				}
				if(item is ICDTValue cdtItem
						&& cdtItem.HasItems
						&& cdtItem.OrderAction != OrderActions.UnOrdered)
				{
					yield return (T) (IValue) new ListValue<IValue>(cdtItem
																		.ToEnumerable()
																		.Order());
				}
				else
				{
					yield return item;
				}
			}
		}

		#endregion

		#region Equality/ToString

		public readonly bool Equals(IValue? other)
		{
			if(other is null)
				return this.IsNull;
			if(other is ICDTValue<T> lstValue)
				return ReferenceEquals(this.Collection, lstValue.Collection);

			return false;
		}

		public readonly override bool Equals(object? obj)
		{
			if(obj is null)
				return this.IsNull;
			if(obj is ICDTValue<T> lstValue)
				return this.Equals(lstValue);

			return false;
		}

		public readonly override int GetHashCode() => this.Object?.GetHashCode() ?? 0;

		public readonly override string? ToString()
		{
			if(this.Collection is null)
				return "{}";
			var items = string.Join(',',
									this.Collection
										.Select(x => x.IsString
														? $"\"{x}\""
														: x.ToString()));

			return $"{{{items}}}";
		}

		public readonly string DebuggerString()
				=> $"ListValue<{this.UnderlyingType}>({this.Collection?.Count() ?? 0})";

		#endregion

		#region Type Checks

		public readonly bool IsCDT => true;

		public readonly bool IsList => true;

		public readonly bool IsMap => false;

		public readonly bool IsDouble => false;

		public readonly bool IsInteger => false;

		public readonly bool IsBoolean => false;

		public readonly bool IsString => false;

		public readonly bool IsBlob => false;

		public readonly bool IsGeoSpatial => false;

		public readonly bool IsHyperLogLog => false;

		public readonly bool IsNull => this.DBType == AerospikeDBTypes.Null;

		public readonly bool IsNumeric => false;

		public readonly AerospikeDBTypes DBType { get; }

		#endregion

		#region Conversions
		public readonly bool TryConvert<R>(out R value)
			where R : unmanaged
		{
			value = default;
			return false;
		}

		public readonly bool TryConvert(out string? value)
		{
			value = default;
			return false;
		}

		private static (R castValue, bool converted) TryCast<R>(T value, bool throwInvalidCast)
		{
			if(value is IScalarValue scalarValue)
			{
				if(scalarValue.TryConvert(typeof(R), out var oValue))
				{
					return ((R) oValue, true);
				}
				if(throwInvalidCast)
					throw new InvalidCastException($"Cannot cast from {value} to {typeof(R).Name}");

				#pragma warning disable CS8619
				return (default, false);
				#pragma warning restore CS8619
			}

			if(value is ICDTValue cdtValue)
			{
				if(typeof(R).Equals(typeof(object))
					|| typeof(R).IsAssignableTo(typeof(IEnumerable)))
				{
					if(typeof(R).IsGenericType
							&& !typeof(R).GetGenericArguments()[0].Equals(typeof(object)))
					{
						var mthd = value.GetType()
										.GetMethod("TryConvert", 1,
													BindingFlags.Public | BindingFlags.Instance, null,
													[Type.MakeGenericSignatureType(typeof(IList<>), Type.MakeGenericMethodParameter(0)).MakeByRefType()],
													null)
										?.MakeGenericMethod(typeof(R).GetGenericArguments()[0]);

						var mthdParams = new object[1];
						var result = mthd?.Invoke(value, mthdParams) ?? false;
						if((bool) result)
						{
							return ((R) mthdParams[0], true);
						}
					}
					else if(cdtValue.TryConvert(out IList<object>? oValue) && oValue is not null)
					{
						return ((R) oValue, true);
					}
				}
			}
			if(throwInvalidCast)
				throw new InvalidCastException($"Cannot cast from {value} to {typeof(R).Name}");

			#pragma warning disable CS8619
			return (default, false);
			#pragma warning restore CS8619
		}

		public readonly bool TryConvert<R>(out IList<R>? value)
		{
			if(this.IsNull || this.Collection is null)
			{
				value = default;
				return false;
			}

			try
			{
				if(!typeof(R).Equals(typeof(Object)) && typeof(R).IsAssignableTo(typeof(IValue)))
				{
					value = this.Collection.Cast<R>().ToList();					
				}
				else
				{
					value = this.Collection.Select(i => TryCast<R>(i, true).castValue).ToList();
				}
			}
			catch(SystemException e) when (e is InvalidCastException || e is FormatException)
			{
				value = null;
				return false;
			}
			return true;
		}

		public readonly bool TryConvert<K, V>(out IDictionary<K, V>? value)
		{
			value = default;
			return true;
		}

		public readonly bool TryConvert<R>(out ListValue<R>? value)
			where R : IValue
		{
			if(this.IsNull || this.Collection is null)
			{
				value = default;
				return false;
			}

			if(typeof(T) == typeof(R))
			{
				value = (ListValue<R>) (IValue) this;
				return true;
			}

			if(this.TryConvert<R>(out IList<R>? newLst) && newLst is not null)
			{
				value = new ListValue<R>(newLst);
				return true;
			}

			value = default;
			return false;
		}

		public readonly bool TryConvert<R>(out IValue<R>? value)
			where R : IComparable => TryConvert<R>(out value);
		
		/// <summary>
		/// Generate unique server hash value from set name and value.  
		/// The hash function is RIPEMD-160 (a 160 bit hash).
		/// </summary>
		/// <param name="setName">optional set name, enter null for the Aerospike NULL set</param>
		/// <returns>unique server hash value</returns>
		/// <exception cref="AerospikeException">if digest computation fails</exception>
		public readonly byte[] ComputeDigest(string? setName)
		{
			//Need to add logic
			return [];
		}

		#endregion

		#region Enumerable
		/// <inheritdoc/>
		public readonly IEnumerable<IValue> ToEnumerable()
							=> this.Collection?.Cast<IValue>() ?? Enumerable.Empty<IValue>();

		/// <inheritdoc/>
		public readonly IEnumerable<R> ToEnumerable<R>()
		{
			if(this.TryConvert(out IList<R>? value))
			{
				return value ?? Enumerable.Empty<R>();
			}
			throw new InvalidCastException($"Cannot convert {this} element&apos;s to {typeof(R)}.");
		}

		public readonly IEnumerable<R> Cast<R>()
		{
			if(this.IsNull || this.Collection is null)
			{
				return Enumerable.Empty<R>();
			}

			if(!typeof(R).Equals(typeof(Object)) && typeof(R).IsAssignableTo(typeof(IValue)))
			{
				return this.Collection.Cast<R>();
			}
			
			return this.Collection.Select(i => TryCast<R>(i, true).castValue);
		}

		public readonly IEnumerable<R> OfType<R>()
		{
			if(this.IsNull || this.Collection is null)
			{
				return Enumerable.Empty<R>();
			}

			if(!typeof(R).Equals(typeof(Object)) && typeof(R).IsAssignableTo(typeof(IValue)))
			{
				return this.Collection.OfType<R>();
			}

			return this.Collection
						.Select(i => TryCast<R>(i, false))
						.Where(i => i.converted)
						.Select(i => i.castValue)
						.Cast<R>();
		}

		#endregion

		#region IReadOnlyList

		public readonly int Count => this.Collection?.Count() ?? 0;

		#pragma warning disable CS8603 // Possible null reference return.
		public readonly T this[int index] => this.Collection is null
												? default
												: this.Collection.ElementAtOrDefault(index);

		public readonly IEnumerator<T> GetEnumerator() => this.Collection is null
															? Enumerable.Empty<T>().GetEnumerator()
															: this.Collection.GetEnumerator();
		#pragma warning restore CS8603 // Possible null reference return.

		readonly IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		#endregion

		/// <inheritdoc/>
		public byte[] DBSerializer() => [];

		public static bool operator ==(ListValue<T> left, ListValue<T> right) => left.Equals(right);
		public static bool operator !=(ListValue<T> left, ListValue<T> right) => !left.Equals(right);

	}

	public static class ListValueHelpers
	{
		public static ListValue<ScalarValue<R>> ToAerospikeList<R>(this IEnumerable<R> collection, OrderActions orderAction = OrderActions.UnOrdered)
			where R : unmanaged, IEquatable<R>, IComparable<R>, IConvertible
			=> new (collection.Select(i => new ScalarValue<R>(i)), orderAction);

		public static ListValue<ScalarValue<string>> ToAerospikeList(this IEnumerable<string> collection, OrderActions orderAction = OrderActions.UnOrdered)
			=> new(collection.Select(i => i.ToAerospikeValue()), orderAction);

		public static ListValue<ScalarValue<R>> ToAerospikeList<R>(this IEnumerable<ScalarValue<R>> collection, OrderActions orderAction = OrderActions.UnOrdered)
			where R : unmanaged, IEquatable<R>, IComparable<R>, IConvertible
			=> new(collection, orderAction);

		public static ListValue<ScalarValue<string>> ToAerospikeList(this IEnumerable<ScalarValue<string>> collection, OrderActions orderAction = OrderActions.UnOrdered)
			=> new(collection, orderAction);

		public static ListValue<IValue> ToAerospikeList(this IEnumerable<IValue> collection, OrderActions orderAction = OrderActions.UnOrdered)
			=> new(collection, orderAction);

		public static ListValue<IValue> ToAerospikeList(this IEnumerable<object> collection, OrderActions orderAction = OrderActions.UnOrdered)
			=> new(collection.Select(i => i.ToAerospikeValue()), orderAction);

		public static IValue? ToAerospikeValue<R>(this IEnumerable<R> collection)
			where R : unmanaged, IEquatable<R>, IComparable<R>, IConvertible
			=> ToAerospikeList(collection);

		public static IValue? ToAerospikeValue(this IEnumerable<string> collection)
			=> ToAerospikeList(collection);

		public static IValue? ToAerospikeValue<R>(this IEnumerable<ScalarValue<R>> collection)
			where R : unmanaged, IEquatable<R>, IComparable<R>, IConvertible
			=> ToAerospikeList(collection);

		public static IValue? ToAerospikeValue(this IEnumerable<ScalarValue<string>> collection)
			=> ToAerospikeList(collection);

		public static IValue? ToAerospikeValue(this IEnumerable<IValue> collection)
			=> ToAerospikeList(collection);

		public static IValue? ToAerospikeValue(this IEnumerable<object> collection)
			=> ToAerospikeList(collection);
	}
}
