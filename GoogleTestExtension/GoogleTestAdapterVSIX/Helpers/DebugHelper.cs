using System.Diagnostics;
using System.Windows.Forms;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapterVSIX.Helpers
{
    public class DebugHelper
    {
        // for developing and testing the test adapter itself
        private static bool UnitTestMode = false;

        private static bool AlreadyAskedForDebugger { get; set; } = false;

        private TestEnvironment TestEnvironment { get; }

        public DebugHelper(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }


        public void CheckDebugModeForExecutionCode()
        {
            CheckDebugMode("execution");
        }

        public void CheckDebugModeForDiscoveryCode()
        {
            CheckDebugMode("discovery");
        }

        private void CheckDebugMode(string taskType)
        {
            string processName = Process.GetCurrentProcess().MainModule.ModuleName;
            int processId = Process.GetCurrentProcess().Id;

            TestEnvironment.DebugInfo($"Test {taskType} is running in the process '{processName}' with id {processId}.");
            if (TestEnvironment.Options.DevelopmentMode && !UnitTestMode && !Debugger.IsAttached && !AlreadyAskedForDebugger)
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