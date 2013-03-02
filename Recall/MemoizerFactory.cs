using System;

namespace Recall
{
    public sealed class MemoizerFactory : IMemoizerFactory
    {
        #region Static Fields

        public static IMemoizerFactory DefaultInstance = Defaults.DefaultMemoizerFactory;

        private static readonly Type GenericMemoizerType = typeof(Memoizer<,>);

        #endregion

        #region Fields

        private MemoizerSettings _settings = Defaults.DefaultMemoizerSettings;
        private Type _genericCacheType = Defaults.DefaultGenericCacheType;

        #endregion

        #region Properties

        public MemoizerSettings Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        public Type GenericCacheType
        {
            get { return _genericCacheType; }
            set { _genericCacheType = value; }
        }

        #endregion

        #region Public Methods

        public IMemoizer<TResult> Create<TResult>()
        {
            var constructedCacheType = GenericCacheType.MakeGenericType(typeof(string), typeof (CacheEntry<TResult>));
            var constructedMemoizerType = GenericMemoizerType.MakeGenericType(typeof (TResult), constructedCacheType);
            
            var ctor = constructedMemoizerType.GetConstructor(new Type[]{});
            var memoizer = (IMemoizer<TResult>) ctor.Invoke(null);
            memoizer.Settings = Settings;
            return memoizer;
        }

        #endregion
    }
}
