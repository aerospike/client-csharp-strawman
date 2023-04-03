<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
</Query>

#load "Namespace.linq"

public interface ICluster
{
	public string Name { get; }
	
	public IEnumerable<INamespace> Namespaces { get; }
	
	public IConnection Connection { get; }
	
	public IHost[] Seeds { get; }
	
	public INode[] Nodes { get; }
	
	// partition map. we need feedback from server if something bad is happening
	// 
}

public interface IConnection
{
	public Socket Socket { get; }
	
	// username and password. should we have a user class?
	// password should be coming back masked. should be in constructor of concrete class
	// LDAP info
	
	// SSL, TLS stuff
}

public interface IHost
{
	public string Name { get; }
	
	public string TlsName { get; }
	
	public int Port { get; }
	
	public IPAddress IP { get; }
}

public interface INode
{
	public string Name { get; }
	
	public ICluster Cluster { get; }
	
	public IHost Host { get; }
	
	public IPEndPoint Address { get; }
	
	public NodeStates State { get; } // nodeStates is enum
	
	// partition 
	
	// how do we discover what paritions are in the cluster?
	
	// Look at Info.Request() in Aerospike documentation. may need to hunt through existing code to find stuff
	
	// route to multiple interfaces
}

public interface IClient
{
	public ICluster Cluster { get; }
}

public interface IPolicy
{
	
}
// filter will not be on policy class
// talk to Tim about this, also get input from Meher
// look at each policy and determine if stateful and stateless
// include method to give read out on full set of policies