using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Helpers
{
    public class EnvironmentVariablesParser
    {
        public const string Separator = SettingsWrapper.TraitsRegexesPairSeparator;

        // following https://stackoverflow.com/a/20635858/1276129
        private static readonly Regex NameRegex = new Regex(
            @"^[\w\(\){}\[\]$*+-\\/""#',;.@!?]+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        private const int MaxNameLength = 256;
        private const int MaxValueLength = 32768;

        private readonly ILogger _logger;

        public EnvironmentVariablesParser(ILogger logger)
        {
            _logger = logger;
        }

        public IDictionary<string, string> ParseEnvironmentVariablesString(string option, bool ignoreErrors = true)
        {
            var envVarDictionary = new Dictionary<string, string>();
            foreach (var envVar in option.Split(new [] {Separator}, StringSplitOptions.RemoveEmptyEntries))
            {
                var index = envVar.IndexOf("=", StringComparison.InvariantCulture);
                if (index == -1)
                {
                    string errorMsg = $"Environment variables must be of the form Name=Value, but '{envVar}' is lacking the =";
                    if (ignoreErrors)
                    {
                        _logger?.LogWarning(errorMsg);
                        continue;
                    }

                    throw new Exception(errorMsg);
                }

                string name = envVar.Substring(0, index);
                string value = envVar.Substring(index + 1);

                if (!NameRegex.IsMatch(name))
                {
                    string errorMsg = $"Environment variable names must match the regex '${NameRegex}', but '{name}' does not";
                    if (ignoreErrors)
                    {
                        _logger?.LogWarning(errorMsg);
                        continue;
                    }

                    throw new Exception(errorMsg);
                }

                if (name.Length > MaxNameLength)
                {
                    string errorMsg = $"Environment variable names must not be longer than {MaxNameLength} chars, but '${name}' has a length of {name.Length}";
                    if (ignoreErrors)
                    {
                        _logger?.LogWarning(errorMsg);
                        continue;
                    }

                    throw new Exception(errorMsg);
                }

                if (value.Length > MaxValueLength)
                {
                    string errorMsg = $"Environment variable values must not be longer than {MaxValueLength} chars, but '${value}' has a length of {value.Length}";
                    if (ignoreErrors)
                    {
                        _logger?.LogWarning(errorMsg);
                        continue;
                    }

                    throw new Exception(errorMsg);
                }

                envVarDictionary.Add(name, value);
            }

            return envVarDictionary;
        }

    }

}