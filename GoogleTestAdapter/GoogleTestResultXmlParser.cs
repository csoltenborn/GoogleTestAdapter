using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Globalization;

namespace GoogleTestAdapter
{
    public class GoogleTestResultXmlParser
    {
        private const string ERROR_MSG_NO_XML_FILE = "Output file does not exist, did your tests crash?";

        private static readonly NumberFormatInfo NumberFormatInfo = new CultureInfo("en-US").NumberFormat;

        private readonly IMessageLogger Logger;

        private readonly string XmlResultFile;
        private readonly List<TestCase> TestCases;

        public GoogleTestResultXmlParser(string xmlResultFile, IEnumerable<TestCase> testCases, IMessageLogger logger)
        {
            this.Logger = logger;
            this.XmlResultFile = xmlResultFile;
            this.TestCases = testCases.ToList();
        }

        public List<TestResult> getTestResults()
        {
            if (File.Exists(XmlResultFile))
            {
                return ParseTestResults();
            }

            Logger.SendMessage(TestMessageLevel.Warning, ERROR_MSG_NO_XML_FILE);
            return new List<TestResult>();
        }

        private List<TestResult> ParseTestResults()
        {
            List<TestResult> results = new List<TestResult>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(XmlResultFile);
                Logger.SendMessage(TestMessageLevel.Informational, "Opened results from " + XmlResultFile);

                XmlNodeList testsuiteNodes = doc.DocumentElement.SelectNodes("/testsuites/testsuite");
                foreach (XmlNode testsuiteNode in testsuiteNodes)
                {
                    XmlNodeList testcaseNodes = testsuiteNode.SelectNodes("testcase");
                    foreach (XmlNode testcaseNode in testcaseNodes)
                    {
                        results.Add(ParseTestResult(testcaseNode));
                    }
                }

            }
            catch (XmlException e)
            {
                Logger.SendMessage(TestMessageLevel.Warning, "Test result file could not be parsed (completely) - your test has probably crashed. Exception message: " + e.Message);
            }

            return results;
        }

        private TestResult ParseTestResult(XmlNode testcaseNode)
        {
            string className = testcaseNode.Attributes["classname"].InnerText;
            string testcaseName = testcaseNode.Attributes["name"].InnerText;
            string qualifiedName = className + "." + testcaseName;

            TestResult testresult = new TestResult(TestCases.FindTestcase(qualifiedName));

            testresult.ComputerName = Environment.MachineName;
            testresult.DisplayName = " ";

            string duration = testcaseNode.Attributes["time"].InnerText;
            testresult.Duration = ParseDuration(duration);

            string testcaseStatus = testcaseNode.Attributes["status"].InnerText;
            switch (testcaseStatus)
            {
                case "run":
                    XmlNodeList failureNodes = testcaseNode.SelectNodes("failure");
                    if (failureNodes.Count == 0)
                    {
                        testresult.Outcome = TestOutcome.Passed;
                    }
                    else
                    {
                        testresult.Outcome = TestOutcome.Failed;
                        testresult.ErrorMessage = CreateErrorMessage(failureNodes);
                    }
                    break;
                case "notrun":
                    testresult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    string Msg = "Unknown testcase status: " + testcaseStatus + ". Please send this information to the developer.";
                    Logger.SendMessage(TestMessageLevel.Error, Msg);
                    throw new Exception(Msg);
            }

            return testresult;
        }

        private string CreateErrorMessage(XmlNodeList failureNodes)
        {
            List<string> errorMessages = new List<string>();
            foreach (XmlNode failureNode in failureNodes)
            {
                errorMessages.Add(failureNode.InnerText);
            }
            return string.Join("\n\n", errorMessages);
        }

        private TimeSpan ParseDuration(string duration)
        {
            double Duration = double.Parse(duration, NumberFormatInfo);
            if (Duration <= 0.001)
            {
                Duration = 0.001;
            }
            return TimeSpan.FromSeconds(Duration);
        }

    }

}