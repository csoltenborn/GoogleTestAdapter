using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseFactory
    {
        private readonly TestEnvironment _testEnvironment;
        private readonly string _executable;
        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly MethodSignatureCreator _signatureCreator = new MethodSignatureCreator();

        public TestCaseFactory(string executable, TestEnvironment testEnvironment, IDiaResolverFactory diaResolverFactory)
        {
            _testEnvironment = testEnvironment;
            _executable = executable;
            _diaResolverFactory = diaResolverFactory;
        }

        public IList<TestCase> CreateTestCases()
        {
            var launcher = new ProcessLauncher(_testEnvironment, _testEnvironment.Options.GetPathExtension(_executable));
            int processReturnCode;
            List<string> consoleOutput = launcher.GetOutputOfCommand("", _executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, out processReturnCode);
            if (processReturnCode != 0)
            {
                string messsage =
                    $"Could not list test cases of executable '{_executable}': executing process failed with return code {processReturnCode}";
                messsage += $"\nCommand executed: '{_executable} {GoogleTestConstants.ListTestsOption.Trim()}', working directory: '{Path.GetDirectoryName(_executable)}'";
                if (consoleOutput.Count(s => !string.IsNullOrEmpty(s)) > 0)
                    messsage += $"\nOutput of command:\n{string.Join("\n", consoleOutput)}";
                else
                    messsage += "\nCommand produced no output";

                _testEnvironment.LogWarning(messsage);

                return new List<TestCase>();
            }

            IList<TestCaseDescriptor> testCaseDescriptors = new ListTestsParser(_testEnvironment).ParseListTestsOutput(consoleOutput);
            if (_testEnvironment.Options.ParseSymbolInformation)
            {
                List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(testCaseDescriptors, _testEnvironment.Options.GetPathExtension(_executable));
                return testCaseDescriptors.Select(descriptor => CreateTestCase(descriptor, testCaseLocations)).ToList();
            }

            return testCaseDescriptors.Select(CreateTestCase).ToList();
        }

        private List<TestCaseLocation> GetTestCaseLocations(IList<TestCaseDescriptor> testCaseDescriptors, string pathExtension)
        {
            var testMethodSignatures = new List<string>();
            foreach (TestCaseDescriptor descriptor in testCaseDescriptors)
            {
                testMethodSignatures.AddRange(_signatureCreator.GetTestMethodSignatures(descriptor));
            }

            string filterString = "*" + GoogleTestConstants.TestBodySignature;
            var resolver = new TestCaseResolver(_diaResolverFactory, _testEnvironment);
            return resolver.ResolveAllTestCases(_executable, testMethodSignatures, filterString, pathExtension);
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor)
        {
            var testCase = new TestCase(
                descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, "", 0);
            testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, new List<Trait>()));
            return testCase;
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor, List<TestCaseLocation> testCaseLocations)
        {
            TestCaseLocation location = testCaseLocations.FirstOrDefault(
                l => _signatureCreator.GetTestMethodSignatures(descriptor).Any(s => Regex.IsMatch(l.Symbol, s)));

            if (location != null)
            {
                var testCase = new TestCase(
                    descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, location.Sourcefile, (int)location.Line);
                testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, location.Traits));
                return testCase;
            }

            _testEnvironment.LogWarning($"Could not find source location for test {descriptor.FullyQualifiedName}");
            return new TestCase(
                descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, "", 0);
        }

        private IList<Trait> GetFinalTraits(string displayName, List<Trait> traits)
        {
            var afterTraits =
                _testEnvironment.Options.TraitsRegexesAfter
                    .Where(p => Regex.IsMatch(displayName, p.Regex))
                    .Select(p => p.Trait)
                    .ToArray();

            var namesOfAfterTraits = afterTraits
                .Select(t => t.Name)
                .Distinct()
                .ToArray();

            var namesOfTestAndAfterTraits = namesOfAfterTraits
                .Union(traits.Select(t => t.Name))
                .Distinct()
                .ToArray();

            var beforeTraits =_testEnvironment.Options.TraitsRegexesBefore
                .Where(p => 
                    !namesOfTestAndAfterTraits.Contains(p.Trait.Name) 
                    && Regex.IsMatch(displayName, p.Regex))
                .Select(p => p.Trait);

            var testTraits = traits
                .Where(t => !namesOfAfterTraits.Contains(t.Name));

            var finalTraits = new List<Trait>();
            finalTraits.AddRange(beforeTraits);
            finalTraits.AddRange(testTraits);
            finalTraits.AddRange(afterTraits);

            return finalTraits;
        }

    }

}