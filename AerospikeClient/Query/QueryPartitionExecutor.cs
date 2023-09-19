/* 
 * Copyright 2012-2023 Aerospike, Inc.
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aerospike.Client
{
	public sealed class QueryPartitionExecutor : IQueryExecutor
	{
		private readonly Cluster cluster;
		private readonly QueryPolicy policy;
		private readonly Statement statement;
		private readonly CancellationTokenSource cancel;
		private readonly PartitionTracker tracker;
		private volatile Exception exception;
		private int maxConcurrentThreads;
		private int completedCount;
		
		public QueryPartitionExecutor
		(
			Cluster cluster,
			QueryPolicy policy,
			Statement statement,
			int nodeCapacity,
			PartitionTracker tracker
		)
		{
			this.cluster = cluster;
			this.policy = policy;
			this.statement = statement;
			this.cancel = new CancellationTokenSource();
			this.tracker = tracker;            
        }

        public IEnumerable<KeyRecord> Run(object obj)
		=> this.Execute().SelectMany(r => r);

        public IEnumerable<IEnumerable<KeyRecord>> Execute()
		{			
			ulong taskId = statement.PrepareTaskId();
                       
            var collection = new ConcurrentBag<IEnumerable<KeyRecord>>();

            while (true)
			{
				List<NodePartitions> list = tracker.AssignPartitionsToNodes(cluster, statement.ns);
                // Initialize maximum number of nodes to query in parallel.
                this.maxConcurrentThreads = (policy.maxConcurrentNodes == 0 || policy.maxConcurrentNodes >= list.Count) ? list.Count : policy.maxConcurrentNodes;
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxConcurrentThreads
                };

                Parallel.ForEach(list,
								parallelOptions,
				(nodePartitions, cancellationToken) =>
				{
                    var command = new QueryPartitionCommand(cluster, policy, statement, taskId, tracker, nodePartitions);
                    collection.Add(command.ExecuteCommandKeyRecordResult());
                });
                
				if (exception != null)
				{
					break;
				}

				if (tracker.IsComplete(cluster, policy))
				{
					break;
				}

				if (policy.sleepBetweenRetries > 0)
				{
					// Sleep before trying again.
					Util.Sleep(policy.sleepBetweenRetries);
				}

				Interlocked.Exchange(ref completedCount, 0);
				exception = null;

				// taskId must be reset on next pass to avoid server duplicate query detection.
				taskId = RandomShift.ThreadLocalInstance.NextLong();
			}

			return collection;
		}
		
		public void CheckForException()
		{
			// Throw an exception if an error occurred.
			if (exception != null)
			{
				// Wrap exception because throwing will reset the exception's stack trace.
				// Wrapped exceptions preserve the stack trace in the inner exception.
				AerospikeException ae = new AerospikeException("Query Failed: " + exception.Message, exception);
				tracker.PartitionError();
				ae.Iteration = tracker.iteration;
				throw ae;
			}
		}

	}
}
