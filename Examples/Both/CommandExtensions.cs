using System;
using System.Diagnostics;

namespace Examples.Both
{
    internal static class CommandExtensions
    {
        public static TimeSpan Instrument(this ICommand command)
        {
            var clock = new Stopwatch();
            clock.Start();
            command.Execute();
            clock.Stop();
            return clock.Elapsed;
        }

        public static void InstrumentAsync(this IAsyncCommand command, Action<TimeSpan> callback)
        {
            var clock = new Stopwatch();
            clock.Start();
            command.ExecuteAsync(
                () =>
                    {
                        clock.Stop();
                        callback(clock.Elapsed);
                    });
        }
    }
}