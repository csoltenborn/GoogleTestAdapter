using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class SequentialTestRunnerTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void CancelingRunnerStopsTestExecution()
        {
            List<TestCase2> allTestCases = AllTestCasesOfConsoleApplication1;
            List<TestCase2> testCasesToRun = GetTestCasesOfConsoleApplication1("Crashing.LongRunning", "LongRunningTests.Test3");

            Stopwatch stopwatch = new Stopwatch();
            ITestRunner runner = new SequentialTestRunner(MockFrameworkReporter.Object, TestEnvironment);
            Thread thread = new Thread(() => runner.RunTests(allTestCases, testCasesToRun, "", MockRunContext.Object, MockFrameworkHandle.Object));

            stopwatch.Start();
            thread.Start();
            Thread.Sleep(1000);
            runner.Cancel();
            thread.Join();
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 2000); // 1st test should be executed
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000); // 2nd test should not be executed 
        }

    }

}