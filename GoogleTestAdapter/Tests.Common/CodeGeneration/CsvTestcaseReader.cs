using System;
using GoogleTestAdapter.Tests.Common.Helpers;

namespace GoogleTestAdapter.Tests.Common.CodeGeneration
{
    public class CsvTestcaseReader : CsvReader<CsvTestCase>
    {
        public CsvTestcaseReader(string csvFile) : base(csvFile, '\t', true) { }

        protected override CsvTestCase GetObject(string[] columns)
        {
            return new CsvTestCase
            {
                TestExecutablePlaceholder = columns[0],
                TestExecutable = GetTestExecutable(columns[0]),
                SettingsFilePlaceholder = GetSettingsPart(columns[1]),
                SettingsFile = GetSettingsFile(columns[1]),
                TestCaseFilter = columns[2],
                EnableCodeCoverage = bool.Parse(columns[3]),
                InIsolation = bool.Parse(columns[4]),
                Ignore = bool.Parse(columns[5])
            };
        }

        private string GetSettingsFile(string key)
        {
            switch (key)
            {
                case "false":
                    return "";
                case "true":
                    return TestResources.UserTestSettingsForGeneratedTests_Project;
                case "Solution":
                    return TestResources.UserTestSettingsForGeneratedTests_Solution;
                case "SolutionProject":
                    return TestResources.UserTestSettingsForGeneratedTests_SolutionProject;
                default:
                    throw new Exception("Unknown key: " + key);
            }
        }

        private string GetSettingsPart(string key)
        {
            switch (key)
            {
                case "false":
                    return "";
                case "true":
                    return "_Settings";
                default:
                    return "_" + key + "Settings";
            }
        }

        private string GetTestExecutable(string key)
        {
            switch(key)
            {
                case "SampleTests":
                    return TestResources.Tests_DebugX86;
                case "SampleTests170":
                    return TestResources.Tests_DebugX86_Gtest170;
                case "SampleTestsX64":
                    return TestResources.Tests_ReleaseX64;
                case "LoadTests":
                    return TestResources.LoadTests_ReleaseX86;
                case "HardCrashingSampleTests":
                    return TestResources.CrashingTests_DebugX86;
                default:
                    throw new Exception("Unknown test executable key: " + key);
            }
        }

    }
}