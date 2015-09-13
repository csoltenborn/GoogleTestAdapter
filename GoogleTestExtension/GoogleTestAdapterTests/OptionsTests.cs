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
            string source = "${TestDirectory}";
            string result = Options.ReplacePlaceholders(source, "mydir");
            Assert.AreEqual("mydir", result);

            source = "${TestDirectory} ${TestDirectory}";
            result = Options.ReplacePlaceholders(source, "mydir");
            Assert.AreEqual("mydir mydir", result);

            source = "${testdirectory}";
            result = Options.ReplacePlaceholders(source, "mydir");
            Assert.AreEqual("${testdirectory}", result);
        }

        [TestMethod]
        public void TraitsRegexOptionsFailsNicelyIfInvokedWithUnparsableString()
        {
            PrivateObject OptionsAccessor = new PrivateObject(new Options());
            List<RegexTraitPair> Result = OptionsAccessor.Invoke("ParseTraitsRegexesString", "vrr<erfwe") as List<RegexTraitPair>;

            Assert.IsNotNull(Result);
            Assert.AreEqual(0, Result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfEmpty()
        {
            PrivateObject OptionsAccessor = new PrivateObject(new Options());
            List<RegexTraitPair> Result = OptionsAccessor.Invoke("ParseTraitsRegexesString", "") as List<RegexTraitPair>;

            Assert.IsNotNull(Result);
            Assert.AreEqual(0, Result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfOne()
        {
            PrivateObject OptionsAccessor = new PrivateObject(new Options());
            string OptionsString = "MyTest*///Type,Small";
            List<RegexTraitPair> Result = OptionsAccessor.Invoke("ParseTraitsRegexesString", OptionsString) as List<RegexTraitPair>;

            Assert.IsNotNull(Result);
            Assert.AreEqual(1, Result.Count);
            Assert.AreEqual("MyTest*", Result[0].Regex);
            Assert.AreEqual("Type", Result[0].Trait.Name);
            Assert.AreEqual("Small", Result[0].Trait.Value);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfTwo()
        {
            PrivateObject OptionsAccessor = new PrivateObject(new Options());
            string OptionsString = "MyTest*///Type,Small//||//*MyOtherTest*///Category,Integration";
            List<RegexTraitPair> Result = OptionsAccessor.Invoke("ParseTraitsRegexesString", OptionsString) as List<RegexTraitPair>;

            Assert.IsNotNull(Result);
            Assert.AreEqual(2, Result.Count);

            Assert.AreEqual("MyTest*", Result[0].Regex);
            Assert.AreEqual("Type", Result[0].Trait.Name);
            Assert.AreEqual("Small", Result[0].Trait.Value);

            Assert.AreEqual("*MyOtherTest*", Result[1].Regex);
            Assert.AreEqual("Category", Result[1].Trait.Name);
            Assert.AreEqual("Integration", Result[1].Trait.Value);
        }

    }
}
