using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Scheduling
{
    [Serializable]
    [XmlRoot]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public class GtaTestDurations
    {
        public string Executable { get; set; }
        public List<TestDuration> TestDurations { get; set; } = new List<TestDuration>();
    }

    [Serializable]
    public struct TestDuration
    {
        public TestDuration(string test, int duration)
        {
            Test = test;
            Duration = duration;
        }

        [XmlAttribute]
        public string Test { get; set; }

        [XmlAttribute]
        public int Duration { get; set; }
    }


    public class TestDurationSerializer
    {
        private static object Lock { get; } = new object();

        private readonly XmlSerializer _serializer = new XmlSerializer(typeof(GtaTestDurations));


        public IDictionary<TestCase, int> ReadTestDurations(IEnumerable<TestCase> testcases)
        {
            IDictionary<string, List<TestCase>> groupedTestcases = testcases.GroupByExecutable();
            var durations = new Dictionary<TestCase, int>();
            foreach (string executable in groupedTestcases.Keys)
            {
                durations = durations.Union(ReadTestDurations(executable, groupedTestcases[executable])).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            return durations;
        }

        public void UpdateTestDurations(IEnumerable<TestResult> testResults)
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
            var durations = new Dictionary<TestCase, int>();
            string durationsFile = GetDurationsFile(executable);
            if (!File.Exists(durationsFile))
            {
                return durations;
            }

            GtaTestDurations container;
            lock (Lock)
            {
                container = LoadTestDurations(durationsFile);
            }

            IDictionary<string, TestDuration> durationsMap = container.TestDurations.ToDictionary(x => x.Test, x => x);
            foreach (TestCase testcase in testcases)
            {
                TestDuration pair;
                if (durationsMap.TryGetValue(testcase.FullyQualifiedName, out pair))
                    durations.Add(testcase, pair.Duration);
            }

            return durations;
        }

        private void UpdateTestDurations(string executable, List<TestResult> testresults)
        {
            string durationsFile = GetDurationsFile(executable);
            GtaTestDurations container = File.Exists(durationsFile) ? LoadTestDurations(durationsFile) : new GtaTestDurations();
            container.Executable = Path.GetFullPath(executable);

            IDictionary<string, TestDuration> durations = container.TestDurations.ToDictionary(x => x.Test, x => x);
            foreach (TestResult testResult in 
                testresults.Where(tr => tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))
            {
                durations[testResult.TestCase.FullyQualifiedName] =
                    new TestDuration(testResult.TestCase.FullyQualifiedName, GetDuration(testResult));
            }

            container.TestDurations.Clear();
            container.TestDurations.AddRange(durations.Values);

            SaveTestDurations(container, durationsFile);
        }

        private GtaTestDurations LoadTestDurations(string durationsFile)
        {
            using (var fileStream = new FileStream(durationsFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return _serializer.Deserialize(fileStream) as GtaTestDurations;
            }
        }

        private void SaveTestDurations(GtaTestDurations durations, string durationsFile)
        {
            using (var fileStream = new StreamWriter(durationsFile))
            {
                _serializer.Serialize(fileStream, durations);
            }
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
            return executable + GoogleTestConstants.DurationsExtension;
        }

    }

}