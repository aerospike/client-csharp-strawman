<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Policy.linq"
#load "Node.linq"

public interface IConnection
{
	public Socket Socket { get; }
	
	public DateTime LastUsed { get; }

	public INode Node { get; }
	public ICommand Command { get; }

	public void SetTimeout(int timeoutMillis);

	public void Write(byte[] buffer, int length);

	public void ReadFully(byte[] buffer, int length);
	
	public Stream GetStream();

	/// <summary>
	/// Is socket closed from client perspective only.
	/// </summary>
	public bool IsClosed();

	/// <summary>
	/// Shutdown and close socket.
	/// </summary>
	public void Close();
	
	public void Reset();

	// talk to security compliance officer about SSL/security/LDAP
}

public class Connection : IConnection
{
	public Socket Socket { get; }
	
	public DateTime LastUsed { get; }

	public INode Node { get; }
	
	public ICommand Command { get; }

	public Connection(IPEndPoint address, int timeoutMillis)
	{
		try
		{
			Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}
		catch (Exception e)
		{
			throw new AerospikeException.Connection(e);
		}

		try
		{
			Socket.NoDelay = true;

			if (timeoutMillis > 0)
			{
				Socket.SendTimeout = timeoutMillis;
				Socket.ReceiveTimeout = timeoutMillis;
			}
			else
			{
				// Never allow timeoutMillis of zero (no timeout) because WaitOne returns 
				// immediately when that happens!
				// Retry functionality will attempt to reconnect later.
				timeoutMillis = 2000;
			}
			System.Threading.Tasks.Task task = Socket.ConnectAsync(address);

			if (!task.Wait(timeoutMillis))
			{
				// Connection timed out.
				throw new SocketException((int)SocketError.TimedOut);
			}
			LastUsed = DateTime.UtcNow;
		}
		catch (Exception e)
		{
			Socket.Dispose();
			throw new AerospikeException.Connection(e);
		}
	}

	public void SetTimeout(int timeoutMillis);

	public void Write(byte[] buffer, int length);

	public void ReadFully(byte[] buffer, int length);

	public Stream GetStream();

	/// <summary>
	/// Is socket closed from client perspective only.
	/// </summary>
	public bool IsClosed();

	/// <summary>
	/// Shutdown and close socket.
	/// </summary>
	public void Close();

	/// <summary>
	/// GetHostAddresses with timeout.
	/// </summary>
	/// <param name="host">Host name.</param>
	/// <param name="timeoutMillis">Timeout in milliseconds</param>
	public static IPAddress[] GetHostAddresses(string host, int timeoutMillis);
	
	private void InitError(INode node);
	
	public void Reset();
}



