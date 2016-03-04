using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Helpers
{

    public class TestProcessLauncher
    {
        private TestEnvironment TestEnvironment { get; }
        private bool IsBeingDebugged { get; }

        public TestProcessLauncher(TestEnvironment testEnvironment, bool isBeingDebugged)
        {
            TestEnvironment = testEnvironment;
            IsBeingDebugged = isBeingDebugged;
        }


        public List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError, IDebuggedProcessLauncher debuggedLauncher)
        {
            int dummy;
            return GetOutputOfCommand(workingDirectory, command, param, printTestOutput, throwIfError, debuggedLauncher, out dummy);
        }

        public List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError, IDebuggedProcessLauncher debuggedLauncher, out int processExitCode)
        {
            if (IsBeingDebugged)
            {
                var output = new List<string>();
                processExitCode = LaunchProcessWithDebuggerAttached(workingDirectory, command, param, printTestOutput, debuggedLauncher);
                return output;
            }

            var actualLauncher = new ProcessLauncher(TestEnvironment, TestEnvironment.Options.PathExtension);
            return actualLauncher.GetOutputOfCommand(workingDirectory, command, param, printTestOutput, 
                throwIfError, out processExitCode);
        }


        private int LaunchProcessWithDebuggerAttached(string workingDirectory, string command, string param, bool printTestOutput,
            IDebuggedProcessLauncher handle)
        {
            TestEnvironment.LogInfo("Attaching debugger to " + command);
            if (printTestOutput)
            {
                TestEnvironment.DebugInfo(
                    "Note that due to restrictions of the VS Unit Testing framework, the test executable's output can not be displayed in the test console when debugging tests!");
            }
            int processId = handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, TestEnvironment.Options.PathExtension);
            Process process = Process.GetProcessById(processId);
            ProcessWaiter waiter = new ProcessWaiter(process);
            waiter.WaitForExit();
            process.Dispose();
            return waiter.ProcessExitCode;
        }

    }

}