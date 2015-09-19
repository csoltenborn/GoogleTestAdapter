using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace GoogleTestAdapter.Scheduling
{
    [Serializable]
    public struct SerializableKeyValuePair<TK, TV>
    {
        public SerializableKeyValuePair(TK key, TV value)
        {
            Key = key;
            Value = value;
        }

        public TK Key
        { get; set; }

        public TV Value
        { get; set; }
    }

    [Serializable]
    public class TestDurationsContainer
    {
        public string Executable { get; set; }
        public List<SerializableKeyValuePair<string, int>> TestDurations { get; set; } = new List<SerializableKeyValuePair<string, int>>();
    }

    internal class TestDurationSerializer
    {
        private static object Lock { get; } = new object();
        private static readonly SerializableKeyValuePair<string, int> Default = new SerializableKeyValuePair<string, int>();

        private XmlSerializer Serializer { get; } = new XmlSerializer(typeof (TestDurationsContainer));

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

            TestDurationsContainer container = LoadTestDurations(durationsFile);

            foreach (TestCase testcase in testcases)
            {
                SerializableKeyValuePair<string, int> pair = container.TestDurations.FirstOrDefault(p => p.Key == testcase.FullyQualifiedName);
                if (!pair.Equals(Default))
                {
                    durations.Add(testcase, pair.Value);
                }
            }

            return durations;
        }

        private void UpdateTestDurations(string executable, List<TestResult> testresults)
        {
            string durationsFile = GetDurationsFile(executable);
            TestDurationsContainer container = File.Exists(durationsFile) ? LoadTestDurations(durationsFile) : new TestDurationsContainer();
            container.Executable = executable;

            foreach (TestResult testResult in testresults.Where(tr => tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))
            {
                SerializableKeyValuePair<string, int> pair = container.TestDurations.FirstOrDefault(p => p.Key == testResult.TestCase.FullyQualifiedName);
                if (!pair.Equals(Default))
                {
                    container.TestDurations.Remove(pair);
                }
                container.TestDurations.Add(new SerializableKeyValuePair<string, int>(testResult.TestCase.FullyQualifiedName, GetDuration(testResult)));
            }

            SaveTestDurations(container, durationsFile);
        }

        private TestDurationsContainer LoadTestDurations(string durationsFile)
        {
            FileStream fileStream = new FileStream(durationsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            TestDurationsContainer container = Serializer.Deserialize(fileStream) as TestDurationsContainer;
            fileStream.Close();
            return container;
        }

        private void SaveTestDurations(TestDurationsContainer durations, string durationsFile)
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
            return executable + Constants.FileEndingTestDurations;
        }

    }

}