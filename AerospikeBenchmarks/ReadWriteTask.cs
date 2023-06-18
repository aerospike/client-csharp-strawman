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
using Aerospike.Client;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Aerospike.Benchmarks
{
	sealed class ReadWriteTask
	{
        private readonly AerospikeClient client;
        private readonly Args args;
        public readonly Metrics metrics;
        private readonly long keyStart;
        private readonly RandomShift random;
        private readonly ILatencyManager LatencyMgr;
        private readonly bool useLatency;
		public readonly WriteTask writeTask;
        
        public ReadWriteTask(AerospikeClient client,
								Args args,
								Metrics readMetrics,
								ILatencyManager readLatencyManager,
                                long keyStart,
                                WriteTask writeTask)
        {
            this.client = client;
            this.args = args;
			this.metrics = readMetrics;
			this.LatencyMgr = readLatencyManager;
			this.keyStart = keyStart;
            this.random = new RandomShift();
			this.useLatency = this.LatencyMgr is not null;
			this.writeTask = writeTask;
        }
        
        public async Task Run()
        {
            
            int maxConcurrentCommands = args.commandMax;

            if (maxConcurrentCommands > args.recordsWrite)
            {
                maxConcurrentCommands = args.recordsWrite;
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrentCommands
            };

            await this.RunCommand(args.recordsWrite, options);
        }

        public async Task RunCommand(int maxNbrRecs, 
                                        ParallelOptions parallelOptions)
		{
            var iterator = new bool[maxNbrRecs];

            //Start metrics processing!
            this.metrics.Start();
            this.writeTask.metrics.Start();

            await Parallel.ForEachAsync(iterator,
                                            parallelOptions,
                    async (ignore, cancellationToken) =>
            {
                // Roll a percentage die.
                int die = random.Next(0, 100);

                if (die < args.readPct)
                {
                    if (args.batchSize <= 1)
                    {
                        int key = random.Next(0, args.records);
                        await Read(key);
                    }
                    else
                    {
                        throw new NotImplementedException("Batches are not implemented");
                    }
                }
                else
                {
                    // Perform Single record write even if in batch mode.
                    await writeTask.Write(random.Next(0, args.records));
                }
            });
		}

		private async Task Read(long userKey)
		{
			Key key = new(args.ns, args.set, userKey);
            var capturedTime = this.useLatency
                                 ? this.metrics.Elapsed
                                 : TimeSpan.Zero;

            await client.Get(args.policy, key, args.binName)
				.ContinueWith(task =>
				{
                    if (task.IsCompletedSuccessfully)
                    {
                        if (this.useLatency)
                        {
                            var latency = this.metrics.Elapsed - capturedTime;
                            PrefStats.RecordEvent(latency,
                                                    metrics.Type.ToString(),
                                                    nameof(Read),
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
                }, TaskContinuationOptions.AttachedToParent
                        | TaskContinuationOptions.ExecuteSynchronously);
			
		}

	}
}
