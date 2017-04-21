using System;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.VsPackage.Helpers
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger() : base(() => true)
        {
        }

        public override void Log(Severity severity, string message)
        {
            Console.WriteLine($"{severity}: {message}");
        }

    }

}