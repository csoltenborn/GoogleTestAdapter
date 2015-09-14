using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Linq;
using System.Globalization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    public class XmlTestResultParser
    {
        private const string ERROR_MSG_NO_XML_FILE = "Output file does not exist, did your tests crash?";

        private static readonly NumberFormatInfo NUMBER_FORMAT_INFO = new CultureInfo("en-US").NumberFormat;

        private readonly IMessageLogger Logger;

        private readonly string XmlResultFile;
        private readonly List<TestCase> TestCasesRun;

        public XmlTestResultParser(string xmlResultFile, IEnumerable<TestCase> testCases, IMessageLogger logger)
        {
            this.Logger = logger;
            this.XmlResultFile = xmlResultFile;
            this.TestCasesRun = testCases.ToList();
        }

        public List<TestResult> GetTestResults()
        {
            if (File.Exists(XmlResultFile))
            {
                return ParseTestResults();
            }

            Logger.SendMessage(TestMessageLevel.Warning, ERROR_MSG_NO_XML_FILE);
            return new List<TestResult>();
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private List<TestResult> ParseTestResults()
        {
            List<TestResult> TestResults = new List<TestResult>();
            try
            {
                XmlDocument XmlDocument = new XmlDocument();
                XmlDocument.Load(XmlResultFile);
                DebugUtils.LogUserDebugMessage(Logger, new GoogleTestAdapterOptions(), TestMessageLevel.Informational, "Loaded test results from " + XmlResultFile);

                XmlNodeList TestsuiteNodes = XmlDocument.DocumentElement.SelectNodes("/testsuites/testsuite");
                foreach (XmlNode TestsuiteNode in TestsuiteNodes)
                {
                    XmlNodeList TestcaseNodes = TestsuiteNode.SelectNodes("testcase");
                    TestResults.AddRange(TestcaseNodes.Cast<XmlNode>().Select(ParseTestResult).Where(Result => Result != null));
                }
            }
            catch (XmlException e)
            {
                Logger.SendMessage(TestMessageLevel.Warning, "Test result file could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message);
            }
            catch (NullReferenceException e)
            {
                Logger.SendMessage(TestMessageLevel.Warning, "Test result file could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message);
            }

            return TestResults;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private TestResult ParseTestResult(XmlNode testcaseNode)
        {
            string ClassName = testcaseNode.Attributes["classname"].InnerText;
            string TestCaseName = testcaseNode.Attributes["name"].InnerText;
            string QualifiedName = ClassName + "." + TestCaseName;

            TestCase TestCase = TestCasesRun.FindTestcase(QualifiedName);
            if (TestCase == null)
            {
                return null;
            }

            TestResult TestResult = new TestResult(TestCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = " "
            };

            string Duration = testcaseNode.Attributes["time"].InnerText;
            TestResult.Duration = ParseDuration(Duration);

            string TestCaseStatus = testcaseNode.Attributes["status"].InnerText;
            switch (TestCaseStatus)
            {
                case "run":
                    XmlNodeList FailureNodes = testcaseNode.SelectNodes("failure");
                    if (FailureNodes.Count == 0)
                    {
                        TestResult.Outcome = TestOutcome.Passed;
                    }
                    else
                    {
                        TestResult.Outcome = TestOutcome.Failed;
                        TestResult.ErrorMessage = CreateErrorMessage(FailureNodes);
                    }
                    break;
                case "notrun":
                    TestResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    string Msg = "Unknown testcase status: " + TestCaseStatus + ". Please send this information to the developer.";
                    Logger.SendMessage(TestMessageLevel.Error, Msg);
                    throw new Exception(Msg);
            }

            return TestResult;
        }

        private string CreateErrorMessage(XmlNodeList failureNodes)
        {
            IEnumerable<string> ErrorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText);
            return string.Join("\n\n", ErrorMessages);
        }

        private TimeSpan ParseDuration(string duration)
        {
            double Duration = double.Parse(duration, NUMBER_FORMAT_INFO);
            if (Duration <= 0.001)
            {
                Duration = 0.001;
            }
            return TimeSpan.FromSeconds(Duration);
        }

    }

}