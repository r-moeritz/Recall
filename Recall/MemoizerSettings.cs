using System;

namespace Recall
{
    public sealed class MemoizerSettings
    {
        public int MaxItems { get; set; }
        public TimeSpan MaxAge { get; set; }
    }
}
