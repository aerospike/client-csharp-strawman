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
	
	public TlsPolicy TlsPolicy { get; } // configurable
	
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

	public IPolicy Policy { get; } // placeholder for Policy class

	public bool Connected { get; }

	public void AddSeeds(IHost[] hosts);

	public void Run();

	public ClusterStats GetStats();

	public INode GetRandomNode();

	public INode[] ValidateNodes();

	public INode GetNode(string nodeName);
	
	public void PrintPartitionMap();

	public void ChangePassword(byte[] user, byte[] password, byte[] passwordHash);

	public void InterruptTendSleep();

	public void Close();

	public void ScheduleCommandExecution(ICommand command);
	
	public void ReleaseBuffer(BufferSegment segment);
}

public class Cluster : ICluster
{
	public string Name { get; }
	
	public IClient Client { get; }

	public IEnumerable<INamespace> Namespaces { get; }

	public TlsPolicy TlsPolicy { get; }
	
	public IPolicy Policy { get; } // placeholder for Policy class

	// Initial host nodes specified by user.
	public IHost[] Seeds { get; }

	// All host aliases for all nodes in cluster.
	// Only accessed within cluster tend thread.
	public INode[] Nodes { get; }

	public IDictionary<string, IPartition> PartitionMap { get; }

	public string Version { get; } // version of the software running

	public byte[] Username { get; }

	public byte[] Password { get; }

	// Command scheduler.
	private AsyncScheduler Scheduler { get; }

	/// <summary>
	/// Return count of add node failures in the most recent cluster tend iteration.
	/// </summary>
	public int InvalidNodeCount { get; }

	public bool Connected { get; }

	public Cluster(string clusterName, IHost[] hosts, TlsPolicy tlsPolicy)
	{
		this.Name = (clusterName != null)? clusterName : "";
		this.TlsPolicy = tlsPolicy;
		this.Seeds = hosts;
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

	public void ScheduleCommandExecution(ICommand command);

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
	Reject,

	/// <summary>
	/// Block until a previous command completes. 
	/// </summary>
	Block,

	/// <summary>
	/// Delay until a previous command completes.
	/// </summary>
	/// <remarks>This is the asynchronous equivalent of <see cref="BLOCK"/>.</remarks>
	Delay,
}