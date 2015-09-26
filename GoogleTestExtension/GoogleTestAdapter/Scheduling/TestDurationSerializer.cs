using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Scheduling
{
    [Serializable]
    public struct TestDuration
    {
        public TestDuration(string test, int duration)
        {
            Test = test;
            Duration = duration;
        }

        [XmlAttribute]
        public string Test
        { get; set; }

        [XmlAttribute]
        public int Duration
        { get; set; }
    }

    [Serializable]
    [XmlRoot]
    public class GTATestDurations
    {
        public string Executable { get; set; }
        public List<TestDuration> TestDurations { get; set; } = new List<TestDuration>();
    }

    class TestDurationSerializer
    {
        private const string FileEndingTestDurations = ".gta_testdurations";

        private static object Lock { get; } = new object();
        private static readonly TestDuration Default = new TestDuration();

        private XmlSerializer Serializer { get; } = new XmlSerializer(typeof(GTATestDurations));
        private TestEnvironment TestEnvironment { get; }

        internal TestDurationSerializer(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }

        internal IDictionary<TestCase, int> ReadTestDurations(IEnumerable<TestCase> testcases)
        {
            IDictionary<string, List<TestCase>> groupedTestcases = testcases.GroupByExecutable();
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string executable in groupedTestcases.Keys)
            {
                durations = durations.Union(ReadTestDurations(executable, groupedTestcases[executable])).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            return durations;
        }

        internal void UpdateTestDurations(IEnumerable<TestResult> testResults)
        {
            IDictionary<string, List<TestResult>> groupedTestcases = GroupTestResultsByExecutable(testResults);
            foreach (string executable in groupedTestcases.Keys)
            {
                lock (Lock)
                {
                    // TODO lock on file base
                    UpdateTestDurations(executable, groupedTestcases[executable]);
                }
            }
        }

        private IDictionary<TestCase, int> ReadTestDurations(string executable, List<TestCase> testcases)
        {
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            string durationsFile = GetDurationsFile(executable);
            if (!File.Exists(durationsFile))
            {
                return durations;
            }

            GTATestDurations container = LoadTestDurations(durationsFile);

            foreach (TestCase testcase in testcases)
            {
                TestDuration pair = container.TestDurations.FirstOrDefault(p => p.Test == testcase.FullyQualifiedName);
                if (!pair.Equals(Default))
                {
                    durations.Add(testcase, pair.Duration);
                }
            }

            return durations;
        }

        private void UpdateTestDurations(string executable, List<TestResult> testresults)
        {
            string durationsFile = GetDurationsFile(executable);
            GTATestDurations container = File.Exists(durationsFile) ? LoadTestDurations(durationsFile) : new GTATestDurations();
            container.Executable = Path.GetFullPath(executable);

            foreach (TestResult testResult in testresults.Where(tr => tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))
            {
                TestDuration pair = container.TestDurations.FirstOrDefault(p => p.Test == testResult.TestCase.FullyQualifiedName);
                if (!pair.Equals(Default))
                {
                    container.TestDurations.Remove(pair);
                }
                container.TestDurations.Add(new TestDuration(testResult.TestCase.FullyQualifiedName, GetDuration(testResult)));
            }

            SaveTestDurations(container, durationsFile);
        }

        private GTATestDurations LoadTestDurations(string durationsFile)
        {
            FileStream fileStream = new FileStream(durationsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            GTATestDurations container = Serializer.Deserialize(fileStream) as GTATestDurations;
            fileStream.Close();
            return container;
        }

        private void SaveTestDurations(GTATestDurations durations, string durationsFile)
        {
            TextWriter fileStream = new StreamWriter(durationsFile);
            Serializer.Serialize(fileStream, durations);
            fileStream.Close();
        }

        private IDictionary<string, List<TestResult>> GroupTestResultsByExecutable(IEnumerable<TestResult> testresults)
        {
            Dictionary<string, List<TestResult>> groupedTestResults = new Dictionary<string, List<TestResult>>();
            foreach (TestResult testResult in testresults)
            {
                List<TestResult> group;
                if (groupedTestResults.ContainsKey(testResult.TestCase.Source))
                {
                    group = groupedTestResults[testResult.TestCase.Source];
                }
                else
                {
                    group = new List<TestResult>();
                    groupedTestResults.Add(testResult.TestCase.Source, group);
                }
                group.Add(testResult);
            }
            return groupedTestResults;
        }

        private int GetDuration(TestResult testResult)
        {
            return (int)Math.Ceiling(testResult.Duration.TotalMilliseconds);
        }

        private string GetDurationsFile(string executable)
        {
            return executable + FileEndingTestDurations;
        }

    }

}