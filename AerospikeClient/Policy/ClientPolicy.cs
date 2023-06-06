/* 
 * Copyright 2012-2022 Aerospike, Inc.
 *
 * Portions may be licensed to Aerospike, Inc. under one or more contributor
 * license agreements.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */
using System.Collections.Generic;

namespace Aerospike.Client
{
	/// <summary>
	/// Client initialization arguments.
	/// </summary>
	public class ClientPolicy
	{
		/// <summary>
		/// User authentication to cluster.  Leave null for clusters running without restricted access.
		/// <para>Default: null</para>
		/// </summary>
		public string user;

		/// <summary>
		/// Password authentication to cluster.  The password will be stored by the client and sent to server
		/// in hashed format.  Leave null for clusters running without restricted access.
		/// <para>Default: null</para>
		/// </summary>
		public string password;

		/// <summary>
		/// Expected cluster name.  If populated, the clusterName must match the cluster-name field
		/// in the service section in each server configuration.  This ensures that the specified
		/// seed nodes belong to the expected cluster on startup.  If not, the client will refuse
		/// to add the node to the client's view of the cluster.
		/// <para>Default: null</para>
		/// </summary>
		public string clusterName;

		/// <summary>
		/// Authentication mode.
		/// <para>Default: AuthMode.INTERNAL</para>
		/// </summary>
		public AuthMode authMode = AuthMode.INTERNAL;

		/// <summary>
		/// Initial host connection timeout in milliseconds.  The timeout when opening a connection 
		/// to the server host for the first time.
		/// <para>Default: 1000</para>
		/// </summary>
		public int timeout = 1000;

		/// <summary>
		/// Login timeout in milliseconds.  The timeout used when user authentication is enabled and
		/// a node login is being performed.
		/// <para>Default: 5000</para>
		/// </summary>
		public int loginTimeout = 5000;

		/// <summary>
		/// How to handle cases when the asynchronous maximum number of concurrent connections 
		/// have been reached.  
		/// </summary>
		public MaxCommandAction maxCommandAction = MaxCommandAction.BLOCK;

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
		public int maxCommands = 100;

		/// <summary>
		/// Maximum number of async commands that can be stored in the delay queue when
		/// <see cref="maxCommandAction"/> is <see cref="Aerospike.Client.MaxCommandAction.DELAY"/>
		/// and <see cref="maxCommands"/> is reached.
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
		public int maxCommandsInQueue;

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
		public int minConnsPerNode;

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
		public int maxConnsPerNode = -1;

		/// <summary>
		/// Number of synchronous connection pools used for each node.  Machines with 8 cpu cores or
		/// less usually need just one connection pool per node.  Machines with a large number of cpu
		/// cores may have their synchronous performance limited by contention for pooled connections.
		/// Contention for pooled connections can be reduced by creating multiple mini connection pools
		/// per node.
		/// <para>Default: 1</para>
		/// </summary>
		public int connPoolsPerNode = 1;

		/// <summary>
		/// Size of buffer allocated for each async command. The size should be a multiple of 8 KB.
		/// If not, the size is rounded up to the nearest 8 KB increment.
		/// <para>
		/// If an async command requires a buffer size less than or equal to asyncBufferSize, the
		/// buffer pool will be used. If an async command requires a buffer size greater than
		/// bufferSize, a new single-use buffer will be created on the heap.
		/// </para>
		/// <para>
		/// This field is also used to size the buffer pool for all async commands:
		/// </para>
		/// <code>
		/// buffer pool size = bufferSize * maxCommands
		/// </code> 
		/// <para>
		/// Default: 128 * 1024 (128 KB)
		/// </para>
		/// </summary>
		public int bufferSize = 128 * 1024;

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
		public int maxSocketIdle;

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
		public int maxErrorRate = 100;

		/// <summary>
		/// The number of cluster tend iterations that defines the window for <see cref="maxErrorRate"/>.
		/// One tend iteration is defined as <see cref="tendInterval"/> plus the time to tend all nodes.
		/// At the end of the window, the error count is reset to zero and backoff state is removed
		/// on all nodes.
		/// <para>
		/// Default: 1
		/// </para>
		/// </summary>
		public int errorRateWindow = 1;

		/// <summary>
		/// Interval in milliseconds between cluster tends by maintenance thread.
		/// <para>Default: 1000</para>
		/// </summary>
		public int tendInterval = 1000;

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
		public bool failIfNotConnected = true;

		/// <summary>
		/// Default read policy that is used when read command's policy is null.
		/// </summary>
		public Policy readPolicyDefault = new Policy();

		/// <summary>
		/// Default write policy that is used when write command's policy is null.
		/// </summary>
		public WritePolicy writePolicyDefault = new WritePolicy();

		/// <summary>
		/// Default scan policy that is used when scan command's policy is null.
		/// </summary>
		public ScanPolicy scanPolicyDefault = new ScanPolicy();

		/// <summary>
		/// Default query policy that is used when query command's policy is null.
		/// </summary>
		public QueryPolicy queryPolicyDefault = new QueryPolicy();

		/// <summary>
		///  Default parent policy used in batch read commands. Parent policy fields
		///  include socketTimeout, totalTimeout, maxRetries, etc...
		/// </summary>
		public BatchPolicy batchPolicyDefault = BatchPolicy.ReadDefault();

		/// <summary>
		/// Default parent policy used in batch write commands. Parent policy fields
		/// include socketTimeout, totalTimeout, maxRetries, etc...
		/// </summary>
		public BatchPolicy batchParentPolicyWriteDefault = BatchPolicy.WriteDefault();

		/// <summary>
		/// Default write policy used in batch operate commands.
		/// Write policy fields include generation, expiration, durableDelete, etc...
		/// </summary>
		public BatchWritePolicy batchWritePolicyDefault = new BatchWritePolicy();

		/// <summary>
		/// Default delete policy used in batch delete commands.
		/// </summary>
		public BatchDeletePolicy batchDeletePolicyDefault = new BatchDeletePolicy();

		/// <summary>
		/// Default user defined function policy used in batch UDF excecute commands.
		/// </summary>
		public BatchUDFPolicy batchUDFPolicyDefault = new BatchUDFPolicy();
		
		/// <summary>
		/// Default info policy that is used when info command's policy is null.
		/// </summary>
		public InfoPolicy infoPolicyDefault = new InfoPolicy();

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
		public TlsPolicy tlsPolicy;

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
		public Dictionary<string, string> ipMap;

		/// <summary>
		/// Should use "services-alternate" instead of "services" in info request during cluster
		/// tending.  "services-alternate" returns server configured external IP addresses that client
		/// uses to talk to nodes.  "services-alternate" can be used in place of providing a client "ipMap".
		/// <para>Default: false (use original "services" info request)</para>
		/// </summary>
		public bool useServicesAlternate;

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
		public bool rackAware;

		/// <summary>
		/// Rack where this client instance resides. If <see cref="Aerospike.Client.ClientPolicy.rackIds"/> is set,
		/// rackId is ignored.
		/// <para>
		/// <see cref="Aerospike.Client.ClientPolicy.rackAware"/>, <see cref="Aerospike.Client.Replica.PREFER_RACK"/>
		/// and server rack configuration must also be set to enable this functionality.
		/// </para>
		/// <para>Default: 0</para>
		/// </summary>
		public int rackId;

		/// <summary>
		/// List of acceptable racks in order of preference.
		/// If rackIds is set, <see cref="Aerospike.Client.ClientPolicy.rackId"/> is ignored.
		/// <para>
		/// <see cref="Aerospike.Client.ClientPolicy.rackAware"/>, <see cref="Aerospike.Client.Replica.PREFER_RACK"/>
		/// and server rack configuration must also be set to enable this functionality.
		/// </para>
		/// <para>Default: null</para>
		/// </summary>
		public List<int> rackIds;
		
		/// <summary>
		/// Copy client policy from another client policy.
		/// </summary>
		public ClientPolicy(ClientPolicy other)
		{
			this.user = other.user;
			this.password = other.password;
			this.clusterName = other.clusterName;
			this.authMode = other.authMode;
			this.timeout = other.timeout;
			this.loginTimeout = other.loginTimeout;
			this.minConnsPerNode = other.minConnsPerNode;
			this.maxConnsPerNode = other.maxConnsPerNode;
			this.maxSocketIdle = other.maxSocketIdle;
			this.maxCommandAction = other.maxCommandAction;
			this.maxCommands = other.maxCommands;
			this.maxCommandsInQueue = other.maxCommandsInQueue;
			this.minConnsPerNode = other.minConnsPerNode;
			this.maxConnsPerNode = other.maxConnsPerNode;
			this.bufferSize = other.bufferSize;
			this.maxErrorRate = other.maxErrorRate;
			this.errorRateWindow = other.errorRateWindow;
			this.tendInterval = other.tendInterval;
			this.failIfNotConnected = other.failIfNotConnected;
			this.readPolicyDefault = new Policy(other.readPolicyDefault);
			this.writePolicyDefault = new WritePolicy(other.writePolicyDefault);
			this.scanPolicyDefault = new ScanPolicy(other.scanPolicyDefault);
			this.queryPolicyDefault = new QueryPolicy(other.queryPolicyDefault);
			this.batchPolicyDefault = new BatchPolicy(other.batchPolicyDefault);
			this.batchParentPolicyWriteDefault = new BatchPolicy(other.batchParentPolicyWriteDefault);
			this.batchWritePolicyDefault = new BatchWritePolicy(other.batchWritePolicyDefault);
			this.batchDeletePolicyDefault = new BatchDeletePolicy(other.batchDeletePolicyDefault);
			this.batchUDFPolicyDefault = new BatchUDFPolicy(other.batchUDFPolicyDefault);
			this.infoPolicyDefault = new InfoPolicy(other.infoPolicyDefault);
			this.tlsPolicy = (other.tlsPolicy != null) ? new TlsPolicy(other.tlsPolicy) : null;
			this.ipMap = other.ipMap;
			this.useServicesAlternate = other.useServicesAlternate;
			this.rackAware = other.rackAware;
			this.rackId = other.rackId;
			this.rackIds = (other.rackIds != null) ? new List<int>(other.rackIds) : null;
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public ClientPolicy()
		{
		}
	}
}
