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
        private static readonly string FilenameRegex = "[^" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]";

        public string ErrorMessage { get; private set; }
        public string ErrorStackTrace { get; private set; }

        private IList<string> ErrorMessages { get; }

        private Regex ColonRegex { get; set; }
        private Regex BracketsRegex { get; set; }

        public ErrorMessageParser(string completeErrorMessage, string baseDir)
        {
            InitRegexPatterns(baseDir);
            ErrorMessages = SplitErrorMessage(completeErrorMessage);
        }

        public ErrorMessageParser(XmlNodeList failureNodes, string baseDir)
        {
            InitRegexPatterns(baseDir);
            ErrorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText).ToList();
        }

        private void InitRegexPatterns(string baseDir)
        {
            string escapedBaseDir = Regex.Escape(baseDir);
            ColonRegex = new Regex($@"({escapedBaseDir}{FilenameRegex}*):([0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            BracketsRegex = new Regex($@"({escapedBaseDir}{FilenameRegex}*)\(([0-9]+)\):", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public void Parse()
        {
            switch (ErrorMessages.Count)
            {
                case 0:
                    ErrorMessage = "";
                    ErrorStackTrace = ""; break;
                case 1:
                    HandleSingleFailure();
                    break;
                default:
                    HandleMultipleFailures();
                    break;
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

        private void HandleSingleFailure()
        {
            string errorMessage = ErrorMessages[0];
            string stackTrace;
            if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, ColonRegex))
                CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, BracketsRegex);

            ErrorMessage = $"\n{errorMessage}";
            ErrorStackTrace = stackTrace;
        }

        private void HandleMultipleFailures()
        {
            List<string> finalErrorMessages = new List<string>();
            List<string> finalStackTraces = new List<string>();
            for (int i = 0; i < ErrorMessages.Count; i++)
            {
                string errorMessage = ErrorMessages[i];

                int msgId = i + 1;
                string stackTrace;
                if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, ColonRegex, msgId))
                    CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, BracketsRegex, msgId);

                finalErrorMessages.Add($"#{msgId} - {errorMessage}");
                finalStackTraces.Add(stackTrace);
            }

            ErrorMessage = "\n" + string.Join("\n", finalErrorMessages);
            ErrorStackTrace = string.Join("", finalStackTraces);
        }

        private bool CreateErrorMessageAndStacktrace(ref string errorMessage, out string stackTrace, Regex pattern, int msgId = 0)
        {
            Match match = pattern.Match(errorMessage);
            if (match.Success)
            {
                string fullFileName = match.Groups[1].Value;
                string fileName = Path.GetFileName(fullFileName);
                string lineNumber = match.Groups[2].Value;
                string msgReference = msgId == 0 ? "" : $"#{msgId} - ";

                stackTrace = $"at {msgReference}{fileName}:{lineNumber} in {fullFileName}:line {lineNumber}{Environment.NewLine}";
                errorMessage = errorMessage.Replace(match.Value, "").Trim();
            }
            else
            {
                stackTrace = "";
            }

            return match.Success;
        }

    }

}