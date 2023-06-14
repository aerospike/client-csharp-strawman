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

		public async Task RunCommand(long count)
		{
			long currentKey = keyStart + count;
			Key key = new Key(args.ns, args.set, currentKey);
			Bin bin = new Bin(args.binName, args.GetValue(random));
			var watch = new Stopwatch();
			
			if (useLatency)
			{
				watch.Start();
			}
			await client.Put(args.writePolicy, key, bin)
				.ContinueWith(task =>
				{
					if (task.IsCompletedSuccessfully)
					{
						if (useLatency)
						{
							watch.Stop();
							var elapsed = watch.Elapsed;
							this.metrics.Success(elapsed);
							this.LatencyMgr?.Add((long)elapsed.TotalMilliseconds);
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