using System.Text.RegularExpressions;

namespace GoogleTestAdapter.Tests.Common.Helpers
{
    public static class StringExtensions
    {
        // Thank you, Steve B: http://stackoverflow.com/a/6276029/859211
        public static string ReplaceIgnoreCase(this string theString, string oldValue, string newValue)
        {
            // oldValue and newValue should be treated as plain strings not regex. So they need escaping.
            return Regex.Replace(theString, Regex.Escape(oldValue), newValue.Replace("$", "$$"), RegexOptions.IgnoreCase);
        }
    }
}
