using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace GoogleTestAdapter.Scheduling
{
    [Serializable]
    public struct SerializableKeyValuePair<K, V>
    {
        public SerializableKeyValuePair(K key, V value)
        {
            Key = key;
            Value = value;
        }

        public K Key
        { get; set; }

        public V Value
        { get; set; }
    }

    [Serializable]
    public class TestDurationsContainer
    {
        public string Executable { get; set; }
        public List<SerializableKeyValuePair<string, int>> TestDurations { get; set; } = new List<SerializableKeyValuePair<string, int>>();
    }

    public class TestDurationSerializer
    {
        private static readonly SerializableKeyValuePair<string, int> DEFAULT = new SerializableKeyValuePair<string, int>();

        private readonly XmlSerializer serializer = new XmlSerializer(typeof(TestDurationsContainer));

        public IDictionary<TestCase, int> ReadTestDurations(IEnumerable<TestCase> testcases)
        {
            IDictionary<string, List<TestCase>> GroupedTestcases = GoogleTestExecutor.GroupTestcasesByExecutable(testcases);
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            foreach (string executable in GroupedTestcases.Keys)
            {
                durations = durations.Union(ReadTestDurations(executable, GroupedTestcases[executable])).ToDictionary(KVP => KVP.Key, KVP => KVP.Value);
            }
            return durations;
        }

        public void UpdateTestDurations(IEnumerable<TestResult> testResults)
        {
            IDictionary<string, List<TestResult>> GroupedTestcases = GroupTestResultsByExecutable(testResults);
            foreach (string executable in GroupedTestcases.Keys)
            {
                UpdateTestDurations(executable, GroupedTestcases[executable]);
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
                SerializableKeyValuePair<string, int> pair = container.TestDurations.FirstOrDefault(P => P.Key == testcase.FullyQualifiedName);
                if (!pair.Equals(DEFAULT))
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

            foreach (TestResult testResult in testresults)
            {
                SerializableKeyValuePair<string, int> pair = container.TestDurations.FirstOrDefault(P => P.Key == testResult.TestCase.FullyQualifiedName);
                if (!pair.Equals(DEFAULT))
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
            TestDurationsContainer container = serializer.Deserialize(fileStream) as TestDurationsContainer;
            fileStream.Close();
            return container;
        }

        private void SaveTestDurations(TestDurationsContainer durations, string durationsFile)
        {
            TextWriter fileStream = new StreamWriter(durationsFile);
            serializer.Serialize(fileStream, durations);
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
            return executable + ".gtatestdurations";
        }

    }

}