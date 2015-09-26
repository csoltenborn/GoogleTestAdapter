using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Helpers
{

    class TestEnvironment
    {

        internal enum LogType { Normal, UserDebug, Debug }

        // for testing and developing the test adapter itself
        private static bool UnitTestMode = false;
        private static bool DebugMode = false;

        private static bool DiscoveryProcessIdShown { get; set; } = false;

        internal AbstractOptions Options { get; }
        private IMessageLogger Logger { get; }

        internal TestEnvironment(AbstractOptions options, IMessageLogger logger)
        {
            this.Options = options;
            this.Logger = logger;
        }

        internal void LogInfo(string message, LogType logType = LogType.Normal)
        {
            Log(message, logType, TestMessageLevel.Informational);
        }

        internal void LogWarning(string message, LogType logType = LogType.Normal)
        {
            Log(message, logType, TestMessageLevel.Warning);
        }

        internal void LogError(string message, LogType logType = LogType.Normal)
        {
            Log(message, logType, TestMessageLevel.Error);
        }

        private void Log(string message, LogType logType, TestMessageLevel level)
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
                    throw new Exception("Unknown LogType: " + logType.ToString());
            }

            if (log)
            {
                Logger.SendMessage(level, message);
            }
        }

        internal void CheckDebugModeForExecutionCode()
        {
            CheckDebugMode("Test execution code");
        }

        internal void CheckDebugModeForDiscoverageCode()
        {
            if (!DiscoveryProcessIdShown)
            {
                DiscoveryProcessIdShown = true;
                CheckDebugMode("Test discoverage code");
            }
        }

        private void CheckDebugMode(string codeType)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            string message = "GTA: " + codeType + " is running on the process with id " + Process.GetCurrentProcess().Id;
            LogInfo(message, LogType.Debug);
            if (DebugMode && !UnitTestMode)
            {
                MessageBox.Show(message + ". Attach debugger if necessary, then click ok.",
                    "Attach debugger", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

    }

}