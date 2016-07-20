using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Helpers
{

    public class TestProcessLauncher
    {
        private readonly TestEnvironment _testEnvironment;
        private readonly bool _isBeingDebugged;

        public TestProcessLauncher(TestEnvironment testEnvironment, bool isBeingDebugged)
        {
            _testEnvironment = testEnvironment;
            _isBeingDebugged = isBeingDebugged;
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
            if (_isBeingDebugged)
            {
                var output = new List<string>();
                processExitCode = LaunchProcessWithDebuggerAttached(workingDirectory, command, param, printTestOutput, debuggedLauncher);
                return output;
            }

            var actualLauncher = new ProcessLauncher(_testEnvironment, _testEnvironment.Options.GetPathExtension(command));
            return actualLauncher.GetOutputOfCommand(workingDirectory, command, param, printTestOutput, 
                throwIfError, out processExitCode);
        }


        private int LaunchProcessWithDebuggerAttached(string workingDirectory, string command, string param, bool printTestOutput,
            IDebuggedProcessLauncher handle)
        {
            _testEnvironment.LogInfo("Attaching debugger to " + command);
            if (printTestOutput)
            {
                _testEnvironment.DebugInfo(
                    "Note that due to restrictions of the VS Unit Testing framework, the test executable's output can not be displayed in the test console when debugging tests!");
            }
            int processId = handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, _testEnvironment.Options.GetPathExtension(command));
            Process process = Process.GetProcessById(processId);
            var waiter = new ProcessWaiter(process);
            waiter.WaitForExit();
            process.Dispose();
            return waiter.ProcessExitCode;
        }

    }

}