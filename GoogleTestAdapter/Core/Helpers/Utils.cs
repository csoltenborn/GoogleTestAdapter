// This file has been modified by Microsoft on 6/2017.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GoogleTestAdapter.Common;

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

        public static bool DeleteDirectory(string directory)
        {
            return DeleteDirectory(directory, out _);
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

        public static string GetTimestamp()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        /// <exception cref="AggregateException">If at least one of the actions has thrown an exception</exception>
        public static bool SpawnAndWait(Action[] actions, int timeoutInMs = Timeout.Infinite)
        {
            var tasks = new Task[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                tasks[i] = Task.Run(actions[i]);
            }
      
            return Task.WaitAll(tasks, timeoutInMs);
        }

        public static void ValidateRegex(string pattern)
        {
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
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

        public static void ValidateEnvironmentVariables(string value)
        {
            // The parser will throw if the value is not well formed.
            var parser = new EnvironmentVariablesParser(null);
            parser.ParseEnvironmentVariablesString(value, ignoreErrors: false);
        }

        public static bool BinaryFileContainsStrings(string executable, Encoding encoding, IEnumerable<string> strings)
        {
            byte[] file = File.ReadAllBytes(executable);
            return strings.All(s => file.IndexOf(encoding.GetBytes(s)) >= 0);
        }

        public static string[] SplitAdditionalPdbs(string additionalPdbs)
        {
            return additionalPdbs.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool ValidatePattern(string pattern, out string errorMessage)
        {
            return ValidatePattern(pattern, out string dummy1, out string dummy2, out errorMessage);
        }

        private static bool ValidatePattern(string pattern, out string directory, out string filePattern, out string errorMessage)
        {
            errorMessage = "";

            try
            {
                filePattern = Path.GetFileName(pattern);
                if (string.IsNullOrWhiteSpace(filePattern))
                {
                    filePattern = null;
                }
            }
            catch (Exception)
            {
                filePattern = null;
            }

            try
            {
                directory = Path.GetDirectoryName(pattern);
            }
            catch (Exception)
            {
                directory = null;
            }

            if (filePattern == null || directory == null)
            {
                errorMessage = $"Additional PDB pattern '{pattern}' is invalid: ";
                if (filePattern == null && directory == null)
                {
                    errorMessage += "path part and file pattern part can not be found";
                }
                else if (filePattern == null)
                {
                    errorMessage += "file pattern part can not be found";
                }
                else
                {
                    errorMessage += "path part can not be found";
                }

                return false;
            }

            return true;
        }

        public static string[] GetMatchingFiles(string pattern, ILogger logger)
        {
            if (!ValidatePattern(pattern, out string path, out string filePattern, out string errorMessage))
            {
                logger.LogError(errorMessage);
                return new string[]{};
            }

            try
            {
                // ReSharper disable AssignNullToNotNullAttribute
                return Directory.GetFiles(path, filePattern);
                // ReSharper restore AssignNullToNotNullAttribute
            }
            catch (Exception e)
            {
                logger.LogError($"Error while evaluating additional PDB pattern '{pattern}': {e.Message}");
                return new string[] { };
            }
        }
        
    }

}