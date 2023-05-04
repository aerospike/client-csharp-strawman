<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Namespace.linq"
#load "Connection.linq"
#load "Policy.linq"
#load "Node.linq"
#load "Host.linq"
#load "Client.linq"
#load "NodeValidator.linq"
#load "Peers.linq"

public interface ICluster
{
	/// <summary>
	/// Expected cluster name.  If populated, the clusterName must match the cluster-name field
	/// in the service section in each server configuration.  This ensures that the specified
	/// seed nodes belong to the expected cluster on startup.  If not, the client will refuse
	/// to add the node to the client's view of the cluster.
	/// <para>Default: null</para>
	/// </summary>
	public string Name { get; }
	
	public IClient Client { get; }
	
	public IEnumerable<INamespace> Namespaces { get; }
	
	// Initial host nodes specified by user.
	public IHost[] Seeds { get; }

	// All host aliases for all nodes in cluster.
	// Only accessed within cluster tend thread.
	public INode[] Nodes { get; }
	
	public IDictionary<string, IPartition> PartitionMap { get; }
	
	public string Version { get; } // version of the software running

	/// <summary>
	/// User authentication to cluster.  Leave null for clusters running without restricted access.
	/// <para>Default: null</para>
	/// </summary>
	public byte[] Username { get; } // configurable

	/// <summary>
	/// Password authentication to cluster.  The password will be stored by the client and sent to server
	/// in hashed format.  Leave null for clusters running without restricted access.
	/// <para>Default: null</para>
	/// </summary>
	public byte[] Password { get; } // configurable

	/// <summary>
	/// User administration command socket timeout in milliseconds.
	/// <para>Default: 0 (no timeout)</para>
	/// </summary>
	public int UserCommandTimeout { get; } // configurable

	// should we have a user class?
	// password should be coming back masked. should be in constructor of concrete class

	/// <summary>
	/// Secure connection policy for servers that require TLS connections.
	/// Secure connections are only supported for AerospikeClient synchronous commands.
	/// <para>
	/// Secure connections are not supported for asynchronous commands because AsyncClient 
	/// uses the best performing SocketAsyncEventArgs.  Unfortunately, SocketAsyncEventArgs is
	/// not supported by the provided SslStream.
	/// </para>
	/// <para>Default: null (Use normal sockets)</para>
	/// </summary>
	public TlsPolicy TlsPolicy { get; }

	/// <summary>
	/// Initial host connection timeout in milliseconds.  The timeout when opening a connection 
	/// to the server host for the first time.
	/// <para>Default: 1000</para>
	/// </summary>
	public int ConnectionTimeout { get; } // configurable

	/// <summary>
	/// Login timeout in milliseconds.  The timeout used when user authentication is enabled and
	/// a node login is being performed.
	/// <para>Default: 5000</para>
	/// </summary>
	public int LoginTimeout { get; } // configurable

	/// <summary>
	/// Info command socket timeout in milliseconds.
	/// <para>Default: 1000</para>
	/// </summary>
	public int InfoCommandTimeout { get; } // configurable

	/// <summary>
	/// Maximum socket idle in seconds.  Socket connection pools will discard sockets
	/// that have been idle longer than the maximum.
	/// <para>
	/// Connection pools are now implemented by a LIFO stack.  Connections at the tail of the
	/// stack will always be the least used.  These connections are checked for maxSocketIdle
	/// once every 30 tend iterations (usually 30 seconds).
	/// </para>
	/// <para>
	/// If server's proto-fd-idle-ms is greater than zero, then maxSocketIdle should be
	/// at least a few seconds less than the server's proto-fd-idle-ms, so the client does not
	/// attempt to use a socket that has already been reaped by the server.
	/// </para>
	/// <para>
	/// If server's proto-fd-idle-ms is zero (no reap), then maxSocketIdle should also be zero.
	/// Connections retrieved from a pool in transactions will not be checked for maxSocketIdle
	/// when maxSocketIdle is zero.  Idle connections will still be trimmed down from peak
	/// connections to min connections (minConnsPerNode and asyncMinConnsPerNode) using a
	/// hard-coded 55 second limit in the cluster tend thread.
	/// </para>
	/// <para>Default: 0</para>
	/// </summary>
	public int MaxSocketIdle { get; } // configurable

	/// <summary>
	/// Maximum number of errors allowed per node per <see cref="errorRateWindow"/> before backoff
	/// algorithm throws <see cref="Aerospike.Client.AerospikeException.Backoff"/> on database
	/// commands to that node. If maxErrorRate is zero, there is no error limit and
	/// the exception will not be thrown.
	/// <para>
	/// The counted error types are any error that causes the connection to close (socket errors
	/// and client timeouts) and <see cref="Aerospike.Client.ResultCode.DEVICE_OVERLOAD"/>.
	/// </para>
	/// <para>
	/// Default: 100
	/// </para>
	/// </summary>
	public int MaxErrorRate { get; } // configurable

	/// <summary>
	/// The number of cluster tend iterations that defines the window for <see cref="maxErrorRate"/>.
	/// One tend iteration is defined as <see cref="tendInterval"/> plus the time to tend all nodes.
	/// At the end of the window, the error count is reset to zero and backoff state is removed
	/// on all nodes.
	/// <para>
	/// Default: 1
	/// </para>
	/// </summary>
	public int ErrorRateWindow { get; } // configurable

	/// <summary>
	/// Interval in milliseconds between cluster tends by maintenance thread.
	/// <para>Default: 1000</para>
	/// </summary>
	public int TendInterval { get; } // configurable

	/// <summary>
	/// Should cluster instantiation fail if the client fails to connect to a seed or
	/// all the seed's peers.
	/// <para>
	/// If true, throw an exception if all seed connections fail or a seed is valid,
	/// but all peers from that seed are not reachable.
	/// </para>
	/// <para>
	/// If false, a partial cluster will be created and the client will automatically connect
	/// to the remaining nodes when they become available.
	/// </para>
	/// <para>
	/// Default: true
	/// </para>
	/// </summary>
	public bool FailIfNotConnected { get; } // configurable 

	/// <summary>
	/// How to handle cases when the asynchronous maximum number of concurrent connections 
	/// have been reached.  
	/// </summary>
	public MaxCommandAction MaxCommandAction { get; } // configurable

	/// <summary>
	/// Maximum number of concurrent asynchronous commands that can be active at any point in time.
	/// Concurrent commands can target different nodes of the Aerospike cluster. Each command will 
	/// use one concurrent connection. The number of concurrent open connections is therefore
	/// limited by:
	/// <para>
	/// max open connections = asyncMaxCommands
	/// </para>
	/// The actual number of open connections to each node of the Aerospike cluster depends on how
	/// balanced the commands are between nodes and are limited to asyncMaxConnsPerNode for any
	/// given node. For an extreme case where all commands may be destined to the same node of the
	/// cluster, asyncMaxCommands should not be set greater than asyncMaxConnsPerNode to avoid
	/// running out of connections to the node.
	/// <para>
	/// Further, this maximum number of open connections across all nodes should not exceed the
	/// total socket file descriptors available on the client machine. The socket file descriptors
	/// available can be determined by the following command:
	/// </para>
	/// <para>ulimit -n</para>
	/// <para>Default: 100</para>
	/// </summary>
	public int MaxCommands { get; } // configurable

	/// <summary>
	/// Maximum number of async commands that can be stored in the delay queue when
	/// <see cref="asyncMaxCommandAction"/> is <see cref="Aerospike.Client.MaxCommandAction.DELAY"/>
	/// and <see cref="asyncMaxCommands"/> is reached.
	/// Queued commands consume memory, but they do not consume connections.
	/// <para>
	/// If this limit is reached, the next async command will be rejected with exception
	/// <see cref="Aerospike.Client.AerospikeException.CommandRejected"/>.
	/// If this limit is zero, all async commands will be accepted into the delay queue.
	/// </para>
	/// <para>
	/// The optimal value will depend on your application's magnitude of command bursts and the
	/// amount of memory available to store commands.
	/// </para>
	/// <para>
	/// Default: 0 (no delay queue limit)
	/// </para>
	/// </summary>
	public int MaxCommandsInQueue { get; } // configurable

	/// <summary>
	/// Minimum number of asynchronous connections allowed per server node.  Preallocate min connections
	/// on client node creation.  The client will periodically allocate new connections if count falls
	/// below min connections.
	/// <para>
	/// Server proto-fd-idle-ms and client <see cref="Aerospike.Client.ClientPolicy.maxSocketIdle"/>
	/// should be set to zero (no reap) if asyncMinConnsPerNode is greater than zero.  Reaping connections
	/// can defeat the purpose of keeping connections in reserve for a future burst of activity.
	/// </para>
	/// <para>
	/// Default: 0
	/// </para>
	/// </summary>
	public int MinConnsPerNode { get; } // configurable

	/// <summary>
	/// Maximum number of asynchronous connections allowed per server node.  Transactions will go
	/// through retry logic and potentially fail with "ResultCode.NO_MORE_CONNECTIONS" if the maximum
	/// number of connections would be exceeded.
	/// <para>
	/// The number of connections used per node depends on concurrent commands in progress
	/// plus sub-commands used for parallel multi-node commands (batch, scan, and query).
	/// One connection will be used for each command.
	/// </para>
	/// <para>
	/// If the value is -1, the value will be set to <see cref="Aerospike.Client.ClientPolicy.maxConnsPerNode"/>.
	/// </para>
	/// <para>
	/// Default: -1 (Use maxConnsPerNode)
	/// </para>
	/// </summary>
	public int MaxConnsPerNode { get; } // configurable

	/// <summary>
	/// Size of buffer allocated for each async command. The size should be a multiple of 8 KB.
	/// If not, the size is rounded up to the nearest 8 KB increment.
	/// <para>
	/// If an async command requires a buffer size less than or equal to asyncBufferSize, the
	/// buffer pool will be used. If an async command requires a buffer size greater than
	/// asyncBufferSize, a new single-use buffer will be created on the heap.
	/// </para>
	/// <para>
	/// This field is also used to size the buffer pool for all async commands:
	/// </para>
	/// <code>
	/// buffer pool size = asyncBufferSize * asyncMaxCommands
	/// </code> 
	/// <para>
	/// Default: 128 * 1024 (128 KB)
	/// </para>
	/// </summary>
	public int BufferSize { get; } // configurable

	/// <summary>
	/// Allow batch to be processed immediately in the server's receiving thread for in-memory
	/// namespaces. If false, the batch will always be processed in separate service threads.
	/// <para>
	/// For batch transactions with smaller sized records (&lt;= 1K per record), inline
	/// processing will be significantly faster on in-memory namespaces.
	/// </para>
	/// <para>
	/// Inline processing can introduce the possibility of unfairness because the server
	/// can process the entire batch before moving onto the next command.
	/// </para>
	/// <para>
	/// Default: true
	/// </para>
	/// </summary>
	public bool AllowInline { get; } // configurable

	/// <summary>
	/// Allow batch to be processed immediately in the server's receiving thread for SSD
	/// namespaces. If false, the batch will always be processed in separate service threads.
	/// Server versions &lt; 6.0 ignore this field.
	/// <para>
	/// Inline processing can introduce the possibility of unfairness because the server
	/// can process the entire batch before moving onto the next command.
	/// </para>
	/// <para>
	/// Default: false
	/// </para>
	/// </summary>
	public bool AllowInlineSSD { get; } // configurable

	/// <summary>
	/// Allow read operations to use replicated data partitions instead of master
	/// partition. By default, both read and write operations are directed to the
	/// master partition.
	/// <para>
	/// This variable is currently only used in batch read/exists operations. For 
	/// batch, this variable should only be set to true when the replication factor
	/// is greater than or equal to the number of nodes in the cluster.
	/// </para>
	/// <para>Default: false</para>
	/// </summary>
	public bool AllowProleReads { get; } // configruable

	/// <summary>
	/// Should all batch keys be attempted regardless of errors. This field is used on both
	/// the client and server. The client handles node specific errors and the server handles
	/// key specific errors.
	/// <para>
	/// If true, every batch key is attempted regardless of previous key specific errors.
	/// Node specific errors such as timeouts stop keys to that node, but keys directed at
	/// other nodes will continue to be processed.
	/// </para>
	/// <para>
	/// If false, the server will stop the batch to its node on most key specific errors.
	/// The exceptions are <see cref="Aerospike.Client.ResultCode.KEY_NOT_FOUND_ERROR"/> and
	/// <see cref="Aerospike.Client.ResultCode.FILTERED_OUT"/> which never stop the batch.
	/// The client will stop the entire batch on node specific errors for sync commands
	/// that are run in sequence (maxConcurrentThreads == 1). The client will not stop
	/// the entire batch for async commands or sync commands run in parallel.
	/// </para>
	/// <para>
	/// Server versions &lt; 6.0 do not support this field and treat this value as false
	/// for key specific errors.
	/// </para>
	/// <para>Default: true</para>
	/// </summary>
	public bool RespondAllKeys { get; } // configruable

	/// <summary>
	/// Desired consistency guarantee when committing a transaction on the server. The default 
	/// (COMMIT_ALL) indicates that the server should wait for master and all replica commits to 
	/// be successful before returning success to the client. 
	/// <para>
	/// Default: CommitLevel.COMMIT_ALL
	/// </para>
	/// </summary>
	public CommitLevel CommitLevel { get; } // configruable

	/// <summary>
	/// Qualify how to handle record deletes based on record generation. The default (NONE)
	/// indicates that the generation is not used to restrict deletes.
	/// <para>
	/// Default: GenerationPolicy.NONE
	/// </para>
	/// </summary>
	public GenerationPolicy GenerationPolicy { get; }

	/// <summary>
	/// If the transaction results in a record deletion, leave a tombstone for the record.
	/// This prevents deleted records from reappearing after node failures.
	/// Valid for Aerospike Server Enterprise Edition only.
	/// <para>
	/// Default: false (do not tombstone deleted records).
	/// </para>
	/// </summary>
	public bool DurableDelete { get; } // configurable

	/// <summary>
	/// Send user defined key in addition to hash digest.
	/// If true, the key will be stored with the tombstone record on the server.
	/// <para>
	/// Default: false (do not send the user defined key)
	/// </para>
	/// </summary>
	public bool SendKey { get; } // configurable

	/// <summary>
	/// Read policy for AP (availability) namespaces.
	/// <para>
	/// Default: <see cref="Aerospike.Client.ReadModeAP.ONE"/>
	/// </para>
	/// </summary>
	public ReadModeAP readModeAP { get; } // configurable

	/// <summary>
	/// Read policy for SC (strong consistency) namespaces.
	/// <para>
	/// Default: <see cref="Aerospike.Client.ReadModeSC.SESSION"/>
	/// </para>
	/// </summary>
	public ReadModeSC readModeSC { get; } // configurable

	/// <summary>
	/// Record expiration. Also known as ttl (time to live).
	/// Seconds record will live before being removed by the server.
	/// <para>
	/// Expiration values:
	/// <ul>
	/// <li>-2: Do not change ttl when record is updated.</li>
	/// <li>-1: Never expire.</li>
	/// <li>0: Default to namespace configuration variable "default-ttl" on the server.</li>
	/// <li>&gt; 0: Actual ttl in seconds.</li>
	/// </ul>
	/// </para>
	/// <para>Default: 0</para>
	/// </summary>
	public int Expiration { get; } // configurable

	/// <summary>
	/// Maximum number of concurrent requests to server nodes at any point in time.
	/// If there are 16 nodes in the cluster and maxConcurrentNodes is 8, then queries 
	/// will be made to 8 nodes in parallel.  When a query completes, a new query will 
	/// be issued until all 16 nodes have been queried.
	/// <para>Default: 0 (issue requests to all server nodes in parallel)</para>
	/// </summary>
	public int MaxConcurrentNodes { get; } // configurable

	/// <summary>
	/// Number of records to place in queue before blocking.
	/// Records received from multiple server nodes will be placed in a queue.
	/// A separate thread consumes these records in parallel.
	/// If the queue is full, the producer threads will block until records are consumed.
	/// <para>Default: 5000</para>
	/// </summary>
	public int RecordQueueSize { get; } // configurable

	/// <summary>
	/// Should bin data be retrieved. If false, only record digests (and user keys
	/// if stored on the server) are retrieved.
	/// <para>Default: true</para>
	/// </summary>
	public bool IncludeBinData { get; } // configurable

	/// <summary>
	/// Terminate query if cluster is in migration state. If the server supports partition
	/// queries or the query filter is null (scan), this field is ignored.
	/// <para>Default: false</para>
	/// </summary>
	public bool FailOnClusterChange { get; } // configurable

	/// <summary>
	/// Is query expected to return less than 100 records per node.
	/// If true, the server will optimize the query for a small record set.
	/// This field is ignored for aggregation queries, background queries
	/// and server versions &lt; 6.0.
	/// <para>Default: false</para>
	/// </summary>
	public bool ShortQuery { get; } // configurable

	/// <summary>
	/// Approximate number of records to return to client. This number is divided by the
	/// number of nodes involved in the scan.  The actual number of records returned
	/// may be less than maxRecords if node record counts are small and unbalanced across
	/// nodes.
	/// <para>
	/// Default: 0 (do not limit record count)
	/// </para>
	/// </summary>
	public long ScanMaxRecords { get; }

	/// <summary>
	/// Limit returned records per second (rps) rate for each server.
	/// Do not apply rps limit if recordsPerSecond is zero.
	/// <para>
	/// Default: 0
	/// </para>
	/// </summary>
	public int ScanRecordsPerSecond { get; }

	/// <summary>
	/// Should scan requests be issued in parallel. 
	/// <para>Default: true</para>
	/// </summary>
	public bool ScanConcurrentNodes { get; }

	/// <summary>
	/// Qualify how to handle writes where the record already exists.
	/// <para>Default: RecordExistsAction.UPDATE</para>
	/// </summary>
	public RecordExistsAction RecordExistsAction { get; }

	/// <summary>
	/// For client operate(), return a result for every operation.
	/// <para>
	/// Some operations do not return results by default (ListOperation.clear() for example).
	/// This can make it difficult to determine the desired result offset in the returned
	/// bin's result list.
	/// </para>
	/// <para>
	/// Setting respondAllOps to true makes it easier to identify the desired result offset 
	/// (result offset equals bin's operate sequence).  If there is a map operation in operate(),
	/// respondAllOps will be forced to true for that operate() call.
	/// </para>
	/// <para>Default: false</para>
	/// </summary>
	public bool RespondAllOps { get; }

	public bool Connected { get; }

	public void AddSeeds(IHost[] hosts);

	public void Run();

	public ClusterStats GetStats();

	public Node GetRandomNode();

	public Node[] ValidateNodes();

	public Node GetNode(string nodeName);
	
	public void PrintPartitionMap();

	public void ChangePassword(byte[] user, byte[] password, byte[] passwordHash);

	public void InterruptTendSleep();

	public void Close();

	public void ScheduleCommandExecution(AsyncCommand command);
	
	public void ReleaseBuffer(BufferSegment segment);
}

public class Cluster : ICluster
{
	public string Name { get; }
	
	public IClient Client { get; }

	public IEnumerable<INamespace> Namespaces { get; }

	public TlsPolicy TlsPolicy { get; }

	// Initial host nodes specified by user.
	public IHost[] Seeds { get; }

	// All host aliases for all nodes in cluster.
	// Only accessed within cluster tend thread.
	public INode[] Nodes { get; }

	public IDictionary<string, IPartition> PartitionMap { get; }

	public string Version { get; } // version of the software running

	public byte[] Username { get; }

	public byte[] Password { get; }
	
	public int UserCommandTimeout { get; }

	public int ConnectionTimeout { get; } // = 1000; how do we do defaults in the config manager?

	public int LoginTimeout { get; } // = 5000;
	
	public int InfoCommandTimeout { get; } // = 1000;
	
	public int MaxSocketIdle { get; }

	public int MaxErrorRate { get; } // = 100;

	public int ErrorRateWindow { get; } // = 1;

	public int TendInterval { get; } // = 1000;

	public bool FailIfNotConnected { get; } // = true;
	
	public MaxCommandAction MaxCommandAction { get; } // = MaxCommandAction.BLOCK;

	public int MaxCommands { get; } // configurable = 100;

	public int MaxCommandsInQueue { get; }

	public int MinConnsPerNode { get; }

	public int MaxConnsPerNode { get; }
	
	public int BufferSize { get; } // = 128 * 1024;

	public bool AllowInline { get; } // configurable

	public bool AllowInlineSSD { get; } // configurable

	public bool AllowProleReads { get; } // configruable

	public bool RespondAllKeys { get; } // configruable

	public CommitLevel CommitLevel { get; } // configruable

	public GenerationPolicy GenerationPolicy { get; }

	public bool DurableDelete { get; } // configurable

	public bool SendKey { get; } // configurable

	public ReadModeAP readModeAP { get; } // configurable

	public ReadModeSC readModeSC { get; } // configurable

	public int Expiration { get; } // configurable

	public int MaxConcurrentNodes { get; } // configurable

	public int RecordQueueSize { get; } // configurable

	public bool IncludeBinData { get; } // configurable

	public bool FailOnClusterChange { get; } // configurable

	public bool ShortQuery { get; } // configurable

	public long ScanMaxRecords { get; }

	public int ScanRecordsPerSecond { get; }

	public bool ScanConcurrentNodes { get; }

	public RecordExistsAction RecordExistsAction { get; }

	public bool RespondAllOps { get; }

	// Command scheduler.
	private AsyncScheduler Scheduler { get; }

	// Contiguous pool of byte buffers.
	private BufferPool BufferPool { get; }

	// Maximum socket idle to validate connections in transactions.
	private double maxSocketIdleMillisTran { get; }

	// Maximum socket idle to trim peak connections to min connections.
	private double maxSocketIdleMillisTrim { get; }

	// Minimum connections per node.
	public int minConnsPerNode { get; }

	// Maximum connections per node.
	public int maxConnsPerNode { get; }

	/// <summary>
	/// Return count of add node failures in the most recent cluster tend iteration.
	/// </summary>
	public int InvalidNodeCount { get; }

	public bool Connected
	{
		get
		{
			// Must copy array reference for copy on write semantics to work.
			Node[] nodeArray = nodes;

			if (nodeArray.Length > 0 && tendValid)
			{
				// Even though nodes exist, they may not be currently responding.  Check further.
				foreach (Node node in nodeArray)
				{
					// Mark connected if any node is active and cluster tend consecutive info request 
					// failures are less than 5.
					if (node.active && node.failures < 5)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public Cluster(string clusterName, IHost[] hosts, TlsPolicy tlsPolicy)
	{
		this.Name = (clusterName != null)? clusterName : "";
		this.TlsPolicy = tlsPolicy;
		this.Seeds = hosts;

		// process and store dynamic config values
		if (maxSocketIdle < 0)
		{
			throw new AerospikeException("Invalid maxSocketIdle: " + maxSocketIdle);
		}

		if (maxSocketIdle == 0)
		{
			maxSocketIdleMillisTran = 0.0;
			maxSocketIdleMillisTrim = 55000.0;
		}
		else
		{
			maxSocketIdleMillisTran = (double)(maxSocketIdle * 1000);
			maxSocketIdleMillisTrim = maxSocketIdleMillisTran;
		}

		MaxErrorRate = maxErrorRate;
		ErrorRateWindow = errorRateWindow;
		ConnectionTimeout = timeout;
		LoginTimeout = loginTimeout;
		TendInterval = tendInterval;
	}

	public void InitTendThread(bool failIfNotConnected);

	public void AddSeeds(IHost[] hosts);

	private bool FindSeed(IHost search);

	/// <summary>
	/// Tend the cluster until it has stabilized and return control.
	/// This helps avoid initial database request timeout issues when
	/// a large number of threads are initiated at client startup.
	/// </summary>
	private void WaitTillStabilized(bool failIfNotConnected);

	public void Run();

	internal bool IsConnCurrentTran(DateTime lastUsed);

	internal bool IsConnCurrentTrim(DateTime lastUsed);

	internal bool UseTls();

	/// <summary>
	/// Check health of all nodes in the cluster.
	/// </summary>
	private void Tend(bool failIfNotConnected, bool isInit);

	private bool SeedNode(Peers peers, bool failIfNotConnected);

	private void AddSeedAndPeers(Node seed, Peers peers);

	private void RefreshPeers(Peers peers);

	internal Node CreateNode(NodeValidator nv, bool createMinConn);

	private List<Node> FindNodesToRemove(int refreshCount);

	private bool FindNodeInPartitionMap(Node filter);

	private void AddNodes(Node seed, Peers peers);

	private void AddNodes(Dictionary<string, Node> nodesToAdd);

	private void AddNode(Node node);

	private void RemoveNodes(List<Node> nodesToRemove);

	/// <summary>
	/// Remove nodes using copy on write semantics.
	/// </summary>
	private void RemoveNodesCopy(List<Node> nodesToRemove);

	private static bool FindNode(Node search, List<Node> nodeList);

	internal bool IsConnCurrentTran(DateTime lastUsed);

	internal bool IsConnCurrentTrim(DateTime lastUsed);

	internal bool UseTls();

	public ClusterStats GetStats();

	public Node GetRandomNode();

	public Node[] ValidateNodes();

	public Node GetNode(string nodeName);

	private Node FindNode(string nodeName);

	public void PrintPartitionMap();

	public void ChangePassword(byte[] user, byte[] password, byte[] passwordHash);

	private static bool SupportsPartitionQuery(Node[] nodes);

	public void InterruptTendSleep();

	public void Close();

	public void ScheduleCommandExecution(AsyncCommand command);

	public void ReleaseBuffer(BufferSegment segment);
}

public interface IPartition
{
	public INode[][] Replicas { get; }

	public int[] Regimes { get; }

	public bool SCMode { get; }
}

/// <summary>
/// Concurrent bounded LIFO stack with ability to pop from head or tail.
/// <para>
/// The standard library concurrent stack, ConcurrentStack, does not
/// allow pop from both head and tail.
/// </para>
/// </summary>
public sealed class Pool<T>
{
	private readonly T[] items;
	private int head;
	private int tail;
	private int size;
	internal readonly int minSize;
	private volatile int total; // total items: inUse + inPool

	/// <summary>
	/// Construct stack pool.
	/// </summary>
	public Pool(int minSize, int maxSize)
	{
		this.minSize = minSize;
		items = new T[maxSize];
	}

	/// <summary>
	/// Insert item at head of stack.
	/// </summary>
	public bool Enqueue(T item)
	{
		Monitor.Enter(this);

		try
		{
			if (size == items.Length)
			{
				return false;
			}

			items[head] = item;

			if (++head == items.Length)
			{
				head = 0;
			}
			size++;
			return true;
		}
		finally
		{
			Monitor.Exit(this);
		}
	}

	/// <summary>
	/// Insert item at tail of stack.
	/// </summary>
	public bool EnqueueLast(T item)
	{
		Monitor.Enter(this);

		try
		{
			if (size == items.Length)
			{
				return false;
			}

			if (tail == 0)
			{
				tail = items.Length - 1;
			}
			else
			{
				tail--;
			}
			items[tail] = item;
			size++;
			return true;
		}
		finally
		{
			Monitor.Exit(this);
		}
	}

	/// <summary>
	/// Pop item from head of stack.
	/// </summary>
	public bool TryDequeue(out T item)
	{
		Monitor.Enter(this);

		try
		{
			if (size == 0)
			{
				item = default(T);
				return false;
			}

			if (head == 0)
			{
				head = items.Length - 1;
			}
			else
			{
				head--;
			}
			size--;

			item = items[head];
			items[head] = default(T);
			return true;
		}
		finally
		{
			Monitor.Exit(this);
		}
	}

	/// <summary>
	/// Pop item from tail of stack.
	/// </summary>
	public bool TryDequeueLast(out T item)
	{
		Monitor.Enter(this);

		try
		{
			if (size == 0)
			{
				item = default(T);
				return false;
			}
			item = items[tail];
			items[tail] = default(T);

			if (++tail == items.Length)
			{
				tail = 0;
			}
			size--;
			return true;
		}
		finally
		{
			Monitor.Exit(this);
		}
	}

	/// <summary>
	/// Return item count.
	/// </summary>
	public int Count
	{
		get
		{
			Monitor.Enter(this);

			try
			{
				return size;
			}
			finally
			{
				Monitor.Exit(this);
			}
		}
	}

	/// <summary>
	/// Return pool capacity.
	/// </summary>
	public int Capacity
	{
		get { return items.Length; }
	}

	/// <summary>
	/// Return number of connections that might be closed.
	/// </summary>
	public int Excess()
	{
		return total - minSize;
	}

	/// <summary>
	/// Increment total connections.
	/// </summary>
	public int IncrTotal()
	{
		return Interlocked.Increment(ref total);
	}

	/// <summary>
	/// Decrement total connections.
	/// </summary>
	public int DecrTotal()
	{
		return Interlocked.Decrement(ref total);
	}

	/// <summary>
	/// Return total connections.
	/// </summary>
	public int Total
	{
		get { return total; }
	}
}

/// <summary>
/// How to handle cases when the asynchronous maximum number of concurrent database commands have been exceeded.
/// </summary>
public enum MaxCommandAction
{
	/// <summary>
	/// Reject database command.
	/// </summary>
	REJECT,

	/// <summary>
	/// Block until a previous command completes. 
	/// </summary>
	BLOCK,

	/// <summary>
	/// Delay until a previous command completes.
	/// </summary>
	/// <remarks>This is the asynchronous equivalent of <see cref="BLOCK"/>.</remarks>
	DELAY,
}