using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Runners
{
    public interface ITestRunner
    {
        // TODO remove isBeingDebugged parameter (use debuggedLauncher != null)
        void RunTests(IEnumerable<TestCase2> allTestCases, IEnumerable<TestCase2> testCasesToRun,
            string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher);

        void Cancel();
    }
}