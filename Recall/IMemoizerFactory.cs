using System;

namespace Recall
{
    public interface IMemoizerFactory
    {
        MemoizerSettings Settings { get; }

        Type GenericCacheType { get; }

        IMemoizer<TResult> Create<TResult>();
    }
}
