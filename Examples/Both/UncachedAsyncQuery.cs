using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Examples.Both
{
    internal class UncachedAsyncQuery : IAsyncCommand
    {
        private Action<int, Action<IEnumerable<int>>> _asyncQuery;

        protected Action<int, Action<IEnumerable<int>>> AsyncQuery
        {
            get { return _asyncQuery ?? (_asyncQuery = GetAsyncQuery()); }
        }

        protected virtual Action<int, Action<IEnumerable<int>>> GetAsyncQuery()
        {
            return BaseAsyncQuery;
        }

        protected static void BaseAsyncQuery(int arg, Action<IEnumerable<int>> callback)
        {
            Task<IEnumerable<int>>.Factory.StartNew(
                () =>
                    {
                        Thread.Sleep(Constants.QueryDuration);
                        return Enumerable.Range(0, Int16.MaxValue).Where(i => i < arg);
                    }
                ).ContinueWith(task => callback(task.Result));
        }

        public void ExecuteAsync(Action callback)
        {
            AsyncQuery(Constants.PredicateValue, _ => callback());
        }
    }
}