using System.Xml;
using System.Xml.XPath;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [TestClass]
    public class RunSettingsProviderTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void Constructor__InstanceHasCorrectName()
        {
            new RunSettingsProvider().Name.Should().Be(GoogleTestConstants.SettingsName);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Load_SolutionSettings_SettingsAreMerged()
        {
            var provider = new RunSettingsProvider();
            provider.SettingsContainer.Should().BeNull();

            var settingsDoc = new XmlDocument();
            settingsDoc.Load(TestResources.ProviderDeliveredTestSettings);
            XPathNavigator navigator = settingsDoc.CreateNavigator();

            navigator.MoveToChild(Constants.RunSettingsName, "").Should().BeTrue();
            navigator.MoveToChild(GoogleTestConstants.SettingsName, "").Should().BeTrue();
            navigator.MoveToChild("SolutionSettings", "").Should().BeTrue();
            navigator.MoveToRoot();

            navigator.MoveToRoot();
            navigator.MoveToChild(Constants.RunSettingsName, "").Should().BeTrue();
            navigator.MoveToChild(GoogleTestConstants.SettingsName, "").Should().BeTrue();
            provider.Load(navigator.ReadSubtree());

            provider.SettingsContainer.Should().NotBeNull();
            provider.SettingsContainer.SolutionSettings.BatchForTestSetup.Should().Be("Solution");
        }

    }

}