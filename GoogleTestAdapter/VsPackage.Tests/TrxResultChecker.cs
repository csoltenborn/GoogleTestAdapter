using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using FluentAssertions;
using GoogleTestAdapter.VsPackage.Helpers;
using GoogleTestAdapterUiTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.XmlDiffPatch;

namespace GoogleTestAdapter.VsPackage
{
    public class TrxResultChecker
    {
        private const string ResourceLocation = "GoogleTestAdapter.VsPackage.Resources.Trx2TestRun.xslt";

        private readonly string _solutionFile;

        public TrxResultChecker(string solutionFile)
        {
            _solutionFile = solutionFile;
        }

#pragma warning disable 162
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        public void RunTestsAndCheckOutput(string typeName, string arguments, string testCaseName)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            string projectDir = Path.Combine(Path.GetDirectoryName(_solutionFile),
                @"..\GoogleTestAdapter\VsPackage.Tests.Generated");

            string goldenFilesDir = Path.Combine(projectDir, "GoldenFiles");
            Directory.CreateDirectory(goldenFilesDir);

            string diffFilesDir = Path.Combine(projectDir, "TestErrors");
            Directory.CreateDirectory(diffFilesDir);

            string expectationFile = Path.Combine(goldenFilesDir,
                ResultChecker.GetGoldenFileName(typeName, testCaseName, ".xml"));
            string htmlDiffFile = Path.Combine(diffFilesDir, ResultChecker.GetGoldenFileName(typeName, testCaseName, ".html"));

            string resultsFile = RunExecutableAndGetResultsFile(arguments);
            if (resultsFile == null)
            {
                File.Exists(expectationFile).Should().BeFalse($"Test run did not produce result trx file, but expectation file exists at {expectationFile}");
                return;
            }

            string transformedResultFile = TransformResultsFile(resultsFile);
            CheckIfFileIsParsable(transformedResultFile);

            if (!File.Exists(expectationFile))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                File.Copy(transformedResultFile, expectationFile, true);
                Assert.Inconclusive($"First time this test runs, created golden file - check for correctness! File: {expectationFile}");
            }
            CheckIfFileIsParsable(expectationFile);

            string diffFile;
            if (!CompareXmlFiles(expectationFile, transformedResultFile, out diffFile))
            {
                string htmlContent = CreateDiffAsHtml(expectationFile, diffFile);
                File.WriteAllText(htmlDiffFile, htmlContent);

                if (ResultChecker.OverwriteTestResults)
                {
                    File.Copy(transformedResultFile, expectationFile, true);
                    Assert.Inconclusive($"Updated golden file '{expectationFile}', test should now pass");
                }

                Assert.Fail($@"Files differ, see file:///{htmlDiffFile}");
            }
        }
#pragma warning restore 162

        private string RunExecutableAndGetResultsFile(string arguments)
        {
            string command = VsExperimentalInstance.GetVsTestConsolePath(VsExperimentalInstance.Versions.VS2015);
            string workingDir = "";

            var launcher = new TestProcessLauncher();
            List<string> standardOut;
            List<string> standardErr;
            launcher.GetOutputStreams(workingDir, command, arguments, out standardOut, out standardErr);

            return ParseResultsFileFromOutput(standardOut);
        }

        private string TransformResultsFile(string resultsFile)
        {
            using (
                Stream stream =
                    Assembly.GetAssembly(typeof(AbstractConsoleIntegrationTests))
                        .GetManifestResourceStream(ResourceLocation))
            {
                var xsltTransformation = new XslCompiledTransform();
                // ReSharper disable once AssignNullToNotNullAttribute
                xsltTransformation.Load(XmlReader.Create(stream), new XsltSettings { EnableScript = true }, null);
                string transformedResultsFile = Path.GetTempFileName();
                xsltTransformation.Transform(resultsFile, transformedResultsFile);
                return transformedResultsFile;
            }
        }

        private string CreateDiffAsHtml(string expectationFile, string diffFile)
        {
            XmlDiffView xmlDiffView = new XmlDiffView();
            using (var expectationFileReader = new XmlTextReader(expectationFile))
            using (var diffFileReader = new XmlTextReader(diffFile))
            {
                xmlDiffView.Load(expectationFileReader, diffFileReader);
            }

            StringBuilder htmlContent = new StringBuilder();
            StringWriter xmlDiffWriter = new StringWriter(htmlContent);

            xmlDiffWriter.Write("<html><body><table width='100%'>");
            xmlDiffView.GetHtml(xmlDiffWriter);
            xmlDiffWriter.Write("</table></body></html>");
            return htmlContent.ToString();
        }

        private bool CompareXmlFiles(string expectationFile, string transformedResultFile, out string diffFile)
        {
            diffFile = Path.GetTempFileName();

            // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
            var xmldiff = new XmlDiff(XmlDiffOptions.IgnoreDtd |
                                      XmlDiffOptions.IgnoreNamespaces |
                                      XmlDiffOptions.IgnorePrefixes |
                                      XmlDiffOptions.IgnoreWhitespace |
                                      XmlDiffOptions.IgnoreXmlDecl);
            // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
            using (var writer = new XmlTextWriter(diffFile, Encoding.UTF8))
            {
                return xmldiff.Compare(expectationFile, transformedResultFile, false, writer);
            }
        }

        private void CheckIfFileIsParsable(string file)
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Action loadExpectationFileAction = () => XDocument.Load(file);
            loadExpectationFileAction.ShouldNotThrow($"Could not parse file {file}");
        }

        private string ParseResultsFileFromOutput(List<string> standardOut)
        {
            string resultsFileRegex = @"Results File: (.*)";
            string resultsFile = null;
            foreach (string line in standardOut)
            {
                Match match = Regex.Match(line, resultsFileRegex);
                if (match.Success)
                {
                    resultsFile = match.Groups[1].Value;
                    break;
                }
            }
            return resultsFile;
        }

    }

}