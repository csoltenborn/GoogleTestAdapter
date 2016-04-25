using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseFactory
    {
        private TestEnvironment TestEnvironment { get; }
        private string Executable { get; }
        private IDiaResolverFactory DiaResolverFactory { get; }
        private MethodSignatureCreator SignatureCreator { get; } = new MethodSignatureCreator();

        public TestCaseFactory(string executable, TestEnvironment testEnvironment, IDiaResolverFactory diaResolverFactory)
        {
            TestEnvironment = testEnvironment;
            Executable = executable;
            DiaResolverFactory = diaResolverFactory;
        }

        public IList<TestCase> CreateTestCases()
        {
            var launcher = new ProcessLauncher(TestEnvironment, TestEnvironment.Options.PathExtension);
            List<string> consoleOutput = launcher.GetOutputOfCommand("", Executable, GoogleTestConstants.ListTestsOption.Trim(), false, false);
            IList<TestCaseDescriptor> testCaseDescriptors = new ListTestsParser(TestEnvironment).ParseListTestsOutput(consoleOutput);

            if (TestEnvironment.Options.ParseSymbolInformation)
            {
                List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(testCaseDescriptors, TestEnvironment.Options.PathExtension);
                return testCaseDescriptors.Select(descriptor => CreateTestCase(descriptor, testCaseLocations)).ToList();
            }
            else
            {
                return testCaseDescriptors.Select(descriptor => CreateTestCase(descriptor)).ToList();
            }
        }

        private List<TestCaseLocation> GetTestCaseLocations(IList<TestCaseDescriptor> testCaseDescriptors, string pathExtension)
        {
            var testMethodSignatures = new List<string>();
            foreach (TestCaseDescriptor descriptor in testCaseDescriptors)
            {
                testMethodSignatures.AddRange(SignatureCreator.GetTestMethodSignatures(descriptor));
            }

            string filterString = "*" + GoogleTestConstants.TestBodySignature;
            var resolver = new TestCaseResolver(DiaResolverFactory, TestEnvironment);
            return resolver.ResolveAllTestCases(Executable, testMethodSignatures, filterString, pathExtension);
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor)
        {
            var testCase = new TestCase(
                descriptor.FullyQualifiedName, Executable, descriptor.DisplayName, "", 0);
            testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, new List<Trait>()));
            return testCase;
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor, List<TestCaseLocation> testCaseLocations)
        {
            TestCaseLocation location = testCaseLocations.FirstOrDefault(
                l => SignatureCreator.GetTestMethodSignatures(descriptor).Any(s => l.Symbol.Contains(s)));

            if (location != null)
            {
                var testCase = new TestCase(
                    descriptor.FullyQualifiedName, Executable, descriptor.DisplayName, location.Sourcefile, (int)location.Line);
                testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, location.Traits));
                return testCase;
            }

            TestEnvironment.LogWarning($"Could not find source location for test {descriptor.FullyQualifiedName}");
            return new TestCase(
                descriptor.FullyQualifiedName, Executable, descriptor.DisplayName, "", 0);
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