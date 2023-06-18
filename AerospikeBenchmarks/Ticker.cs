using Aerospike.Benchmarks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerospikeBenchmarks
{
    internal sealed class Ticker
    {
        public Ticker(Args args, 
                        Metrics readMetrics,
                        Metrics writeMetrics,
                        ILatencyManager readLatencyManager,
                        ILatencyManager writeLatencyManager)
        {
            this.Args = args;
            this.WriteMetrics = writeMetrics;
            this.ReadMetrics = readMetrics;
            this.ReadLatencyManager = readLatencyManager;
            this.WriteLatencyManager = writeLatencyManager;
        }

        public Args Args { get; }
        public Metrics WriteMetrics { get; }
        public Metrics ReadMetrics { get; }
        public ILatencyManager WriteLatencyManager { get; }
        public ILatencyManager ReadLatencyManager { get; }
        public StringBuilder LatencyBuilder { get; private set; }
        public string LatencyHeader { get; private set; }

        public void Run()
        {
           
            if (WriteMetrics.Type == Metrics.MetricTypes.Write)
            {
                LatencyBuilder = new StringBuilder(200);
                LatencyHeader = WriteLatencyManager.PrintHeader();
            }

            Timer = new Timer(TimerCallBack,
                                this,
                                Timeout.Infinite,
                                Timeout.Infinite);

            Interlocked.Exchange(ref TimerEntry, 0);
            Timer.Change(TimerInterval, Timeout.Infinite);            
        }

        public void WaitForAllToPrint()
        {
            if (!StopTimer)
            {
                Timer.Change(Timeout.Infinite, Timeout.Infinite);

                if (!StopTimer
                        && Interlocked.Read(ref TimerEntry) == 0 //Not running
                        && this.WriteMetrics.CurrentBlockCounters.Count > 0) //We have something to report
                {
                    TimerCallBack(this);
                }

                this.Stop();
            }
        }

        public void Stop()
        {
            StopTimer = true;

            if (WriteMetrics.Type == Metrics.MetricTypes.Write)
            {
                Console.WriteLine("Latency Summary");

                if (LatencyHeader != null)
                {
                    Console.WriteLine(LatencyHeader);
                }
                Console.WriteLine(WriteLatencyManager.PrintSummary(LatencyBuilder, "write"));
            }
        }

        public static Timer Timer { get; private set; }

        static long TimerEntry = 0;
        static bool StopTimer = false;
        public static int TimerInterval = 1000;

        public readonly static Stopwatch AppRunningTimer = Stopwatch.StartNew();

        private static void TimerCallBack(object state)
        {
            var ticker = (Ticker) state;

            if (StopTimer)
            {
                Interlocked.Exchange(ref TimerEntry, 0);
                Timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            if (Interlocked.Read(ref TimerEntry) > 0)
            {
                Timer.Change(TimerInterval, TimerInterval);
                return;
            }

            Interlocked.Increment(ref TimerEntry);

            try
            {
                bool blockDisplayed = false;

                (Metrics.BlockCounters, long, double) DisplayBlockInfo(Metrics metrics, 
                                                                        bool displayClock)
                {
                    (Metrics.BlockCounters windowBlock, long totalCount, double totalLatency) = metrics.NewBlockCounter();
                    var tps = totalCount / AppRunningTimer.Elapsed.TotalSeconds;

                    if (windowBlock.Count > 0)
                    {
                        if (displayClock)
                        {
                            Console.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        }

                        Console.Write($" {metrics.Type}(count={totalCount:###,###,##0} tps={tps:###,###,##0} timeouts={windowBlock.TimeoutCount:#,###,##0} errors={windowBlock.ErrorCount:#,###,##0} cnt={windowBlock.Count:#,###,##0})");
                        blockDisplayed = true;                        
                    }
                    return (windowBlock, totalCount, tps);
                }


                (Metrics.BlockCounters block, long totalCount, double tps) writeWindow = new();
                (Metrics.BlockCounters block, long totalCount, double tps) readWindow = new();
                
                if (ticker.WriteMetrics is not null)
                {
                    writeWindow = DisplayBlockInfo(ticker.WriteMetrics, !blockDisplayed);
                }
                if (ticker.ReadMetrics is not null)
                {
                    readWindow = DisplayBlockInfo(ticker.ReadMetrics, !blockDisplayed);
                }

                if(blockDisplayed) 
                {
                    if(ticker.WriteMetrics is not null && ticker.ReadMetrics is not null)
                    {
                        Console.Write(" Total(count={0:#,###,##0} tps={1:#,###,##0} timeouts={2:#,###,##0} errors={3:#,###,##0} cnt={4:#,###,##0})",
                                        writeWindow.totalCount + readWindow.totalCount,
                                        writeWindow.totalCount + readWindow.totalCount,
                                        writeWindow.block.TimeoutCount + readWindow.block.TimeoutCount,
                                        writeWindow.block.ErrorCount + readWindow.block.ErrorCount,
                                        writeWindow.block.Count + readWindow.block.Count); 
                    }

                    Console.WriteLine();

                    if (ticker.LatencyHeader is not null)
                    {
                        Console.WriteLine(ticker.LatencyHeader);
                    }

                    if (ticker.WriteLatencyManager is not null)
                    {                        
                        Console.WriteLine(ticker.WriteLatencyManager.PrintResults(ticker.LatencyBuilder, ticker.WriteMetrics.Type.ToString()));
                    }
                    if (ticker.ReadLatencyManager is not null)
                    {
                        Console.WriteLine(ticker.ReadLatencyManager.PrintResults(ticker.LatencyBuilder, ticker.ReadMetrics.Type.ToString()));
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref TimerEntry);
                Timer.Change(TimerInterval, Timeout.Infinite);
            }
        }
    }
}
