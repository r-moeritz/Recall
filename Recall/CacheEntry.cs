using System;
using System.Collections.Generic;

namespace Recall
{
    public class CacheEntry<T>
    {
        internal CacheEntry(IEnumerable<T> items)
        {
            _items = items;
            Created = DateTime.Now;
        }

        public DateTime Created { get; private set; }

        public DateTime LastAccessed { get; private set; }

        public int AccessCount { get; private set; }

        private readonly IEnumerable<T> _items;

        public IEnumerable<T> Items
        {
            get
            {
                LastAccessed = DateTime.Now;
                ++AccessCount;
                return _items;
            }
        }
    }
}