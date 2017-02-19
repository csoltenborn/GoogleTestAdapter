using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.TestAdapter.Framework;

namespace VsDebuggerAttacherWrapper
{
    class Program
    {
        private class ConsoleLogger : LoggerBase
        {
            public ConsoleLogger() : base(inDebugMode: () => false)
            {
            }

            public override void Log(Severity severity, string message)
            {
                Console.WriteLine(message);
            }
        }

        private const int ExitSuccess = 0;
        private const int ExitFailure = 1;

        public static int Main(string[] args)
        {
            var logger = new ConsoleLogger();

            if (args.Length != 2 ||
                !int.TryParse(args[0], out int debuggeeProcessId) ||
                !int.TryParse(args[1], out int visualStudioProcessId))
            {
                logger.LogError("Invalid arguments!");
                return ExitFailure;
            }

            var attacher = new VsDebuggerAttacher(logger, visualStudioProcessId);
            return attacher.AttachDebugger(debuggeeProcessId) ? ExitSuccess : ExitFailure;
        }
    }
}
