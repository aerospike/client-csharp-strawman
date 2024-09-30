using System.Diagnostics.CodeAnalysis;

namespace NewClient
{
	/// <summary>
	/// The Aerospike database type 
	/// </summary>
	public enum AerospikeDBTypes
	{
		Null = 0,
		Interger = 1,
		Double = 2,
		Boolean = 17,
		String = 3,
		List = 20,
		Map = 19,
		Blob = 4,
		GeoJSON = 23,
		HyperLogLog = 18
	}

	/// <summary>
	/// Basic representation of an <see cref="Aerospike Bin and Value" href = "https://aerospike.com/docs/server/architecture/data-model"/>.
	/// </summary>
	public interface IValue : IComparable
	{
		/// <summary>
		/// .Net native value.
		/// </summary>
		[AllowNull]
		object? Object { get; }
		/// <summary>
		/// The value's underlying .net type
		/// </summary>
		[NotNull]
		Type UnderlyingType { get; }

		/// <summary>
		/// Clones this instance. If it is a collection, the collection will be copied.
		/// </summary>
		/// <returns>new instance of an IValue</returns>
		/// <see cref="ScalarValue{T}.CloneInstance"/>
		/// <see cref="ListValue{T}.CloneInstance"/>
		IValue Clone();

		#region Aerospike Type Check
		/// <summary>
		/// Returns true if the value is an Aerospike collection data type
		/// </summary>
		bool IsCDT { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike List data type
		/// </summary>
		bool IsList { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike Map (Dictorary) data type
		/// </summary>
		bool IsMap { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike Double data type
		/// </summary>
		bool IsDouble { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike Integer (Int64) data type
		/// </summary>
		bool IsInteger { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike Boolean (bool) data type
		/// </summary>
		bool IsBoolean { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike String data type
		/// </summary>
		bool IsString { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike <seealso cref="Blob" href="https://aerospike.com/docs/server/guide/data-types/blob"/> (array of bytes) data type
		/// </summary>
		bool IsBlob { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike <seealso cref="Geospatial" href="https://aerospike.com/docs/server/guide/data-types/geospatial"> data type
		/// </summary>
		bool IsGeoSpatial { get; }
		/// <summary>
		/// Returns true if the value is an Aerospike <seealso cref="HyperLogLog" href="https://aerospike.com/docs/server/guide/data-types/hll"/> data type
		/// </summary>
		bool IsHyperLogLog { get; }

		/// <summary>
		/// Returns true if the value is an Aerospike Null value.
		/// </summary>
		bool IsNull { get; }

		/// <summary>
		/// Returns true if the value is an Aerospike Numeric type (Integer or Double)
		/// </summary>
		public bool IsNumeric { get; }

		/// <summary>
		/// Returns the database type. 
		/// </summary>
		AerospikeDBTypes DBType { get; }

		#endregion

		#region Conversions 
		/// <summary>
		/// Will try to convert the value to a value of type "R".
		/// </summary>
		/// <typeparam name="R">
		/// The .net type the value will be converted too.
		/// This can be a .net native type.
		/// </typeparam>
		/// <param name="value">
		/// If successful, the converted value of type "R".
		/// If not successful, the default value of type "R".
		/// </param>
		/// <returns>
		/// True if the value was converted.
		/// </returns>
		bool TryConvert<R>(out R value)
			where R : unmanaged;

		bool TryConvert(out string? value);

		/// <summary>
		/// Tries the convert an IValue instance into a list based on <typeparamref name="R"/>.
		/// </summary>
		/// <typeparam name="R">
		/// This can be an C# native type or an IValue derived type.
		/// If R is &apos;object&apos; type, the list returned will be C# types.
		/// </typeparam>
		/// <param name="value">
		/// The returned list or null indicating that the conversion failed.
		/// </param>
		/// <returns><c>true</c> if conversion successful, <c>false</c> otherwise.</returns>
		bool TryConvert<R>(out IList<R>? value);
		bool TryConvert<K, V>(out IDictionary<K, V>? value);
		#endregion

		#region IEnumerable
		/// <summary>
		/// Converts an IValue into an enumerable.
		/// If a CDT, the CDT is returned.
		/// </summary>
		/// <returns>
		/// Always returns an IEnumerable
		/// </returns>
		IEnumerable<IValue> ToEnumerable();

		/// <summary>
		/// Converts to enumerable based on <typeparamref name="R"/>
		/// </summary>
		/// <typeparam name="R">
		/// R can be a .net native type or derived from IValue.
		/// If R is an &apos;object&apos;, each element is converted to it&apos;s .net native type.
		/// </typeparam>
		/// <returns>IEnumerable&lt;R&gt;.</returns>
		/// <exception cref="InvalidCastException">
		/// Thrown if instance cannot be converted to <typeparamref name="R"/>.
		/// </exception>
		IEnumerable<R> ToEnumerable<R>();
		#endregion

		#region DB Methods
		/// <summary>
		/// Generate unique server hash value from set name and value.  
		/// The hash function is RIPEMD-160 (a 160 bit hash).
		/// </summary>
		/// <param name="setName">optional set name, enter null for the Aerospike NULL set</param>
		/// <returns>unique server hash value</returns>
		/// <exception cref="AerospikeException">if digest computation fails</exception>
		byte[] ComputeDigest(string? setName = null);

		/// <summary>
		/// Serialize this object into the proper database protocol object.
		/// </summary>
		/// <returns>Database protocol buffer</returns>
		byte[] DBSerializer();

		#endregion

		#region Comparison Operations

		/// <summary>
		/// Determines whether the specified instance is equal to the current instance based on <see cref="Object"/>
		/// </summary>
		/// <param name="other">
		/// The instance to compare with the current instance.
		/// </param>
		/// <returns>
		/// True if <paramref name="other"/> is equal to the current instance.
		/// </returns>
		bool Equals(IValue? other);

		/// <summary>
		/// Compares this instance with <paramref name="other"/> and returns an integer that indicates whether this instance precedes, follows, or appears in the same position in the sort order as <paramref name="other"/>.
		/// The comparison is based on <see cref="Object"/>.
		/// </summary>
		/// <returns>
		/// Less than zero 		-- This instance precedes <paramref name="other"/>
		/// Zero				-- This instance has the same position in the sort order as <paramref name="other"/>
		/// Greater than zero	-- This instance follows <paramref name="other"/>
		/// </returns>
		int CompareTo(IValue? other);

		#endregion
	}

	public interface IValue<V> : IValue, IComparable<V>, IComparer<V>
	{
		/// <summary>
		/// The .Net native value, if possible...
		/// </summary>
		[AllowNull]
		V? Value { get; }

		bool TryConvert<R>(out IValue<R>? value)
			where R : IComparable;
	}

	

}