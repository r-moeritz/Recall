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
        #region Static Fields

        public static readonly Func<IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>,
            IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>>> DefaultEvictionOrderer = EvictionPolicy.LRU;

        #endregion

        #region Fields

        private readonly TCache _cache = new TCache();
        private readonly object _locker = new object();
        private MemoizerSettings _settings = Defaults.DefaultMemoizerSettings;
        private readonly IDictionary<string, Queue<TaskCompletionSource<IEnumerable<TResult>>>> _taskQueues
            = new Dictionary<string, Queue<TaskCompletionSource<IEnumerable<TResult>>>>();
        private readonly IDictionary<string, Queue<Action<IEnumerable<TResult>>>> _callbackQueues
            = new Dictionary<string, Queue<Action<IEnumerable<TResult>>>>();

        public Func<IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>,
        IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>>> EvictionOrderer { get; set; }

        #endregion

        #region Properties

        public MemoizerSettings Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        #endregion

        #region Public Methods

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
                                       var firstCallback          = false;
                                       Queue<Action<IEnumerable<TResult>>> queue = null;

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               items = _cache[key].Items;
                                           }
                                           else
                                           {
                                               // Cache miss.
                                               _callbackQueues.TryGetValue(key, out queue);

                                               if (queue == null)
                                               {
                                                   queue = new Queue<Action<IEnumerable<TResult>>>();
                                                   _callbackQueues.Add(key, queue);
                                               }

                                               queue.Enqueue(callback);
                                               firstCallback = (queue.Count == 1);
                                           }
                                       }
                                       
                                       if (firstCallback)
                                       {
                                           // ... cache miss continued: call async function.
                                           action(
                                               results =>
                                               {
                                                   var queueEmpty = false;

                                                   if (results == null || !results.Any())
                                                   {
                                                       while (!queueEmpty)
                                                       {
                                                           lock (_locker)
                                                           {
                                                               callback   = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           callback(Enumerable.Empty<TResult>());
                                                       }
                                                   }
                                                   else
                                                   {
                                                       lock (_locker)
                                                       {
                                                           // Make space in the cache for function results.
                                                           EvictItems(results.Count());

                                                           // Add function results to cache.
                                                           var entry = new CacheEntry<TResult>(results);
                                                           _cache[key] = entry;
                                                       }

                                                       while (!queueEmpty)
                                                       {
                                                           lock (_locker)
                                                           {
                                                               callback = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           callback(results);
                                                       }
                                                   }
                                               });
                                       }
                                       else
                                       { 
                                           // ... cache hit continued.
                                           callback(items);
                                       }
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
                                       var firstCallback = false;
                                       Queue<Action<IEnumerable<TResult>>> queue = null;

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               items = _cache[key].Items;
                                           }
                                           else
                                           {
                                               // Cache miss.
                                               _callbackQueues.TryGetValue(key, out queue);

                                               if (queue == null)
                                               {
                                                   queue = new Queue<Action<IEnumerable<TResult>>>();
                                                   _callbackQueues.Add(key, queue);
                                               }

                                               queue.Enqueue(callback);
                                               firstCallback = (queue.Count == 1);
                                           }
                                       }
                                       
                                       if (firstCallback)
                                       {
                                           // ... cache miss continued: call async function.
                                           action(arg,
                                               results =>
                                               {
                                                   var queueEmpty = false;

                                                   if (results == null || !results.Any())
                                                   {
                                                       while (!queueEmpty)
                                                       {
                                                           lock (_locker)
                                                           {
                                                               callback = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           callback(Enumerable.Empty<TResult>());
                                                       }
                                                   }
                                                   else
                                                   {
                                                       lock (_locker)
                                                       {
                                                           // Make space in the cache for function results.
                                                           EvictItems(results.Count());

                                                           // Add function results to cache.
                                                           var entry = new CacheEntry<TResult>(results);
                                                           _cache[key] = entry;
                                                       }

                                                       while (!queueEmpty)
                                                       {
                                                           lock (_locker)
                                                           {
                                                               callback = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           callback(results);
                                                       }
                                                   }
                                               });
                                       }
                                       else
                                       {   // ... cache hit continued.
                                           callback(items);
                                       }
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
                                       var completionSource = new TaskCompletionSource<IEnumerable<TResult>>();
                                       var firstTask = false;
                                       Queue<TaskCompletionSource<IEnumerable<TResult>>> queue = null;

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               var results = _cache[key].Items;
                                               completionSource.TrySetResult(results);
                                           }
                                           else
                                           {
                                               // Cache miss.
                                               _taskQueues.TryGetValue(key, out queue);
                                               if (queue == null)
                                               {
                                                   queue = new Queue<TaskCompletionSource<IEnumerable<TResult>>>();
                                                   _taskQueues.Add(key, queue);
                                               }

                                               queue.Enqueue(completionSource);
                                               firstTask = (queue.Count == 1);
                                           }
                                       }

                                       if (firstTask)
                                       {
                                           // ... cache miss continued: call original function.
                                           var task = func();
                                           task.ContinueWith(
                                               _ =>
                                               {
                                                   var queueEmpty = false;

                                                   if (task.Exception != null)
                                                   {
                                                       while (!queueEmpty)
                                                       {
                                                           TaskCompletionSource<IEnumerable<TResult>> cs;

                                                           lock (_locker)
                                                           {
                                                               cs = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           cs.TrySetException(task.Exception);
                                                       }
                                                   }
                                                   else if (task.Result == null || !task.Result.Any())
                                                   {
                                                       while (!queueEmpty)
                                                       {
                                                           TaskCompletionSource<IEnumerable<TResult>> cs;

                                                           lock (_locker)
                                                           {
                                                               cs = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           cs.TrySetResult(Enumerable.Empty<TResult>());
                                                       }
                                                   }
                                                   else
                                                   {
                                                       lock (_locker)
                                                       {
                                                           // Make space in the cache for function results.
                                                           EvictItems(task.Result.Count());

                                                           // Add function results to cache.
                                                           var entry = new CacheEntry<TResult>(task.Result);
                                                           _cache[key] = entry;
                                                       }

                                                       while (!queueEmpty)
                                                       {
                                                           TaskCompletionSource<IEnumerable<TResult>> cs;

                                                           lock (_locker)
                                                           {
                                                               cs = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           cs.TrySetResult(task.Result);
                                                       }
                                                   }
                                               });
                                       }

                                       return completionSource.Task;
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
                                       var completionSource = new TaskCompletionSource<IEnumerable<TResult>>();
                                       var firstTask = false;
                                       Queue<TaskCompletionSource<IEnumerable<TResult>>> queue = null;

                                       lock (_locker)
                                       {
                                           EvictExpiredItems();

                                           if (_cache.ContainsKey(key))
                                           {
                                               // Cache hit.
                                               var results = _cache[key].Items;
                                               completionSource.TrySetResult(results);
                                           }
                                           else
                                           {
                                               // Cache miss.
                                               _taskQueues.TryGetValue(key, out queue);
                                               if (queue == null)
                                               {
                                                   queue = new Queue<TaskCompletionSource<IEnumerable<TResult>>>();
                                                   _taskQueues.Add(key, queue);
                                               }

                                               queue.Enqueue(completionSource);
                                               firstTask = (queue.Count == 1);
                                           }
                                       }

                                       if (firstTask)
                                       {
                                           // ... cache miss continued: call original function.
                                           var task = func(arg);
                                           task.ContinueWith(
                                               _ =>
                                               {
                                                   var queueEmpty = false;

                                                   if (task.Exception != null)
                                                   {
                                                       while (!queueEmpty)
                                                       {
                                                           TaskCompletionSource<IEnumerable<TResult>> cs;

                                                           lock (_locker)
                                                           {
                                                               cs = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           cs.TrySetException(task.Exception);
                                                       }
                                                   }
                                                   else if (task.Result == null || !task.Result.Any())
                                                   {
                                                       while (!queueEmpty)
                                                       {
                                                           TaskCompletionSource<IEnumerable<TResult>> cs;

                                                           lock (_locker)
                                                           {
                                                               cs = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           cs.TrySetResult(Enumerable.Empty<TResult>());
                                                       }
                                                   }
                                                   else
                                                   {
                                                       lock (_locker)
                                                       {
                                                           // Make space in the cache for function results.
                                                           EvictItems(task.Result.Count());

                                                           // Add function results to cache.
                                                           var entry = new CacheEntry<TResult>(task.Result);
                                                           _cache[key] = entry;
                                                       }

                                                       while (!queueEmpty)
                                                       {
                                                           TaskCompletionSource<IEnumerable<TResult>> cs;

                                                           lock (_locker)
                                                           {
                                                               cs = queue.Dequeue();
                                                               queueEmpty = (queue.Count == 0);
                                                           }

                                                           cs.TrySetResult(task.Result);
                                                       }
                                                   }
                                               });
                                       }

                                       return completionSource.Task;
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

        #region Private Methods

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
            if (Settings.MaxItems <= 0 || _cache.Count == 0) return;

            var evictionCount = _cache.Sum(pair => pair.Value.Items.Count()) + newItemCount - Settings.MaxItems;
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
            if (Settings.MaxAge == TimeSpan.Zero || _cache.Count == 0) return;
            var evictees = _cache.Where(p => DateTime.Now.Subtract(p.Value.Created) > Settings.MaxAge).ToArray();
            foreach (var pair in evictees)
            {
                _cache.Remove(pair);
            }
        }

        #endregion
    }
}