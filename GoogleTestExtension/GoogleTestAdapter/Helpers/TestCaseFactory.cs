using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DiaAdapter;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Helpers
{
    internal class TestCaseFactory
    {

        private class TestCaseDescriptor
        {
            internal string Suite { get; }
            internal string Name { get; }
            internal string Param { get; }
            internal string TypeParam { get; }

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

            public override string ToString()
            {
                return DisplayName;
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
                    typeParam = typeParam.Replace("struct ", "");
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

        private class MethodSignatureCreator
        {

            internal IEnumerable<string> GetTestMethodSignatures(TestCaseDescriptor descriptor)
            {
                if (descriptor.TypeParam != null)
                {
                    return GetTypedTestMethodSignatures(descriptor);
                }
                if (descriptor.Param != null)
                {
                    return GetParameterizedTestMethodSignature(descriptor).Yield();
                }

                return GetTestMethodSignature(descriptor.Suite, descriptor.Name).Yield();
            }

            private IEnumerable<string> GetTypedTestMethodSignatures(TestCaseDescriptor descriptor)
            {
                List<string> result = new List<string>();

                // remove instance number
                string suite = descriptor.Suite.Substring(0, descriptor.Suite.LastIndexOf("/"));

                // remove prefix
                if (suite.Contains("/"))
                {
                    int index = suite.IndexOf("/");
                    suite = suite.Substring(index + 1, suite.Length - index - 1);
                }

                string typeParam = ListTestsParser.GetEnclosedTypeParam(descriptor.TypeParam);

                // <testcase name>_<test name>_Test<type param value>::TestBody
                result.Add(GetTestMethodSignature(suite, descriptor.Name, typeParam));

                // gtest_case_<testcase name>_::<test name><type param value>::TestBody
                string signature =
                    $"gtest_case_{suite}_::{descriptor.Name}{typeParam}{GoogleTestConstants.TestBodySignature}";
                result.Add(signature);

                return result;
            }

            private string GetParameterizedTestMethodSignature(TestCaseDescriptor descriptor)
            {
                // remove instance number
                int index = descriptor.Suite.IndexOf('/');
                string suite = index < 0 ? descriptor.Suite : descriptor.Suite.Substring(index + 1);

                index = descriptor.Name.IndexOf('/');
                string testName = index < 0 ? descriptor.Name : descriptor.Name.Substring(0, index);

                return GetTestMethodSignature(suite, testName);
            }

            private string GetTestMethodSignature(string suite, string testCase, string typeParam = "")
            {
                return suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature;
            }

        }

        private class TestCaseLocation : SourceFileLocation
        {
            internal List<Trait> Traits { get; } = new List<Trait>();

            internal TestCaseLocation(string symbol, string sourceFile, uint line) : base(symbol, sourceFile, line)
            {
            }
        }

        private class TestCaseResolver
        {

            // see GTA_Traits.h
            private const string TraitSeparator = "__GTA__";
            private const string TraitAppendix = "_GTA_TRAIT";

            internal List<TestCaseLocation> ResolveAllTestCases(string executable, List<string> testMethodSignatures, string symbolFilterString, List<string> errorMessages)
            {
                List<TestCaseLocation> testCaseLocationsFound =
                    FindTestCaseLocationsInBinary(executable, testMethodSignatures, symbolFilterString, errorMessages).ToList();

                if (testCaseLocationsFound.Count == 0)
                {
                    NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(executable, errorMessages);
                    List<string> imports = parser.Imports;

                    string moduleDirectory = Path.GetDirectoryName(executable);

                    foreach (string import in imports)
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        string importedBinary = Path.Combine(moduleDirectory, import);
                        if (File.Exists(importedBinary))
                        {
                            testCaseLocationsFound.AddRange(FindTestCaseLocationsInBinary(importedBinary, testMethodSignatures, symbolFilterString, errorMessages));
                        }
                    }
                }
                return testCaseLocationsFound;
            }


            private IEnumerable<TestCaseLocation> FindTestCaseLocationsInBinary(
                string binary, List<string> testMethodSignatures, string symbolFilterString, List<string> errorMessages)
            {
                DiaResolver resolver = new DiaResolver(binary);
                IEnumerable<SourceFileLocation> allTestMethodSymbols = resolver.GetFunctions(symbolFilterString);
                IEnumerable<SourceFileLocation> allTraitSymbols = resolver.GetFunctions("*" + TraitAppendix);

                IEnumerable<TestCaseLocation> result = allTestMethodSymbols
                    .Where(nsfl => testMethodSignatures.Any(tms => nsfl.Symbol.Contains(tms))) // Contains() instead of == because nsfl might contain namespace
                    .Select(nsfl => ToTestCaseLocation(nsfl, allTraitSymbols))
                    .ToList(); // we need to force immediate query execution, otherwise our session object will already be released

                errorMessages.AddRange(resolver.ErrorMessages);
                resolver.Dispose();

                return result;
            }

            private TestCaseLocation ToTestCaseLocation(SourceFileLocation location, IEnumerable<SourceFileLocation> allTraitSymbols)
            {
                List<Trait> traits = GetTraits(location, allTraitSymbols);
                TestCaseLocation testCaseLocation = new TestCaseLocation(location.Symbol, location.Sourcefile, location.Line);
                testCaseLocation.Traits.AddRange(traits);
                return testCaseLocation;
            }

            private List<Trait> GetTraits(SourceFileLocation nativeSymbol, IEnumerable<SourceFileLocation> allTraitSymbols)
            {
                List<Trait> traits = new List<Trait>();
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (SourceFileLocation nativeTraitSymbol in allTraitSymbols)
                {
                    int indexOfSerializedTrait = nativeTraitSymbol.Symbol.LastIndexOf("::", StringComparison.Ordinal) + "::".Length;
                    string testClassSignature = nativeTraitSymbol.Symbol.Substring(0, indexOfSerializedTrait - "::".Length);
                    if (nativeSymbol.Symbol.StartsWith(testClassSignature))
                    {
                        int lengthOfSerializedTrait = nativeTraitSymbol.Symbol.Length - indexOfSerializedTrait - TraitAppendix.Length;
                        string serializedTrait = nativeTraitSymbol.Symbol.Substring(indexOfSerializedTrait, lengthOfSerializedTrait);
                        string[] data = serializedTrait.Split(new[] { TraitSeparator }, StringSplitOptions.None);
                        traits.Add(new Trait(data[0], data[1]));
                    }
                }

                return traits;
            }

        }


        private TestEnvironment TestEnvironment { get; }
        private string Executable { get; }
        private MethodSignatureCreator SignatureCreator { get; } = new MethodSignatureCreator();

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
                testMethodSignatures.AddRange(SignatureCreator.GetTestMethodSignatures(descriptor));
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
                l => SignatureCreator.GetTestMethodSignatures(descriptor).Any(
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