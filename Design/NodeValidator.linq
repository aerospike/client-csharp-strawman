<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Node.linq"
#load "Host.linq"
#load "Connection.linq"
#load "Cluster.linq"
#load "Peers.linq"

public sealed class NodeValidator
{
	internal Node Fallback { get; set; }
	internal string Name { get; set; }
	internal List<IHost> Aliases { get; set; }
	internal Host PrimaryHost { get; set; }
	internal IPEndPoint PrimaryAddress { get; set; }
	internal Connection PrimaryConn { get; set; }
	internal byte[] SessionToken { get; set; }
	internal DateTime? SessionExpiration { get; set; }
	internal uint Features{ get; set; }

	/// <summary>
	/// Return first valid node referenced by seed host aliases. In most cases, aliases
	/// reference a single node.  If round robin DNS configuration is used, the seed host
	/// may have several addresses that reference different nodes in the cluster.
	/// </summary>
	public Node SeedNode(Cluster cluster, Host host, Peers peers)
	{
		Name = null;
		Aliases = null;
		PrimaryHost = null;
		PrimaryAddress = null;
		PrimaryConn = null;
		SessionToken = null;
		SessionExpiration = null;
		Features = 0;

		IPAddress[] addresses = Connection.GetHostAddresses(host.Name, cluster.ConnectionTimeout);
		Exception exception = null;

		// Try all addresses because they might point to different nodes.
		foreach (IPAddress address in addresses)
		{
			try
			{
				ValidateAddress(cluster, address, host.TlsName, host.Port, true);

				// Only set aliases when they were not set by load balancer detection logic.
				if (this.Aliases == null)
				{
					SetAliases(address, host.TlsName, host.Port);
				}

				Node node = cluster.CreateNode(this, false);

				if (ValidatePeers(peers, node))
				{
					return node;
				}
			}
			catch (Exception e)
			{
				// Log exception and continue to next alias.
				if (Log.DebugEnabled())
				{
					Log.Debug(cluster.Context, "Address " + address + ' ' + host.Port + " failed: " + Util.GetErrorMessage(e));
				}

				if (exception == null)
				{
					exception = e;
				}
			}
		}

		// Fallback signifies node exists, but is suspect.
		// Return null so other seeds can be tried.
		if (Fallback != null)
		{
			return null;
		}

		// Exception can't be null here because Connection.GetHostAddresses()
		// will throw exception if aliases length is zero.
		throw exception;
	}

	private bool ValidatePeers(Peers peers, Node node)
	{
		try
		{
			peers.refreshCount = 0;
			node.RefreshPeers(peers);
		}
		catch (Exception)
		{
			node.Close();
			throw;
		}

		if (node.PeersCount == 0)
		{
			// Node is suspect because multiple seeds are used and node does not have any peers.
			if (Fallback == null)
			{
				Fallback = node;
			}
			else
			{
				node.Close();
			}
			return false;
		}

		// Node is valid. Drop fallback if it exists.
		if (Fallback != null)
		{
			if (Log.InfoEnabled())
			{
				Log.Info(node.Cluster.Context, "Skip orphan node: " + Fallback);
			}
			Fallback.Close();
			Fallback = null;
		}
		return true;
	}

	/// <summary>
	/// Verify that a host alias references a valid node.
	/// </summary>
	public void ValidateNode(Cluster cluster, Host host)
	{
		IPAddress[] addresses = Connection.GetHostAddresses(host.Name, cluster.ConnectionTimeout);
		Exception exception = null;

		foreach (IPAddress address in addresses)
		{
			try
			{
				ValidateAddress(cluster, address, host.TlsName, host.Port, false);
				SetAliases(address, host.TlsName, host.Port);
				return;
			}
			catch (Exception e)
			{
				// Log exception and continue to next alias.
				if (Log.DebugEnabled())
				{
					Log.Debug(cluster.Context, "Address " + address + ' ' + host.Port + " failed: " + Util.GetErrorMessage(e));
				}

				if (exception == null)
				{
					exception = e;
				}
			}
		}

		// Exception can't be null here because Connection.GetHostAddresses()
		// will throw exception if aliases length is zero.
		throw exception;
	}

	private void ValidateAddress(Cluster cluster, IPAddress address, string tlsName, int port, bool detectLoadBalancer)
	{
		IPEndPoint socketAddress = new IPEndPoint(address, port);
		Connection conn = (cluster.TlsPolicy != null) ?
			new TlsConnection(cluster, tlsName, socketAddress, cluster.ConnectionTimeout, null) :
			new Connection(socketAddress, cluster.ConnectionTimeout, null);

		try
		{
			if (cluster.AuthEnabled)
			{
				// Login
				AdminCommand admin = new AdminCommand(ThreadLocalData.GetBuffer(), 0);
				admin.Login(cluster, conn, out SessionToken, out SessionExpiration);

				if (cluster.TlsPolicy != null && cluster.TlsPolicy.forLoginOnly)
				{
					// Switch to using non-TLS socket.
					SwitchClear sc = new SwitchClear(cluster, conn, SessionToken);
					conn.Close();
					address = sc.clearAddress;
					socketAddress = sc.clearSocketAddress;
					conn = sc.clearConn;

					// Disable load balancer detection since non-TLS address has already
					// been retrieved via service info command.
					detectLoadBalancer = false;
				}
			}

			List<string> commands = new List<string>(5);
			commands.Add("node");
			commands.Add("partition-generation");
			commands.Add("features");

			bool hasClusterName = cluster.HasClusterName;

			if (hasClusterName)
			{
				commands.Add("cluster-name");
			}

			string addressCommand = null;

			if (detectLoadBalancer)
			{
				// Seed may be load balancer with changing address. Determine real address.
				addressCommand = (cluster.TlsPolicy != null) ?
					cluster.UseServicesAlternate ? "service-tls-alt" : "service-tls-std" :
					cluster.UseServicesAlternate ? "service-clear-alt" : "service-clear-std";

				commands.Add(addressCommand);
			}

			// Issue commands.
			Dictionary<string, string> map = Info.Request(conn, commands);

			// Node returned results.
			this.PrimaryHost = new Host(address.ToString(), tlsName, port);
			this.PrimaryAddress = socketAddress;
			this.PrimaryConn = conn;

			ValidateNode(map);
			ValidatePartitionGeneration(map);
			SetFeatures(map);

			if (hasClusterName)
			{
				ValidateClusterName(cluster, map);
			}

			if (addressCommand != null)
			{
				SetAddress(cluster, map, addressCommand, tlsName);
			}
		}
		catch (Exception)
		{
			conn.Close();
			throw;
		}
	}

	private void ValidateNode(Dictionary<string, string> map)
	{
		if (!map.TryGetValue("node", out this.Name))
		{
			throw new AerospikeException.InvalidNode("Node name is null");
		}
	}

	private void ValidatePartitionGeneration(Dictionary<string, string> map)
	{
		string genString;
		int gen;

		if (!map.TryGetValue("partition-generation", out genString))
		{
			throw new AerospikeException.InvalidNode("Node " + this.Name + ' ' + this.PrimaryHost + " did not return partition-generation");
		}

		try
		{
			gen = Convert.ToInt32(genString);
		}
		catch (Exception)
		{
			throw new AerospikeException.InvalidNode("Node " + this.Name + ' ' + this.PrimaryHost + " returned invalid partition-generation: " + genString);
		}

		if (gen == -1)
		{
			throw new AerospikeException.InvalidNode("Node " + this.Name + ' ' + this.PrimaryHost + " is not yet fully initialized");
		}
	}

	private void SetFeatures(Dictionary<string, string> map)
	{
		try
		{
			string featuresString = map["features"];
			string[] list = featuresString.Split(';');

			foreach (string feature in list)
			{
				if (feature.Equals("pscans"))
				{
					this.Features |= Node.HAS_PARTITION_SCAN;
				}
				else if (feature.Equals("query-show"))
				{
					this.Features |= Node.HAS_QUERY_SHOW;
				}
				else if (feature.Equals("batch-any"))
				{
					this.Features |= Node.HAS_BATCH_ANY;
				}
				else if (feature.Equals("pquery"))
				{
					this.Features |= Node.HAS_PARTITION_QUERY;
				}
			}
		}
		catch (Exception)
		{
			// Unexpected exception. Use defaults.
		}

		// This client requires partition scan support. Partition scans were first
		// supported in server version 4.9. Do not allow any server node into the
		// cluster that is running server version < 4.9.
		if ((this.Features & Node.HAS_PARTITION_SCAN) == 0)
		{
			throw new AerospikeException("Node " + this.name + ' ' + this.PrimaryHost +
				" version < 4.9. This client requires server version >= 4.9");
		}
	}

	private void ValidateClusterName(Cluster cluster, Dictionary<string, string> map)
	{
		string id;

		if (!map.TryGetValue("cluster-name", out id) || !cluster.Name.Equals(id))
		{
			throw new AerospikeException.InvalidNode("Node " + this.Name + ' ' + this.PrimaryHost + ' ' +
					" expected cluster name '" + cluster.Name + "' received '" + id + "'");
		}
	}

	private void SetAddress(Cluster cluster, Dictionary<string, string> map, string addressCommand, string tlsName)
	{
		string result;

		if (!map.TryGetValue(addressCommand, out result) || result == null || result.Length == 0)
		{
			// Server does not support service level call (service-clear-std, ...).
			// Load balancer detection is not possible.
			return;
		}

		List<Host> hosts = Host.ParseServiceHosts(result);
		Host h;

		// Search real hosts for seed.
		foreach (Host host in hosts)
		{
			h = host;

			string alt;
			if (cluster.Client.IpMap != null && cluster.Client.IpMap.TryGetValue(h.Name, out alt))
			{
				h = new Host(alt, h.Port);
			}

			if (h.Equals(this.PrimaryHost))
			{
				// Found seed which is not a load balancer.
				return;
			}
		}

		// Seed not found, so seed is probably a load balancer.
		// Find first valid real host.
		foreach (Host host in hosts)
		{
			try
			{
				h = host;

				string alt;
				if (cluster.Client.IpMap != null && cluster.Client.IpMap.TryGetValue(h.Name, out alt))
				{
					h = new Host(alt, h.Port);
				}

				IPAddress[] addresses = Connection.GetHostAddresses(h.Name, cluster.ConnectionTimeout);

				foreach (IPAddress address in addresses)
				{
					try
					{
						IPEndPoint socketAddress = new IPEndPoint(address, h.Port);
						Connection conn = (cluster.TlsPolicy != null) ?
							new TlsConnection(cluster, tlsName, socketAddress, cluster.ConnectionTimeout, null) :
							new Connection(socketAddress, cluster.ConnectionTimeout, null);

						try
						{
							if (this.SessionToken != null)
							{
								if (!AdminCommand.Authenticate(cluster, conn, this.SessionToken))
								{
									throw new AerospikeException("Authentication failed");
								}
							}

							// Authenticated connection.  Set real host.
							SetAliases(address, tlsName, h.Port);
							this.PrimaryHost = new Host(address.ToString(), tlsName, h.Port);
							this.PrimaryAddress = socketAddress;
							this.PrimaryConn.Close();
							this.PrimaryConn = conn;
							return;
						}
						catch (Exception)
						{
							conn.Close();
						}
					}
					catch (Exception)
					{
						// Try next address.
					}
				}
			}
			catch (Exception)
			{
				// Try next host.
			}
		}

		// Failed to find a valid address. IP Address is probably internal on the cloud
		// because the server access-address is not configured.  Log warning and continue
		// with original seed.
		if (Log.InfoEnabled())
		{
			Log.Info(cluster.Context, "Invalid address " + result + ". access-address is probably not configured on server.");
		}
	}

	private void SetAliases(IPAddress address, string tlsName, int port)
	{
		// Add capacity for current address plus IPV6 address and hostname.
		this.Aliases = new List<IHost>(3);
		this.Aliases.Add(new Host(address.ToString(), tlsName, port));
	}
}

sealed class SwitchClear
{
	internal IPAddress clearAddress;
	internal IPEndPoint clearSocketAddress;
	internal Connection clearConn;

	// Switch from TLS connection to non-TLS connection.
	internal SwitchClear(Cluster cluster, Connection conn, byte[] sessionToken)
	{
		// Obtain non-TLS addresses.
		string command = cluster.UseServicesAlternate ? "service-clear-alt" : "service-clear-std";
		string result = Info.Request(conn, command);
		List<Host> hosts = Host.ParseServiceHosts(result);
		Host clearHost;

		// Find first valid non-TLS host.
		foreach (Host host in hosts)
		{
			try
			{
				clearHost = host;

				string alternativeHost;
				if (cluster.Client.IpMap != null && cluster.Client.IpMap.TryGetValue(clearHost.Name, out alternativeHost))
				{
					clearHost = new Host(alternativeHost, clearhost.Port);
				}

				IPAddress[] addresses = Connection.GetHostAddresses(clearHost.Name, cluster.ConnectionTimeout);

				foreach (IPAddress ia in addresses)
				{
					try
					{
						clearAddress = ia;
						clearSocketAddress = new IPEndPoint(ia, clearhost.Port);
						clearConn = new Connection(clearSocketAddress, cluster.ConnectionTimeout, null);

						try
						{
							if (sessionToken != null)
							{
								if (!AdminCommand.Authenticate(cluster, clearConn, sessionToken))
								{
									throw new AerospikeException("Authentication failed");
								}
							}
							return; // Authenticated clear connection.
						}
						catch (Exception)
						{
							clearConn.Close();
						}
					}
					catch (Exception)
					{
						// Try next address.
					}
				}
			}
			catch (Exception)
			{
				// Try next host.
			}
		}
		throw new AerospikeException("Invalid non-TLS address: " + result);
	}
}