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
using System.Text;
using Aerospike.Client;
using AerospikeBenchmarks;

namespace Aerospike.Benchmarks
{
	sealed class ReadWrite
	{
		private readonly Args args;
        private readonly Metrics writeMetrics;
		private readonly Metrics readMetrics;
        private readonly ILatencyManager latencyManager;

        public ReadWrite(Args args, Metrics writeMetrics, Metrics readMetrics, ILatencyManager latencyMgr)
		{
			this.args = args;
			this.writeMetrics = writeMetrics;
			this.readMetrics = readMetrics;
			this.latencyManager = latencyMgr;
		}

		public void Run(AerospikeClient client)
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

			var numTasks = new bool[args.records];
			Array.Fill(numTasks, true);

			var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = maxConcurrentCommands
			};

			var task = new ReadWriteTask(client, args, writeMetrics);

            //Ticker.Run(args, metrics, latencyManager);

            Parallel.ForEachAsync(numTasks, options, async (num, cancellationToken) =>
			{
				await task.RunCommand();
			});			
		}		
	}
}
