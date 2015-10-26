using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Helpers
{
    public class RegexTraitParser
    {
        private TestEnvironment TestEnvironment { get; }


        public RegexTraitParser(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }


        public List<RegexTraitPair> ParseTraitsRegexesString(string option)
        {
            List<RegexTraitPair> result = new List<RegexTraitPair>();

            string[] pairs = option.Split(
                new[] { Options.TraitsRegexesPairSeparator },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                try
                {
                    result.Add(ParseRegexTraitPair(pair));
                }
                catch (Exception e)
                {
                    TestEnvironment.LogError(
                        "Could not parse pair '" + pair + "', exception message: " + e.Message);
                }
            }

            return result;
        }


        private RegexTraitPair ParseRegexTraitPair(string pair)
        {
            string[] values = pair.Split(
                new[] { Options.TraitsRegexesRegexSeparator }, StringSplitOptions.None);
            string[] trait = values[1].Split(
                new[] { Options.TraitsRegexesTraitSeparator }, StringSplitOptions.None);
            string regex = values[0];
            string traitName = trait[0];
            string traitValue = trait[1];
            return new RegexTraitPair(regex, traitName, traitValue);
        }

    }

}