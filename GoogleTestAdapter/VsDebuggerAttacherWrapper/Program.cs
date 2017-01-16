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

        private const int EXIT_SUCCESS = 0;
        private const int EXIT_FAILURE = 1;

        public static int Main(string[] args)
        {
            var logger = new ConsoleLogger();

            int debuggeeProcessId, visualStudioProcessId;
            if (args.Length != 2 ||
                !int.TryParse(args[0], out debuggeeProcessId) ||
                !int.TryParse(args[1], out visualStudioProcessId))
            {
                logger.LogError("Invalid arguments!");
                return EXIT_FAILURE;
            }

            var attacher = new VsDebuggerAttacher(logger, visualStudioProcessId);
            return attacher.AttachDebugger(debuggeeProcessId) ? EXIT_SUCCESS : EXIT_FAILURE;
        }
    }
}
