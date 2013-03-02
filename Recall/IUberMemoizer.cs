using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recall
{
    public interface IUberMemoizer : IMemoizer
    {
        IMemoizerFactory Factory { get; set; }
    }
}
