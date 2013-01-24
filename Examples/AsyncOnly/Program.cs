using Recall;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Examples.AsyncOnly
{
    internal class Program
    {
        private const int QueryDuration = 5000;
        private const int QueryArg = 255;

        private static void AsyncQuery(int arg, Action<IEnumerable<int>> callback)
        {
            Task<IEnumerable<int>>.Factory.StartNew(() =>
                                                        {
                                                            Thread.Sleep(QueryDuration);
                                                            return Enumerable.Range(0, Int16.MaxValue)
                                                                .Where(i => i < arg);
                                                        }
                ).ContinueWith(task => callback(task.Result));
        }

        private static void ExecuteAsyncQuery(Action<int, Action<IEnumerable<int>>> query, int arg,
                                              Action callback = null)
        {
            var clock = new Stopwatch();
            clock.Start();

            query(arg,
                  results =>
                      {
                          clock.Stop();
                          Console.WriteLine("{0} results returned in {1} seconds",
                                            results.Count(), clock.Elapsed.TotalSeconds);
                          if (callback == null)
                          {
                              Console.Write("Press any key to exit.");
                          }
                          else
                          {
                              callback();
                          }
                      }
                );
        }

        private static void Main()
        {
            Console.WriteLine("Executing queries. Please be patient.");
            var memoizer = new Memoizer<int, Dictionary<string, CacheEntry<int>>>();
            var memoizedAsyncFunc = memoizer.Memoize<int>(AsyncQuery);
            ExecuteAsyncQuery(memoizedAsyncFunc.InvokeAsync, QueryArg,
                              () => ExecuteAsyncQuery(memoizedAsyncFunc.InvokeAsync, QueryArg));
            Console.Read();
        }
    }
}