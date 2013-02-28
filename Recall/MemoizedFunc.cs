using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Recall
{
    public class MemoizedFunc<TResult>
    {
        internal MemoizedFunc() { }
        public Func<IEnumerable<TResult>> Invoke { get; set; }
        public Action Invalidate { get; set; }
    }

    public class MemoizedFunc<TArg, TResult>
    {
        internal MemoizedFunc() { }
        public Func<TArg, IEnumerable<TResult>> Invoke { get; set; }
        public Action<TArg> Invalidate { get; set; }
    }

    public class MemoizedAsyncFunc<TResult>
    {
        internal MemoizedAsyncFunc() { }
        public Action<Action<IEnumerable<TResult>>> InvokeAsync { get; set; }
        public Action Invalidate { get; set; }
    }

    public class MemoizedAsyncFunc<TArg, TResult>
    {
        internal MemoizedAsyncFunc() { }
        public Action<TArg, Action<IEnumerable<TResult>>> InvokeAsync { get; set; }
        public Action<TArg> Invalidate { get; set; }
    }

    public class MemoizedTaskAsyncFunc<TResult>
    {
        internal MemoizedTaskAsyncFunc() { }
        public Func<Task<IEnumerable<TResult>>> InvokeAsync { get; set; }
        public Action Invalidate { get; set; }
    }

    public class MemoizedTaskAsyncFunc<TArg, TResult>
    {
        internal MemoizedTaskAsyncFunc() { }
        public Func<TArg, Task<IEnumerable<TResult>>> InvokeAsync { get; set; }
        public Action<TArg> Invalidate { get; set; }
    }
}
