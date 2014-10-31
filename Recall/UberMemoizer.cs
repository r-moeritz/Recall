using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recall
{
    public sealed class UberMemoizer : IUberMemoizer
    {
        #region Static Fields

        public static readonly IUberMemoizer DefaultInstance = Defaults.DefaultUberMemoizer;

        #endregion

        #region Fields

        private IDictionary<Type, object> _memoizers = new Dictionary<Type, object>();
        private IMemoizerFactory _factory = Defaults.DefaultMemoizerFactory;

        #endregion

        #region Properties

        public IMemoizerFactory Factory
        {
            get { return _factory; }
            set { _factory = value; }
        }

        #endregion

        #region Public Methods

        public MemoizedFunc<TResult> Memoize<TResult>(Func<IEnumerable<TResult>> func)
        {
            var memoizer = GetMemoizer<TResult>();
            return memoizer.Memoize(func);
        }

        public MemoizedFunc<TArg, TResult> Memoize<TArg, TResult>(Func<TArg, IEnumerable<TResult>> func)
        {
            var memoizer = GetMemoizer<TResult>();
            return memoizer.Memoize<TArg>(func);
        }

        public MemoizedAsyncFunc<TResult> Memoize<TResult>(Action<Action<IEnumerable<TResult>>> action)
        {
            var memoizer = GetMemoizer<TResult>();
            return memoizer.Memoize(action);
        }

        public MemoizedAsyncFunc<TArg, TResult> Memoize<TArg, TResult>(Action<TArg, Action<IEnumerable<TResult>>> action)
        {
            var memoizer = GetMemoizer<TResult>();
            return memoizer.Memoize<TArg>(action);
        }

        public MemoizedTaskAsyncFunc<TResult> MemoizeTask<TResult>(Func<Task<IEnumerable<TResult>>> func)
        {
            var memoizer = GetMemoizer<TResult>();
            return memoizer.MemoizeTask(func);
        }

        public MemoizedTaskAsyncFunc<TArg, TResult> MemoizeTask<TArg, TResult>(Func<TArg, Task<IEnumerable<TResult>>> func)
        {
            var memoizer = GetMemoizer<TResult>();
            return memoizer.MemoizeTask<TArg>(func);
        }

        public void InvalidateAll<TResult>()
        {
            var memoizer = GetMemoizer<TResult>();
            memoizer.InvalidateAll();
        }

        #endregion

        #region Private Methods

        private IMemoizer<TResult> GetMemoizer<TResult>()
        {
            var t = typeof (TResult);
            object memoizer;
            if (!_memoizers.TryGetValue(t, out memoizer))
            {
                memoizer = Factory.Create<TResult>();
                _memoizers.Add(t, memoizer);
            }
            return (IMemoizer<TResult>) memoizer;
        }

        #endregion
    }
}
