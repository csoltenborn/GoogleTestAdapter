using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Execution
{
    interface IGoogleTestRunner
    {
        void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle, string userParameters);
        void Cancel();
    }
}