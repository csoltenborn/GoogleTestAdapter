using System.Collections.Generic;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Runners
{
    public interface ITestRunner
    {
        void RunTests(IEnumerable<TestCase2> allTestCases, IEnumerable<TestCase2> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle);

        void Cancel();
    }
}