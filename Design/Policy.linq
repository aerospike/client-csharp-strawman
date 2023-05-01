<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Security.Authentication</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

/// <summary>
/// TLS connection policy.
/// Secure connections are only supported for AerospikeClient synchronous commands.
/// <para>
/// Secure connections are not supported for asynchronous commands because AsyncClient 
/// uses the best performing SocketAsyncEventArgs.  Unfortunately, SocketAsyncEventArgs is
/// not supported by the provided SslStream.
/// </para>
/// </summary>
public class TlsPolicy 
{
	/// <summary>
	/// Allowable TLS protocols that the client can use for secure connections.
	/// Multiple protocols can be specified.  Example:
	/// <code>
	/// TlsPolicy policy = new TlsPolicy();
	/// policy.protocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
	/// </code>
	/// Default: SslProtocols.Tls12 
	/// </summary>
	public SslProtocols Protocols { get; set; }

	/// <summary>
	/// Reject server certificates whose serial numbers match a serial number in this array.
	/// <para>Default: null (Do not exclude by certificate serial number)</para>
	/// </summary>
	public byte[][] RevokeCertificates { get; set; }

	/// <summary>
	/// Client certificates to pass to server when server requires mutual authentication.
	/// <para>Default: null (Client authenticates server, but server does not authenticate client)</para>
	/// </summary>
	public X509CertificateCollection ClientCertificates { get; set; }

	/// <summary>
	/// Use TLS connections only for login authentication.  All other communication with
	/// the server will be done with non-TLS connections. 
	/// <para>Default: false (Use TLS connections for all communication with server)</para>
	/// </summary>
	public bool ForLoginOnly { get; }

	/// <summary>
	/// Copy constructor.
	/// </summary>
	public TlsPolicy(TlsPolicy other)
	{
		this.Protocols = other.Protocols;
		this.RevokeCertificates = other.RevokeCertificates;
		this.ClientCertificates = other.ClientCertificates;
		this.ForLoginOnly = other.ForLoginOnly;
	}

	/// <summary>
	/// Constructor for TLS properties.
	/// </summary>
	public TlsPolicy(string protocolString, string revokeString, string clientCertificateFile, bool forLoginOnly)
	{
		ParseSslProtocols(protocolString);
		ParseRevokeString(revokeString);
		ParseClientCertificateFile(clientCertificateFile);
		this.ForLoginOnly = forLoginOnly;
	}

	private void ParseSslProtocols(string protocolString)
	{
		if (protocolString == null)
		{
			return;
		}

		protocolString = protocolString.Trim();

		if (protocolString.Length == 0)
		{
			return;
		}

		Protocols = SslProtocols.None;
		string[] list = protocolString.Split(',');

		foreach (string item in list)
		{
			string s = item.Trim();

			if (s.Length > 0)
			{
				Protocols |= (SslProtocols)Enum.Parse(typeof(SslProtocols), s);
			}
		}
	}

	private void ParseRevokeString(string revokeString)
	{
		if (revokeString == null)
		{
			return;
		}

		revokeString = revokeString.Trim();

		if (revokeString.Length == 0)
		{
			return;
		}

		RevokeCertificates = Util.HexStringToByteArrays(revokeString);
	}

	private void ParseClientCertificateFile(string clientCertificateFile)
	{
		if (clientCertificateFile == null)
		{
			return;
		}

		clientCertificateFile = clientCertificateFile.Trim();

		if (clientCertificateFile.Length == 0)
		{
			return;
		}

		X509Certificate2 cert = new X509Certificate2(clientCertificateFile);
		ClientCertificates = new X509CertificateCollection();
		ClientCertificates.Add(cert);
	}
}