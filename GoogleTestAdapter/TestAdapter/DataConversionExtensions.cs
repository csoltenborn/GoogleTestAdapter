using System;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using VsTestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using VsTrait = Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait;

namespace GoogleTestAdapter.TestAdapter
{

    public static class DataConversionExtensions
    {

        private static bool _noTraitsSupportedErrorShown;

        private const string NoTraitsSupportedErrorMessage =
            "Visual Studio versions prior to 2012 update 1 do not support traits. Please update your VS installation.";

        public static TestCase ToTestCase(this VsTestCase vsTestCase, ILogger logger)
        {
            var testCase = new TestCase(vsTestCase.FullyQualifiedName, vsTestCase.Source, 
                vsTestCase.DisplayName, vsTestCase.CodeFilePath, vsTestCase.LineNumber);
            ConvertTraitsSafely(() => testCase.Traits.AddRange(vsTestCase.Traits.Select(ToTrait)), logger);
            return testCase;
        }

        public static VsTestCase ToVsTestCase(this TestCase testCase, ILogger logger)
        {
            var vsTestCase = new VsTestCase(testCase.FullyQualifiedName, TestExecutor.ExecutorUri, testCase.Source)
            {
                DisplayName = testCase.DisplayName,
                CodeFilePath = testCase.CodeFilePath,
                LineNumber = testCase.LineNumber
            };
            ConvertTraitsSafely(() => vsTestCase.Traits.AddRange(testCase.Traits.Select(ToVsTrait)), logger);
            return vsTestCase;
        }

        private static void ConvertTraitsSafely(Action action, ILogger logger)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception)
            {
                if (!_noTraitsSupportedErrorShown && logger != null)
                {
                    _noTraitsSupportedErrorShown = true;
                    logger.LogError(NoTraitsSupportedErrorMessage);
                }
            }
        }


        private static Trait ToTrait(this VsTrait trait)
        {
            return new Trait(trait.Name, trait.Value);
        }

        private static VsTrait ToVsTrait(this Trait trait)
        {
            return new VsTrait(trait.Name, trait.Value);
        }


        public static VsTestResult ToVsTestResult(this TestResult testResult)
        {
            return new VsTestResult(ToVsTestCase(testResult.TestCase, null))
            {
                Outcome = testResult.Outcome.ToVsTestOutcome(),
                ComputerName = testResult.ComputerName,
                DisplayName = testResult.DisplayName,
                Duration = testResult.Duration,
                ErrorMessage = testResult.ErrorMessage,
                ErrorStackTrace = testResult.ErrorStackTrace
            };
        }


        public static VsTestOutcome ToVsTestOutcome(this TestOutcome testOutcome)
        {
            switch (testOutcome)
            {
                case TestOutcome.Passed:
                    return VsTestOutcome.Passed;
                case TestOutcome.Failed:
                    return VsTestOutcome.Failed;
                case TestOutcome.Skipped:
                    return VsTestOutcome.Skipped;
                case TestOutcome.None:
                    return VsTestOutcome.None;
                case TestOutcome.NotFound:
                    return VsTestOutcome.NotFound;
                default:
                    throw new Exception();
            }
        }

        public static Severity GetSeverity(this TestMessageLevel level)
        {
            switch (level)
            {
                case TestMessageLevel.Informational:
                    return Severity.Info;
                case TestMessageLevel.Warning:
                    return Severity.Warning;
                case TestMessageLevel.Error:
                    return Severity.Error;
                default:
                    throw new InvalidOperationException($"Unknown enum literal: {level}");
            }
        }

    }

}