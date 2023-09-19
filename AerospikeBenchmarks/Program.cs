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
using AerospikeBenchmarks;

namespace Aerospike.Benchmarks
{
    class Program
    {
        async static Task Main(string[] args)
        {
            try
            {
                await RunBenchmarks();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        private async static Task RunBenchmarks()
        {
            Log.SetCallback(LogCallback);

            Args args = new();
            args.Print();

            Log.Level level = args.debug ? Log.Level.DEBUG : Log.Level.INFO;
            Log.SetLevel(level);

            var policy = new ClientPolicy()
            {
                user = args.user,
                password = args.password,
                tlsPolicy = args.tlsPolicy,
                authMode = args.authMode,
                maxCommands = args.commandMax,
            	minConnsPerNode = 100,
            	maxConnsPerNode = 100,
			    maxErrorRate = 10,
			    errorRateWindow = 5
		    };
            
            var client = new AerospikeClient(policy, args.hosts);
            Ticker ticker = null;

            try
            {
                long keyStart = 0;
                var metricsWrite = new Metrics(Metrics.MetricTypes.Write, args);
                ILatencyManager latencyMgrWrite = new LatencyManager();
                Metrics metricsRead = null;
                ILatencyManager latencyMgrRead = null;

                if (!args.writeonly)
                {
                    metricsRead = new Metrics(Metrics.MetricTypes.Read, args);
                    latencyMgrRead = new LatencyManager();
                }

                args.SetServerSpecific(client);

                ticker = new Ticker(args,
                                    metricsRead,
                                    metricsWrite,
                                    latencyMgrRead,
                                    latencyMgrWrite);
                ticker.Run();

                var writeTask = new WriteTask(client,
                                                args,
                                                metricsWrite,
                                                keyStart,
                                                latencyMgrWrite);

                if (metricsRead is null)
                {
                    await writeTask.Run();
                }
                else
                {
                    var readWriteTask = new ReadWriteTask(client,
                                                            args,
                                                            metricsRead,
                                                            latencyMgrRead,
                                                            keyStart,
                                                            writeTask);
                    await readWriteTask.Run();
                }
            }
            finally
            {
                client.Close();
                ticker?.WaitForAllToPrint();
                //ticker?.Stop();
            }

            if (PrefStats.EnableTimings)
            {
                PrefStats.ToCSV(args.LatencyFileCSV);
                PrefStats.ToJson(args.LatencyFileJson);
            }
        }

        private static void LogCallback(Log.Level level, string message)
        {
            Console.WriteLine(level.ToString() + ' ' + message);
        }
    }
}
