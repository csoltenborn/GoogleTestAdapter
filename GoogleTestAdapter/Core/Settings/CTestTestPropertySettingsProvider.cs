using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Settings
{
    /// <summary>
    /// Provides the test properties of executables built by CMake (e.g. LABELS, TIMEOUT, DISABLED, WORKING_DIRECTORY,
    /// ENVIRONMENT), as reported by "ctest --show-only=json-v1" within the executable's build tree.
    /// </summary>
    public class CTestTestPropertySettingsProvider
    {
        public const string CMakeCacheFile = "CMakeCache.txt";
        public const string GoogleTestFilterOption = "--gtest_filter=";

        private static readonly TimeSpan CTestTimeout = TimeSpan.FromSeconds(30);

        // shared by all instances, since test discovery and execution might happen within the same process
        private static readonly IDictionary<string, CacheEntry> Cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly object CacheLock = new object();

        private class CacheEntry
        {
            public DateTime CreationTimeUtc { get; set; }
            public TestPropertySettingsContainer Container { get; set; }
        }

        private readonly ILogger _logger;

        public CTestTestPropertySettingsProvider(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// The test properties of the executable's build tree, null if the executable has not been built by CMake
        /// or if CTest does not know any tests of the executable.
        /// </summary>
        public virtual TestPropertySettingsContainer GetContainer(string executable)
        {
            try
            {
                string buildDir = FindBuildDir(executable);
                if (buildDir == null)
                    return null;

                lock (CacheLock)
                {
                    // rebuilding the executable or reconfiguring the build tree invalidates the test properties
                    string cacheKey = Path.GetFullPath(executable);
                    DateTime lastChangeUtc = new[]
                    {
                        File.GetLastWriteTimeUtc(executable),
                        File.GetLastWriteTimeUtc(Path.Combine(buildDir, CMakeCacheFile)),
                        File.GetLastWriteTimeUtc(Path.Combine(buildDir, "CTestTestfile.cmake"))
                    }.Max();
                    if (Cache.TryGetValue(cacheKey, out CacheEntry entry) && entry.CreationTimeUtc >= lastChangeUtc)
                        return entry.Container;

                    entry = new CacheEntry
                    {
                        CreationTimeUtc = DateTime.UtcNow,
                        Container = ReadContainer(buildDir, executable)
                    };
                    Cache[cacheKey] = entry;
                    return entry.Container;
                }
            }
            catch (Exception e)
            {
                _logger.DebugWarning($"Could not read CTest test properties of executable '{executable}': {e.Message}");
                return null;
            }
        }

        private TestPropertySettingsContainer ReadContainer(string buildDir, string executable)
        {
            IDictionary<string, string> cacheEntries = ReadCMakeCache(Path.Combine(buildDir, CMakeCacheFile));
            string ctest = GetCTestExecutable(cacheEntries);
            if (ctest == null)
            {
                _logger.DebugInfo($"Could not find ctest executable of CMake build tree '{buildDir}'");
                return null;
            }

            foreach (string configuration in GetCandidateConfigurations(cacheEntries, buildDir, executable))
            {
                string json = RunCTest(ctest, buildDir, configuration);
                if (json == null)
                    continue;

                var container = new TestPropertySettingsContainer(ParseShowOnlyJson(json));
                if (container.ContainsExecutable(executable))
                {
                    string configurationString = configuration == null ? "" : $" (configuration '{configuration}')";
                    _logger.DebugInfo($"Found CTest test properties of executable '{executable}' in CMake build tree '{buildDir}'{configurationString}");
                    return container;
                }
            }

            _logger.DebugInfo($"CTest does not know any tests of executable '{executable}' (CMake build tree '{buildDir}')");
            return null;
        }

        public static string FindBuildDir(string executable)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(executable));
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, CMakeCacheFile)))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        public static IDictionary<string, string> ReadCMakeCache(string cmakeCacheFile)
        {
            // entries look like NAME:TYPE=VALUE
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(cmakeCacheFile))
            {
                if (line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                int colon = line.IndexOf(':');
                int equals = line.IndexOf('=');
                if (colon > 0 && equals > colon)
                    entries[line.Substring(0, colon)] = line.Substring(equals + 1);
            }
            return entries;
        }

        private static string GetCTestExecutable(IDictionary<string, string> cacheEntries)
        {
            // the ctest belonging to the CMake which has generated the build tree
            if (cacheEntries.TryGetValue("CMAKE_CTEST_COMMAND", out string ctest) && File.Exists(ctest))
                return ctest;

            if (cacheEntries.TryGetValue("CMAKE_COMMAND", out string cmake) && !string.IsNullOrWhiteSpace(cmake))
            {
                ctest = Path.Combine(Path.GetDirectoryName(cmake) ?? "", "ctest.exe");
                if (File.Exists(ctest))
                    return ctest;
            }

            return null;
        }

        /// <summary>
        /// Single-config generators do not need a configuration. For multi-config generators, the configurations
        /// appearing within the executable's path are tried first.
        /// </summary>
        public static IEnumerable<string> GetCandidateConfigurations(IDictionary<string, string> cacheEntries, string buildDir, string executable)
        {
            if (!cacheEntries.TryGetValue("CMAKE_CONFIGURATION_TYPES", out string configurationTypes)
                || string.IsNullOrWhiteSpace(configurationTypes))
                return new string[] { null };

            var configurations = configurationTypes
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();

            string relativeDir = Path.GetDirectoryName(Path.GetFullPath(executable)).Substring(buildDir.Length);
            var pathParts = new HashSet<string>(
                relativeDir.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            return configurations.Where(c => pathParts.Contains(c))
                .Concat(configurations.Where(c => !pathParts.Contains(c)));
        }

        private string RunCTest(string ctest, string buildDir, string configuration)
        {
            string arguments = "--show-only=json-v1";
            if (configuration != null)
                arguments += $" -C \"{configuration}\"";

            var startInfo = new ProcessStartInfo(ctest, arguments)
            {
                WorkingDirectory = buildDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    return null;

                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit((int)CTestTimeout.TotalMilliseconds))
                {
                    try { process.Kill(); } catch (Exception) { /* process has exited in the meantime */ }
                    _logger.DebugWarning($"'{ctest} {arguments}' did not finish within {CTestTimeout.TotalSeconds}s (working directory: '{buildDir}')");
                    return null;
                }

                if (process.ExitCode != 0)
                {
                    _logger.DebugWarning($"'{ctest} {arguments}' returned exit code {process.ExitCode} (working directory: '{buildDir}'): {error.Result.Trim()}");
                    return null;
                }

                return output.Result;
            }
        }

        public static IList<TestPropertySettingsContainer.TestProperties> ParseShowOnlyJson(string json)
        {
            XElement root;
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), XmlDictionaryReaderQuotas.Max))
            {
                root = XElement.Load(reader);
            }

            var tests = new List<TestPropertySettingsContainer.TestProperties>();
            foreach (XElement test in Items(root.Element("tests")))
            {
                List<string> command = Items(test.Element("command")).Select(c => c.Value).ToList();
                // tests which are not available in the given configuration do not have a command
                if (command.Count == 0)
                    continue;

                var properties = Items(test.Element("properties"))
                    .Where(p => p.Element("name") != null && p.Element("value") != null)
                    .GroupBy(p => p.Element("name").Value)
                    .ToDictionary(g => g.Key, g => g.First().Element("value"));

                tests.Add(new TestPropertySettingsContainer.TestProperties
                {
                    Name = GetGoogleTestName(command) ?? (string)test.Element("name"),
                    Command = command[0],
                    WorkingDirectory = GetString(properties, "WORKING_DIRECTORY"),
                    Environment = GetEnvironment(properties),
                    Labels = GetStrings(properties, "LABELS"),
                    Timeout = GetTimeout(properties),
                    Disabled = GetBool(properties, "DISABLED")
                });
            }
            return tests;
        }

        /// <summary>
        /// Tests added by gtest_discover_tests() (or by add_test() with a filter) run exactly one Google Test test,
        /// which allows to match it even if the CTest name differs (e.g. because of TEST_PREFIX).
        /// </summary>
        private static string GetGoogleTestName(IList<string> command)
        {
            string filter = command
                .Skip(1)
                .LastOrDefault(arg => arg.StartsWith(GoogleTestFilterOption))?
                .Substring(GoogleTestFilterOption.Length);

            return string.IsNullOrEmpty(filter) || filter.IndexOfAny(new[] { '*', '?', ':', '-' }) >= 0
                ? null
                : filter;
        }

        private static IEnumerable<XElement> Items(XElement array)
        {
            return array?.Elements("item") ?? Enumerable.Empty<XElement>();
        }

        private static string GetString(IDictionary<string, XElement> properties, string name)
        {
            return properties.TryGetValue(name, out XElement value) ? value.Value : null;
        }

        private static IList<string> GetStrings(IDictionary<string, XElement> properties, string name)
        {
            if (!properties.TryGetValue(name, out XElement value))
                return new List<string>();

            return (string)value.Attribute("type") == "array"
                ? Items(value).Select(i => i.Value).ToList()
                : value.Value.Split(';').ToList();
        }

        private static bool GetBool(IDictionary<string, XElement> properties, string name)
        {
            if (!properties.TryGetValue(name, out XElement value))
                return false;

            // CMake's notion of true
            string boolean = value.Value.Trim().ToUpperInvariant();
            return boolean == "TRUE" || boolean == "ON" || boolean == "YES" || boolean == "Y"
                   || (double.TryParse(boolean, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && number != 0);
        }

        private static TimeSpan? GetTimeout(IDictionary<string, XElement> properties)
        {
            string timeout = GetString(properties, "TIMEOUT");
            return double.TryParse(timeout, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : (TimeSpan?)null;
        }

        private static IDictionary<string, string> GetEnvironment(IDictionary<string, XElement> properties)
        {
            var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string variable in GetStrings(properties, "ENVIRONMENT"))
            {
                int equals = variable.IndexOf('=');
                if (equals > 0)
                    environment[variable.Substring(0, equals)] = variable.Substring(equals + 1);
            }
            return environment;
        }
    }
}
