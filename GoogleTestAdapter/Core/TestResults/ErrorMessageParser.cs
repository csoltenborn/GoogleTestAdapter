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
        private static readonly string ExceptionMessageAt;
        private static readonly string ExceptionMessageIn;
        private static readonly string ExceptionMessageLine;

        static ErrorMessageParser()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception e)
            {
                string pattern = @"   ([a-zA-Z]+) .*\) ([a-zA-Z]+) .*:([a-zA-Z]+) [0-9]+";
                Match match = Regex.Match(e.StackTrace, pattern);
                if (match.Success)
                {
                    ExceptionMessageAt = match.Groups[1].Value;
                    ExceptionMessageIn = match.Groups[2].Value;
                    ExceptionMessageLine = match.Groups[3].Value;
                }
                else
                {
                    ExceptionMessageAt = "at";
                    ExceptionMessageIn = "in";
                    ExceptionMessageLine = "line";
                }
            }
        }


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
                string at = ExceptionMessageAt;
                string _in = ExceptionMessageIn;
                string line = ExceptionMessageLine;

                string fileName = Path.GetFileName(sourceFile);
                string lineNumber = match.Groups[1].Value;
                stackTrace = $"   {at} {fileName}:{lineNumber} {_in} {sourceFile}:{line} {lineNumber}.";

                errorMessage = errorMessage.Replace(match.Groups[0].Value, "").Trim();
            }
            return match.Success;
        }

    }

}