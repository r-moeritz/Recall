using System.Collections.Generic;
using System.Linq;

namespace Recall
{
    public static class EvictionPolicy
    {
        public static IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>> LRU<TResult>(
            IEnumerable<KeyValuePair<string, CacheEntry<TResult>>> pairs)
        {
            return pairs.OrderBy(p => p.Value.LastAccessed);
        }

        public static IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>> LU<TResult>(
            IEnumerable<KeyValuePair<string, CacheEntry<TResult>>> pairs)
        {
            return pairs.OrderBy(p => p.Value.AccessCount);
        }

        public static IOrderedEnumerable<KeyValuePair<string, CacheEntry<TResult>>> FIFO<TResult>(
            IEnumerable<KeyValuePair<string, CacheEntry<TResult>>> pairs)
        {
            return pairs.OrderBy(p => p.Value.Created);
        }
    }
}