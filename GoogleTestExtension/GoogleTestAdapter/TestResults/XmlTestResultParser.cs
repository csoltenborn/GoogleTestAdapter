using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestResults
{
    class XmlTestResultParser
    {
        private const string ErrorMsgNoXmlFile = "GTA: Output file does not exist, did your tests crash?";

        private static readonly NumberFormatInfo NumberFormatInfo = new CultureInfo("en-US").NumberFormat;

        private TestEnvironment TestEnvironment { get; }
        private string XmlResultFile { get; }
        private List<TestCase> TestCasesRun { get; }

        internal XmlTestResultParser(IEnumerable<TestCase> testCasesRun, string xmlResultFile, TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
            this.XmlResultFile = xmlResultFile;
            this.TestCasesRun = testCasesRun.ToList();
        }

        internal List<TestResult> GetTestResults()
        {
            if (File.Exists(XmlResultFile))
            {
                return ParseTestResults();
            }

            TestEnvironment.LogWarning(ErrorMsgNoXmlFile);
            return new List<TestResult>();
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private List<TestResult> ParseTestResults()
        {
            List<TestResult> testResults = new List<TestResult>();
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(XmlResultFile);
                TestEnvironment.LogInfo("GTA: Loaded test results from " + XmlResultFile, TestEnvironment.LogType.UserDebug);

                XmlNodeList testsuiteNodes = xmlDocument.DocumentElement.SelectNodes("/testsuites/testsuite");
                foreach (XmlNode testsuiteNode in testsuiteNodes)
                {
                    XmlNodeList testcaseNodes = testsuiteNode.SelectNodes("testcase");
                    testResults.AddRange(testcaseNodes.Cast<XmlNode>().Select(ParseTestResult).Where(xn => xn != null));
                }
            }
            catch (XmlException e)
            {
                TestEnvironment.LogWarning("GTA: Test result file " + XmlResultFile + " could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message, TestEnvironment.LogType.UserDebug);
            }
            catch (NullReferenceException e)
            {
                TestEnvironment.LogWarning("GTA: Test result file " + XmlResultFile + " could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message, TestEnvironment.LogType.UserDebug);
            }

            return testResults;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private TestResult ParseTestResult(XmlNode testcaseNode)
        {
            string className = testcaseNode.Attributes["classname"].InnerText;
            string testCaseName = testcaseNode.Attributes["name"].InnerText;
            string qualifiedName = className + "." + testCaseName;

            TestCase testCase = TestCasesRun.FindTestcase(qualifiedName);
            if (testCase == null)
            {
                return null;
            }

            TestResult testResult = new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = " "
            };

            string duration = testcaseNode.Attributes["time"].InnerText;
            testResult.Duration = ParseDuration(duration);

            string testCaseStatus = testcaseNode.Attributes["status"].InnerText;
            switch (testCaseStatus)
            {
                case "run":
                    XmlNodeList failureNodes = testcaseNode.SelectNodes("failure");
                    if (failureNodes.Count == 0)
                    {
                        testResult.Outcome = TestOutcome.Passed;
                    }
                    else
                    {
                        testResult.Outcome = TestOutcome.Failed;
                        testResult.ErrorMessage = CreateErrorMessage(failureNodes);
                    }
                    break;
                case "notrun":
                    testResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    string msg = "GTA: Unknown testcase status: " + testCaseStatus;
                    TestEnvironment.LogError(msg);
                    throw new Exception(msg);
            }

            return testResult;
        }

        private string CreateErrorMessage(XmlNodeList failureNodes)
        {
            IEnumerable<string> errorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText);
            return string.Join("\n\n", errorMessages);
        }

        private TimeSpan ParseDuration(string durationString)
        {
            double duration = double.Parse(durationString, NumberFormatInfo);
            if (duration <= 0.001)
            {
                duration = 0.001;
            }
            return TimeSpan.FromSeconds(duration);
        }

    }

}