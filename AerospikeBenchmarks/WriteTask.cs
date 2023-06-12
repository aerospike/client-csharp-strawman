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
using Aerospike.Client;

namespace Aerospike.Benchmarks
{
	sealed class WriteTask
	{
		private readonly AerospikeClient client;
		private readonly Args args;
		private readonly Metrics metrics;
		private readonly long keyStart;
		private readonly long keyMax;
		private readonly RandomShift random;
		private readonly Stopwatch watch;
		private long keyCount;
		private long begin;
		private readonly bool useLatency;

		public WriteTask(AerospikeClient client, Args args, Metrics metrics, long keyStart, long keyMax)
		{
			this.client = client;
			this.args = args;
			this.metrics = metrics;
			this.keyStart = keyStart;
			this.keyMax = keyMax;
			this.random = new RandomShift();
			this.keyStart = keyStart;
			this.keyMax = keyMax;
			this.useLatency = metrics.writeLatency != null;
			watch = Stopwatch.StartNew();
		}

		public async Task Start()
		{
			await RunCommand(keyCount);
		}

		public async Task RunCommand(long count)
		{
			long currentKey = keyStart + count;
			Key key = new Key(args.ns, args.set, currentKey);
			Bin bin = new Bin(args.binName, args.GetValue(random));

			if (useLatency)
			{
				begin = watch.ElapsedMilliseconds;
			}
			await client.Put(args.writePolicy, key, bin)
				.ContinueWith(task =>
				{
					if (task.IsCompletedSuccessfully)
					{
						if (useLatency)
						{
							WriteSuccessLatency().Wait();
						}
						else
						{
							WriteSuccess().Wait();
						}
					}
					else if (task.IsFaulted)
					{
						WriteFailure(task.Exception).Wait();
					}

					return true;
				}, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
		}

		private async Task WriteSuccessLatency()
		{
			long elapsed = watch.ElapsedMilliseconds - Volatile.Read(ref begin);
			metrics.writeLatency.Add(elapsed);
			await WriteSuccess();
		}

		private async Task WriteSuccess()
		{
			metrics.WriteSuccess();
			long count = Interlocked.Increment(ref keyCount);

			/*if (count < keyMax)
			{
				// Try next command.
				await RunCommand(count);
			}*/
		}

		private async Task WriteFailure(AerospikeException ae)
		{
			metrics.WriteFailure(ae);
			// Retry command with same key.
			await RunCommand(keyCount);
		}

		private async Task WriteFailure(Exception e)
		{
			metrics.WriteFailure(e);
			await RunCommand(keyCount);
		}
	}
}