using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Examples.Both
{
    internal class UncachedQuery : ICommand
    {
        private Func<int, IEnumerable<int>> _query;

        protected Func<int, IEnumerable<int>> Query
        {
            get { return _query ?? (_query = GetQuery()); }
        }

        protected virtual Func<int, IEnumerable<int>> GetQuery()
        {
            return BaseQuery;
        }

        protected static IEnumerable<int> BaseQuery(int arg)
        {
            Thread.Sleep(Constants.QueryDuration);
            return Enumerable.Range(0, Int16.MaxValue).Where(i => i < arg);
        }

        public void Execute()
        {
            Query(Constants.PredicateValue);
        }
    }
}