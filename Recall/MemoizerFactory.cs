using System;

namespace Recall
{
    public sealed class MemoizerFactory : IMemoizerFactory
    {
        private static readonly Type GenericMemoizerType = typeof(Memoizer<,>);

        #region Implementation of IMemoizerFactory

        public MemoizerSettings Settings { get; set; }

        public Type GenericCacheType { get; set; }

        public IMemoizer<T> Create<T>()
        {
            var constructedCacheType = GenericCacheType.MakeGenericType(typeof(string), typeof (CacheEntry<T>));
            var constructedMemoizerType = GenericMemoizerType.MakeGenericType(typeof (T), constructedCacheType);
            
            var ctor = constructedMemoizerType.GetConstructor(new Type[]{});
            var memoizer = (IMemoizer<T>)ctor.Invoke(null);
            memoizer.Settings = Settings;
            return memoizer;
        }

        #endregion
    }
}
