using System.Xml;
using System.Xml.XPath;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

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
            provider.Settings.Should().BeNull();

            var settingsDoc = new XmlDocument();
            settingsDoc.Load(TestResources.SolutionTestSettings);
            XPathNavigator navigator = settingsDoc.CreateNavigator();

            navigator.MoveToChild("RunSettings", "").Should().BeTrue();
            navigator.MoveToChild(GoogleTestConstants.SettingsName, "").Should().BeTrue();

            provider.Load(navigator.ReadSubtree());

            provider.Settings.Should().NotBeNull();
            provider.Settings.BatchForTestSetup.Should().Be("Solution");
        }

    }

}