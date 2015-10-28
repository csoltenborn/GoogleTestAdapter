using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Helpers
{

    public class TestEnvironment
    {

        public enum LogType { Normal, UserDebug, Debug }


        // for developing and testing the test adapter itself
        private static bool DebugMode = false;
        private static bool UnitTestMode = false;

        private static bool DiscoveryProcessIdShown { get; set; } = false;

        private static readonly object Lock = new object();


        public AbstractOptions Options { get; }
        private IMessageLogger Logger { get; }


        public TestEnvironment(AbstractOptions options, IMessageLogger logger)
        {
            this.Options = options;
            this.Logger = logger;
        }


        public void LogInfo(string message, LogType logType = LogType.Normal)
        {
            Log(message, logType, TestMessageLevel.Informational, "");
        }

        public void LogWarning(string message, LogType logType = LogType.Normal)
        {
            Log(message, logType, TestMessageLevel.Warning, "Warning: ");
        }

        public void LogError(string message, LogType logType = LogType.Normal)
        {
            Log(message, logType, TestMessageLevel.Error, "ERROR: ");
        }

        public void CheckDebugModeForExecutionCode()
        {
            CheckDebugMode("Test execution code");
        }

        public void CheckDebugModeForDiscoveryCode()
        {
            if (!DiscoveryProcessIdShown)
            {
                DiscoveryProcessIdShown = true;
                CheckDebugMode("Test discovery code");
            }
        }


        private void Log(string message, LogType logType, TestMessageLevel level, string prefix)
        {
            bool log;
            switch (logType)
            {
                case LogType.Normal:
                    log = true;
                    break;
                case LogType.Debug:
                    log = DebugMode;
                    break;
                case LogType.UserDebug:
                    log = Options.UserDebugMode;
                    break;
                default:
                    throw new Exception("Unknown LogType: " + logType);
            }

            if (log)
            {
                lock (Lock)
                {
                    SafeLogMessage(level, prefix + message);
                }
            }
        }

        private void SafeLogMessage(TestMessageLevel level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = " ";
            }
            Logger.SendMessage(level, message);
        }

        private void CheckDebugMode(string codeType)
        {
            string message = codeType +
                " is running in the process with id " + Process.GetCurrentProcess().Id;
            LogInfo(message, LogType.Debug);
            if (DebugMode && !UnitTestMode)
            {
                MessageBox.Show(message + ". Attach debugger if necessary, then click ok.", "Attach debugger",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

    }

}