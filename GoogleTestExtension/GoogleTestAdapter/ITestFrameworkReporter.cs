using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter
{
    public interface ITestFrameworkReporter
    {
        void ReportTestResults(IFrameworkHandle frameworkHandle, IEnumerable<TestResult> testResults);
        void ReportTestsFound(ITestCaseDiscoverySink sink, IEnumerable<TestCase> testCases);
        void ReportTestsStarted(IFrameworkHandle frameworkHandle, IEnumerable<TestCase> testCases);
    }
}