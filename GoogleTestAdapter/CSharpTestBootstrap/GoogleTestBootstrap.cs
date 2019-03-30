using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;


[ExcludeFromCodeCoverage]
public class UnitSuiteInfo
{
    public String SuiteName;

    /// <summary>
    /// Gets list of supported suites (classes with test methods)
    /// </summary>
    virtual public IEnumerable<UnitSuiteInfo> GetSuites()
    {
        return Enumerable.Empty<UnitSuiteInfo>();
    }

    /// <summary>
    /// Queries list of specific unit tests in specific suite (test methods in one class)
    /// </summary>
    /// <returns></returns>
    virtual public IEnumerable<UnitTestInfo> GetUnitTests()
    { 
        return Enumerable.Empty<UnitTestInfo>();
    }
}

[ExcludeFromCodeCoverage]
public class UnitTestInfo
{
    public String UnitTestName;

    /// <summary>
    /// Source code location, full path
    /// </summary>
    public String sourceCodePath = null;

    /// <summary>
    /// Source code line number
    /// </summary>
    public int line = 1;

    /// <summary>
    /// True if test will not be executed ([Ignore] attribute on), false - executed
    /// </summary>
    public bool ignored = false;

    /// <summary>
    /// Exception type which method is allowed to throw, null if exceptions are not allowed
    /// </summary>
    public Type ExceptionType = null;

    /// <summary>
    /// This method is must throw exception if test fails.
    /// </summary>
    /// <param name="isLastMethod">true if given method is last invoked, and api must clean up / release test class resources</param>
    /// <exception cref="OperationCanceledException">Can be thrown to cancel ongoing tests</exception>
    virtual public void InvokeTest(bool isLastMethod, TestResults localTestResults)
    { 
    }
}

[ExcludeFromCodeCoverage]
public class GoogleTestBootstrap
{
    //
    // https://stackoverflow.com/questions/3469368/how-to-handle-accessviolationexception
    //
    // We want to catch all exceptions here, even process corrupted exceptions.
    //
    [HandleProcessCorruptedStateExceptions]

    /// <summary>
    /// Starts google test console main function, which in a turn either lists available tests or executes tests
    /// </summary>
    /// <param name="runTests">true if run tests by default, false if not</param>
    /// <param name="args">command line arguments</param>
    /// <param name="suits">test suites to use for test discovery and execution</param>
    /// <returns>return true if command arguments were handled, false if not (application can continue)</returns>
    public static bool TestMain(bool runTests, string[] args, params UnitSuiteInfo[] suits)
    {
        var asm = Assembly.GetExecutingAssembly();
        String exeName = Path.GetFileName(asm.Location);
        bool bListTests = false;
        Dictionary<String, List<String>> filterClassMethodsToRun = null;
        XDocument testsuites = new XDocument( new XDeclaration("1.0", "utf-8", ""), new XElement("testsuites"));
        String slash = "[-/]+";     // "-tests", "--tests" or "/tests"

        // Google unit testing uses --gtest_output=xml:<file>, can be shorten to "-out:<file>"
        Regex reOutput = new Regex(slash + "(gtest_)?out(put=)?(xml)?:(.*)$");

        // Google unit testing uses --gtest_list_tests, can be shorten to "-tests"
        Regex reTests = new Regex(slash + "(gtest_list_)?tests");

        // Google unit testing uses --gtest_filter=, can be shorten to "-filter=<class name>.<method name>" or "-filter:..."
        Regex reFilter = new Regex(slash + "(gtest_)?filter.(.*)");

        // Additional command line argument to run testing - "-test" or "-t"
        Regex reDoTest = new Regex(slash + "t(est)?$");

        bool printGoogleUnitTestFormat = false;

        String xmlFilePath = null;
        StringBuilder errorMessageHeadline = new StringBuilder();
        StringBuilder errorMessage = new StringBuilder();
        Regex reStackFrame = new Regex("^ *at +(.*?) in +(.*):line ([0-9]+)$");

        foreach (var arg in args)
        {
            if (reDoTest.Match(arg).Success)
            {
                runTests = true;
                continue;
            }

            if (reTests.Match(arg).Success)
            {
                bListTests = true;
                runTests = false;
                printGoogleUnitTestFormat = true;
                continue;
            }

            var filtMatch = reFilter.Match(arg);
            if (filtMatch.Success)
            {
                runTests = true;
                printGoogleUnitTestFormat = true;
                filterClassMethodsToRun = new Dictionary<string, List<string>>();

                foreach (String classMethod in filtMatch.Groups[2].ToString().Split(':'))
                {
                    var items = classMethod.Split('.').ToArray();
                    if (items.Length < 2)
                        continue;

                    String className = items[0];

                    if (!filterClassMethodsToRun.ContainsKey(className))
                        filterClassMethodsToRun.Add(className, new List<string>());

                    filterClassMethodsToRun[className].Add(items[1]);
                }
                continue;
            }

            var match = reOutput.Match(arg);
            if (match.Success)
            {
                runTests = true;
                printGoogleUnitTestFormat = true;
                xmlFilePath = Path.GetFullPath(match.Groups[4].ToString());
            }
        }

        if (!runTests && !bListTests)
            return false;


        if (runTests && !printGoogleUnitTestFormat)
            Console.Write("Testing ");

        Stopwatch timer = new Stopwatch();
        TestResults totalTestResults = new TestResults();
        bool allowContinueTesting = true;
        Stopwatch totalTestTimer = new Stopwatch();
        totalTestTimer.Start();

        List<UnitSuiteInfo> testSuites = new List<UnitSuiteInfo>();

        foreach (var suiteLister in suits)
            testSuites.AddRange(suiteLister.GetSuites());

        // Sort alphabetically so would be executed in same order as in Test Explorer
        testSuites.Sort((a, b) => a.SuiteName.CompareTo(b.SuiteName));

        foreach (UnitSuiteInfo testSuite in testSuites)
        {
            String suiteName = testSuite.SuiteName;
            List<String> filterMethods = null;
            XElement testsuite = null;
            TestResults localTestResults = null;

            // Filter classes to execute
            if (filterClassMethodsToRun != null)
            {
                if (!filterClassMethodsToRun.ContainsKey(suiteName))
                    continue;

                filterMethods = filterClassMethodsToRun[suiteName];
            }

            if(printGoogleUnitTestFormat)
                Console.WriteLine(suiteName);

            List<UnitTestInfo> unitTests = testSuite.GetUnitTests().ToList();


            for (int i = 0; i < unitTests.Count; i++)
            {
                UnitTestInfo testinfo = unitTests[i];
                bool isLastMethod = i == unitTests.Count - 1;

                if (!isLastMethod)
                    isLastMethod = unitTests.Skip(i + 1).Take(unitTests.Count - i - 1).Select(x => x.ignored).Contains(false);

                if (bListTests)
                    Console.WriteLine("  <loc>" + testinfo.sourceCodePath + "(" + testinfo.line + ")");

                String unitTestName = testinfo.UnitTestName;
                String displayUnitTestName = unitTestName;

                if (unitTestName.Contains('.'))
                {
                    displayUnitTestName = Path.GetFileNameWithoutExtension(unitTestName) + "  # GetParam() = " + Path.GetExtension(unitTestName);
                    unitTestName = Path.GetFileNameWithoutExtension(unitTestName);
                }

                if (filterMethods != null)
                {
                    if (!filterMethods.Contains(unitTestName) && !filterMethods.Contains("*"))
                        continue;
                }

                // Info about current class in xml report
                if (localTestResults == null)
                {
                    localTestResults = new TestResults();
                    testsuite = new XElement("testsuite");
                    testsuite.Add(new XAttribute("name", suiteName));
                    testsuites.Root.Add(testsuite);
                }


                if (bListTests)
                {
                    Console.WriteLine("  " + displayUnitTestName);
                    localTestResults.tests++;
                    continue;
                }

                if (testinfo.ignored)
                {
                    localTestResults.disabled++;

                    if (printGoogleUnitTestFormat)
                        Console.WriteLine("[  SKIPPED ] " + suiteName + "." + unitTestName + " (0 ms)");

                    XElement skippedMethod = new XElement("testcase");
                    skippedMethod.Add(new XAttribute("name", unitTestName));
                    skippedMethod.Add(new XAttribute("status", "notrun"));
                    skippedMethod.Add(new XAttribute("time", 0));
                    skippedMethod.Add(new XAttribute("classname", suiteName));
                    testsuite.Add(skippedMethod);
                    continue;
                }

                if (printGoogleUnitTestFormat)
                    Console.WriteLine("[ RUN      ] " + suiteName + "." + unitTestName);

                try
                {
                    timer.Restart();

                    try
                    {
                        localTestResults.tests++;
                        errorMessageHeadline.Clear();
                        errorMessage.Clear();
                        testinfo.InvokeTest(isLastMethod, localTestResults);
                    }
                    finally
                    {
                        timer.Stop();
                    }
                }
                catch (Exception _ex)
                {
                    Exception ex = _ex;
                    if (_ex.InnerException != null)
                        ex = _ex.InnerException;

                    bool isFailure = true;
                    if (testinfo.ExceptionType == ex.GetType())
                        isFailure = false;

                    if (ex.GetType() == typeof(OperationCanceledException))
                        allowContinueTesting = false;

                    if (isFailure)
                    {
                        localTestResults.failures++;

                        String errorMsgShort = ex.Message;
                        errorMessage.AppendLine("Test method " + suiteName + "." + unitTestName + " threw exception: ");
                        errorMessage.AppendLine(ex.GetType().FullName + ": " + ex.Message);
                        errorMessage.AppendLine("Call stack:");

                        foreach (String frameEntry in ex.StackTrace.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var fmatch = reStackFrame.Match(frameEntry);

                            if (fmatch.Success)
                                errorMessage.AppendLine(fmatch.Groups[2] + "(" + fmatch.Groups[3] + "): " + fmatch.Groups[1]);
                        }

                        errorMessageHeadline.Append(errorMsgShort);
                    }
                }

                long elapsedTime = timer.ElapsedMilliseconds;
                XElement methodTestInfo = new XElement("testcase");
                methodTestInfo.Add(new XAttribute("name", unitTestName));
                methodTestInfo.Add(new XAttribute("status", "run"));
                methodTestInfo.Add(new XAttribute("time", elapsedTime / 1000.0));
                methodTestInfo.Add(new XAttribute("classname", suiteName));

                String testState = "[       OK ]";

                if (errorMessageHeadline.Length != 0)
                {
                    String msg = errorMessage.ToString();
                    String errorMessageOneLiner = errorMessageHeadline.ToString().Replace("\n", "").Replace("\r", "");


                    XElement failure = new XElement("failure", new XCData(msg));
                    failure.Add(new XAttribute("message", errorMessageOneLiner));
                    methodTestInfo.Add(failure);

                    // Needs to be printed to console window as well, otherwise not seen by visual studio
                    Console.WriteLine(msg);
                    //MessageBox.Show(
                    //    "errorMessageOneLiner: " + errorMessageOneLiner + "\r\n\r\n" +
                    //    "msg: " + msg
                    //);

                    testState = "[  FAILED  ]";
                }

                if (printGoogleUnitTestFormat)
                {
                    Console.WriteLine(testState + " " + suiteName + "." + unitTestName + " (" + elapsedTime.ToString() + " ms)");
                    Console.Out.Flush();
                }
                else
                {
                    Console.Write(".");
                }

                testsuite.Add(methodTestInfo);

                if (!allowContinueTesting)
                    break;
            }

            // class scan complete, fetch results if necessary
            if (testsuite != null)
            { 
                FetchTestResults(testsuite, localTestResults);
                totalTestResults.Add(localTestResults);
            }

            if (!allowContinueTesting)
                break;
        }

        totalTestTimer.Stop();

        // Save test results if requivested.
        if (xmlFilePath != null)
        {
            try
            {
                String dir = Path.GetDirectoryName(xmlFilePath);
                if (dir != "" && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                FetchTestResults(testsuites.Root, totalTestResults);
                testsuites.Save(xmlFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Could not save '" + xmlFilePath + ": " + ex.Message);
            }
        }

        if (runTests)
        {
            if (!printGoogleUnitTestFormat)
            { 
                Console.WriteLine(" ok.");

                String summaryLine = (totalTestResults.tests - totalTestResults.failures) + " tests passed";

                if( totalTestResults.files != 0 )
                    summaryLine += " (" + totalTestResults.files + " files verified)";

                if (totalTestResults.failures != 0 )
                    summaryLine += ", " + totalTestResults.failures + " FAILED";

                if (totalTestResults.disabled != 0)
                    summaryLine += ", " + totalTestResults.disabled + " skipped";

                Console.WriteLine(summaryLine);
                Console.WriteLine();
            }

            TimeSpan elapsedtime = totalTestTimer.Elapsed;
            String elapsed = "";
            if (elapsedtime.Minutes != 0)
                elapsed += elapsedtime.Minutes + " min ";

            elapsed += elapsedtime.ToString(@"ss\.ff") + " sec";
            Console.WriteLine("Test time: " + elapsed);
        }

        return true;
    }

    static void FetchTestResults(XElement node, TestResults tr)
    {
        foreach (var f in tr.GetType().GetFields())
        {
            String v = f.GetValue(tr).ToString();
            node.Add(new XAttribute(f.Name, v));
        }
    }

}

[ExcludeFromCodeCoverage]
public class TestResults
{
    public int tests = 0;
    public int files = 0;
    public int failures = 0;
    public int disabled = 0;
    public int errors = 0;

    public void Add(TestResults local)
    {
        tests += local.tests;
        files += local.files;
        failures += local.failures;
        disabled += local.disabled;
        errors += local.errors;
    }
};

