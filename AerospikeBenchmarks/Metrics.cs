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
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Schema;
using Aerospike.Client;

namespace Aerospike.Benchmarks
{
	sealed public class Metrics
	{
		public enum MetricTypes
		{
			None,
			Read,
			Write
		}
		
		public struct BlockCounters
		{
			internal BlockCounters(long instanceStartTimeTicks) => InstanceStartTimeTicks = instanceStartTimeTicks;

            /// <summary>
            /// The amount of time since <see cref="Metrics.Start"/> was called.
            /// This represents the time this block was created based on <see cref="Metrics.MetricsStopWatch"/>.
            /// </summary>
            public readonly long InstanceStartTimeTicks;
            /// <summary>
            /// The number of transactions that occurred within this block of time
            /// </summary>
            public long Count = 0;
            public long TimeoutCount = 0;
            public long ErrorCount = 0;
			/// <summary>
			/// The aggravation of all latencies that occurred within this block of time 
			/// </summary>
			public long TimingTicks = 0;
			/// <summary>
			/// The amount of time since <see cref="Metrics.Start"/> was called.
			/// This field is ONLY populated when <see cref="Metrics.NewBlockCounter"/> is called (this block became an old block)
			/// </summary>
			/// <seealso cref="InstanceStartTimeTicks"/>
			public long InstanceEndTimeTicks = 0;

            /// <summary>
            /// The elapsed time in ticks from when the block was created and ended. 
            /// If negative, the block is current and it cannot be calculated
            /// </summary>
            /// <seealso cref="InstanceStartTimeTicks"/>
            /// <seealso cref="InstanceEndTimeTicks"/>
            public readonly long InstanceElapsedTicks => InstanceEndTimeTicks - InstanceStartTimeTicks;

			/// <summary>
			/// Transaction per second for this block based on <see cref="Metrics.MetricsStopWatch"/>.
			/// </summary>
			/// <returns>
			/// TPS as a double or 0 to indicate that this block is still current and the TPS cannot be calculated
			/// </returns>
			public double TPS() => InstanceEndTimeTicks == 0
										? 0
										: Count / TimeSpan.FromTicks(InstanceElapsedTicks).TotalSeconds;
        }

        private readonly Args Args;        
		public readonly MetricTypes Type;

        /// <summary>
        /// The running execution time of this Metrics from <see cref="Start"/> 
        /// </summary>
        private readonly Stopwatch MetricsStopWatch;

        private BlockCounters CurrentBlockCounterFld;
        public BlockCounters CurrentBlockCounters { get => this.CurrentBlockCounterFld; }

        /// <summary>
        /// This counters are only updated when <see cref="NewBlockCounter"/> is executed
        /// </summary>
        public long TotalCount;
        /// <summary>
        /// This counters are only updated when <see cref="NewBlockCounter"/> is executed
        /// </summary>
        public long TotalTicks;
		
        internal Metrics(MetricTypes type, Args args)
		{
			this.Args = args;
			this.Type = type;
			
            MetricsStopWatch = new Stopwatch();
		}

		/// <summary>
		/// Starts the running stop watch and initializes the instance to allow count/timings...
		/// This must be called before any trans processing starts.
		/// </summary>
		public void Start()
		{
			MetricsStopWatch.Start();
			this.NewBlockCounter();
		}


		public TimeSpan Elapsed => MetricsStopWatch.Elapsed;

        /// <summary>
        /// Creates a new <see cref="BlockCounters"/> and makes it the current block for the metrics.
        /// The new block will be initialized based on the <see cref="MetricsStopWatch"/> elapsed time.
		/// The old block will be updated with the same time as the newly initialized block.
        /// </summary>
        /// <returns>
		/// Returns the old block counter, Running total number of transactions, and Running total of all trans latencies.
		/// </returns>
        public (BlockCounters, long, double) NewBlockCounter()
		{
			var instanceTime = MetricsStopWatch.ElapsedTicks;
			BlockCounters newBlock = new(instanceTime);
			var oldBlock = this.CurrentBlockCounterFld;
			oldBlock.InstanceEndTimeTicks = instanceTime;

            this.CurrentBlockCounterFld = newBlock;
           
            return (oldBlock,
                        Interlocked.Add(ref this.TotalCount, oldBlock.Count),
                        Interlocked.Add(ref this.TotalTicks, oldBlock.TimingTicks));
        }
		
        public void Success(TimeSpan elapsed)
		{
			Interlocked.Increment(ref this.CurrentBlockCounterFld.Count);
			Interlocked.Add(ref this.CurrentBlockCounterFld.TimingTicks, elapsed.Ticks);            
        }

		public void Success() => Interlocked.Increment(ref this.CurrentBlockCounterFld.Count);		

		public void Failure(AerospikeException ae)
		{
			if (ae.Result == ResultCode.TIMEOUT)
			{
				Interlocked.Increment(ref this.CurrentBlockCounterFld.TimeoutCount);
			}
			else
			{
				Failure((Exception)ae);
			}
		}

		public void Failure(Exception e)
		{
			Interlocked.Increment(ref this.CurrentBlockCounterFld.ErrorCount);

			if (Args.debug)
			{
				if (e is AggregateException ae)
				{
                    ae.Handle(ex =>
                    {
                        System.Diagnostics.Debug.WriteLine("Write error: " + ex.Message + System.Environment.NewLine + ex.StackTrace);
                        Console.WriteLine("Write error: " + ex.Message + System.Environment.NewLine + ex.StackTrace);
						return true;
                    });
                }
				else
				{
					System.Diagnostics.Debug.WriteLine("Write error: " + e.Message + System.Environment.NewLine + e.StackTrace);
					Console.WriteLine("Write error: " + e.Message + System.Environment.NewLine + e.StackTrace);
				}
			}
		}
	}
}
