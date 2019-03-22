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
        private static readonly Regex SplitRegex;
        private static readonly Regex ParseRegex;
        private static readonly Regex ScopedTraceRegex;
        private static readonly Regex ScopedTraceStartRegex;
        private static readonly Regex callStackRegex;

        static ErrorMessageParser()
        {
            IEnumerable<char> invalidChars =
                Path.GetInvalidFileNameChars().Where(c => Path.GetInvalidPathChars().Contains(c));
            var validCharRegex = $"[^{Regex.Escape(new string(invalidChars.ToArray()))}]";

            string file = $"([a-z]:{validCharRegex}*)";
            string line = "([0-9]+)";
            string fileAndLine = $@"{file}(?::{line}|\({line}\):)";
            string error = @"(?:error: |Failure\n)";

            SplitRegex = new Regex($"{fileAndLine}:? {error}", RegexOptions.IgnoreCase);
            ParseRegex = new Regex($"^{fileAndLine}(?::? {error})?", RegexOptions.IgnoreCase);
            // TODO make expression parse "unknown file: error: SEH exception with code 0xc0000005 thrown in the test body."
            ScopedTraceRegex = new Regex($@"{file}\({line}\): (.*)", RegexOptions.IgnoreCase);
            ScopedTraceStartRegex
                = new Regex(@"Google Test trace:\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            callStackRegex = new Regex(@"^(.*)[\r\n]+call stack:\s*(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public string ErrorMessage { get; private set; }
        public string ErrorStackTrace { get; private set; }

        private string _outputBeforeFirstFailure = "";
        private IList<string> ErrorMessages { get; }

        public ErrorMessageParser(string consoleOutput)
        {
            ErrorMessages = SplitConsoleOutput(consoleOutput);
        }

        public ErrorMessageParser(XmlNodeList failureNodes)
        {
            ErrorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText).ToList();
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

            if (_outputBeforeFirstFailure != "")
            {
                if (!_outputBeforeFirstFailure.EndsWith("\n") && !_outputBeforeFirstFailure.EndsWith("\r\n"))
                    _outputBeforeFirstFailure += "\n";
                ErrorMessage = $"{_outputBeforeFirstFailure}{ErrorMessage}";
            }
        }

        public static string CreateStackTraceEntry(string label, string fullFileName, string lineNumber)
        {
            return $"at {label} in {fullFileName}:line {lineNumber}{Environment.NewLine}";
        }

        private IList<string> SplitConsoleOutput(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return new List<string>();

            MatchCollection matches = SplitRegex.Matches(errorMessage);
            if (matches.Count == 0)
                return new List<string>{ errorMessage };

            var errorMessages = new List<string>();
            int startIndex = 0;
            int length = matches[0].Index;
            if (length > 0)
                _outputBeforeFirstFailure = errorMessage.Substring(startIndex, length);

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
            CreateErrorMessageAndStacktrace(ref errorMessage, out var stackTrace);

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
                CreateErrorMessageAndStacktrace(ref errorMessage, out var stackTrace, msgId);

                finalErrorMessages.Add($"#{msgId} - {errorMessage}");
                finalStackTraces.Add(stackTrace);
            }

            ErrorMessage = string.Join("\n", finalErrorMessages);
            ErrorStackTrace = string.Join("", finalStackTraces);
        }

        private void CreateErrorMessageAndStacktrace(ref string errorMessage, out string stackTrace, int msgId = 0)
        {
            //
            // C# style reporting:
            //
            // <error message line 1>
            // <error message line 2>
            // <error message line 3>
            // ...
            // call stack:
            // file(line): error message stack 1
            // file(line): error message stack 2
            // ...
            Match cstackMatch = callStackRegex.Match(errorMessage);
            if (cstackMatch.Success)
            {
                errorMessage = cstackMatch.Groups[1].ToString().Trim();
                String callStackText = cstackMatch.Groups[2].ToString();

                stackTrace = "";

                MatchCollection matches = ScopedTraceRegex.Matches(callStackText);
                foreach (Match traceMatch in matches)
                {
                    String file = traceMatch.Groups[1].Value;
                    String line = traceMatch.Groups[2].Value;
                    String traceMessage = traceMatch.Groups[3].Value.Trim();
                    stackTrace += CreateStackTraceEntry(traceMessage, file, line);
                }
                return;
            }

            Match match = ParseRegex.Match(errorMessage);
            if (!match.Success)
            {
                stackTrace = "";
                return;
            }

            string fullFileName = match.Groups[1].Value;
            string fileName = Path.GetFileName(fullFileName);
            string lineNumber = match.Groups[2].Value;
            if (string.IsNullOrEmpty(lineNumber))
                lineNumber = match.Groups[3].Value;

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