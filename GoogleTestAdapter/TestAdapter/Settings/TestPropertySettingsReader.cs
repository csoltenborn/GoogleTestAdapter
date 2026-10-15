using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    /// <summary>
    /// Reads the test properties (working directory, environment) Visual Studio provides for tests of CMake projects.
    /// The settings are read from the run settings xml rather than through a settings provider, since a provider
    /// with the same name is registered by Microsoft's Test Adapter for Google Test.
    /// </summary>
    public static class TestPropertySettingsReader
    {
        public const string TestPropertySettingsName = "TestPropertySettingsForGoogleAdapter";

        public static TestPropertySettingsContainer Read(string runSettingsXml, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(runSettingsXml) || !runSettingsXml.Contains(TestPropertySettingsName))
                return null;

            try
            {
                XElement settings = XDocument.Parse(runSettingsXml).Root?.Element(TestPropertySettingsName);
                if (settings == null)
                    return null;

                var tests = settings
                    .Elements("Tests")
                    .Elements("TestProperties")
                    .Select(test => new TestPropertySettingsContainer.TestProperties
                    {
                        Name = (string)test.Element("Name"),
                        Command = (string)test.Element("Command"),
                        WorkingDirectory = (string)test.Element("WorkingDirectory"),
                        Environment = GetEnvironment(test)
                    })
                    .ToList();

                var container = new TestPropertySettingsContainer(tests);
                logger.DebugInfo($"Found CMake test properties of {tests.Count} test(s)");
                return container.IsEmpty ? null : container;
            }
            catch (XmlException e)
            {
                logger.LogWarning($"Could not read {TestPropertySettingsName} from run settings, CMake test properties will be ignored: {e.Message}");
                return null;
            }
        }

        private static IDictionary<string, string> GetEnvironment(XElement test)
        {
            var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement envVar in test.Elements("Environment").Elements("EnvVar"))
            {
                string name = (string)envVar.Element("Name");
                if (!string.IsNullOrEmpty(name))
                    environment[name] = (string)envVar.Element("Value") ?? "";
            }
            return environment;
        }
    }
}
