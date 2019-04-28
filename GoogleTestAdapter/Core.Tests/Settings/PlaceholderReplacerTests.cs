using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Settings
{
    [TestClass]
    public class PlaceholderReplacerTests
    {
        private class PlaceholderAndValue
        {
            public string Placeholder { get; }
            public object Value { get; }

            public PlaceholderAndValue(string placeholder, object value)
            {
                Placeholder = placeholder;
                Value = value;
            }
        }

        private class MethodnameAndPlaceholder
        {
            public string MethodName { get; }
            public string Placeholder { get; }

            public MethodnameAndPlaceholder(string methodName, string placeholder)
            {
                MethodName = methodName;
                Placeholder = placeholder;
            }
        }

        private static readonly string[] MethodNames = {
            nameof(PlaceholderReplacer.ReplaceAdditionalPdbsPlaceholders),
            nameof(PlaceholderReplacer.ReplaceAdditionalTestExecutionParamPlaceholdersForDiscovery),
            nameof(PlaceholderReplacer.ReplaceAdditionalTestExecutionParamPlaceholdersForExecution),
            nameof(PlaceholderReplacer.ReplaceSetupBatchPlaceholders),
            nameof(PlaceholderReplacer.ReplaceTeardownBatchPlaceholders),
            nameof(PlaceholderReplacer.ReplacePathExtensionPlaceholders),
            nameof(PlaceholderReplacer.ReplaceWorkingDirPlaceholdersForDiscovery),
            nameof(PlaceholderReplacer.ReplaceWorkingDirPlaceholdersForExecution)
        };

        private static readonly List<PlaceholderAndValue> PlaceholdersAndExpectedValues = new List<PlaceholderAndValue>
        {
            new PlaceholderAndValue(PlaceholderReplacer.SolutionDirPlaceholder, TestResources.SampleTestsSolutionDir),
            new PlaceholderAndValue(PlaceholderReplacer.PlatformNamePlaceholder, "Win33"),
            new PlaceholderAndValue(PlaceholderReplacer.ConfigurationNamePlaceholder, "MyDebug"),
            // ReSharper disable once AssignNullToNotNullAttribute
            new PlaceholderAndValue(PlaceholderReplacer.ExecutableDirPlaceholder, Path.GetFullPath(Path.GetDirectoryName(TestResources.Tests_DebugX86))),
            new PlaceholderAndValue(PlaceholderReplacer.ExecutablePlaceholder, TestResources.Tests_DebugX86),
            new PlaceholderAndValue(PlaceholderReplacer.TestDirPlaceholder, "testDirectory"),
            new PlaceholderAndValue(PlaceholderReplacer.ThreadIdPlaceholder, 42)
        };

        private static readonly List<MethodnameAndPlaceholder> UnsupportedCombinations = new List<MethodnameAndPlaceholder>
        {
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceAdditionalPdbsPlaceholders), PlaceholderReplacer.TestDirPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceAdditionalTestExecutionParamPlaceholdersForDiscovery), PlaceholderReplacer.TestDirPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplacePathExtensionPlaceholders), PlaceholderReplacer.TestDirPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceWorkingDirPlaceholdersForDiscovery), PlaceholderReplacer.TestDirPlaceholder),

            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceAdditionalPdbsPlaceholders), PlaceholderReplacer.ThreadIdPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceAdditionalTestExecutionParamPlaceholdersForDiscovery), PlaceholderReplacer.ThreadIdPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplacePathExtensionPlaceholders), PlaceholderReplacer.ThreadIdPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceWorkingDirPlaceholdersForDiscovery), PlaceholderReplacer.ThreadIdPlaceholder),

            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceSetupBatchPlaceholders), PlaceholderReplacer.ExecutablePlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceSetupBatchPlaceholders), PlaceholderReplacer.ExecutableDirPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceSetupBatchPlaceholders), PlaceholderReplacer.ConfigurationNamePlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceSetupBatchPlaceholders), PlaceholderReplacer.PlatformNamePlaceholder),

            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceTeardownBatchPlaceholders), PlaceholderReplacer.ExecutablePlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceTeardownBatchPlaceholders), PlaceholderReplacer.ExecutableDirPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceTeardownBatchPlaceholders), PlaceholderReplacer.ConfigurationNamePlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceTeardownBatchPlaceholders), PlaceholderReplacer.PlatformNamePlaceholder),
        };


        [TestMethod]
        [TestCategory(Unit)]
        public void AllReplacementsTest()
        {
            Mock<HelperFilesCache> mockHelperFilesCache = new Mock<HelperFilesCache>();
            mockHelperFilesCache.Setup(c => c.GetReplacementsMap(It.IsAny<string>())).Returns(
                new Dictionary<string, string>
                {
                    {nameof(IGoogleTestAdapterSettings.ConfigurationName), "MyDebug"},
                    {nameof(IGoogleTestAdapterSettings.PlatformName), "Win33"}
                });
            Mock<IGoogleTestAdapterSettings> mockOptions = new Mock<IGoogleTestAdapterSettings>();
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            var placeholderReplacer = new PlaceholderReplacer(
                () => TestResources.SampleTestsSolutionDir, 
                () => mockOptions.Object,
                mockHelperFilesCache.Object,
                mockLogger.Object);

            foreach (string methodName in MethodNames)
            {
                foreach (PlaceholderAndValue placeholder in PlaceholdersAndExpectedValues)
                {
                    if (!UnsupportedCombinations.Any(combination =>
                        combination.MethodName == methodName && combination.Placeholder == placeholder.Placeholder))
                    {
                        var result = InvokeMethodWithStandardParameters(placeholderReplacer, methodName, placeholder.Placeholder);
                        result.Should().Be(placeholder.Value.ToString(), $"{methodName} should replace {placeholder.Placeholder} with {(object) placeholder.Value.ToString()}");
                        mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AllReplacementMethods_UnknownPlaceholderResultsInWarning()
        {
            Mock<HelperFilesCache> mockHelperFilesCache = new Mock<HelperFilesCache>();
            mockHelperFilesCache.Setup(c => c.GetReplacementsMap(It.IsAny<string>()))
                .Returns(new Dictionary<string, string>());
            Mock<IGoogleTestAdapterSettings> mockOptions = new Mock<IGoogleTestAdapterSettings>();
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            var replacer = new PlaceholderReplacer(() => "solutiondir", () => mockOptions.Object,
                mockHelperFilesCache.Object, mockLogger.Object);

            string placeholder = "$(UnknownPlaceholder)";
            foreach (string methodName in MethodNames)
            {
                mockLogger.Reset();
                string result = InvokeMethodWithStandardParameters(replacer, methodName, placeholder);
                result.Should().Be(placeholder);
                mockLogger.Verify(l => l.LogWarning(It.Is<string>(msg => msg.Contains(placeholder))), Times.Once);
            }
        }

        private string InvokeMethodWithStandardParameters(PlaceholderReplacer placeholderReplacer, string methodName,
            string input)
        {
            var method = typeof(PlaceholderReplacer).GetMethod(methodName);
            // ReSharper disable once PossibleNullReferenceException
            var parameters = method.GetParameters();

            var parameterValues = new List<object> {input};
            for (int i = 1; i < parameters.Length; i++)
            {
                parameterValues.Add(GetValue(parameters[i]));
            }

            return (string) method.Invoke(placeholderReplacer, parameterValues.ToArray());
        }

        private object GetValue(ParameterInfo parameter)
        {
            switch (parameter.Name)
            {
                case "executable": return TestResources.Tests_DebugX86;
                case "threadId": return 42;
                default: return parameter.Name;
            }
        }

    }
}