using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestAdapter.Framework
{

    public class VsTestFrameworkLogger : ILogger
    {

        private static readonly object Lock = new object();

        private IMessageLogger Logger { get; }

        public VsTestFrameworkLogger(IMessageLogger logger)
        {
            Logger = logger;
        }


        public void LogInfo(string message)
        {
            LogSafe(TestMessageLevel.Informational, message);
        }

        public void LogWarning(string message)
        {
            LogSafe(TestMessageLevel.Warning, $"Warning: {message}");
        }

        public void LogError(string message)
        {
            LogSafe(TestMessageLevel.Error, $"ERROR: {message}");
        }


        private void LogSafe(TestMessageLevel level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                // Visual Studio 2013 is very picky about empty lines...
                // But it accepts an 'INVISIBLE SEPARATOR' (U+2063)  :-)
                message = "\u2063";
            }

            lock (Lock)
            {
                Logger.SendMessage(level, message);
            }
        }

    }

}