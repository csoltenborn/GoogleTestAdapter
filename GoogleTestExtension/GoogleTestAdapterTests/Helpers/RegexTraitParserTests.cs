using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class RegexTraitParserTests : AbstractGoogleTestExtensionTests
    {
        private RegexTraitParser Parser { get; set; }

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            Parser = new RegexTraitParser(TestEnvironment);
        }


        [TestMethod]
        public void TraitsRegexOptionsFailsNicelyIfInvokedWithUnparsableString()
        {
            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString("vrr<erfwe");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfEmpty()
        {
            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString("");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfOne()
        {
            string optionsString = CreateTraitsRegex("MyTest*", "Type", "Small");

            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString(optionsString);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("MyTest*", result[0].Regex);
            Assert.AreEqual("Type", result[0].Trait.Name);
            Assert.AreEqual("Small", result[0].Trait.Value);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfTwo()
        {
            string optionsString = ConcatTraitsRegexes(
                CreateTraitsRegex("MyTest*", "Type", "Small"),
                CreateTraitsRegex("*MyOtherTest*", "Category", "Integration"));

            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString(optionsString);

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

        private string ConcatTraitsRegexes(params string[] regexes)
        {
            return string.Join(Options.TraitsRegexesPairSeparator, regexes);
        }

    }

}