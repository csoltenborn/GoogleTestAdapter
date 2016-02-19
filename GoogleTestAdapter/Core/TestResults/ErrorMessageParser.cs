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
        public string ErrorMessage { get; private set; }
        public string ErrorStackTrace { get; private set; }

        private Regex ColonRegex { get; set; }
        private Regex BracketsRegex { get; set; }

        private IList<string> ErrorMessages { get; } 
        private string SourceFile { get; }

        public ErrorMessageParser(string completeErrorMessage, string sourceFile)
        {
            InitRegexPatterns(sourceFile);
            SourceFile = sourceFile;
            ErrorMessages = SplitErrorMessage(completeErrorMessage);
        }

        public ErrorMessageParser(XmlNodeList failureNodes, string sourceFile)
        {
            InitRegexPatterns(sourceFile);
            SourceFile = sourceFile;
            ErrorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText).ToList();
        }

        private void InitRegexPatterns(string sourceFile)
        {
            string escapedSourceFile = Regex.Escape(sourceFile);
            ColonRegex = new Regex(escapedSourceFile + ":([0-9]+)", RegexOptions.Compiled);
            BracketsRegex = new Regex(escapedSourceFile + @"\(([0-9]+)\):", RegexOptions.Compiled);
        }

        public void Parse()
        {
            if (ErrorMessages.Count == 0)
            {
                ErrorMessage = "";
                ErrorStackTrace = "";
            }
            else if (ErrorMessages.Count == 1)
            {
                string errorMessage = ErrorMessages[0];
                string stackTrace;
                if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, ColonRegex))
                    CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, BracketsRegex);

                ErrorMessage = $"\n{errorMessage}";
                ErrorStackTrace = stackTrace;
            }
            else
            {
                List<string> finalErrorMessages = new List<string>();
                List<string> finalStackTraces = new List<string>();
                for (int i = 0; i < ErrorMessages.Count; i++)
                {
                    string errorMessage = ErrorMessages[i];
                    string stackTrace;
                    if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, ColonRegex, i + 1))
                        CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, BracketsRegex, i + 1);

                    finalErrorMessages.Add($"#{i + 1} - {errorMessage}");
                    finalStackTraces.Add(stackTrace);
                }

                ErrorMessage = "\n" + string.Join("\n", finalErrorMessages);
                ErrorStackTrace = string.Join("", finalStackTraces);
            }
        }

        private IList<string> SplitErrorMessage(string errorMessage)
        {
            List<Match> allMatches = new List<Match>();

            MatchCollection matches = ColonRegex.Matches(errorMessage);
            for (int i = 0; i < matches.Count; i++)
                allMatches.Add(matches[i]);

            matches = BracketsRegex.Matches(errorMessage);
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

        private bool CreateErrorMessageAndStacktrace(ref string errorMessage, out string stackTrace, Regex pattern, int msgNumber = 0)
        {
            stackTrace = "";
            Match match = pattern.Match(errorMessage);
            if (match.Success)
            {
                string fileName = Path.GetFileName(SourceFile);
                string lineNumber = match.Groups[1].Value;
                string msgReference = msgNumber == 0 ? "" : $"#{msgNumber} - ";

                stackTrace = $"at {msgReference}{fileName}:{lineNumber} in {SourceFile}:line {lineNumber}{Environment.NewLine}";
                errorMessage = errorMessage.Replace(match.Groups[0].Value, "").Trim();
            }
            return match.Success;
        }

    }

}