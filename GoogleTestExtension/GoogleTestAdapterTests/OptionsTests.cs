using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace GoogleTestAdapter
{
    [TestClass]
    public class OptionsTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void AdditionalTestParameter_PlaceholdersAreTreatedCorrectly()
        {
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder);
            string result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder + " " + Options.TestDirPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir mydir", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder.ToLower());
            result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual(Options.TestDirPlaceholder.ToLower(), result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.ThreadIdPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("4711", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder + ", " + Options.ThreadIdPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("mydir, 4711", result);
        }

        [TestMethod]
        public void TraitsRegexOptionsFailsNicelyIfInvokedWithUnparsableString()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", "vrr<erfwe") as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfEmpty()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", "") as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfOne()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            string OptionsString = CreateTraitsRegex("MyTest*", "Type", "Small");
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
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            string optionsString = ConcatTraisRegexes(
                CreateTraitsRegex("MyTest*", "Type", "Small"),
                CreateTraitsRegex("*MyOtherTest*", "Category", "Integration"));
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

        private string CreateTraitsRegex(string regex, string name, string value)
        {
            return regex +
                Options.TraitsRegexesRegexSeparator + name +
                Options.TraitsRegexesTraitSeparator + value;
        }

        private string ConcatTraisRegexes(params string[] regexes)
        {
            return string.Join(Options.TraitsRegexesPairSeparator, regexes);
        }

    }

}