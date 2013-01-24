using System;
using System.Collections.Generic;
using Recall;

namespace Examples.Both
{
    internal class CachedAsyncQuery : UncachedAsyncQuery
    {
        protected override Action<int, Action<IEnumerable<int>>> GetAsyncQuery()
        {
            var memoizer = new Memoizer<int, Dictionary<string, CacheEntry<int>>>();
            var memoizedAsyncFunc = memoizer.Memoize<int>(BaseAsyncQuery);
            return memoizedAsyncFunc.InvokeAsync;
        }
    }
}