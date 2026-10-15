using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleTestAdapter.Settings
{
    /// <summary>
    /// Properties of a test as configured with CMake: working directory and environment (WORKING_DIRECTORY and
    /// ENVIRONMENT, provided by Visual Studio for tests of CMake projects or read from CTest), labels, timeout,
    /// and whether the test is disabled (LABELS, TIMEOUT, and DISABLED, read from CTest).
    /// </summary>
    public class TestPropertySettings : IEquatable<TestPropertySettings>
    {
        /// <summary>
        /// Compares the settings which affect how a test executable is run, i.e., working directory and environment.
        /// </summary>
        public static readonly IEqualityComparer<TestPropertySettings> ExecutionEnvironmentComparer = new ExecutionEnvironmentEqualityComparer();

        public string WorkingDirectory { get; }
        public IDictionary<string, string> Environment { get; }
        public IReadOnlyList<string> Labels { get; }
        public TimeSpan? Timeout { get; }
        public bool Disabled { get; }

        public TestPropertySettings(string workingDirectory, IDictionary<string, string> environment,
            IEnumerable<string> labels = null, TimeSpan? timeout = null, bool disabled = false)
        {
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory;
            Environment = new Dictionary<string, string>(environment ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            Labels = (labels ?? Enumerable.Empty<string>()).Where(l => !string.IsNullOrWhiteSpace(l)).Distinct().ToList();
            // CTest: a timeout of 0 means no timeout
            Timeout = timeout > TimeSpan.Zero ? timeout : null;
            Disabled = disabled;
        }

        public bool Equals(TestPropertySettings other)
        {
            return ExecutionEnvironmentComparer.Equals(this, other)
                   && Labels.SequenceEqual(other.Labels)
                   && Timeout == other.Timeout
                   && Disabled == other.Disabled;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TestPropertySettings);
        }

        public override int GetHashCode()
        {
            return ExecutionEnvironmentComparer.GetHashCode(this);
        }

        public override string ToString()
        {
            string environment = string.Join(", ", Environment.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            string result = $"working directory: '{WorkingDirectory}', environment: {{{environment}}}";
            if (Labels.Count > 0)
                result += $", labels: {{{string.Join(", ", Labels)}}}";
            if (Timeout.HasValue)
                result += $", timeout: {Timeout.Value.TotalSeconds}s";
            if (Disabled)
                result += ", disabled";
            return result;
        }

        private class ExecutionEnvironmentEqualityComparer : IEqualityComparer<TestPropertySettings>
        {
            public bool Equals(TestPropertySettings x, TestPropertySettings y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x is null || y is null)
                    return false;

                return string.Equals(x.WorkingDirectory, y.WorkingDirectory, StringComparison.OrdinalIgnoreCase)
                       && x.Environment.Count == y.Environment.Count
                       && x.Environment.All(kvp => y.Environment.TryGetValue(kvp.Key, out string value) && value == kvp.Value);
            }

            public int GetHashCode(TestPropertySettings settings)
            {
                return settings == null
                    ? 0
                    : StringComparer.OrdinalIgnoreCase.GetHashCode(settings.WorkingDirectory ?? "") ^ settings.Environment.Count;
            }
        }
    }

    public class TestPropertySettingsContainer
    {
        public class TestProperties
        {
            public string Name { get; set; }
            public string Command { get; set; }
            public string WorkingDirectory { get; set; }
            public IDictionary<string, string> Environment { get; set; }
            public IList<string> Labels { get; set; }
            public TimeSpan? Timeout { get; set; }
            public bool Disabled { get; set; }
        }

        private readonly IDictionary<string, IDictionary<string, TestPropertySettings>> _settingsByExecutable =
            new Dictionary<string, IDictionary<string, TestPropertySettings>>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, TestPropertySettings> _commonSettingsByExecutable =
            new Dictionary<string, TestPropertySettings>(StringComparer.OrdinalIgnoreCase);

        public TestPropertySettingsContainer(IEnumerable<TestProperties> tests)
        {
            foreach (var test in tests)
            {
                string executable = GetFullPath(test.Command);
                if (executable == null || string.IsNullOrEmpty(test.Name))
                    continue;

                if (!_settingsByExecutable.TryGetValue(executable, out var settingsOfTests))
                {
                    settingsOfTests = new Dictionary<string, TestPropertySettings>();
                    _settingsByExecutable.Add(executable, settingsOfTests);
                }
                // CMake might provide the same test more than once - first one wins
                if (!settingsOfTests.ContainsKey(test.Name))
                    settingsOfTests.Add(test.Name, new TestPropertySettings(
                        test.WorkingDirectory, test.Environment, test.Labels, test.Timeout, test.Disabled));
            }

            foreach (var executableAndSettings in _settingsByExecutable)
            {
                var distinctSettings = executableAndSettings.Value.Values.Distinct().ToList();
                if (distinctSettings.Count == 1)
                    _commonSettingsByExecutable.Add(executableAndSettings.Key, distinctSettings[0]);
            }
        }

        public bool IsEmpty => _settingsByExecutable.Count == 0;

        public bool ContainsExecutable(string executable)
        {
            string fullPath = GetFullPath(executable);
            return fullPath != null && _settingsByExecutable.ContainsKey(fullPath);
        }

        /// <summary>
        /// Settings shared by all tests of the executable, null if there are none or if they differ between tests.
        /// </summary>
        public TestPropertySettings GetSettingsForExecutable(string executable)
        {
            string fullPath = GetFullPath(executable);
            return fullPath != null && _commonSettingsByExecutable.TryGetValue(fullPath, out var settings)
                ? settings
                : null;
        }

        public TestPropertySettings GetSettingsForTest(string executable, string fullyQualifiedName)
        {
            string fullPath = GetFullPath(executable);
            if (fullPath == null || !_settingsByExecutable.TryGetValue(fullPath, out var settingsOfTests))
                return null;

            if (settingsOfTests.TryGetValue(fullyQualifiedName, out var settings))
                return settings;

            // gtest_add_tests() names parameterized tests like "*/Suite.Test/*"
            int firstSlash = fullyQualifiedName.IndexOf('/');
            int lastSlash = fullyQualifiedName.LastIndexOf('/');
            if (firstSlash >= 0 && lastSlash > firstSlash
                && settingsOfTests.TryGetValue($"*{fullyQualifiedName.Substring(firstSlash, lastSlash - firstSlash)}/*", out settings))
                return settings;

            // names do not need to match the ones of Google Test (e.g., gtest_discover_tests() with TEST_PREFIX,
            // or pretty names of parameterized tests), but usually all tests of an executable share their settings
            return GetSettingsForExecutable(executable);
        }

        private static string GetFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return Path.GetFullPath(path.Trim('"'));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
