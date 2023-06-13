﻿/* 
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

namespace Aerospike.Benchmarks
{
	sealed class Initialize
	{
		private readonly Args args;
		private readonly Metrics metrics;

		public Initialize(Args args, Metrics metrics)
		{
			this.args = args;
			this.metrics = metrics;
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

			//var numTasks = new int[args.records];
			//numTasks = Enumerable.Range(0, args.records).ToArray();

			long keysPerCommand = args.recordsInit / maxConcurrentCommands;
			long keysRem = args.recordsInit - (keysPerCommand * maxConcurrentCommands);
			long keyStart = 0;

			WriteTask[] tasks = new WriteTask[args.records];

			for (int i = 0; i < args.records; i++)
			{
				// Allocate separate tasks for each seed command and reuse them in callbacks.
				long keyCount = (i < keysRem) ? keysPerCommand + 1 : keysPerCommand;
				tasks[i] = new WriteTask(client, args, metrics, keyStart, keyCount);
				keyStart += keyCount;
			}

			metrics.Start();

			var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = maxConcurrentCommands
			};

			//var task = new WriteTask(client, args, metrics, 0, args.records);

			Parallel.ForEachAsync(tasks, options, async (task, cancellationToken) =>
			{
				await task.RunCommand(0);
			});

			RunTicker();
		}

		private void RunTicker()
		{
			StringBuilder latencyBuilder = null;
			string latencyHeader = null;

			if (metrics.writeLatency != null)
			{
				latencyBuilder = new StringBuilder(200);
				latencyHeader = metrics.writeLatency.PrintHeader();
			}

			// Give tasks a chance to create stats for first period.
			Thread.Sleep(900);

			long total = 0;

			while (total < args.recordsInit)
			{
				long time = metrics.Time;

				int writeCurrent = Interlocked.Exchange(ref metrics.writeCount, 0);
				int writeTimeoutCurrent = Interlocked.Exchange(ref metrics.writeTimeoutCount, 0);
				int writeErrorCurrent = Interlocked.Exchange(ref metrics.writeErrorCount, 0);
				total += writeCurrent;

				long elapsed = metrics.NextPeriod(time);
				long writeTps = (long)writeCurrent * 1000L / elapsed;
				string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

				Console.WriteLine(dt + " write(count={0} tps={1} timeouts={2} errors={3})",
					total, writeTps, writeTimeoutCurrent, writeErrorCurrent);

				if (metrics.writeLatency != null)
				{
					if (latencyHeader != null)
					{
						Console.WriteLine(latencyHeader);
					}
					Console.WriteLine(metrics.writeLatency.PrintResults(latencyBuilder, "write"));
				}
				Thread.Sleep(1000);
			}

			if (metrics.writeLatency != null)
			{
				Console.WriteLine("Latency Summary");

				if (latencyHeader != null)
				{
					Console.WriteLine(latencyHeader);
				}
				Console.WriteLine(metrics.writeLatency.PrintSummary(latencyBuilder, "write"));
			}
		}
	}
}
