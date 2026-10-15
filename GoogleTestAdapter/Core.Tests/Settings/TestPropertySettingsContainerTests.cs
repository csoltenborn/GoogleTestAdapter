using System;
using System.Collections.Generic;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Settings
{
    [TestClass]
    public class TestPropertySettingsContainerTests : TestsBase
    {
        private const string Executable = @"C:\build\tests\MyTests.exe";

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_ExactName_ReturnsSettingsOfTest()
        {
            var container = new TestPropertySettingsContainer(new[]
            {
                CreateTest("Suite.Test1", @"C:\build\dir1"),
                CreateTest("Suite.Test2", @"C:\build\dir2")
            });

            container.GetSettingsForTest(Executable, "Suite.Test1").WorkingDirectory.Should().Be(@"C:\build\dir1");
            container.GetSettingsForTest(Executable, "Suite.Test2").WorkingDirectory.Should().Be(@"C:\build\dir2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_ExecutablePathDiffersInCaseAndNotation_ReturnsSettingsOfTest()
        {
            var container = new TestPropertySettingsContainer(new[]
            {
                CreateTest("Suite.Test1", @"C:\build\dir1", command: @"c:\BUILD\other\..\tests\mytests.EXE"),
                CreateTest("Suite.Test2", @"C:\build\dir2")
            });

            container.GetSettingsForTest(Executable, "Suite.Test1").WorkingDirectory.Should().Be(@"C:\build\dir1");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_ParameterizedTestAddedByGtestAddTests_ReturnsSettingsOfTest()
        {
            var container = new TestPropertySettingsContainer(new[]
            {
                CreateTest("*/Suite.Test/*", @"C:\build\dir1"),
                CreateTest("Suite.Other", @"C:\build\dir2")
            });

            container.GetSettingsForTest(Executable, "Instance/Suite.Test/3").WorkingDirectory.Should().Be(@"C:\build\dir1");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_NameDoesNotMatchButAllTestsShareSettings_ReturnsSharedSettings()
        {
            var container = new TestPropertySettingsContainer(new[]
            {
                CreateTest("prefix.Suite.Test1", @"C:\build\dir", new Dictionary<string, string> { { "MYVAR", "value" } }),
                CreateTest("Instance/Suite.Test/pretty_name", @"C:\build\dir", new Dictionary<string, string> { { "myvar", "value" } })
            });

            var settings = container.GetSettingsForTest(Executable, "Instance/Suite.Test/0");

            settings.WorkingDirectory.Should().Be(@"C:\build\dir");
            settings.Environment["MYVAR"].Should().Be("value");
            container.GetSettingsForExecutable(Executable).Should().BeSameAs(settings);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_NameDoesNotMatchAndTestsDifferInSettings_ReturnsNull()
        {
            var container = new TestPropertySettingsContainer(new[]
            {
                CreateTest("Suite.Test1", @"C:\build\dir1"),
                CreateTest("Suite.Test2", @"C:\build\dir2")
            });

            container.GetSettingsForTest(Executable, "Suite.Test3").Should().BeNull();
            container.GetSettingsForExecutable(Executable).Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_OtherExecutable_ReturnsNull()
        {
            var container = new TestPropertySettingsContainer(new[] { CreateTest("Suite.Test1", @"C:\build\dir1") });

            container.GetSettingsForTest(@"C:\build\tests\OtherTests.exe", "Suite.Test1").Should().BeNull();
            container.GetSettingsForExecutable(@"C:\build\tests\OtherTests.exe").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Constructor_DuplicateAndInvalidEntries_AreIgnored()
        {
            var container = new TestPropertySettingsContainer(new[]
            {
                CreateTest("Suite.Test1", @"C:\build\dir1"),
                CreateTest("Suite.Test1", @"C:\build\dir2"),
                CreateTest(null, @"C:\build\dir3"),
                CreateTest("Suite.Test2", @"C:\build\dir4", command: null),
                CreateTest("Suite.Test3", @"C:\build\dir5", command: "C:\\in<valid|path"),
            });

            container.IsEmpty.Should().BeFalse();
            container.GetSettingsForTest(Executable, "Suite.Test1").WorkingDirectory.Should().Be(@"C:\build\dir1");
            container.GetSettingsForExecutable(Executable).WorkingDirectory.Should().Be(@"C:\build\dir1");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Constructor_NoValidEntries_IsEmpty()
        {
            new TestPropertySettingsContainer(new[] { CreateTest("Suite.Test", @"C:\build\dir", command: "") })
                .IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Equals_SameValuesInDifferentCase_AreEqual()
        {
            var settings = new TestPropertySettings(@"C:\Build", new Dictionary<string, string> { { "Var", "Value" } });
            var otherSettings = new TestPropertySettings(@"c:\build", new Dictionary<string, string> { { "VAR", "Value" } });
            var differentValue = new TestPropertySettings(@"C:\Build", new Dictionary<string, string> { { "Var", "value" } });

            settings.Should().Be(otherSettings);
            settings.GetHashCode().Should().Be(otherSettings.GetHashCode());
            settings.Should().NotBe(differentValue);
            new TestPropertySettings(" ", null).WorkingDirectory.Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Equals_DifferentLabelsTimeoutOrDisabled_AreNotEqualButShareExecutionEnvironment()
        {
            var environment = new Dictionary<string, string> { { "Var", "Value" } };
            var settings = new TestPropertySettings(@"C:\Build", environment, new[] { "unit" });
            var others = new[]
            {
                new TestPropertySettings(@"C:\Build", environment, new[] { "unit", "slow" }),
                new TestPropertySettings(@"C:\Build", environment, new[] { "unit" }, TimeSpan.FromSeconds(2)),
                new TestPropertySettings(@"C:\Build", environment, new[] { "unit" }, disabled: true)
            };

            foreach (var other in others)
            {
                settings.Should().NotBe(other);
                TestPropertySettings.ExecutionEnvironmentComparer.Equals(settings, other).Should().BeTrue();
                TestPropertySettings.ExecutionEnvironmentComparer.GetHashCode(settings)
                    .Should().Be(TestPropertySettings.ExecutionEnvironmentComparer.GetHashCode(other));
            }
            settings.Should().Be(new TestPropertySettings(@"c:\build", environment, new[] { "unit" }));
            TestPropertySettings.ExecutionEnvironmentComparer.Equals(settings, new TestPropertySettings(@"C:\Other", environment))
                .Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Constructor_TimeoutZeroAndEmptyLabels_AreIgnored()
        {
            var settings = new TestPropertySettings(null, null, new[] { "unit", "", " ", "unit" }, TimeSpan.Zero);

            settings.Timeout.Should().BeNull();
            settings.Labels.Should().BeEquivalentTo(new[] { "unit" });
            settings.Disabled.Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForTest_TestWithLabelsTimeoutAndDisabled_PropertiesAreProvided()
        {
            var test = CreateTest("Suite.Test1", @"C:\build\dir1");
            test.Labels = new List<string> { "unit", "fast" };
            test.Timeout = TimeSpan.FromSeconds(3);
            test.Disabled = true;
            var container = new TestPropertySettingsContainer(new[] { test });

            var settings = container.GetSettingsForTest(Executable, "Suite.Test1");

            settings.Labels.Should().BeEquivalentTo(new[] { "unit", "fast" });
            settings.Timeout.Should().Be(TimeSpan.FromSeconds(3));
            settings.Disabled.Should().BeTrue();
            container.ContainsExecutable(Executable).Should().BeTrue();
            container.ContainsExecutable(@"C:\build\tests\OtherTests.exe").Should().BeFalse();
        }

        private static TestPropertySettingsContainer.TestProperties CreateTest(string name, string workingDirectory,
            IDictionary<string, string> environment = null, string command = Executable)
        {
            return new TestPropertySettingsContainer.TestProperties
            {
                Name = name,
                Command = command,
                WorkingDirectory = workingDirectory,
                Environment = environment
            };
        }
    }
}
