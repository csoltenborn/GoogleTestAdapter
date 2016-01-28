using System;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.VsPackage.Helpers
{

    public class ConsoleLogger : ILogger
    {

        public void LogError(string message)
        {
            Console.WriteLine("ERROR:" + message);
        }

        public void LogInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine("Warning:" + message);
        }

    }

}