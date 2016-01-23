using System.Xml;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [TestClass]
    public class RunSettingsProviderTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void Constructor__InstanceHasCorrectName()
        {
            Assert.AreEqual(GoogleTestConstants.SettingsName, new RunSettingsProvider().Name);
        }

        [TestMethod]
        public void Load_SolutionSettings_SettingsAreMerged()
        {
            RunSettingsProvider provider = new RunSettingsProvider();
            Assert.IsNull(provider.Settings);

            XmlDocument settingsDoc = new XmlDocument();
            settingsDoc.Load(SolutionTestSettings);
            XPathNavigator navigator = settingsDoc.CreateNavigator();
            Assert.IsTrue(navigator.MoveToChild("RunSettings", ""));
            Assert.IsTrue(navigator.MoveToChild(GoogleTestConstants.SettingsName, ""));

            provider.Load(navigator.ReadSubtree());

            Assert.IsNotNull(provider.Settings);
            Assert.AreEqual("Solution", provider.Settings.BatchForTestSetup);
        }

    }

}