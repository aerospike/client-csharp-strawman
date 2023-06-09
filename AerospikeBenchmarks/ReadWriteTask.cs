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
using System.Diagnostics;

namespace Aerospike.Benchmarks
{
	sealed class ReadWriteTask
	{
		internal readonly Args args;
		internal readonly Metrics metrics;
		internal bool valid;
		private readonly AerospikeClient client;
		private readonly RandomShift random;
		private readonly Stopwatch watch;
		private long begin;
		private readonly bool useLatency;

		public ReadWriteTask(AerospikeClient client, Args args, Metrics metrics)
		{
			this.args = args;
			this.metrics = metrics;
			this.valid = true;
			this.client = client;
			this.random = new RandomShift();
			this.useLatency = metrics.writeLatency != null;
			watch = Stopwatch.StartNew();
		}

		public async Task RunCommand()
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
				/*else
				{
					await BatchRead();
				}*/
			}
			else
			{
				// Perform Single record write even if in batch mode.
				int key = random.Next(0, args.records);
				await Write(key);
			}
		}

		private async Task Write(int userKey)
		{
			Key key = new Key(args.ns, args.set, userKey);
			Bin bin = new Bin(args.binName, args.GetValue(random));

			try
			{
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
			catch (AerospikeException ae)
			{
				await WriteFailure(ae);
			}
			catch (Exception e)
			{
				await WriteFailure(e);
			}
		}

		private async Task WriteSuccessLatency()
		{
			long elapsed = watch.ElapsedMilliseconds - Volatile.Read(ref begin);
			metrics.writeLatency.Add(elapsed);
			//await WriteSuccess();
		}

		private async Task WriteSuccess()
		{
			metrics.WriteSuccess();
			//await RunCommand();
		}

		private async Task WriteFailure(AerospikeException ae)
		{
			metrics.WriteFailure(ae);
			//await RunCommand();
		}

		private async Task WriteFailure(Exception e)
		{
			metrics.WriteFailure(e);
			//await RunCommand();
		}

		private async Task Read(int userKey)
		{
			Key key = new Key(args.ns, args.set, userKey);

			try
			{
				if (useLatency)
				{
					begin = watch.ElapsedMilliseconds;
				}
				await client.Get(args.policy, key, args.binName)
					.ContinueWith(task =>
					{
						if (task.IsCompletedSuccessfully)
						{
							if (useLatency)
							{
								ReadSuccessLatency().Wait();
							}
							else
							{
								ReadSuccess().Wait();
							}
						}
						else if (task.IsFaulted)
						{
							ReadFailure(task.Exception).Wait();
						}

						return true;
					}, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
			}
			catch (AerospikeException ae)
			{
				await ReadFailure(ae);
			}
			catch (Exception e)
			{
				await ReadFailure(e);
			}
		}

		private async Task ReadSuccessLatency()
		{
			long elapsed = watch.ElapsedMilliseconds - Volatile.Read(ref begin);
			metrics.readLatency.Add(elapsed);
			await ReadSuccess();
		}

		private async Task ReadSuccess()
		{
			metrics.ReadSuccess();
			//await RunCommand();
		}

		private async Task ReadFailure(AerospikeException ae)
		{
			metrics.ReadFailure(ae);
			//await RunCommand();
		}

		private async Task ReadFailure(Exception e)
		{
			metrics.ReadFailure(e);
			//await RunCommand();
		}

		/*private async Task BatchRead()
		{
			Key[] keys = new Key[args.batchSize];

			for (int i = 0; i < keys.Length; i++)
			{
				long keyIdx = random.Next(0, args.records);
				keys[i] = new Key(args.ns, args.set, keyIdx);
			}

			try
			{
				if (useLatency)
				{
					begin = watch.ElapsedMilliseconds;
				}
				await client.Get(args.batchPolicy, keys, args.binName);
			}
			catch (AerospikeException ae)
			{
				await BatchFailure(ae);
			}
			catch (Exception e)
			{
				await BatchFailure(e);
			}
		}

		private async Task BatchSuccessLatency()
		{
			long elapsed = watch.ElapsedMilliseconds - Volatile.Read(ref begin);
			metrics.readLatency.Add(elapsed);
			await ReadSuccess();
		}

		private async Task BatchSuccess()
		{
			metrics.ReadSuccess();
			await RunCommand();
		}

		private async Task BatchFailure(AerospikeException ae)
		{
			metrics.ReadFailure(ae);
			await RunCommand();
		}

		private async Task BatchFailure(Exception e)
		{
			metrics.ReadFailure(e);
			await RunCommand();
		}*/
	}
}
