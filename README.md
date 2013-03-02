# Recall

> For the memory of a lifetime, Recall, Recall, Recall.

A memoization and caching library for .NET with support for asynchronous methods.

## Usage

See simple task async example below. Further examples included in source distribution.

```csharp
const int QueryDuration = 5000;
const int QueryArg = 255;

static void Main()
{
    Console.WriteLine("Executing queries. Please be patient.");
    
    var memoizedTaskAsyncFunc = UberMemoizer.DefaultInstance.MemoizeTask<int, int>(TaskAsyncQuery);

    ExecuteTaskAsyncQuery(memoizedTaskAsyncFunc, QueryArg).Wait();
    ExecuteTaskAsyncQuery(memoizedTaskAsyncFunc, QueryArg).Wait();

    Console.Write("Press any key to exit.");
    Console.Read();
}

static Task ExecuteTaskAsyncQuery(MemoizedTaskAsyncFunc<int, int> query, int arg)
{
    var clock = new Stopwatch();
    clock.Start();

    return query.InvokeAsync(arg).ContinueWith(
        task =>
            {
                clock.Stop();
                Console.WriteLine("{0} results returned in {1} seconds",
                                  task.Result.Count(), clock.Elapsed.TotalSeconds);
            });
}

static Task<IEnumerable<int>> TaskAsyncQuery(int arg)
{
    return Task.Factory.StartNew(
        () =>
            {
                Thread.Sleep(QueryDuration);
                return Enumerable.Range(0, Int16.MaxValue)
                    .Where(i => i < arg);
            });
}
```

## License

Copyright (c) Ralph Möritz 2012.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**
