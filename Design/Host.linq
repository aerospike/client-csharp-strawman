<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

#load "Cluster.linq"

public interface IHost
{
	public string Name { get; }
	
	public string TlsName { get; }
	
	public int Port { get; }
	
	public IPAddress IP { get; }
}

public class Host : IHost
{
	public string Name { get; }

	public string TlsName { get; }

	public int Port { get; }

	public IPAddress IP { get; }

	public Host(string name, IPAddress ip, int port)
	{
		this.Name = name;
		this.IP = ip;
		this.Port = port;
	}

	/// <summary>
	/// Initialize host.
	/// </summary>
	public Host(string name, string tlsName, IPAddress ip, int port)
	{
		this.Name = name;
		this.TlsName = tlsName;
		this.IP = ip;
		this.Port = port;
	}

	/// <summary>
	/// Parse hosts from string format: hostname1[:tlsname1][:port1],...
	/// <para>
	/// Hostname may also be an IP address in the following formats.
	/// </para>
	/// <ul>
	/// <li>IPv4: xxx.xxx.xxx.xxx</li>
	/// <li>IPv6: [xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx]</li>
	/// <li>IPv6: [xxxx::xxxx]</li>
	/// </ul>
	/// <para>
	/// IPv6 addresses must be enclosed by brackets.
	/// tlsname and port are optional.
	/// </para>
	/// </summary>
	public static IHost[] ParseHosts(string str, string defaultTlsName, int defaultPort);

	/// <summary>
	/// Parse server service hosts from string format: hostname1:port1,...
	/// <para>
	/// Hostname may also be an IP address in the following formats.
	/// <ul>
	/// <li>IPv4: xxx.xxx.xxx.xxx</li>
	/// <li>IPv6: [xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx]</li>
	/// <li>IPv6: [xxxx::xxxx]</li>
	/// </ul>
	/// IPv6 addresses must be enclosed by brackets.
	/// </para>
	/// </summary>
	public static List<IHost> ParseServiceHosts(string str);
}
