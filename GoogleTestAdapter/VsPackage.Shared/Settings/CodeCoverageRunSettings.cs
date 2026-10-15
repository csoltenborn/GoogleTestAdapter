using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    /// <summary>
    /// Adjusts the settings of Visual Studio's code coverage (Test > Analyze Code Coverage) to Google Test executables.
    /// </summary>
    public static class CodeCoverageRunSettings
    {
        public const string DataCollectorUri = "datacollector://Microsoft/CodeCoverage/2.0";
        public const string DataCollectorFriendlyName = "Code Coverage";

        public const string EnableDynamicNativeInstrumentation = "EnableDynamicNativeInstrumentation";

        // GTA's assemblies are loaded into the test host, but are of no interest to the user
        public const string GoogleTestAdapterModulePath = @".*\\GoogleTestAdapter\.[^\\]*\.dll$";

        /// <summary>
        /// Test executables are native child processes of the test host, which Visual Studio's code coverage does not
        /// instrument unless dynamic native instrumentation is enabled (by default, it is only enabled if configured in
        /// the code coverage options, which are disabled by default) or they are linked with /PROFILE. Dynamic native
        /// instrumentation is thus enabled unless it is configured explicitly, and GTA's assemblies are excluded.
        /// </summary>
        /// <param name="enableDynamicNativeInstrumentation">False for versions of Visual Studio whose code coverage does
        /// not know the setting (i.e., before VS 2022)</param>
        /// <returns>True if the run settings contain a code coverage data collector</returns>
        public static bool Configure(IXPathNavigable runSettingsDocument, bool enableDynamicNativeInstrumentation = true)
        {
            XPathNavigator root = runSettingsDocument.CreateNavigator();
            // ReSharper disable once PossibleNullReferenceException
            List<XPathNavigator> collectors = root
                .Select("/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector")
                .Cast<XPathNavigator>()
                .Where(IsCodeCoverageDataCollector)
                .Select(n => n.Clone())
                .ToList();

            foreach (XPathNavigator collector in collectors)
            {
                XPathNavigator codeCoverage = GetOrAppendChild(GetOrAppendChild(collector, "Configuration"), "CodeCoverage");
                if (enableDynamicNativeInstrumentation && !HasChild(codeCoverage, EnableDynamicNativeInstrumentation))
                    codeCoverage.AppendChildElement(null, EnableDynamicNativeInstrumentation, null, true.ToString());

                XPathNavigator exclude = GetOrAppendChild(GetOrAppendChild(codeCoverage, "ModulePaths"), "Exclude");
                bool isExcluded = exclude.SelectChildren("ModulePath", "").Cast<XPathNavigator>()
                    .Any(n => n.Value == GoogleTestAdapterModulePath);
                if (!isExcluded)
                    exclude.AppendChildElement(null, "ModulePath", null, GoogleTestAdapterModulePath);
            }
            return collectors.Count > 0;
        }

        private static bool IsCodeCoverageDataCollector(XPathNavigator collector)
        {
            return string.Equals(collector.GetAttribute("uri", ""), DataCollectorUri, StringComparison.OrdinalIgnoreCase)
                || string.Equals(collector.GetAttribute("friendlyName", ""), DataCollectorFriendlyName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasChild(XPathNavigator parent, string name)
        {
            return parent.SelectChildren(name, "").Count > 0;
        }

        private static XPathNavigator GetOrAppendChild(XPathNavigator parent, string name)
        {
            XPathNavigator child = parent.Clone();
            if (child.MoveToChild(name, ""))
                return child;

            parent.AppendChildElement(null, name, null, null);
            child = parent.Clone();
            child.MoveToChild(name, "");
            return child;
        }
    }
}
