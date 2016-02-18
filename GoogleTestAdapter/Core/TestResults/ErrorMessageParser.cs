using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace GoogleTestAdapter.TestResults
{

    public class ErrorMessageParser
    {
        public string ErrorMessage { get; }
        public string ErrorStackTrace { get; }

        public ErrorMessageParser(string completeErrorMessage, string sourceFile)
            : this(SplitErrorMessage(completeErrorMessage, sourceFile), sourceFile)
        { }

        public ErrorMessageParser(XmlNodeList failureNodes, string sourceFile)
            : this((from XmlNode failureNode in failureNodes select failureNode.InnerText).ToList(), sourceFile)
        { }

        private ErrorMessageParser(IList<string> errorMessages, string sourceFile)
        {
            if (errorMessages.Count == 0)
            {
                ErrorMessage = "";
                ErrorStackTrace = "";
            }
            else if (errorMessages.Count == 1)
            {
                string errorMessage = errorMessages[0];
                string pattern = Regex.Escape(sourceFile) + ":([0-9]+)";
                string stackTrace;
                if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, pattern, sourceFile))
                {
                    pattern = Regex.Escape(sourceFile) + @"\(([0-9]+)\):";
                    CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, pattern, sourceFile);
                }
                ErrorMessage = "\n" + errorMessage;
                ErrorStackTrace = stackTrace;
            }
            else
            {
                List<string> finalErrorMessages = new List<string>();
                List<string> finalStackTraces = new List<string>();
                for (int i = 0; i < errorMessages.Count; i++)
                {
                    string errorMessage = errorMessages[i];
                    string pattern = Regex.Escape(sourceFile) + ":([0-9]+)";
                    string stackTrace;
                    if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, pattern, sourceFile, i + 1))
                    {
                        pattern = Regex.Escape(sourceFile) + @"\(([0-9]+)\):";
                        CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, pattern, sourceFile, i + 1);
                    }

                    finalErrorMessages.Add($"#{i + 1} - {errorMessage}");
                    finalStackTraces.Add(stackTrace);
                }

                ErrorMessage = "\n" + string.Join("\n", finalErrorMessages);
                ErrorStackTrace = string.Join("", finalStackTraces);
            }
        }

        private static IList<string> SplitErrorMessage(string errorMessage, string sourceFile)
        {
            List<Match> allMatches = new List<Match>();

            string pattern = Regex.Escape(sourceFile) + ":([0-9]+)";
            MatchCollection matches = Regex.Matches(errorMessage, pattern);
            for (int i = 0; i < matches.Count; i++)
                allMatches.Add(matches[i]);

            pattern = Regex.Escape(sourceFile) + @"\(([0-9]+)\):";
            matches = Regex.Matches(errorMessage, pattern);
            for (int i = 0; i < matches.Count; i++)
                allMatches.Add(matches[i]);

            allMatches.Sort((x, y) => x.Index.CompareTo(y.Index));

            List<string> errorMessages = new List<string>();
            if (allMatches.Count > 0)
            {
                int startIndex, length;
                for (int i = 0; i < allMatches.Count - 1; i++)
                {
                    startIndex = allMatches[i].Index;
                    length = allMatches[i + 1].Index - startIndex;
                    errorMessages.Add(errorMessage.Substring(startIndex, length));
                }
                startIndex = allMatches[allMatches.Count - 1].Index;
                length = errorMessage.Length - startIndex;
                errorMessages.Add(errorMessage.Substring(startIndex, length));
            }

            return errorMessages;
        }

        private bool CreateErrorMessageAndStacktrace(ref string errorMessage, out string stackTrace, string pattern, string sourceFile, int msgNumber = 0)
        {
            stackTrace = "";
            Match match = Regex.Match(errorMessage, pattern);
            if (match.Success)
            {
                string fileName = Path.GetFileName(sourceFile);
                string lineNumber = match.Groups[1].Value;
                if (msgNumber == 0)
                    stackTrace = $"at {fileName}:{lineNumber} in {sourceFile}:line {lineNumber}{Environment.NewLine}";
                else
                    stackTrace = $"at #{msgNumber} - {fileName}:{lineNumber} in {sourceFile}:line {lineNumber}{Environment.NewLine}";

                errorMessage = errorMessage.Replace(match.Groups[0].Value, "").Trim();
            }
            return match.Success;
        }

    }

}