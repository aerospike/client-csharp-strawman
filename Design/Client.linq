<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Cluster.linq"
#load "Policy.linq"
#load "Host.linq"

public interface IClient
{
	public ICluster Cluster { get; }

	/// <summary>
	/// A IP translation table is used in cases where different clients use different server 
	/// IP addresses.  This may be necessary when using clients from both inside and outside 
	/// a local area network.  Default is no translation.
	/// <para>
	/// The key is the IP address returned from friend info requests to other servers.  The 
	/// value is the real IP address used to connect to the server.
	/// </para>
	/// <para>Default: null (no IP address translation)</para>
	/// </summary>
	public Dictionary<IPAddress, IPAddress> IpMap { get; }

	/// <summary>
	/// Should use "services-alternate" instead of "services" in info request during cluster
	/// tending.  "services-alternate" returns server configured external IP addresses that client
	/// uses to talk to nodes.  "services-alternate" can be used in place of providing a client "ipMap".
	/// <para>Default: false (use original "services" info request)</para>
	/// </summary>
	public bool UseServicesAlternate { get; }

	/// <summary>
	/// Authentication mode.
	/// <para>Default: AuthMode.INTERNAL</para>
	/// </summary>
	public AuthModeType AuthMode { get; } //= AuthMode.INTERNAL;

	// Is authentication enabled
	public bool AuthEnabled { get; }

	/// <summary>
	/// Track server rack data.  This field is useful when directing read commands to the server node
	/// that contains the key and exists on the same rack as the client.  This serves to lower cloud
	/// provider costs when nodes are distributed across different racks/data centers.
	/// <para>
	/// <see cref="Aerospike.Client.ClientPolicy.rackId"/> or <see cref="Aerospike.Client.ClientPolicy.rackIds"/>, 
	/// <see cref="Aerospike.Client.Replica.PREFER_RACK"/> and server rack configuration must also be set to
	/// enable this functionality.
	/// </para>
	/// <para>Default: false</para>
	/// </summary>
	public bool RackAware { get; }

	/// <summary>
	/// Rack where this client instance resides. If <see cref="Aerospike.Client.ClientPolicy.rackIds"/> is set,
	/// rackId is ignored.
	/// <para>
	/// <see cref="Aerospike.Client.ClientPolicy.rackAware"/>, <see cref="Aerospike.Client.Replica.PREFER_RACK"/>
	/// and server rack configuration must also be set to enable this functionality.
	/// </para>
	/// <para>Default: 0</para>
	/// </summary>
	public int RackId { get; }

	/// <summary>
	/// List of acceptable racks in order of preference.
	/// If rackIds is set, <see cref="Aerospike.Client.ClientPolicy.rackId"/> is ignored.
	/// <para>
	/// <see cref="Aerospike.Client.ClientPolicy.rackAware"/>, <see cref="Aerospike.Client.Replica.PREFER_RACK"/>
	/// and server rack configuration must also be set to enable this functionality.
	/// </para>
	/// <para>Default: null</para>
	/// </summary>
	public List<int> RackIds { get; }


	// log level and context	
}

public class Client : IClient
{
	public ICluster Cluster { get; }
	
	public Dictionary<IPAddress, IPAddress> IpMap { get; }

	public bool UseServicesAlternate { get; }
	
	public AuthModeType AuthMode { get; } //= AuthMode.INTERNAL;
	
	public bool AuthEnabled { get; internal set; }

	public bool RackAware { get; }
	
	public int RackId { get; }
	
	public List<int> RackIds { get; }

	public Client(string clusterName, IHost[] hosts, TlsPolicy tlsPolicy, AuthModeType authMode)
	{
		this.AuthMode = authMode;

		// Default TLS names when TLS enabled.
		if (tlsPolicy != null)
		{
			bool useClusterName = clusterName != null && clusterName.Length > 0;

			for (int i = 0; i < hosts.Length; i++)
			{
				IHost host = hosts[i];

				if (host.TlsName == null)
				{
					string tlsName = useClusterName ? clusterName : host.Name;
					hosts[i] = new Host(host.Name, tlsName, host.IP, host.Port);
				}
			}
		}
		else
		{
			if (authMode == AuthModeType.EXTERNAL || authMode == AuthModeType.PKI)
			{
				throw new AerospikeException("TLS is required for authentication mode: " + authMode);
			}
		}

		// process and store dynamic config values
		IpMap = ipMap;
		UseServicesAlternate = useServicesAlternate;
		RackAware = rackAware;

		if (RackIds != null && rackIds.Count > 0)
		{
			RackIds = rackIds.ToArray();
		}
		else
		{
			RackIds = new int[] { rackId };
		}

		// TODO username and password is at connection level
		if (authMode == AuthModeType.PKI) 
		{
			this.AuthEnabled = true;
			this.user = null;
		}
		else if (policy.user != null && policy.user.Length > 0)
		{
			this.AuthEnabled = true;
			this.user = ByteUtil.StringToUtf8(policy.user);

			// Only store clear text password if external authentication is used.
			if (authMode != AuthModeType.INTERNAL)
			{
				this.password = ByteUtil.StringToUtf8(policy.password);
			}

			string pass = policy.password;

			if (pass == null)
			{
				pass = "";
			}

			pass = AdminCommand.HashPassword(pass);
			this.passwordHash = ByteUtil.StringToUtf8(pass);
		}
		else
		{
			this.authEnabled = false;
			this.user = null;
		}
		
		this.Cluster = new Cluster(clusterName, hosts, tlsPolicy);
	}
}

/// <summary>
/// Authentication mode.
/// </summary>
public enum AuthModeType
{
	/// <summary>
	/// Use internal authentication when user/password defined. Hashed password is stored
	/// on the server. Do not send clear password. This is the default.
	/// </summary>
	INTERNAL,

	/// <summary>
	/// Use external authentication (like LDAP) when user/password defined. Specific external
	/// authentication is configured on server. If TLS defined, send clear password on node
	/// login via TLS. Throw exception if TLS is not defined.
	/// </summary>
	EXTERNAL,

	/// <summary>
	/// Use external authentication (like LDAP) when user/password defined. Specific external
	/// authentication is configured on server.  Send clear password on node login whether or
	/// not TLS is defined. This mode should only be used for testing purposes because it is
	/// not secure authentication.
	/// </summary>
	EXTERNAL_INSECURE,

	/// <summary>
	/// Authentication and authorization based on a certificate.  No user name or
	/// password needs to be configured. Requires TLS and a client certificate.
	/// Requires server version 5.7.0+
	/// </summary>
	PKI
}

