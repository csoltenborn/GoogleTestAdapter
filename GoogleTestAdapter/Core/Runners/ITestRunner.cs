using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Runners
{

    public interface ITestRunner
    {
        void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, string workingDir,
            string userParameters);

        void Cancel();
    }

}