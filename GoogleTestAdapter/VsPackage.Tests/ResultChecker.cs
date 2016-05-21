using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.VsPackage
{
    public class ResultChecker
    {
        internal const bool OverwriteTestResults = false;

        private readonly string _goldenFilesDirectory;
        private readonly string _testErrorsDirectory;
        private readonly string _fileExtension;

        public ResultChecker(string goldenFilesDir, string testErrorsDir, string fileExtension)
        {
            _goldenFilesDirectory = goldenFilesDir;
            _testErrorsDirectory = testErrorsDir;
            this._fileExtension = fileExtension;
        }

        public void CheckResults(string testResults, string typeName, [CallerMemberName] string testCaseName = null)
        {
            string expectationFile = Path.Combine(_goldenFilesDirectory, typeName + "__" + testCaseName + _fileExtension);
            string resultFile = Path.Combine(_testErrorsDirectory, typeName + "__" + testCaseName + _fileExtension);

            if (!File.Exists(expectationFile))
            {
                File.WriteAllText(expectationFile, testResults);
                Assert.Inconclusive("This is the first time this test runs.");
            }

            string expectedResult = File.ReadAllText(expectationFile);
            string msg;
            bool stringsAreEqual = AreEqual(expectedResult, testResults, out msg);
            if (!stringsAreEqual)
            {
#pragma warning disable CS0162 // Unreachable code (because overwriteTestResults is compile time constant)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (OverwriteTestResults)
                // ReSharper disable HeuristicUnreachableCode
                {
                    File.WriteAllText(expectationFile, testResults);
                    Assert.Inconclusive("Test results changed and have been overwritten. Differences: " + msg);
                }
                // ReSharper restore HeuristicUnreachableCode
                else
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    Directory.CreateDirectory(Path.GetDirectoryName(resultFile));
                    File.WriteAllText(resultFile, testResults);
                    Assert.Fail("Test result doesn't match expectation. Result written to: " + resultFile + ". Differences: " + msg);
                }
#pragma warning restore CS0162
            }
            else if (File.Exists(resultFile))
            {
                File.Delete(resultFile);
            }
        }

        private bool AreEqual(string expectedResult, string result, out string msg)
        {
            // normalize file endings
            expectedResult = Regex.Replace(expectedResult, @"\r\n|\n\r|\n|\r", "\r\n");
            result = Regex.Replace(result, @"\r\n|\n\r|\n|\r", "\r\n");

            bool areEqual = true;
            List<string> messages = new List<string>();
            if (expectedResult.Length != result.Length)
            {
                areEqual = false;
                messages.Add($"Length differs, expected: {expectedResult.Length}, actual: {result.Length}");
            }

            for (int i = 0; i < Math.Min(expectedResult.Length, result.Length); i++)
            {
                if (expectedResult[i] != result[i])
                {
                    areEqual = false;
                    messages.Add($"First difference at position {i}\n"
                        + $"context 1: '{GetContext(expectedResult, i)}'\n"
                        + $"context 2: '{GetContext(result, i)}'");
                    break;
                }
            }

            msg = string.Join("\n", messages);
            return areEqual;
        }

        private string GetContext(string result, int position, int contextLength = 40)
        {
            int leftContextLength = contextLength / 2;
            int rightContextLength = contextLength - leftContextLength;

            if (position - leftContextLength < 0)
            {
                int delta = leftContextLength - position;
                leftContextLength -= delta;
                rightContextLength += delta;
            }

            if (position + rightContextLength > result.Length)
            {
                int delta = position + rightContextLength - result.Length;
                rightContextLength -= delta;
            }

            return result.Substring(position - leftContextLength, leftContextLength + rightContextLength);
        }

    }

}