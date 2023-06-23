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
namespace Aerospike.Client
{
	/// <summary>
	/// Task used to poll for long running execute job completion.
	/// </summary>
	public sealed class ExecuteTask
	{
		private readonly ulong taskId;
		private readonly bool scan;
		private readonly Cluster cluster;
		private InfoPolicy policy;
		private System.Timers.Timer queryTimer;
		public const int NOT_FOUND = 0;
		public const int IN_PROGRESS = 1;
		public const int COMPLETE = 2;

		/// <summary>
		/// Initialize task with fields needed to query server nodes.
		/// </summary>
		public ExecuteTask(Cluster cluster, Policy policy, Statement statement, ulong taskId)
		{
			this.cluster = cluster;
			this.policy = new InfoPolicy(policy);
			this.taskId = taskId;
			this.scan = statement.filter == null;
		}

		/// <summary>
		/// Wait for asynchronous task to complete using default sleep interval (1 second).
		/// The timeout is passed from the original task policy. If task is not complete by timeout,
		/// an exception is thrown.  Do not timeout if timeout set to zero.
		/// </summary>
		public void Wait()
		{
			Wait(1000);
		}

		/// <summary>
		/// Wait for asynchronous task to complete using given sleep interval and timeout in milliseconds.
		/// If task is not complete by timeout, an exception is thrown.  Do not timeout if timeout set to
		/// zero.
		/// </summary>
		public void Wait(int sleepInterval, int timeout)
		{
			policy = new InfoPolicy();
			policy.timeout = timeout;
			Wait(sleepInterval);
		}

		/// <summary>
		/// Wait for asynchronous task to complete using given sleep interval in milliseconds.
		/// The timeout is passed from the original task policy. If task is not complete by timeout,
		/// an exception is thrown.  Do not timeout if policy timeout set to zero.
		/// </summary>
		public void Wait(int sleepInterval)
		{
			DateTime deadline = DateTime.UtcNow.AddMilliseconds(policy.timeout);
			queryTimer = new(interval: sleepInterval);
			queryTimer.Elapsed += async (sender, e) =>
			{
				await QueryStatus()
				.ContinueWith(task =>
				{
					if (task.IsCompletedSuccessfully)
					{
						int status = task.Result;
						// The server can remove task listings immediately after completion
						// (especially for background query execute), so "NOT_FOUND" can 
						// really mean complete. If not found and timeout not defined,
						// consider task complete.
						if (status == COMPLETE || (status == NOT_FOUND && policy.timeout == 0))
						{
							queryTimer.Stop();
							return true;
						}
						else if (policy.timeout > 0 && DateTime.UtcNow.AddMilliseconds(sleepInterval) > deadline)
						{
							// Timeout has been reached or will be reached after next sleep.
							// Do not throw timeout exception when status is "NOT_FOUND" because the server will drop 
							// background query execute task listings immediately after completion (which makes client
							// polling worthless).  This should be fixed by having server take an extra argument to query
							// execute command that says if server should wait till command is complete before responding 
							// to client.
							if (status == NOT_FOUND)
							{
								queryTimer.Stop();
								return true;
							}
							else
							{
								throw new AerospikeException.Timeout(policy.timeout, true);
							}
						}
					}
					return false;
				});
			};
		}

		/// <summary>
		/// Query all nodes for task completion status.
		/// </summary>
		public async Task<int> QueryStatus()
		{
			int retVal = COMPLETE;
			// All nodes must respond with complete to be considered done.
			Node[] nodes = cluster.ValidateNodes();
			
			string module = (scan) ? "scan" : "query";
			string cmd1 = "query-show:trid=" + taskId;
			string cmd2 = module + "-show:trid=" + taskId;
			string cmd3 = "jobs:module=" + module + ";cmd=get-job;trid=" + taskId;

			foreach (Node node in nodes)
			{
				string command;

				if (node.HasPartitionQuery)
				{
					// query-show works for both scan and query.
					command = cmd1;
				}
				else if (node.HasQueryShow)
				{
					// scan-show and query-show are separate.
					command = cmd2;
				}
				else
				{
					// old job monitor syntax.
					command = cmd3;
				}

				await Info.Request(policy, node, command)
					.ContinueWith(task =>
					{
						if (task.IsCompletedSuccessfully)
						{
							string response = task.Result;
							if (response.StartsWith("ERROR:2"))
							{
								// Query not found.
								if (node.HasPartitionQuery)
								{
									// Server >= 6.0:  Query has completed.
									// Continue checking other nodes.
									return true;
								}

								// Server < 6.0: Query could be complete or has not started yet.
								// Return NOT_FOUND and let the calling methods handle it.
								retVal = NOT_FOUND;
								return true;
							}

							if (response.StartsWith("ERROR:"))
							{
								throw new AerospikeException(command + " failed: " + response);
							}

							string find = "status=";
							int index = response.IndexOf(find);

							if (index < 0)
							{
								throw new AerospikeException(command + " failed: " + response);
							}

							int begin = index + find.Length;
							int end = response.IndexOf(':', begin);
							string status = response.Substring(begin, end - begin);

							// Newer servers use "done" while older servers use "DONE"
							if (!status.StartsWith("done", System.StringComparison.OrdinalIgnoreCase))
							{
								retVal = IN_PROGRESS;
							}
							return true;
						}
						// task faulted or cancelled
						return false;
					});
				if (retVal == NOT_FOUND || retVal == IN_PROGRESS) 
				{
					return retVal;
				}
			}

			return COMPLETE;
		}
	}
}
