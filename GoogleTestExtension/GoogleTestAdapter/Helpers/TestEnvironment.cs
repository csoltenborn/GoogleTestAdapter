using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GoogleTestAdapter.Helpers
{

    public class TestEnvironment
    {

        private enum LogType { Normal, Debug }

        // for developing and testing the test adapter itself
        private static bool UnitTestMode = false;

        private static bool AlreadyAskedForDebugger { get; set; } = false;


        public Options Options { get; }
        private ILogger Logger { get; }


        public TestEnvironment(Options options, ILogger logger)
        {
            this.Options = options;
            this.Logger = logger;
        }


        public void LogInfo(string message)
        {
            if (ShouldBeLogged(LogType.Normal))
            {
                Logger.LogInfo(message);
            }
        }

        public void LogWarning(string message)
        {
            if (ShouldBeLogged(LogType.Normal))
            {
                Logger.LogWarning(message);
            }
        }

        public void LogError(string message)
        {
            if (ShouldBeLogged(LogType.Normal))
            {
                Logger.LogError(message);
            }
        }

        public void DebugInfo(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                Logger.LogInfo(message);
            }
        }

        public void DebugWarning(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                Logger.LogWarning(message);
            }
        }

        public void DebugError(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                Logger.LogError(message);
            }
        }

        public void CheckDebugModeForExecutionCode()
        {
            CheckDebugMode("execution");
        }

        public void CheckDebugModeForDiscoveryCode()
        {
            CheckDebugMode("discovery");
        }


        private bool ShouldBeLogged(LogType logType)
        {
            switch (logType)
            {
                case LogType.Normal:
                    return true;
                case LogType.Debug:
                    return Options.DebugMode;
                default:
                    throw new Exception("Unknown LogType: " + logType);
            }
        }

        private void CheckDebugMode(string taskType)
        {
            string processName = Process.GetCurrentProcess().MainModule.ModuleName;
            int processId = Process.GetCurrentProcess().Id;

            DebugInfo($"Test {taskType} is running in the process '{processName}' with id {processId}.");
            if (Options.DevelopmentMode && !UnitTestMode && !Debugger.IsAttached && !AlreadyAskedForDebugger)
            {
                AlreadyAskedForDebugger = true;

                string title = $"Attach debugger to test {taskType}?";
                string message = $"Test {taskType} is spawning a new process."
                    + "\nDo you want to attach the debugger?"
                    + "\n"
                    + "\nNote: if Visual Studio is already attached to some other process,"
                    + " you cannot select it as debugger in the following dialog."
                    + " In order to debug multiple processes at the same time please"
                    + $" attach manually to the process '{processName}' with id {processId}."
                    + "\n"
                    + "\nTip: Starting the Visual Studio experimental instance with 'Debug/Start"
                    + " Debugging (F5)' will attach the debugger to 'devenv.exe' and you cannot"
                    + " automatically attach to test discovery or execution. Use 'Debug/Start"
                    + " Without Debugging (Ctrl+F5)' instead and be happy.";

                DialogResult result = MessageBox.Show(message, title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    Debugger.Launch();
            }
        }

    }

}