using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestAdapterOptionsTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void AdditionalTestParameter_PlaceholdersAreTreatedCorrectly()
        {
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(GoogleTestAdapterOptions.TestDirPlaceholder);
            string result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(GoogleTestAdapterOptions.TestDirPlaceholder+ " " + GoogleTestAdapterOptions.TestDirPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir mydir", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(GoogleTestAdapterOptions.TestDirPlaceholder.ToLower());
            result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual(GoogleTestAdapterOptions.TestDirPlaceholder.ToLower(), result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(GoogleTestAdapterOptions.ThreadIdPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("4711", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(GoogleTestAdapterOptions.TestDirPlaceholder + ", " + GoogleTestAdapterOptions.ThreadIdPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("mydir, 4711", result);
        }

        [TestMethod]
        public void TraitsRegexOptionsFailsNicelyIfInvokedWithUnparsableString()
        {
            PrivateObject optionsAccessor = new PrivateObject(new GoogleTestAdapterOptions());
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", "vrr<erfwe") as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfEmpty()
        {
            PrivateObject optionsAccessor = new PrivateObject(new GoogleTestAdapterOptions());
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", "") as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfOne()
        {
            PrivateObject optionsAccessor = new PrivateObject(new GoogleTestAdapterOptions());
            string OptionsString = "MyTest*///Type,Small";
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", OptionsString) as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("MyTest*", result[0].Regex);
            Assert.AreEqual("Type", result[0].Trait.Name);
            Assert.AreEqual("Small", result[0].Trait.Value);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfTwo()
        {
            PrivateObject optionsAccessor = new PrivateObject(new GoogleTestAdapterOptions());
            string optionsString = "MyTest*///Type,Small//||//*MyOtherTest*///Category,Integration";
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", optionsString) as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            Assert.AreEqual("MyTest*", result[0].Regex);
            Assert.AreEqual("Type", result[0].Trait.Name);
            Assert.AreEqual("Small", result[0].Trait.Value);

            Assert.AreEqual("*MyOtherTest*", result[1].Regex);
            Assert.AreEqual("Category", result[1].Trait.Name);
            Assert.AreEqual("Integration", result[1].Trait.Value);
        }

    }
}
