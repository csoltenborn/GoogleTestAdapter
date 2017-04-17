﻿using System;
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
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.XmlDiffPatch;

namespace GoogleTestAdapter.Tests.Common.ResultChecker
{
    public class TrxResultChecker
    {
        private const string ResourceLocation = "GoogleTestAdapter.Tests.Common.Resources.Trx2TestRun.xslt";

        private readonly string _goldenFilesDir;
        private readonly string _diffFilesDir;

        public TrxResultChecker(string solutionFile)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            string projectDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionFile),
                @"..\GoogleTestAdapter\VsPackage.Tests.Generated"));

            _goldenFilesDir = Path.Combine(projectDir, "GoldenFiles");
            Directory.CreateDirectory(_goldenFilesDir);

            _diffFilesDir = Path.Combine(projectDir, "TestErrors");
        }

#pragma warning disable 162
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        [SuppressMessage("ReSharper", "RedundantLogicalConditionalExpressionOperand")]
        public void RunTestsAndCheckOutput(string typeName, string arguments, string testCaseName)
        {
            string goldenFile = Path.Combine(_goldenFilesDir,
                TestResources.GetGoldenFileName(typeName, testCaseName, ".xml"));

            string resultsFile = RunExecutableAndGetResultsFile(arguments);
            if (resultsFile == null)
            {
                goldenFile.AsFileInfo().Should().NotExist("Test run did not produce result trx file, therefore no golden file should exist");
                return;
            }

            string transformedResultFile = TransformResultsFile(resultsFile);
            CheckIfFileIsParsable(transformedResultFile);

            if (!File.Exists(goldenFile))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                File.Copy(transformedResultFile, goldenFile, true);
                Assert.Inconclusive($"First time this test runs, created golden file - check for correctness! File: {goldenFile}");
            }
            CheckIfFileIsParsable(goldenFile);
            string diffFile;
            if (!CompareXmlFiles(goldenFile, transformedResultFile, out diffFile))
            {
                if (!Directory.Exists(_diffFilesDir))
                    Directory.CreateDirectory(_diffFilesDir);

                string htmlDiffFile = Path.Combine(_diffFilesDir,
                    TestResources.GetGoldenFileName(typeName, testCaseName, ".html"));
                string resultFile = Path.ChangeExtension(htmlDiffFile, ".xml");

                string htmlContent = CreateDiffAsHtml(goldenFile, diffFile);
                File.WriteAllText(htmlDiffFile, htmlContent);

                File.Copy(transformedResultFile, resultFile, true);

                if (TestMetadata.OverwriteTestResults && !CiSupport.IsRunningOnBuildServer)
                {
                    File.Copy(transformedResultFile, goldenFile, true);
                    Assert.Inconclusive($"Updated golden file file:///{goldenFile}, test should now pass");
                }

                Assert.Fail($@"Files differ, see file:///{htmlDiffFile}");
            }
        }
#pragma warning restore 162

        private string RunExecutableAndGetResultsFile(string arguments)
        {
            string command = TestResources.GetVsTestConsolePath(TestMetadata.VersionUnderTest);
            command.AsFileInfo().Should().Exist();

            string workingDir = "";

            var launcher = new TestProcessLauncher();
            List<string> standardOut, standardErr;
            launcher.GetOutputStreams(workingDir, command, arguments, out standardOut, out standardErr);

            return ParseResultsFileFromOutput(standardOut);
        }

        private string TransformResultsFile(string resultsFile)
        {
            using (
                Stream stream =
                    Assembly.GetAssembly(GetType())
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
            var xmlDiffView = new XmlDiffView();
            using (var expectationFileReader = new XmlTextReader(expectationFile))
            using (var diffFileReader = new XmlTextReader(diffFile))
            {
                xmlDiffView.Load(expectationFileReader, diffFileReader);
            }

            var htmlContent = new StringBuilder();
            var xmlDiffWriter = new StringWriter(htmlContent);

            xmlDiffWriter.Write("<html><body><table width='100%'><tr><th>Expected</th><th>Actual</th></tr>");
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
            string resultsFileRegex = @"(?:Results File|Ergebnisdatei): (.*)";
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