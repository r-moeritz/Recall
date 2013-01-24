using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Recall;

namespace Examples.Task
{
    using System.Threading.Tasks;

    internal class Program
    {
        private const int QueryDuration = 5000;
        private const int QueryArg = 255;

        private static Task<IEnumerable<int>> TaskAsyncQuery(int arg)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        Thread.Sleep(QueryDuration);
                        return Enumerable.Range(0, Int16.MaxValue)
                            .Where(i => i < arg);
                    });
        }

        private static Task ExecuteTaskAsyncQuery(MemoizedTaskAsyncFunc<int, int> query, int arg)
        {
            var clock = new Stopwatch();
            clock.Start();

            return query.InvokeAsync(arg).ContinueWith(
                task =>
                    {
                        clock.Stop();
                        Console.WriteLine("{0} results returned in {1} seconds",
                                          task.Result.Count(), clock.Elapsed.TotalSeconds);
                    });
        }

        private static void Main()
        {
            Console.WriteLine("Executing queries. Please be patient.");
            
            var memoizer = new Memoizer<int, Dictionary<string, CacheEntry<int>>>();
            var memoizedTaskAsyncFunc = memoizer.MemoizeTask<int>(TaskAsyncQuery);

            ExecuteTaskAsyncQuery(memoizedTaskAsyncFunc, QueryArg).Wait();
            ExecuteTaskAsyncQuery(memoizedTaskAsyncFunc, QueryArg).Wait();

            Console.Write("Press any key to exit.");
            Console.Read();
        }
    }
}
