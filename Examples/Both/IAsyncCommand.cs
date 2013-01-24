using System;

namespace Examples.Both
{
    internal interface IAsyncCommand
    {
        void ExecuteAsync(Action callback);
    }
}