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
using System.Runtime.CompilerServices;
using System.Text;
using Aerospike.Client;
using AerospikeBenchmarks;

namespace Aerospike.Benchmarks
{
	sealed class Initialize
	{
		private readonly Args args;
		private readonly Metrics metrics;
        private readonly ILatencyManager latencyManager;

		public Initialize(Args args, Metrics metrics, ILatencyManager latencyManager)
		{
			this.args = args;
			this.metrics = metrics;
            this.latencyManager = latencyManager;
		}

		public async Task Run(AerospikeClient client)
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

			long keysPerCommand = args.recordsInit / maxConcurrentCommands;
			long keysRem = args.recordsInit - (keysPerCommand * maxConcurrentCommands);
			long keyStart = 0;

            var iterator = new bool[args.recordsInit];
			
            var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = maxConcurrentCommands
			};


			var task = new WriteTask(client, args, metrics, keyStart, latencyManager);
			long counter  = 0;

			var ticker = new Ticker(args, metrics, latencyManager);
            ticker.Run();
            
            await Parallel.ForEachAsync(iterator, options, async (ignore, cancellationToken) =>
			{
               await task.RunCommand(Interlocked.Increment(ref counter));				
			});

			ticker.WaitForAllToPrint();
			//ticker.Stop();
        }

		    
    }
}
