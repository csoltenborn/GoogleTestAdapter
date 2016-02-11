using System;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.TestAdapter
{

    public static class DataConversionExtensions
    {

        public static Model.TestCase ToTestCase(this Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase vsTestCase)
        {
            Model.TestCase testCase = new Model.TestCase(
                vsTestCase.FullyQualifiedName, vsTestCase.Source, vsTestCase.DisplayName,
                vsTestCase.CodeFilePath, vsTestCase.LineNumber);
            testCase.Traits.AddRange(vsTestCase.Traits.Select(ToTrait));
            return testCase;
        }

        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase ToVsTestCase(this Model.TestCase testCase)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase vsTestCase =
                new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(
                    testCase.FullyQualifiedName, TestExecutor.ExecutorUri, testCase.Source);
            vsTestCase.DisplayName = testCase.DisplayName;
            vsTestCase.CodeFilePath = testCase.CodeFilePath;
            vsTestCase.LineNumber = testCase.LineNumber;
            vsTestCase.Traits.AddRange(testCase.Traits.Select(ToVsTrait));
            return vsTestCase;
        }


        public static Model.Trait ToTrait(this Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait trait)
        {
            return new Model.Trait(trait.Name, trait.Value);
        }

        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait ToVsTrait(this Model.Trait trait)
        {
            return new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait(trait.Name, trait.Value);
        }


        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult ToVsTestResult(this Model.TestResult testResult)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult result = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(ToVsTestCase(testResult.TestCase));
            result.Outcome = testResult.Outcome.ToVsTestOutcome();
            result.ComputerName = testResult.ComputerName;
            result.DisplayName = testResult.DisplayName;
            result.Duration = testResult.Duration;
            result.ErrorMessage = testResult.ErrorMessage;
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, "My stderr message"));
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, "My stdout message"));
            result.Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, "My info message"));
            result.Messages.Add(new TestResultMessage(TestResultMessage.DebugTraceCategory, "My debug message"));
            result.ErrorStackTrace = "My stack trace";
            return result;
        }


        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome ToVsTestOutcome(this Model.TestOutcome testOutcome)
        {
            switch (testOutcome)
            {
                case Model.TestOutcome.Passed:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed;
                case Model.TestOutcome.Failed:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed;
                case Model.TestOutcome.Skipped:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped;
                case Model.TestOutcome.None:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.None;
                case Model.TestOutcome.NotFound:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.NotFound;
                default:
                    throw new Exception();
            }
        }

    }

}