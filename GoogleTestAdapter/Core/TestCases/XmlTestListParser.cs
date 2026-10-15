using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    /// <summary>
    /// Parses the XML file written by Google Test (since 1.8.1) if it is called with --gtest_list_tests and
    /// --gtest_output=xml, which contains the source location of each test as provided by the test's macro.
    /// </summary>
    public class XmlTestListParser
    {
        private readonly string _executable;
        private readonly ILogger _logger;

        public XmlTestListParser(string executable, ILogger logger)
        {
            _executable = executable;
            _logger = logger;
        }

        /// <returns>The source locations of the tests by their fully qualified names (empty if the file does not
        /// exist or can not be parsed, e.g. because the executable's version of Google Test does not write it)</returns>
        public IDictionary<string, TestCaseLocation> ParseTestLocations(string xmlFile)
        {
            var locations = new Dictionary<string, TestCaseLocation>();
            if (!File.Exists(xmlFile) || new FileInfo(xmlFile).Length == 0)
            {
                _logger.DebugInfo($"Google Test did not write a test list file for executable {_executable}, test locations are taken from debug symbols only");
                return locations;
            }

            try
            {
#pragma warning disable IDE0017 // Simplify object initialization
                var settings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
#pragma warning restore IDE0017 // Simplify object initialization
                settings.XmlResolver = null;
                using (var reader = XmlReader.Create(new StringReader(GoogleTestXmlFile.ReadAllText(xmlFile, _logger)), settings))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(reader);

                    // ReSharper disable once PossibleNullReferenceException
                    foreach (XmlNode testsuiteNode in xmlDocument.SelectNodes("/testsuites/testsuite"))
                    {
                        string suite = testsuiteNode.Attributes?["name"]?.Value;
                        // ReSharper disable once PossibleNullReferenceException
                        foreach (XmlNode testcaseNode in testsuiteNode.SelectNodes("testcase"))
                        {
                            AddLocation(locations, suite, testcaseNode);
                        }
                    }
                }
            }
            catch (Exception e) when (e is XmlException || e is IOException || e is UnauthorizedAccessException)
            {
                _logger.DebugWarning($"Test list file {xmlFile} of executable {_executable} could not be parsed: {e.Message}");
            }

            return locations;
        }

        private void AddLocation(IDictionary<string, TestCaseLocation> locations, string suite, XmlNode testcaseNode)
        {
            string name = testcaseNode.Attributes?["name"]?.Value;
            string file = GetFullPath(testcaseNode.Attributes?["file"]?.Value);
            if (string.IsNullOrEmpty(suite) || string.IsNullOrEmpty(name) || file == null
                || !uint.TryParse(testcaseNode.Attributes?["line"]?.Value, out uint line) || line == 0)
                return;

            string fullyQualifiedName = $"{suite}.{name}";
            locations[fullyQualifiedName] = new TestCaseLocation(fullyQualifiedName, file, line);
        }

        // __FILE__ is relative if the compiler has been called with a relative path (e.g. by some CMake generators);
        // such a path is only used if it can be resolved relative to the executable's directory
        private string GetFullPath(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return null;

            try
            {
                if (Path.IsPathRooted(file))
                    return Path.GetFullPath(file);

                // ReSharper disable once AssignNullToNotNullAttribute
                string candidate = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_executable), file));
                return File.Exists(candidate) ? candidate : null;
            }
            catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is PathTooLongException)
            {
                return null;
            }
        }
    }

}
