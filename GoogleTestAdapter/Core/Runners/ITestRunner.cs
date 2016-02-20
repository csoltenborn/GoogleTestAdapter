using System.Collections.Generic;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Runners
{

    public interface ITestRunner
    {
        // TODO remove isBeingDebugged parameter (use debuggedLauncher != null)
        void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, 
            string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher);

        void Cancel();
    }

}