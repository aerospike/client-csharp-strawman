using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace NewClient
{
	[Flags]
	public enum OrderActions
	{
		/// <summary>
		/// The Items in the collection are not ordered (default)
		/// </summary>
		UnOrdered = 0,
		/// <summary>
		/// The Value items are ordered. 
		/// </summary>
		/// <seealso cref="https://aerospike.com/docs/server/guide/data-types/cdt-ordering"/>
		ValueOrdered = 0x0001,
		/// <summary>
		/// The keys and values are ordered. 
		/// For lists, this is the same as &apos;ValueOrdered&apos;.
		/// </summary>
		/// <seealso cref="https://aerospike.com/docs/server/guide/data-types/cdt-ordering"/>
		KeyValueOrdered = ValueOrdered | 0x0010
	}
	public interface ICDTValue : IValue
	{
		bool TryConvert<R>(out ListValue<R>? value)
			where R : IValue;

		/// <summary>
		/// Casts the elements of an IEnumerable to the specified type.
		/// </summary>
		/// <typeparam name="R">
		/// The type to cast the elements of source to.
		/// If R is &apos;object&apos;, each element is converted to their .net native type.
		/// </typeparam>
		/// <returns>
		/// An IEnumerable&lt;R&gt; that contains each element of the source sequence cast to the specified type.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// An element in the sequence cannot be cast to type <typeparamref name="R"/>.
		/// </exception>
		IEnumerable<R> Cast<R>();

		/// <summary>
		/// Filters the elements of an IEnumerable based on a specified type.
		/// </summary>
		/// <typeparam name="R">
		/// The type to filter the elements of the sequence on.
		/// If R is &apos;object&apos;, each element is converted to their .net native type.
		/// </typeparam>
		/// <returns>
		/// An IEnumerable&lt;R&gt; that contains elements from the input sequence of type <typeparamref name="R"/>.
		/// </returns>
		IEnumerable<R> OfType<R>();

		/// <summary>
		/// Gets or sets the order action for this collection based on <see cref="OrderActions"/>.
		/// </summary>
		/// <remarks>
		/// To obtain the collection order call <see cref="ICDTValue{T}.GetOrderedCollection"/>.
		/// </remarks>
		/// <value>The order action.</value>
		/// <seealso cref="ICDTValue{T}.GetOrderedCollection()"/>
		/// <seealso cref="OrderActions"/>
		/// <seealso cref="https://aerospike.com/docs/server/guide/data-types/cdt-ordering"/>
		OrderActions OrderAction { get; set; }

		/// <summary>
		/// Gets a value indicating whether this CDT has items.
		/// </summary>
		/// <value><c>true</c> if this instance has items; otherwise, <c>false</c>.</value>
		bool HasItems { get; }
	}

	public interface ICDTValue<T> : IValue<T>, ICDTValue, IReadOnlyCollection<T>
		where T : IValue
	{
		IEnumerable<T>? Collection { get; }

		/// <summary>
		/// Returns the collection based on <see cref="ICDTValue.OrderAction"/>.
		/// </summary>
		/// <returns>The ordered collection</returns>
		/// <seealso cref="ICDTValue.OrderAction"/>
		/// <seealso cref="OrderActions"/>
		/// <seealso cref="https://aerospike.com/docs/server/guide/data-types/cdt-ordering"/>
		IEnumerable<T> GetOrderedCollection();
	}
}
