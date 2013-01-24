using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Recall
{
    public class MemoizedFunc<TResult>
    {
        public Func<IEnumerable<TResult>> Invoke { get; set; }
        public Action Invalidate { get; set; }
    }

    public class MemoizedFunc<TArg, TResult>
    {
        public Func<TArg, IEnumerable<TResult>> Invoke { get; set; }
        public Action<TArg> Invalidate { get; set; }
    }

    public class MemoizedAsyncFunc<TResult>
    {
        public Action<Action<IEnumerable<TResult>>> InvokeAsync { get; set; }
        public Action Invalidate { get; set; }
    }

    public class MemoizedAsyncFunc<TArg, TResult>
    {
        public Action<TArg, Action<IEnumerable<TResult>>> InvokeAsync { get; set; }
        public Action<TArg> Invalidate { get; set; }
    }

    public class MemoizedTaskAsyncFunc<TResult>
    {
      public Func<Task<IEnumerable<TResult>>> InvokeAsync { get; set; }
      public Action Invalidate { get; set; }
    }

    public class MemoizedTaskAsyncFunc<TArg, TResult>
    {
      public Func<TArg, Task<IEnumerable<TResult>>> InvokeAsync { get; set; }
      public Action<TArg> Invalidate { get; set; }
    }
}
