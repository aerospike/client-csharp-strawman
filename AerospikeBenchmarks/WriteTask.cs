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
using System.Diagnostics;
using System.Threading.Tasks;
using Aerospike.Client;
using AerospikeBenchmarks;

namespace Aerospike.Benchmarks
{
	sealed class WriteTask
	{		
		private readonly AerospikeClient client;
		private readonly Args args;
		private readonly Metrics metrics;
		private readonly long keyStart;
		private readonly RandomShift random;
		private readonly ILatencyManager LatencyMgr;
		private readonly bool useLatency;
        private long counter = 0;

        public WriteTask(AerospikeClient client, 
							Args args, 
							Metrics metrics, 
							long keyStart,
                            ILatencyManager latencyManager)
		{
			this.client = client;
			this.args = args;
			this.metrics = metrics;
			this.keyStart = keyStart;
			this.random = new RandomShift();
			this.LatencyMgr = latencyManager;
			this.useLatency = latencyManager != null;
		}

        public async Task Run()
        {
            // Generate commandMax writes to seed the event loops.
            // Then start a new command in each command callback.
            // This effectively throttles new command generation, by only allowing
            // commandMax at any point in time.
            int maxConcurrentCommands = args.commandMax;

            if (maxConcurrentCommands > args.recordsInit)
            {
                maxConcurrentCommands = args.recordsInit;
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrentCommands
            };

            await this.RunCommand(args.recordsInit, options);

        }

        public async Task RunCommand(int maxNbrRecs, ParallelOptions parallelOptions)
		{
            var iterator = new bool[maxNbrRecs];

            //Start metrics processing!
            this.metrics.Start();

            await Parallel.ForEachAsync(iterator,
                                            parallelOptions,
                    async (ignore, cancellationToken) =>
                    {
                        await this.Write(keyStart + Interlocked.Increment(ref counter));
                    });
        }

        public async Task Write(long userKey)
        {
            Key key = new(args.ns, args.set, userKey);
            Bin bin = new(args.binName, args.GetValue(random));
            var capturedTime = this.useLatency
                                ? this.metrics.Elapsed
                                : TimeSpan.Zero;

            await client.Put(args.writePolicy, key, bin)
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        if (this.useLatency)
                        {
                            var latency = this.metrics.Elapsed - capturedTime;
                            PrefStats.RecordEvent(latency,
                                                    metrics.Type.ToString(),
                                                    nameof(Write),
                                                    key);
                            
                            this.metrics.Success(latency);
                            this.LatencyMgr?.Add((long)latency.TotalMilliseconds);
                        }
                        else
                        {
                            this.metrics.Success();
                        }
                    }
                    else if (task.IsFaulted)
                    {
                        this.metrics.Failure(task.Exception);
                    }

                    return true;
                },
                TaskContinuationOptions.AttachedToParent
                    | TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}