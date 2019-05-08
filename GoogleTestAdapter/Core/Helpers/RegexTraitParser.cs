// This file has been modified by Microsoft on 8/2017.

using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;
using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Helpers
{
    public class RegexTraitParser
    {
        private readonly ILogger _logger;

        public RegexTraitParser(ILogger logger)
        {
            _logger = logger;
        }

        public List<RegexTraitPair> ParseTraitsRegexesString(string option, bool ignoreErrors = true)
        {
            var result = new List<RegexTraitPair>();

            string[] pairs = option.Split(
                new[] { SettingsWrapper.TraitsRegexesPairSeparator },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                try
                {
                    result.Add(ParseRegexTraitPair(pair));
                }
                catch (Exception e)
                {
                    string message = String.Format(Resources.ParsePair, pair, e.Message);
                    if (ignoreErrors)
                        _logger?.LogError(message);
                    else
                        throw new Exception(message, e);
                }
            }

            return result;
        }

        private RegexTraitPair ParseRegexTraitPair(string pair)
        {
            string[] values = pair.Split(
                new[] { SettingsWrapper.TraitsRegexesRegexSeparator }, StringSplitOptions.None);
            string[] trait = values[1].Split(
                new[] { SettingsWrapper.TraitsRegexesTraitSeparator }, StringSplitOptions.None);
            string regex = values[0];
            Utils.ValidateRegex(regex);
            string traitName = trait[0];
            string traitValue = trait[1];
            return new RegexTraitPair(regex, traitName, traitValue);
        }

    }

}