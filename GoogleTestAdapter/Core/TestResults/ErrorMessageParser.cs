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
        private static readonly string ValidCharRegex;

        private Regex SplitRegex { get; }
        private Regex ParseRegex { get; }
        private Regex ScopedTraceStartRegex { get; }
        private Regex ScopedTraceRegex { get; }

        static ErrorMessageParser()
        {
            IEnumerable<char> invalidChars =
                Path.GetInvalidFileNameChars().Where(c => Path.GetInvalidPathChars().Contains(c));
            ValidCharRegex = "[^" + Regex.Escape(new string(invalidChars.ToArray())) + "]";
        }

        public string ErrorMessage { get; private set; }
        public string ErrorStackTrace { get; private set; }

        private IList<string> ErrorMessages { get; }

        public ErrorMessageParser(string consoleOutput, string baseDir) : this(baseDir)
        {
            ErrorMessages = SplitConsoleOutput(consoleOutput);
        }

        public ErrorMessageParser(XmlNodeList failureNodes, string baseDir) : this(baseDir)
        {
            ErrorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText).ToList();
        }

        private ErrorMessageParser(string baseDir)
        {
            string escapedBaseDir = Regex.Escape(baseDir);
            string file = $"({escapedBaseDir}{ValidCharRegex}*)";
            string line = "([0-9]+)";
            string fileAndLine = $@"{file}((:{line})|(\({line}\):))";
            string error = @"((error: )|(Failure\n))";

            ParseRegex = new Regex($"{fileAndLine}(:? {error})?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            SplitRegex = new Regex($"{fileAndLine}:? {error}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            ScopedTraceStartRegex = new Regex(@"Google Test trace:\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            ScopedTraceRegex = new Regex($@"{file}\({line}\): (.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public void Parse()
        {
            switch (ErrorMessages.Count)
            {
                case 0:
                    ErrorMessage = "";
                    ErrorStackTrace = "";
                    break;
                case 1:
                    HandleSingleFailure();
                    break;
                default:
                    HandleMultipleFailures();
                    break;
            }
        }

        public static string CreateStackTraceEntry(string label, string fullFileName, string lineNumber)
        {
            return $"at {label} in {fullFileName}:line {lineNumber}{Environment.NewLine}";
        }

        private IList<string> SplitConsoleOutput(string errorMessage)
        {
            MatchCollection matches = SplitRegex.Matches(errorMessage);
            if (matches.Count == 0)
                return new List<string>();

            var errorMessages = new List<string>();
            int startIndex, length;
            for (int i = 0; i < matches.Count - 1; i++)
            {
                startIndex = matches[i].Index;
                length = matches[i + 1].Index - startIndex;
                errorMessages.Add(errorMessage.Substring(startIndex, length));
            }
            startIndex = matches[matches.Count - 1].Index;
            length = errorMessage.Length - startIndex;
            errorMessages.Add(errorMessage.Substring(startIndex, length));

            return errorMessages;
        }

        private void HandleSingleFailure()
        {
            string errorMessage = ErrorMessages[0];
            string stackTrace;
            CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace);

            ErrorMessage = errorMessage;
            ErrorStackTrace = stackTrace;
        }

        private void HandleMultipleFailures()
        {
            var finalErrorMessages = new List<string>();
            var finalStackTraces = new List<string>();
            for (int i = 0; i < ErrorMessages.Count; i++)
            {
                string errorMessage = ErrorMessages[i];
                int msgId = i + 1;
                string stackTrace;
                CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, msgId);

                finalErrorMessages.Add($"#{msgId} - {errorMessage}");
                finalStackTraces.Add(stackTrace);
            }

            ErrorMessage = string.Join("\n", finalErrorMessages);
            ErrorStackTrace = string.Join("", finalStackTraces);
        }

        private void CreateErrorMessageAndStacktrace(ref string errorMessage, out string stackTrace, int msgId = 0)
        {
            Match match = ParseRegex.Match(errorMessage);
            if (!match.Success)
            {
                stackTrace = "";
                return;
            }

            string fullFileName = match.Groups[1].Value;
            string fileName = Path.GetFileName(fullFileName);
            string lineNumber = match.Groups[4]. Value;
            if (string.IsNullOrEmpty(lineNumber))
                lineNumber = match.Groups[6].Value;

            string msgReference = msgId == 0 ? "" : $"#{msgId} - ";

            stackTrace = CreateStackTraceEntry($"{msgReference}{fileName}:{lineNumber}", fullFileName, lineNumber);
            errorMessage = errorMessage.Replace(match.Value, "").Trim();

            match = ScopedTraceStartRegex.Match(errorMessage);
            if (match.Success)
            {
                string scopedTraces = errorMessage.Substring(match.Index + match.Value.Length);
                errorMessage = errorMessage.Substring(0, match.Index).Trim();
                MatchCollection matches = ScopedTraceRegex.Matches(scopedTraces);
                foreach (Match traceMatch in matches)
                {
                    fullFileName = traceMatch.Groups[1].Value;
                    lineNumber = traceMatch.Groups[2].Value;
                    string traceMessage = traceMatch.Groups[3].Value.Trim();

                    stackTrace += CreateStackTraceEntry($"-->{traceMessage}", fullFileName, lineNumber);
                }
            }
        }

    }

}