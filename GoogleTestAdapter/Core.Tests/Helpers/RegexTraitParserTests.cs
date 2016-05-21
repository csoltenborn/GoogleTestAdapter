using System.Collections.Generic;
using FluentAssertions;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class RegexTraitParserTests : AbstractCoreTests
    {
        private RegexTraitParser Parser { get; set; }

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            Parser = new RegexTraitParser(TestEnvironment);
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTraitsRegexesString_UnparsableString_FailsNicely()
        {
            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString("vrr<erfwe");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTraitsRegexesString_EmptyString_EmptyResult()
        {
            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString("");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTraitsRegexesString_OneRegex_ParsedCorrectly()
        {
            string optionsString = CreateTraitsRegex("MyTest*", "Type", "Small");

            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString(optionsString);

            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result[0].Regex.Should().Be("MyTest*");
            result[0].Trait.Name.Should().Be("Type");
            result[0].Trait.Value.Should().Be("Small");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTraitsRegexesString_TwoRegexes_ParsedCorrectly()
        {
            string optionsString = ConcatTraitsRegexes(
                CreateTraitsRegex("MyTest*", "Type", "Small"),
                CreateTraitsRegex("*MyOtherTest*", "Category", "Integration"));

            List<RegexTraitPair> result = Parser.ParseTraitsRegexesString(optionsString);

            result.Should().NotBeNull();
            result.Count.Should().Be(2);

            result[0].Regex.Should().Be("MyTest*");
            result[0].Trait.Name.Should().Be("Type");
            result[0].Trait.Value.Should().Be("Small");

            result[1].Regex.Should().Be("*MyOtherTest*");
            result[1].Trait.Name.Should().Be("Category");
            result[1].Trait.Value.Should().Be("Integration");
        }


        private string CreateTraitsRegex(string regex, string name, string value)
        {
            return regex +
                SettingsWrapper.TraitsRegexesRegexSeparator + name +
                SettingsWrapper.TraitsRegexesTraitSeparator + value;
        }

        private string ConcatTraitsRegexes(params string[] regexes)
        {
            return string.Join(SettingsWrapper.TraitsRegexesPairSeparator, regexes);
        }

    }

}