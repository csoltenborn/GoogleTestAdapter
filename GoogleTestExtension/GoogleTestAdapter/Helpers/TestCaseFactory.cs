using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Helpers
{
    public class TestCaseFactory
    {
        private class TestCaseDescriptor
        {
            private string Suite { get; }
            private string Name { get; }
            private string Param { get; }
            private string TypeParam { get; }

            internal string FullyQualifiedName { get; }
            internal string DisplayName { get; }

            internal TestCaseDescriptor(string suite, string name, string typeParam, string param, string fullyQualifiedName, string displayName)
            {
                Suite = suite;
                Name = name;
                TypeParam = typeParam;
                Param = param;
                DisplayName = displayName;
                FullyQualifiedName = fullyQualifiedName;
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

        private class ListTestsParser
        {
            private TestEnvironment TestEnvironment { get; }

            internal ListTestsParser(TestEnvironment testEnvironment)
            {
                TestEnvironment = testEnvironment;
            }

            internal IList<TestCaseDescriptor> ParseListTestsOutput(string executable)
            {
                ProcessLauncher launcher = new ProcessLauncher(TestEnvironment, false);
                List<string> consoleOutput = launcher.GetOutputOfCommand("", executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, null);
                return ParseConsoleOutput(consoleOutput);
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
                            CreateDescriptor(currentSuite, trimmedLine.Substring(2)));
                    }
                    else
                    {
                        currentSuite = trimmedLine;
                    }
                }

                return testCaseDescriptors;
            }

            private TestCaseDescriptor CreateDescriptor(string suiteLine, string testCaseLine)
            {
                string[] split = suiteLine.Split(new[] { GoogleTestConstants.TypedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
                string suite = split.Length > 0 ? split[0] : suiteLine;
                string typeParam = null;
                if (split.Length > 1)
                {
                    typeParam = split[1];
                    typeParam = typeParam.Replace("class ", "");
                }

                split = testCaseLine.Split(new[] { GoogleTestConstants.ParameterizedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
                string name = split.Length > 0 ? split[0] : testCaseLine;
                string param = null;
                if (split.Length > 1)
                {
                    param = split[1];
                }

                string fullyQualifiedName = $"{suite}.{name}";
                string displayName = GetDisplayName(fullyQualifiedName, typeParam, param);

                return new TestCaseDescriptor(suite, name, typeParam, param, fullyQualifiedName, displayName);
            }

            private static string GetDisplayName(string fullyQalifiedName, string typeParam, string param)
            {
                string displayName = fullyQalifiedName;
                if (!string.IsNullOrEmpty(typeParam))
                {
                    displayName += GetEnclosedTypeParam(typeParam);
                }
                if (!string.IsNullOrEmpty(param))
                {
                    displayName += $" [{param}]";
                }

                return displayName;
            }

            internal static string GetEnclosedTypeParam(string typeParam)
            {
                if (typeParam.EndsWith(">"))
                {
                    typeParam += " ";
                }
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
            IList<TestCaseDescriptor> testCaseDescriptors = new ListTestsParser(TestEnvironment).ParseListTestsOutput(Executable);
            List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(testCaseDescriptors);

            List<TestCase> result = new List<TestCase>();
            foreach (TestCaseDescriptor descriptor in testCaseDescriptors)
            {
                result.Add(CreateTestCase(descriptor, testCaseLocations));
            }

            return result;
        }

        private List<TestCaseLocation> GetTestCaseLocations(IList<TestCaseDescriptor> testCaseDescriptors)
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