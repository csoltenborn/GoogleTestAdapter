using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.Settings
{
    [TestClass]
    public class CodeCoverageRunSettingsTests
    {
        private const string CodeCoveragePath =
            "/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector/Configuration/CodeCoverage";

        private static XmlDocument Load(string dataCollectors)
        {
            var document = new XmlDocument();
            document.LoadXml("<RunSettings><DataCollectionRunSettings><DataCollectors>" + dataCollectors +
                             "</DataCollectors></DataCollectionRunSettings></RunSettings>");
            return document;
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Configure_CodeCoverageWithoutConfiguration_DynamicNativeInstrumentationIsEnabledAndGtaIsExcluded()
        {
            var document = Load(@"<DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" />");

            CodeCoverageRunSettings.Configure(document).Should().BeTrue();

            document.SelectSingleNode($"{CodeCoveragePath}/EnableDynamicNativeInstrumentation").InnerText.Should().Be("True");
            document.SelectNodes($"{CodeCoveragePath}/ModulePaths/Exclude/ModulePath").Should().HaveCount(1);
            document.SelectSingleNode($"{CodeCoveragePath}/ModulePaths/Exclude/ModulePath").InnerText
                .Should().Be(CodeCoverageRunSettings.GoogleTestAdapterModulePath);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Configure_ExplicitDynamicNativeInstrumentation_ValueIsKept()
        {
            var document = Load(@"<DataCollector friendlyName=""Code Coverage""><Configuration><CodeCoverage>" +
                                @"<EnableDynamicNativeInstrumentation>False</EnableDynamicNativeInstrumentation>" +
                                @"<ModulePaths><Exclude><ModulePath>.*Foo.*</ModulePath></Exclude></ModulePaths>" +
                                @"</CodeCoverage></Configuration></DataCollector>");

            CodeCoverageRunSettings.Configure(document);

            document.SelectNodes($"{CodeCoveragePath}/EnableDynamicNativeInstrumentation").Should().HaveCount(1);
            document.SelectSingleNode($"{CodeCoveragePath}/EnableDynamicNativeInstrumentation").InnerText.Should().Be("False");
            document.SelectNodes($"{CodeCoveragePath}/ModulePaths/Exclude/ModulePath").Should().HaveCount(2);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Configure_CalledTwice_GtaIsExcludedOnce()
        {
            var document = Load(@"<DataCollector uri=""datacollector://microsoft/codecoverage/2.0"" />");

            CodeCoverageRunSettings.Configure(document);
            CodeCoverageRunSettings.Configure(document);

            document.SelectNodes($"{CodeCoveragePath}/ModulePaths/Exclude/ModulePath").Should().HaveCount(1);
            document.SelectNodes($"{CodeCoveragePath}/EnableDynamicNativeInstrumentation").Should().HaveCount(1);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Configure_DynamicNativeInstrumentationNotSupported_OnlyGtaIsExcluded()
        {
            var document = Load(@"<DataCollector friendlyName=""Code Coverage"" />");

            CodeCoverageRunSettings.Configure(document, enableDynamicNativeInstrumentation: false).Should().BeTrue();

            document.SelectNodes($"{CodeCoveragePath}/EnableDynamicNativeInstrumentation").Should().HaveCount(0);
            document.SelectNodes($"{CodeCoveragePath}/ModulePaths/Exclude/ModulePath").Should().HaveCount(1);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Configure_NoCodeCoverage_DocumentIsNotChanged()
        {
            var document = Load(@"<DataCollector friendlyName=""Event Log"" />");
            string before = document.OuterXml;

            CodeCoverageRunSettings.Configure(document).Should().BeFalse();
            document.OuterXml.Should().Be(before);

            var empty = new XmlDocument();
            empty.LoadXml("<RunSettings />");
            CodeCoverageRunSettings.Configure(empty).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GoogleTestAdapterModulePath_MatchesGtaAssembliesOnly()
        {
            var regex = new Regex(CodeCoverageRunSettings.GoogleTestAdapterModulePath, RegexOptions.IgnoreCase);

            regex.IsMatch(@"C:\Users\me\AppData\Local\Microsoft\VisualStudio\18.0_x\Extensions\abc\GoogleTestAdapter.Core.dll").Should().BeTrue();
            regex.IsMatch(@"C:\out\GoogleTestAdapter.TestAdapter.dll").Should().BeTrue();
            regex.IsMatch(@"C:\out\Tests_gta.exe").Should().BeFalse();
            regex.IsMatch(@"C:\out\MyGoogleTestAdapterTests.dll").Should().BeFalse();
        }
    }
}
