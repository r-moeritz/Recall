using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recall
{
    public interface IMemoizer
    {
        MemoizedFunc<TResult> Memoize<TResult>(Func<IEnumerable<TResult>> func);

        MemoizedFunc<TArg, TResult> Memoize<TArg, TResult>(Func<TArg, IEnumerable<TResult>> func);

        MemoizedAsyncFunc<TResult> Memoize<TResult>(Action<Action<IEnumerable<TResult>>> action);

        MemoizedAsyncFunc<TArg, TResult> Memoize<TArg, TResult>(Action<TArg, Action<IEnumerable<TResult>>> action);

        MemoizedTaskAsyncFunc<TResult> MemoizeTask<TResult>(Func<Task<IEnumerable<TResult>>> func);

        MemoizedTaskAsyncFunc<TArg, TResult> MemoizeTask<TArg, TResult>(Func<TArg, Task<IEnumerable<TResult>>> func);
    }

    public interface IMemoizer<TResult>
    {
        MemoizerSettings Settings { get; set; }

        Func<IEnumerable<KeyValuePair<string, CacheEntry<TResult>>>,
            IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>>> EvictionOrderer { get; set; }

        MemoizedFunc<TResult> Memoize(Func<IEnumerable<TResult>> func);

        MemoizedFunc<TArg, TResult> Memoize<TArg>(Func<TArg, IEnumerable<TResult>> func);

        MemoizedAsyncFunc<TResult> Memoize(Action<Action<IEnumerable<TResult>>> action);

        MemoizedAsyncFunc<TArg, TResult> Memoize<TArg>(Action<TArg, Action<IEnumerable<TResult>>> action);

        MemoizedTaskAsyncFunc<TResult> MemoizeTask(Func<Task<IEnumerable<TResult>>> func);

        MemoizedTaskAsyncFunc<TArg, TResult> MemoizeTask<TArg>(Func<TArg, Task<IEnumerable<TResult>>> func);
    }
}