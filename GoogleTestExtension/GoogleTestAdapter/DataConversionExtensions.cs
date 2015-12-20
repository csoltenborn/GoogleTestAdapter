using System;
using System.Linq;

namespace GoogleTestAdapterVSIX.TestFrameworkIntegration
{

    public static class DataConversionExtensions
    {

        public static GoogleTestAdapter.Model.TestCase ToTestCase(this Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase vsTestCase)
        {
            GoogleTestAdapter.Model.TestCase testCase = new GoogleTestAdapter.Model.TestCase(vsTestCase.FullyQualifiedName, vsTestCase.ExecutorUri, vsTestCase.Source);
            testCase.DisplayName = vsTestCase.DisplayName;
            testCase.CodeFilePath = vsTestCase.CodeFilePath;
            testCase.LineNumber = vsTestCase.LineNumber;
            testCase.Traits.AddRange(vsTestCase.Traits.Select(ToTrait));
            return testCase;
        }

        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase ToVsTestCase(this GoogleTestAdapter.Model.TestCase testCase)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase vsTestCase = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(testCase.FullyQualifiedName, testCase.ExecutorUri, testCase.Source);
            vsTestCase.DisplayName = testCase.DisplayName;
            vsTestCase.CodeFilePath = testCase.CodeFilePath;
            vsTestCase.LineNumber = testCase.LineNumber;
            vsTestCase.Traits.AddRange(testCase.Traits.Select(ToVsTrait));
            return vsTestCase;
        }


        public static GoogleTestAdapter.Model.Trait ToTrait(this Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait trait)
        {
            return new GoogleTestAdapter.Model.Trait(trait.Name, trait.Value);
        }

        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait ToVsTrait(this GoogleTestAdapter.Model.Trait trait)
        {
            return new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait(trait.Name, trait.Value);
        }


        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult ToVsTestResult(this GoogleTestAdapter.Model.TestResult testResult)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult result = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(ToVsTestCase(testResult.TestCase));
            result.Outcome = testResult.Outcome.ToVsTestOutcome();
            result.ComputerName = testResult.ComputerName;
            result.DisplayName = testResult.DisplayName;
            result.Duration = testResult.Duration;
            result.ErrorMessage = testResult.ErrorMessage;
            return result;
        }


        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome ToVsTestOutcome(this GoogleTestAdapter.Model.TestOutcome testOutcome)
        {
            switch (testOutcome)
            {
                case GoogleTestAdapter.Model.TestOutcome.Passed:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed;
                case GoogleTestAdapter.Model.TestOutcome.Failed:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed;
                case GoogleTestAdapter.Model.TestOutcome.Skipped:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped;
                case GoogleTestAdapter.Model.TestOutcome.None:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.None;
                case GoogleTestAdapter.Model.TestOutcome.NotFound:
                    return Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.NotFound;
                default:
                    throw new Exception();
            }
        }

    }

}