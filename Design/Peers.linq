<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Node.linq"
#load "Host.linq"

public sealed class Peers
{
	public List<Peer> Peers { get; private set; }
	public Dictionary<string, INode> Nodes { get; }
	private HashSet<IHost> InvalidHosts { get; }
	public int RefreshCount { get; }
	public bool GenChanged { get; }

	public Peers(int peerCapacity)
	{
		Peers = new List<Peer>(peerCapacity);
		Nodes = new Dictionary<string, INode>();
		InvalidHosts = new HashSet<IHost>();
	}

	public bool HasFailed(IHost host)
	{
		return InvalidHosts.Contains(host);
	}

	public void Fail(IHost host)
	{
		InvalidHosts.Add(host);
	}

	public int InvalidCount
	{
		get { return InvalidHosts.Count; }
	}

	public void ClusterInitError()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("Peers not reachable: ");

		bool comma = false;

		foreach (IHost host in InvalidHosts)
		{
			if (comma)
			{
				sb.Append(", ");
			}
			else
			{
				comma = true;
			}
			sb.Append(host);
		}
		throw new AerospikeException(sb.ToString());
	}
}

public sealed class Peer
{
	internal String NodeName { get; set; }
	internal String TlsName { get; set; }
	internal List<IHost> Hosts { get; set; }
}