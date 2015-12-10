using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class XmlTestResultParser
    {
        private const string ErrorMsgNoXmlFile = "Output file does not exist, did your tests crash?";

        private static readonly NumberFormatInfo NumberFormatInfo = new CultureInfo("en-US").NumberFormat;


        private TestEnvironment TestEnvironment { get; }
        private string XmlResultFile { get; }
        private List<TestCase> TestCasesRun { get; }


        public XmlTestResultParser(IEnumerable<TestCase> testCasesRun, string xmlResultFile, TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
            this.XmlResultFile = xmlResultFile;
            this.TestCasesRun = testCasesRun.ToList();
        }


        public List<TestResult2> GetTestResults()
        {
            if (File.Exists(XmlResultFile))
            {
                return ParseTestResults();
            }

            TestEnvironment.LogWarning(ErrorMsgNoXmlFile);
            return new List<TestResult2>();
        }


        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private List<TestResult2> ParseTestResults()
        {
            List<TestResult2> testResults = new List<TestResult2>();
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(XmlResultFile);
                TestEnvironment.DebugInfo("Loaded test results from " + XmlResultFile);

                XmlNodeList testsuiteNodes = xmlDocument.DocumentElement.SelectNodes("/testsuites/testsuite");
                foreach (XmlNode testsuiteNode in testsuiteNodes)
                {
                    XmlNodeList testcaseNodes = testsuiteNode.SelectNodes("testcase");
                    testResults.AddRange(testcaseNodes.Cast<XmlNode>().Select(ParseTestResult).Where(xn => xn != null));
                }
            }
            catch (XmlException e)
            {
                TestEnvironment.DebugWarning("Test result file " + XmlResultFile + " could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message);
            }
            catch (NullReferenceException e)
            {
                TestEnvironment.DebugWarning("Test result file " + XmlResultFile + " could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message);
            }

            return testResults;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private TestResult2 ParseTestResult(XmlNode testcaseNode)
        {
            string className = testcaseNode.Attributes["classname"].InnerText;
            string testCaseName = testcaseNode.Attributes["name"].InnerText;
            string qualifiedName = className + "." + testCaseName;

            TestCase testCase = TestCasesRun.FindTestcase(qualifiedName);
            if (testCase == null)
            {
                return null;
            }

            TestResult2 testResult = new TestResult2(testCase)
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
                        testResult.Outcome = TestOutcome2.Passed;
                    }
                    else
                    {
                        testResult.Outcome = TestOutcome2.Failed;
                        testResult.ErrorMessage = CreateErrorMessage(failureNodes);
                    }
                    break;
                case "notrun":
                    testResult.Outcome = TestOutcome2.Skipped;
                    break;
                default:
                    string msg = "Unknown testcase status: " + testCaseStatus;
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