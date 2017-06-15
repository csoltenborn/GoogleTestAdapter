// This file has been modified by Microsoft on 6/2017.

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter.Helpers
{

    public static class Utils
    {

        public static string GetTempDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static bool DeleteDirectory(string directory, out string errorMessage)
        {
            try
            {
                Directory.Delete(directory, true);
                errorMessage = null;
                return true;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }

        public static string GetExtendedPath(string pathExtension)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            return string.IsNullOrEmpty(pathExtension) ? path : $"{pathExtension};{path}";
        }

        public static void TimestampMessage(ref string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            message = $"{timestamp} - {message ?? ""}";
        }

        public static void ValidateRegex(string pattern)
        {
            try
            {
                Regex.Match(string.Empty, pattern);
            }
            catch (ArgumentException e)
            {
                throw new Exception($"Invalid regular expression \"{pattern}\", exception message: {e.Message}");
            }
        }

        public static void ValidateTraitRegexes(string value)
        {
            // The parser will throw if the value is not well formed.
            var parser = new RegexTraitParser(null);
            parser.ParseTraitsRegexesString(value, ignoreErrors: false);
        }
    }

}