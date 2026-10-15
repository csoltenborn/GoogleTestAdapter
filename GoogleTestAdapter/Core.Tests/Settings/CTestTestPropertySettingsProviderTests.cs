using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Settings
{
    [TestClass]
    public class CTestTestPropertySettingsProviderTests : TestsBase
    {
        // shortened output of "ctest --show-only=json-v1" for tests added by gtest_discover_tests() and add_test()
        private const string ShowOnlyJson = @"{
  ""kind"": ""ctestInfo"",
  ""version"": { ""major"": 1, ""minor"": 0 },
  ""tests"": [
    {
      ""name"": ""lbl.Labeled.First"",
      ""config"": ""Debug"",
      ""command"": [ ""C:/build/bin/Debug/LabeledTests.exe"", ""--gtest_filter=Labeled.First"", ""--gtest_also_run_disabled_tests"" ],
      ""properties"": [
        { ""name"": ""LABELS"", ""value"": [ ""unit"", ""fast"" ] },
        { ""name"": ""SKIP_REGULAR_EXPRESSION"", ""value"": [ ""\\[  SKIPPED \\]"" ] },
        { ""name"": ""TIMEOUT"", ""value"": 2.5 },
        { ""name"": ""WORKING_DIRECTORY"", ""value"": ""C:/build/src"" }
      ]
    },
    {
      ""name"": ""AllPropTests"",
      ""command"": [ ""C:/build/bin/Debug/PropTests.exe"", ""--gtest_filter=Props.*"" ],
      ""properties"": [
        { ""name"": ""DISABLED"", ""value"": true },
        { ""name"": ""ENVIRONMENT"", ""value"": [ ""MYVAR=my=value"", ""OTHER=1"" ] },
        { ""name"": ""TIMEOUT"", ""value"": 0 }
      ]
    },
    {
      ""name"": ""NotAvailableInThisConfig"",
      ""properties"": [ { ""name"": ""LABELS"", ""value"": [ ""unit"" ] } ]
    }
  ]
}";

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseShowOnlyJson_TestWithGoogleTestFilter_NameIsTakenFromFilterAndPropertiesAreParsed()
        {
            var tests = CTestTestPropertySettingsProvider.ParseShowOnlyJson(ShowOnlyJson);

            tests.Should().HaveCount(2);
            var test = tests[0];
            test.Name.Should().Be("Labeled.First");
            test.Command.Should().Be("C:/build/bin/Debug/LabeledTests.exe");
            test.WorkingDirectory.Should().Be("C:/build/src");
            test.Labels.Should().BeEquivalentTo(new[] { "unit", "fast" }, o => o.WithStrictOrdering());
            test.Timeout.Should().Be(TimeSpan.FromSeconds(2.5));
            test.Disabled.Should().BeFalse();
            test.Environment.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseShowOnlyJson_TestWithWildcardFilter_NameIsCTestNameAndPropertiesAreParsed()
        {
            var test = CTestTestPropertySettingsProvider.ParseShowOnlyJson(ShowOnlyJson)[1];

            test.Name.Should().Be("AllPropTests");
            test.Command.Should().Be("C:/build/bin/Debug/PropTests.exe");
            test.WorkingDirectory.Should().BeNull();
            test.Labels.Should().BeEmpty();
            test.Timeout.Should().BeNull();
            test.Disabled.Should().BeTrue();
            test.Environment.Should().HaveCount(2);
            test.Environment["MYVAR"].Should().Be("my=value");
            test.Environment["OTHER"].Should().Be("1");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseShowOnlyJson_DisabledAsString_CMakeNotionOfTrueIsUsed()
        {
            foreach (var value in new[] { "\"ON\"", "\"yes\"", "\"1\"", "\"TRUE\"" })
                ParseDisabled(value).Should().BeTrue(value);
            foreach (var value in new[] { "\"OFF\"", "\"0\"", "\"FALSE\"", "\"NOTFOUND\"", "false" })
                ParseDisabled(value).Should().BeFalse(value);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseShowOnlyJson_NoTests_ReturnsEmptyList()
        {
            CTestTestPropertySettingsProvider.ParseShowOnlyJson(@"{ ""kind"": ""ctestInfo"", ""tests"": [] }")
                .Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCandidateConfigurations_SingleConfigGenerator_ReturnsNoConfiguration()
        {
            var cacheEntries = new Dictionary<string, string> { { "CMAKE_BUILD_TYPE", "Debug" } };

            CTestTestPropertySettingsProvider.GetCandidateConfigurations(cacheEntries, @"C:\build", @"C:\build\bin\Tests.exe")
                .Should().BeEquivalentTo(new string[] { null });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCandidateConfigurations_MultiConfigGenerator_ConfigurationOfExecutablePathComesFirst()
        {
            var cacheEntries = new Dictionary<string, string> { { "CMAKE_CONFIGURATION_TYPES", "Debug;Release;RelWithDebInfo" } };

            CTestTestPropertySettingsProvider.GetCandidateConfigurations(cacheEntries, @"C:\build", @"C:\build\bin\release\Tests.exe")
                .Should().BeEquivalentTo(new[] { "Release", "Debug", "RelWithDebInfo" }, o => o.WithStrictOrdering());
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCandidateConfigurations_ConfigurationOnlyInBuildDirPath_IsNotPreferred()
        {
            var cacheEntries = new Dictionary<string, string> { { "CMAKE_CONFIGURATION_TYPES", "Debug;Release" } };

            CTestTestPropertySettingsProvider.GetCandidateConfigurations(cacheEntries, @"C:\Release", @"C:\Release\bin\Tests.exe")
                .Should().BeEquivalentTo(new[] { "Debug", "Release" }, o => o.WithStrictOrdering());
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void FindBuildDirAndReadCMakeCache_BuildTree_AreFoundAndParsed()
        {
            string buildDir = Path.Combine(Path.GetTempPath(), "GTA_CTestProvider_" + Guid.NewGuid().ToString("N"));
            string exeDir = Path.Combine(buildDir, "bin", "Debug");
            Directory.CreateDirectory(exeDir);
            try
            {
                string cacheFile = Path.Combine(buildDir, CTestTestPropertySettingsProvider.CMakeCacheFile);
                File.WriteAllLines(cacheFile, new[]
                {
                    "# This is the CMakeCache file.",
                    "//Path to a program.",
                    "CMAKE_CTEST_COMMAND:INTERNAL=C:/Program Files/CMake/bin/ctest.exe",
                    "CMAKE_CONFIGURATION_TYPES:STRING=Debug;Release",
                    "MY_OPTION:BOOL=a=b",
                    "invalid line"
                });

                CTestTestPropertySettingsProvider.FindBuildDir(Path.Combine(exeDir, "Tests.exe")).Should().Be(buildDir);

                var entries = CTestTestPropertySettingsProvider.ReadCMakeCache(cacheFile);
                entries.Should().HaveCount(3);
                entries["CMAKE_CTEST_COMMAND"].Should().Be("C:/Program Files/CMake/bin/ctest.exe");
                entries["cmake_configuration_types"].Should().Be("Debug;Release");
                entries["MY_OPTION"].Should().Be("a=b");
            }
            finally
            {
                Directory.Delete(buildDir, true);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void FindBuildDir_NoBuildTree_ReturnsNull()
        {
            CTestTestPropertySettingsProvider.FindBuildDir(TestResources.Tests_DebugX86).Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetContainer_NoBuildTree_ReturnsNull()
        {
            new CTestTestPropertySettingsProvider(MockLogger.Object).GetContainer(TestResources.Tests_DebugX86).Should().BeNull();
        }

        private static bool ParseDisabled(string value)
        {
            string json = @"{ ""tests"": [ { ""name"": ""T"", ""command"": [ ""T.exe"" ], ""properties"": [ { ""name"": ""DISABLED"", ""value"": " + value + " } ] } ] }";
            return CTestTestPropertySettingsProvider.ParseShowOnlyJson(json).Single().Disabled;
        }
    }
}
