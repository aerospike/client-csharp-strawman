using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aerospike.Client
{
	/// <summary>
	/// An awaiter for asynchronous socket operations
	/// </summary>
	// adapted from Stephen Toub's code at
	// https://blogs.msdn.microsoft.com/pfxteam/2011/12/15/awaiting-socket-operations/
	public sealed class SocketAwaitable : INotifyCompletion
	{
		// placeholder for when we don't have an actual continuation. does nothing
		readonly static Action _sentinel = () => { };
		// the continuation to use
		Action _continuation;

		/// <summary>
		/// Creates a new instance of the class for the specified <paramref name="eventArgs"/>
		/// </summary>
		/// <param name="eventArgs">The socket event args to use</param>
		public SocketAwaitable(SocketAsyncEventArgs eventArgs)
		{
			if (null == eventArgs) throw new ArgumentNullException("eventArgs");
			EventArgs = eventArgs;
			eventArgs.Completed += delegate
			{
				var prev = _continuation ?? Interlocked.CompareExchange(
					ref _continuation, _sentinel, null);
				if (prev != null) prev();
			};
		}
		/// <summary>
		/// Indicates the event args used by the awaiter
		/// </summary>
		public SocketAsyncEventArgs EventArgs { get; internal set; }
		/// <summary>
		/// Indicates whether or not the operation is completed
		/// </summary>
		public bool IsCompleted { get; internal set; }

		internal void Reset()
		{
			_continuation = null;
		}
		/// <summary>
		/// This method supports the async/await framework
		/// </summary>
		/// <returns>Itself</returns>
		public SocketAwaitable GetAwaiter() { return this; }

		// for INotifyCompletion
		void INotifyCompletion.OnCompleted(Action continuation)
		{
			if (_continuation == _sentinel ||
				Interlocked.CompareExchange(
					ref _continuation, continuation, null) == _sentinel)
			{
				Task.Run(continuation);
			}
		}
		/// <summary>
		/// Checks the result of the socket operation, throwing if unsuccessful
		/// </summary>
		/// <remarks>This is used by the async/await framework</remarks>
		public void GetResult()
		{
			if (EventArgs.SocketError != SocketError.Success)
				throw new SocketException((int)EventArgs.SocketError);
		}
	}

	public static class AsyncHelpers
	{
		/// <summary>
		/// Receive data using the specified awaitable class
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="awaitable">An instance of <see cref="SocketAwaitable"/></param>
		/// <returns><paramref name="awaitable"/></returns>
		public static SocketAwaitable ReceiveAsync(this Socket socket,
		SocketAwaitable awaitable)
		{
			awaitable.Reset();
			if (!socket.ReceiveAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			return awaitable;
		}
		/// <summary>
		/// Sends data using the specified awaitable class
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="awaitable">An instance of <see cref="SocketAwaitable"/></param>
		/// <returns><paramref name="awaitable"/></returns>
		public static SocketAwaitable SendAsync(this Socket socket,
			SocketAwaitable awaitable)
		{
			awaitable.Reset();
			if (!socket.SendAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			return awaitable;
		}
	}
}
