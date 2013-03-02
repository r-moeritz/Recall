using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recall
{
    internal class Defaults
    {
        public static Type DefaultGenericCacheType
        {
            get { return typeof(Dictionary<,>); }
        }

        public static MemoizerSettings DefaultMemoizerSettings
        {
            get
            {
                return new MemoizerSettings
                {
                    MaxAge = TimeSpan.Zero,
                    MaxItems = 0
                };
            }
        }

        public static IMemoizerFactory DefaultMemoizerFactory
        {
            get
            {
                return new MemoizerFactory
                {
                    GenericCacheType = DefaultGenericCacheType,
                    Settings = DefaultMemoizerSettings
                };
            }
        }

        public static IUberMemoizer DefaultUberMemoizer
        {
            get
            {
                return new UberMemoizer
                {
                    Factory = DefaultMemoizerFactory
                };
            }
        }
    }
}
