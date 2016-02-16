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

        public ErrorMessageParser(string errorMessage, string sourceFile)
        {
            string pattern = Regex.Escape(sourceFile) + ":([0-9]+)";
            string stackTrace;
            if (!CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, pattern, sourceFile))
            {
                pattern = Regex.Escape(sourceFile) + @"\(([0-9]+)\):";
                CreateErrorMessageAndStacktrace(ref errorMessage, out stackTrace, pattern, sourceFile);
            }

            ErrorMessage = errorMessage;
            ErrorStackTrace = stackTrace;
        }

        public ErrorMessageParser(XmlNodeList failureNodes, string sourceFile)
            : this(JoinFailuresToErrorMessage(failureNodes), sourceFile)
        { }

        private static string JoinFailuresToErrorMessage(XmlNodeList failureNodes)
        {
            IEnumerable<string> errorMessages = (from XmlNode failureNode in failureNodes select failureNode.InnerText);
            return string.Join("\n\n", errorMessages);
        }

        private bool CreateErrorMessageAndStacktrace(ref string errorMessage, out string stackTrace, string pattern, string sourceFile)
        {
            stackTrace = "";
            Match match = Regex.Match(errorMessage, pattern);
            if (match.Success)
            {
                string fileName = Path.GetFileName(sourceFile);
                string lineNumber = match.Groups[1].Value;
                stackTrace = $"at {fileName}:{lineNumber} in {sourceFile}:line {lineNumber}{Environment.NewLine}";

                errorMessage = errorMessage.Replace(match.Groups[0].Value, "").Trim();
            }
            return match.Success;
        }

    }

}