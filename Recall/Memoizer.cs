using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Recall
{
    public sealed class Memoizer<TResult, TCache> : IMemoizer<TResult>
        where TCache : IDictionary<string, CacheEntry<TResult>>, new()
    {
        public const int DefaultMaxItems = 100;

        public static readonly Func<IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>,
            IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>>> DefaultEvictionOrderer = EvictionPolicy.LRU;

        private readonly TCache _cache = new TCache();
        private readonly object _locker = new object();

        private static Memoizer<TResult, TCache> _defaultInstance;

        public static Memoizer<TResult, TCache> DefaultInstance
        {
            get { return _defaultInstance ?? CreateDefaultInstance(); }
        }

        private static Memoizer<TResult, TCache> CreateDefaultInstance()
        {
            _defaultInstance = new Memoizer<TResult, TCache>
                                   {
                                       MaxItems = DefaultMaxItems,
                                       EvictionOrderer = DefaultEvictionOrderer
                                   };
            return _defaultInstance;
        }

        #region Implementation of IMemoizer

        public int MaxItems { get; set; }

        public TimeSpan MaxAge { get; set; }

        public Func<IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>,
            IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>>> EvictionOrderer { get; set; }

        public MemoizedFunc<TResult> Memoize(Func<IEnumerable<TResult>> func)
        {
            var key = GetMemoryKey(func.Method);
            return new MemoizedFunc<TResult>
                       {
                           Invoke =
                               () =>
                                   {
                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               return _cache[key].Items;
                                           }
                                       }

                                       // Cache miss, call original function.
                                       var results = func();
                                       if (results == null || !results.Any())
                                       {
                                           return Enumerable.Empty<TResult>();
                                       }

                                       lock (_locker)
                                       {
                                           // Make space in the cache for function results.
                                           EvictItems(results.Count());

                                           // Add function results to cache.
                                           var entry = new CacheEntry<TResult>(results);
                                           _cache[key] = entry;                                           
                                       }

                                       return results;
                                   },
                           Invalidate =
                               () => Invalidate(key)
                       };
        }

        public MemoizedFunc<TArg, TResult> Memoize<TArg>(Func<TArg, IEnumerable<TResult>> func)
        {
            return new MemoizedFunc<TArg, TResult>
                       {
                           Invoke =
                               arg =>
                                   {
                                       var key = GetMemoryKey(func.Method, arg);
                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               return _cache[key].Items;
                                           }
                                       }

                                       // Cache miss, call original function.
                                       var results = func(arg);
                                       if (results == null || !results.Any())
                                       {
                                           return Enumerable.Empty<TResult>();
                                       }

                                       lock (_locker)
                                       {
                                           // Make space in the cache for function results.
                                           EvictItems(results.Count());

                                           // Add function results to cache.
                                           var entry = new CacheEntry<TResult>(results);
                                           _cache[key] = entry;                                           
                                       }

                                       return results;
                                   },
                           Invalidate =
                               arg =>
                                   {
                                       var key = GetMemoryKey(func.Method, arg);
                                       Invalidate(key);
                                   }
                       };
        }

        public MemoizedAsyncFunc<TResult> Memoize(Action<Action<IEnumerable<TResult>>> action)
        {
            var key = GetMemoryKey(action.Method);
            return new MemoizedAsyncFunc<TResult>
                       {
                           InvokeAsync =
                               callback =>
                                   {
                                       IEnumerable<TResult> items = null;

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               items = _cache[key].Items;
                                           }
                                       }

                                       if (items != null)
                                       {
                                           callback(items);
                                           return;
                                       }

                                       // Cache miss, call async function.
                                       action(
                                           results =>
                                               {
                                                   if (results == null || !results.Any())
                                                   {
                                                       callback(Enumerable.Empty<TResult>());
                                                       return;
                                                   }

                                                   lock (_locker)
                                                   {
                                                       // Make space in the cache for function results.
                                                       EvictItems(results.Count());

                                                       // Add function results to cache.
                                                       var entry = new CacheEntry<TResult>(results);
                                                       _cache[key] = entry;                                                       
                                                   }

                                                   callback(results);
                                               });
                                   },
                           Invalidate =
                               () => Invalidate(key)
                       };
        }

        public MemoizedAsyncFunc<TArg, TResult> Memoize<TArg>(Action<TArg, Action<IEnumerable<TResult>>> action)
        {
            return new MemoizedAsyncFunc<TArg, TResult>
                       {
                           InvokeAsync =
                               (arg, callback) =>
                                   {
                                       var key = GetMemoryKey(action.Method, arg);
                                       IEnumerable<TResult> items = null;

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               items = _cache[key].Items;
                                           }
                                       }

                                       if (items != null)
                                       {
                                           callback(items);
                                           return;
                                       }

                                       // Cache miss, call async action.
                                       action(arg,
                                              results =>
                                                  {
                                                      if (results == null || !results.Any())
                                                      {
                                                          callback(Enumerable.Empty<TResult>());
                                                          return;
                                                      }

                                                      lock (_locker)
                                                      {
                                                          // Make space in the cache for query results.
                                                          EvictItems(results.Count());

                                                          // Add query results to cache.
                                                          var entry = new CacheEntry<TResult>(results);
                                                          _cache[key] = entry;                                                          
                                                      }

                                                      callback(results);
                                                  });
                                   },
                           Invalidate =
                               arg =>
                                   {
                                       var key = GetMemoryKey(action.Method, arg);
                                       Invalidate(key);
                                   }
                       };
        }

        public MemoizedTaskAsyncFunc<TResult> MemoizeTask(Func<Task<IEnumerable<TResult>>> func)
        {
            var key = GetMemoryKey(func.Method);
            return new MemoizedTaskAsyncFunc<TResult>
                       {
                           InvokeAsync =
                               () =>
                                   {
                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               var results = _cache[key].Items;
                                               return Task.Factory.StartNew(() => results);
                                           }
                                       }

                                       // Cache miss, call original function.
                                       var task = func();
                                       return task.ContinueWith(
                                           _ =>
                                               {
                                                   if (task.Result == null || !task.Result.Any())
                                                   {
                                                       return Enumerable.Empty<TResult>();
                                                   }

                                                   lock (_locker)
                                                   {
                                                       // Make space in the cache for function results.
                                                       EvictItems(task.Result.Count());

                                                       // Add function results to cache.
                                                       var entry = new CacheEntry<TResult>(task.Result);
                                                       _cache[key] = entry;
                                                   }

                                                   return task.Result;
                                               });
                                   },
                           Invalidate =
                               () => Invalidate(key)
                       };
        }

        public MemoizedTaskAsyncFunc<TArg, TResult> MemoizeTask<TArg>(Func<TArg, Task<IEnumerable<TResult>>> func)
        {
            return new MemoizedTaskAsyncFunc<TArg, TResult>
                       {
                           InvokeAsync =
                               arg =>
                                   {
                                       var key = GetMemoryKey(func.Method, arg);

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               var results = _cache[key].Items;
                                               return Task.Factory.StartNew(() => results);
                                           }
                                       }

                                       // Cache miss, call original function.
                                       var task = func(arg);
                                       return task.ContinueWith(
                                           _ =>
                                               {
                                                   if (task.Result == null || !task.Result.Any())
                                                   {
                                                       return Enumerable.Empty<TResult>();
                                                   }

                                                   lock (_locker)
                                                   {
                                                       // Make space in the cache for function results.
                                                       EvictItems(task.Result.Count());

                                                       // Add function results to cache.
                                                       var entry = new CacheEntry<TResult>(task.Result);
                                                       _cache[key] = entry;                                                       
                                                   }

                                                   return task.Result;
                                               });
                                   },
                           Invalidate =
                               arg =>
                                   {
                                       var key = GetMemoryKey(func.Method, arg);
                                       Invalidate(key);
                                   }
                       };
        }

        #endregion

        #region Utility Functions

        private void Invalidate(string key)
        {
            lock (_locker)
            {
                _cache.Remove(key);
            }
        }

        private static string GetMemoryKey(MemberInfo mInfo, object arg = null)
        {
            var type = mInfo.DeclaringType;
            if (type == null)
            {
                return (arg == null)
                           ? mInfo.Name
                           : mInfo.Name + "$" + arg;
            }

            return (arg == null)
                       ? type.FullName + "+" + mInfo.Name
                       : String.Format("{0}+{1}${2}", type.FullName, mInfo.Name, arg);
        }

        private IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>
            GetEvictees(int evictionCount,
                        Func<IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>,
                            IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>>> orderer)
        {
            var markedCount = 0;
            foreach (var pair in orderer(_cache))
            {
                markedCount += pair.Value.Items.Count();
                yield return pair;
                if (markedCount >= evictionCount) yield break;
            }
        }

        private void EvictItems(int newItemCount)
        {
            if (MaxItems <= 0 || _cache.Count == 0) return;

            var evictionCount = _cache.Sum(pair => pair.Value.Items.Count()) + newItemCount - MaxItems;
            if (evictionCount <= 0) return;

            var orderer = EvictionOrderer ?? DefaultEvictionOrderer;
            var evictees = GetEvictees(evictionCount, orderer).ToArray();
            foreach (var pair in evictees)
            {
                _cache.Remove(pair);
            }
        }

        private void EvictExpiredItems()
        {
            if (MaxAge == TimeSpan.Zero || _cache.Count == 0) return;
            var evictees = _cache.Where(p => DateTime.Now.Subtract(p.Value.Created) > MaxAge).ToArray();
            foreach (var pair in evictees)
            {
                _cache.Remove(pair);
            }
        }

        #endregion
    }
}