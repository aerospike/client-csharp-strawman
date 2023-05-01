<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Host.linq"
#load "Cluster.linq"
#load "Connection.linq"
#load "NodeValidator.linq"
#load "Peers.linq"

public interface INode
{
	public string Name { get; }
	
	public ICluster Cluster { get; }
	
	public IHost Host { get; }
	
	public List<IHost> Aliases { get; }
	
	public IPEndPoint Address { get; }

	public NodeStates State { get; }
	
	public IPartition Partition { get; }
	
	public Pool<IConnection> ConnectionPool { get; }

	public volatile bool Active { get; set; } // = true;
	
	// how do we discover what paritions are in the cluster?
	// Look at Info.Request() in Aerospike documentation. 
	// may need to hunt through existing code to find stuff, may need to run commands to see what they return or look at server c code
	// route to multiple interfaces

	/// <summary>
	/// Request current status from server node.
	/// </summary>
	public void Refresh(Peers peers);

	public void SignalLogin();

	/// <summary>
	/// Get a socket connection from connection pool to the server node.
	/// </summary>
	/// <param name="timeoutMillis">connection timeout value in milliseconds if a new connection is created</param>	
	/// <exception cref="AerospikeException">if a connection could not be provided</exception>
	public Connection GetConnection(int timeoutMillis);

	/// <summary>
	/// Put connection back into connection pool.
	/// </summary>
	/// <param name="conn">socket connection</param>
	public void PutConnection(Connection conn);

	/// <summary>
	/// Close pooled connection on error.
	/// </summary>
	public void CloseConnectionOnError(Connection conn);

	/// <summary>
	/// Close pooled connection.
	/// </summary>
	public void CloseConnection(Connection conn);

	public ConnectionStats GetConnectionStats();
	
	public void IncrErrorCount();

	public void ResetErrorCount();

	public bool ErrorCountWithinLimit();

	public void ValidateErrorCount();
	/// <summary>
	/// Return if this node has the same rack as the client for the
	/// given namespace.
	/// </summary>
	public bool HasRack(string ns, int rackId);

	public byte[] SessionToken
	{
		get { return Volatile.Read(ref sessionToken); }
	}

	public bool HasQueryShow
	{
		get { return (features & HAS_QUERY_SHOW) != 0; }
	}

	public bool HasBatchAny
	{
		get { return (features & HAS_BATCH_ANY) != 0; }
	}

	public bool HasPartitionQuery
	{
		get { return (features & HAS_PARTITION_QUERY) != 0; }
	}

	/// <summary>
	/// Close all server node socket connections.
	/// </summary>
	public void Close();
}

public class Node : INode
{
	public string Name { get; }

	public ICluster Cluster { get; }

	public IHost Host { get; }
	
	public List<IHost> Aliases { get; }

	public IPEndPoint Address { get; }

	public NodeStates State { get; }

	public IPartition Partition { get; }

	public Pool<IConnection> ConnectionPool { get; }
	
	public volatile bool Active { get; set; } // = true;

	private int ConnsOpened { get; } // = 1;
	
	private int ConnsClosed { get; }

	private Connection TendConnection { get; }
	public byte[] SessionToken { get { return Volatile.Read(ref _sessionToken); } }
	private byte[] _sessionToken;
	private DateTime? SessionExpiration { get; }
	private volatile Dictionary<string, int> Racks { get; }
	private uint ConnectionIter { get; }
	private volatile int ErrorCount { get; set; }
	private int PeersGeneration { get; } // = -1;
	private int PartitionGeneration { get; } // = -1;
	private int RebalanceGeneration { get; } // = -1;
	private int PeersCount { get; }
	private int ReferenceCount { get; }
	private int Failures { get; }
	private uint Features { get; }
	private volatile int PerformLogin { get; set; }
	private bool PartitionChanged { get; } // = true;
	private bool RebalanceChanged { get; }

	/// <summary>
	/// Initialize server node with connection parameters.
	/// </summary>
	/// <param name="cluster">collection of active server nodes</param>
	/// <param name="nv">connection parameters</param>
	public Node(Cluster cluster, NodeValidator nv)
	{
		this.Cluster = cluster;
		this.Name = nv.Name;
		this.Host = nv.PrimaryHost;
		this.Address = nv.PrimaryAddress;
		this.TendConnection = nv.PrimaryConn;
		this.SessionToken = nv.SessionToken;
		this.SessionExpiration = nv.SessionExpiration;
		this.Features = nv.Features;
		this.RebalanceChanged = cluster.RackAware;
		this.Racks = cluster.RackAware ? new Dictionary<string, int>() : null;

		connectionPools = new Pool<Connection>[cluster.ConnPoolsPerNode];
		int min = cluster.minConnsPerNode / cluster.ConnPoolsPerNode;
		int remMin = cluster.minConnsPerNode - (min * cluster.connPoolsPerNode);
		int max = cluster.maxConnsPerNode / cluster.connPoolsPerNode;
		int remMax = cluster.maxConnsPerNode - (max * cluster.connPoolsPerNode);

		for (int i = 0; i < connectionPools.Length; i++)
		{
			int minSize = i < remMin ? min + 1 : min;
			int maxSize = i < remMax ? max + 1 : max;

			Pool<Connection> pool = new Pool<Connection>(minSize, maxSize);
			connectionPools[i] = pool;
		}
	}

	~Node()
	{
		// Close connections that slipped through the cracks on race conditions.
		CloseConnections();
	}

	public void CreateMinConnections();

	/// <summary>
	/// Request current status from server node.
	/// </summary>
	public void Refresh(Peers peers);

	private bool ShouldLogin();

	internal void RefreshPeers(Peers peers);

	internal void RefreshPartitions(Peers peers);

	internal void RefreshRacks();

	private void Login();

	public void SignalLogin();

	private void VerifyNodeName(Dictionary<string, string> infoMap);

	private void Restart();

	private void VerifyPartitionGeneration(Dictionary<string, string> infoMap);

	private void VerifyRebalanceGeneration(Dictionary<string, string> infoMap);

	internal void RefreshPeers(Peers peers);

	private static bool FindPeerNode(Cluster cluster, Peers peers, string nodeName);

	internal void RefreshPartitions(Peers peers);

	internal void RefreshRacks();

	private void RefreshFailed(Exception e);

	private void CreateConnections(Pool<Connection> pool, int count);

	private Connection CreateConnection(ICommand command);

	/// <summary>
	/// Get a socket connection from connection pool to the server node.
	/// </summary>
	/// <param name="timeoutMillis">connection timeout value in milliseconds if a new connection is created</param>	
	/// <exception cref="AerospikeException">if a connection could not be provided</exception>
	public Connection GetConnection(int timeoutMillis);

	private Connection CreateConnection(int timeout, Pool<Connection> pool);

	/// <summary>
	/// Put connection back into connection pool.
	/// </summary>
	/// <param name="conn">socket connection</param>
	public void PutConnection(Connection conn);

	/// <summary>
	/// Close pooled connection on error.
	/// </summary>
	public void CloseConnectionOnError(Connection conn);

	/// <summary>
	/// Close pooled connection.
	/// </summary>
	public void CloseConnection(Connection conn);

	public void BalanceConnections();

	private void CloseIdleConnections(int count);

	internal void IncrConnTotal();

	internal void DecrConnTotal();

	internal void IncrConnOpened();

	internal void IncrConnClosed();

	public ConnectionStats GetConnectionStats();

	public void IncrErrorCount();

	public void ResetErrorCount();

	public bool ErrorCountWithinLimit();

	public void ValidateErrorCount();
	/// <summary>
	/// Return if this node has the same rack as the client for the
	/// given namespace.
	/// </summary>
	public bool HasRack(string ns, int rackId);

	public bool HasQueryShow
	{
		get { return (features & HAS_QUERY_SHOW) != 0; }
	}

	public bool HasBatchAny
	{
		get { return (features & HAS_BATCH_ANY) != 0; }
	}

	public bool HasPartitionQuery
	{
		get { return (features & HAS_PARTITION_QUERY) != 0; }
	}

	/// <summary>
	/// Return node name, host address and cluster id in string format.
	/// </summary>
	public override sealed string ToString();

	/// <summary>
	/// Get node name hash code.
	/// </summary>
	public override sealed int GetHashCode();

	/// <summary>
	/// Return if node names are equal.
	/// </summary>
	public override sealed bool Equals(object obj);

	/// <summary>
	/// Close all server node socket connections.
	/// </summary>
	public void Close();

	internal void CloseConnections();
	
	internal void CloseOnError(IConnection conn);
}



public enum NodeStates : int
{
	CONNECTED = 1,
	NOT_CONNECTED = 2
	// investigate other node states
	// AdminInfoCommand - figure out command to get back state
}
