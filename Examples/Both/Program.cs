using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Examples.Both
{
    internal class Program
    {
        private static void InstrumentQueries()
        {
            Console.WriteLine("Executing queries. Please be patient.");

            var uncachedQuery = new UncachedQuery();
            var cachedQuery = new CachedQuery();

            var uncachedQueryTasks = new List<Task<TimeSpan>>();
            var cachedQueryTasks = new List<Task<TimeSpan>>();

            foreach (var _ in Enumerable.Range(0, Constants.QueryCount))
            {
                uncachedQueryTasks.Add(Task<TimeSpan>.Factory.StartNew(uncachedQuery.Instrument));
                cachedQueryTasks.Add(Task<TimeSpan>.Factory.StartNew(cachedQuery.Instrument));
            }

            Task.WaitAll(uncachedQueryTasks.Cast<Task>().ToArray());
            var uncachedQueryTime = uncachedQueryTasks.Aggregate(TimeSpan.Zero,
                                                                 (span, task) => span + task.Result);

            Task.WaitAll(cachedQueryTasks.Cast<Task>().ToArray());
            var cachedQueryTime = cachedQueryTasks.Aggregate(TimeSpan.Zero,
                                                             (span, task) => span + task.Result);

            Console.WriteLine("Uncached query:\t{0} seconds", uncachedQueryTime.TotalSeconds);
            Console.WriteLine("Cached query:\t{0} seconds", cachedQueryTime.TotalSeconds);
            Console.WriteLine();
        }

        private static void InstrumentAsyncQueries()
        {
            Console.WriteLine("Executing async queries. Please be patient.");

            var uncachedAsyncQuery = new UncachedAsyncQuery();
            var cachedAsyncQuery = new CachedAsyncQuery();

            var taskQueue = new ConcurrentQueue<Tuple<TimeSpan, bool>>();
            var newTaskSignal = new AutoResetEvent(false);

            foreach (var _ in Enumerable.Range(0, Constants.QueryCount))
            {
                uncachedAsyncQuery.InstrumentAsync(
                    elapsed =>
                        {
                            taskQueue.Enqueue(Tuple.Create(elapsed, false));
                            newTaskSignal.Set();
                        });
            }

            cachedAsyncQuery.InstrumentAsync(
                elapsed1st =>
                    {
                        taskQueue.Enqueue(Tuple.Create(elapsed1st, true));
                        newTaskSignal.Set();

                        foreach (var _ in Enumerable.Range(0, Constants.QueryCount - 1))
                        {
                            cachedAsyncQuery.InstrumentAsync(
                                elapsed =>
                                    {
                                        taskQueue.Enqueue(Tuple.Create(elapsed, true));
                                        newTaskSignal.Set();
                                    });
                        }
                    });

            var uncachedAsyncQueryTime = TimeSpan.Zero;
            var cachedAsyncQueryTime = TimeSpan.Zero;
            var pendingTaskCount = Constants.QueryCount*2;

            while (true)
            {
                newTaskSignal.WaitOne();

                Tuple<TimeSpan, bool> task;
                while (taskQueue.TryDequeue(out task))
                {
                    if (task.Item2)
                    {
                        cachedAsyncQueryTime += task.Item1;
                    }
                    else
                    {
                        uncachedAsyncQueryTime += task.Item1;
                    }

                    --pendingTaskCount;
                }

                if (pendingTaskCount == 0) break;
            }

            Console.WriteLine("Uncached async query:\t{0} seconds", uncachedAsyncQueryTime.TotalSeconds);
            Console.WriteLine("Cached async query:\t{0} seconds", cachedAsyncQueryTime.TotalSeconds);
            Console.WriteLine();
        }

        private static void Main()
        {
            InstrumentQueries();
            InstrumentAsyncQueries();

            Console.Write("Press any key to exit.");
            Console.Read();
        }
    }
}