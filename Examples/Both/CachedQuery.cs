using System;
using System.Collections.Generic;
using Recall;

namespace Examples.Both
{
    internal class CachedQuery : UncachedQuery
    {
        protected override Func<int, IEnumerable<int>> GetQuery()
        {
            var memoizer = new Memoizer<int, Dictionary<string, CacheEntry<int>>>();
            var memoizedFunc = memoizer.Memoize<int>(BaseQuery);
            return memoizedFunc.Invoke;
        }
    }
}