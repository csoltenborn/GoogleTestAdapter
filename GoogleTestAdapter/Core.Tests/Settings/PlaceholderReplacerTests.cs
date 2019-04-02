using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
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
            nameof(PlaceholderReplacer.ReplaceBatchPlaceholders),
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

            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceBatchPlaceholders), PlaceholderReplacer.ExecutablePlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceBatchPlaceholders), PlaceholderReplacer.ExecutableDirPlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceBatchPlaceholders), PlaceholderReplacer.ConfigurationNamePlaceholder),
            new MethodnameAndPlaceholder(nameof(PlaceholderReplacer.ReplaceBatchPlaceholders), PlaceholderReplacer.PlatformNamePlaceholder),
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
            var placeholderReplacer = new PlaceholderReplacer(
                () => TestResources.SampleTestsSolutionDir, 
                () => mockOptions.Object,
                mockHelperFilesCache.Object);

            foreach (string methodName in MethodNames)
            {
                foreach (PlaceholderAndValue placeholder in PlaceholdersAndExpectedValues)
                {
                    if (!UnsupportedCombinations.Any(combination =>
                        combination.MethodName == methodName && combination.Placeholder == placeholder.Placeholder))
                    {
                        GenericReplacementTest(placeholderReplacer, methodName, placeholder.Placeholder, placeholder.Value.ToString());
                    }
                }
            }
        }

        private void GenericReplacementTest(PlaceholderReplacer placeholderReplacer, 
            string methodName, string input, object expected)
        {
            var method = typeof(PlaceholderReplacer).GetMethod(methodName);
            // ReSharper disable once PossibleNullReferenceException
            var parameters = method.GetParameters();

            var parameterValues = new List<object> { input };
            for (int i = 1; i < parameters.Length; i++)
            {
                parameterValues.Add(GetValue(parameters[i]));
            }

            string result = (string) method.Invoke(placeholderReplacer, parameterValues.ToArray());

            result.Should().Be(expected.ToString(), $"{methodName} should replace {input} with {expected}");
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