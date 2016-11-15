using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseFactory
    {
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly string _executable;
        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly MethodSignatureCreator _signatureCreator = new MethodSignatureCreator();

        public TestCaseFactory(string executable, ILogger logger, SettingsWrapper settings, IDiaResolverFactory diaResolverFactory)
        {
            _logger = logger;
            _settings = settings;
            _executable = executable;
            _diaResolverFactory = diaResolverFactory;
        }

        public IList<TestCase> CreateTestCases(Action<TestCase> reportTestCase = null)
        {
            List<string> standardOutput = new List<string>();
            int processExitCode;
            if (_settings.UseNewTestExecutionFramework)
            {
                var testCases = new List<TestCase>();

                var resolver = new NewTestCaseResolver(
                    _executable, 
                    _settings.GetPathExtension(_executable), 
                    _diaResolverFactory,
                    _settings.ParseSymbolInformation,
                    _logger);

                var parser = new StreamingListTestsParser(_settings.TestNameSeparator);
                parser.TestCaseDescriptorCreated += (sender, args) =>
                {
                    TestCase testCase;
                    if (_settings.ParseSymbolInformation)
                    {
                        TestCaseLocation testCaseLocation =
                            resolver.FindTestCaseLocation(_signatureCreator.GetTestMethodSignatures(args.TestCaseDescriptor).ToList());
                        testCase = CreateTestCase(args.TestCaseDescriptor, testCaseLocation.Yield().ToList());
                    }
                    else
                    {
                        testCase = CreateTestCase(args.TestCaseDescriptor);
                    }
                    reportTestCase?.Invoke(testCase);
                    testCases.Add(testCase);
                };

                Action<string> lineAction = s =>
                {
                    standardOutput.Add(s);
                    parser.ReportLine(s);
                };

                var executor = new ProcessExecutor(null, _logger);
                processExitCode = executor.ExecuteCommandBlocking(
                    _executable, 
                    GoogleTestConstants.ListTestsOption.Trim(), 
                    "", 
                    _settings.GetPathExtension(_executable),
                    lineAction);

                if (!CheckProcessExitCode(processExitCode, standardOutput))
                    return new List<TestCase>();

                return testCases;
            }

            var launcher = new ProcessLauncher(_logger, _settings.GetPathExtension(_executable));
            standardOutput = launcher.GetOutputOfCommand("", _executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, out processExitCode);

            if (!CheckProcessExitCode(processExitCode, standardOutput))
                return new List<TestCase>();

            IList<TestCaseDescriptor> testCaseDescriptors = new ListTestsParser(_settings.TestNameSeparator).ParseListTestsOutput(standardOutput);
            if (_settings.ParseSymbolInformation)
            {
                List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(testCaseDescriptors, _settings.GetPathExtension(_executable));
                return testCaseDescriptors.Select(descriptor => CreateTestCase(descriptor, testCaseLocations)).ToList();
            }

            return testCaseDescriptors.Select(CreateTestCase).ToList();
        }

        private bool CheckProcessExitCode(int processExitCode, ICollection<string> standardOutput)
        {
            if (processExitCode != 0)
            {
                string messsage =
                    $"Could not list test cases of executable '{_executable}': executing process failed with return code {processExitCode}";
                messsage +=
                    $"\nCommand executed: '{_executable} {GoogleTestConstants.ListTestsOption.Trim()}', working directory: '{Path.GetDirectoryName(_executable)}'";
                if (standardOutput.Count(s => !string.IsNullOrEmpty(s)) > 0)
                    messsage += $"\nOutput of command:\n{string.Join("\n", standardOutput)}";
                else
                    messsage += "\nCommand produced no output";

                _logger.LogWarning(messsage);
                return false;
            }
            return true;
        }

        private List<TestCaseLocation> GetTestCaseLocations(IList<TestCaseDescriptor> testCaseDescriptors, string pathExtension)
        {
            var testMethodSignatures = new List<string>();
            foreach (TestCaseDescriptor descriptor in testCaseDescriptors)
            {
                testMethodSignatures.AddRange(_signatureCreator.GetTestMethodSignatures(descriptor));
            }

            string filterString = "*" + GoogleTestConstants.TestBodySignature;
            var resolver = new TestCaseResolver(_diaResolverFactory, _logger);
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
                l => l != null && _signatureCreator.GetTestMethodSignatures(descriptor).Any(s => Regex.IsMatch(l.Symbol, s)));

            if (location != null)
            {
                var testCase = new TestCase(
                    descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, location.Sourcefile, (int)location.Line);
                testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, location.Traits));
                return testCase;
            }

            _logger.LogWarning($"Could not find source location for test {descriptor.FullyQualifiedName}");
            return new TestCase(
                descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, "", 0);
        }

        private IList<Trait> GetFinalTraits(string displayName, List<Trait> traits)
        {
            var afterTraits =
                _settings.TraitsRegexesAfter
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

            var beforeTraits =_settings.TraitsRegexesBefore
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