using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Helpers
{
    /*
    Simple tests: 
        Suite=<test_case_name>
        NameAndParam=<test_name>
    Tests with fixture
        Suite=<test_fixture>
        NameAndParam=<test_name>
    Parameterized case: 
        Suite=[<prefix>/]<test_case_name>, 
        NameAndParam=<test_name>/<parameter instantiation nr>  # GetParam() = <parameter instantiation>
    */
    public class TestCaseFactory
    {
        private class TestCaseDescriptor
        {
            private string Suite { get; }
            private string Name { get; }
            private string Param { get; }
            private string TypeParam { get; }

            internal string DisplayName
            {
                get
                {
                    string displayName = Suite + "." + Name;
                    if (!string.IsNullOrEmpty(TypeParam))
                    {
                        displayName += GetEnclosedTypeParam();
                    }
                    if (!string.IsNullOrEmpty(Param))
                    {
                        displayName += $" [{Param}]";
                    }
                    return displayName;
                }
            }

            internal string FullyQualifiedName
            {
                get
                {
                    return Suite + "." + Name;
                }
            }

            internal TestCaseDescriptor(string suiteLine, string testCaseLine)
            {
                string[] split = suiteLine.Split(new[] { GoogleTestConstants.TypedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
                Suite = split.Length > 0 ? split[0] : suiteLine;
                if (split.Length > 1)
                {
                    TypeParam = split[1];
                    TypeParam = TypeParam.Replace("class ", "");
                }

                split = testCaseLine.Split(new[] { GoogleTestConstants.ParameterizedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
                Name = split.Length > 0 ? split[0] : testCaseLine;
                if (split.Length > 1)
                {
                    Param = split[1];
                }
            }

            internal IEnumerable<string> GetTestMethodSignatures()
            {
                if (TypeParam != null)
                {
                    return GetTypedTestMethodSignatures();
                }
                if (Param != null)
                {
                    return GetParameterizedTestMethodSignature().Yield();
                }

                return GetTestMethodSignature(Suite, Name).Yield();
            }

            private IEnumerable<string> GetTypedTestMethodSignatures()
            {
                List<string> result = new List<string>();

                // remove instance number
                string suite = Suite.Substring(0, Suite.LastIndexOf("/"));

                // remove prefix
                if (suite.Contains("/"))
                {
                    int index = suite.IndexOf("/");
                    suite = suite.Substring(index + 1, suite.Length - index - 1);
                }

                string typeParam = GetEnclosedTypeParam();

                // <testcase name>_<test name>_Test<type param value>::TestBody
                result.Add(GetTestMethodSignature(suite, Name, typeParam));

                // gtest_case_<testcase name>_::<test name><type param value>::TestBody
                string signature =
                    $"gtest_case_{suite}_::{Name}{typeParam}{GoogleTestConstants.TestBodySignature}";
                result.Add(signature);

                return result;
            }

            private string GetParameterizedTestMethodSignature()
            {
                // remove instance number
                int index = Suite.IndexOf('/');
                string suite = index < 0 ? Suite : Suite.Substring(index + 1);

                index = Name.IndexOf('/');
                string testName = index < 0 ? Name : Name.Substring(0, index);

                return GetTestMethodSignature(suite, testName);
            }

            private string GetTestMethodSignature(string suite, string testCase, string typeParam = "")
            {
                return suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature;
            }

            private string GetEnclosedTypeParam()
            {
                string typeParam = TypeParam.EndsWith(">") ? TypeParam + " " : TypeParam;
                return $"<{typeParam}>";
            }

        }


        private TestEnvironment TestEnvironment { get; }
        private string Executable { get; }

        public TestCaseFactory(string executable, TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
            Executable = executable;
        }

        public IList<TestCase> CreateTestCases()
        {
            List<string> consoleOutput = new ProcessLauncher(TestEnvironment, false).GetOutputOfCommand("", Executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, null);
            List<TestCaseDescriptor> testCaseDescriptors = ParseConsoleOutput(consoleOutput);
            List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(testCaseDescriptors);

            List<TestCase> result = new List<TestCase>();
            foreach (TestCaseDescriptor descriptor in testCaseDescriptors)
            {
                result.Add(CreateTestCase(descriptor, testCaseLocations));
            }

            return result;
        }

        private List<TestCaseDescriptor> ParseConsoleOutput(List<string> output)
        {
            List<TestCaseDescriptor> testCaseDescriptors = new List<TestCaseDescriptor>();
            string currentSuite = "";
            foreach (string line in output)
            {
                string trimmedLine = line.Trim('.', '\n', '\r');
                if (trimmedLine.StartsWith("  "))
                {
                    testCaseDescriptors.Add(
                        new TestCaseDescriptor(currentSuite, trimmedLine.Substring(2)));
                }
                else
                {
                    currentSuite = trimmedLine;
                }
            }

            return testCaseDescriptors;
        }

        private List<TestCaseLocation> GetTestCaseLocations(List<TestCaseDescriptor> testCaseDescriptors)
        {
            List<string> testMethodSignatures = new List<string>();
            foreach (TestCaseDescriptor descriptor in testCaseDescriptors)
            {
                testMethodSignatures.AddRange(descriptor.GetTestMethodSignatures());
            }

            string filterString = "*" + GoogleTestConstants.TestBodySignature;
            List<string> errorMessages = new List<string>();

            TestCaseResolver resolver = new TestCaseResolver();
            List<TestCaseLocation> testCaseLocations = resolver.ResolveAllTestCases(Executable, testMethodSignatures, filterString, errorMessages);

            foreach (string errorMessage in errorMessages)
            {
                TestEnvironment.LogWarning(errorMessage);
            }

            return testCaseLocations;
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor, List<TestCaseLocation> testCaseLocations)
        {
            TestCaseLocation location = testCaseLocations.Where(
                l => descriptor.GetTestMethodSignatures().Any(
                    s => l.Symbol.Contains(s)))
                .FirstOrDefault();

            if (location != null)
            {
                TestCase testCase = new TestCase(
                    descriptor.FullyQualifiedName, Executable, descriptor.DisplayName, location.Sourcefile, (int)location.Line);
                testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, location.Traits));
                return testCase;
            }
            else
            {
                TestEnvironment.LogWarning($"Could not find source location for test {descriptor.FullyQualifiedName}");
                return new TestCase(
                    descriptor.FullyQualifiedName, Executable, descriptor.DisplayName, "", 0);
            }
        }

        private List<Trait> GetFinalTraits(string displayName, List<Trait> traits)
        {
            foreach (RegexTraitPair pair in TestEnvironment.Options.TraitsRegexesBefore.Where(p => Regex.IsMatch(displayName, p.Regex)))
            {
                if (!traits.Exists(T => T.Name == pair.Trait.Name))
                {
                    traits.Add(pair.Trait);
                }
            }

            foreach (RegexTraitPair pair in TestEnvironment.Options.TraitsRegexesAfter.Where(p => Regex.IsMatch(displayName, p.Regex)))
            {
                bool replacedTrait = false;
                foreach (Trait traitToModify in traits.ToArray().Where(T => T.Name == pair.Trait.Name))
                {
                    replacedTrait = true;
                    traits.Remove(traitToModify);
                    if (!traits.Contains(pair.Trait))
                    {
                        traits.Add(pair.Trait);
                    }
                }
                if (!replacedTrait)
                {
                    traits.Add(pair.Trait);
                }
            }
            return traits;
        }

    }

}