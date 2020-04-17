// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestCases
{

    public class TestCaseFactory
    {
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly string _executable;
        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly MethodSignatureCreator _signatureCreator = new MethodSignatureCreator();

        public TestCaseFactory(string executable, ILogger logger, SettingsWrapper settings,
            IDiaResolverFactory diaResolverFactory)
        {
            _logger = logger;
            _settings = settings;
            _executable = executable;
            _diaResolverFactory = diaResolverFactory;
        }

        public IList<TestCase> CreateTestCases(Action<TestCase> reportTestCase = null)
        {
            List<string> standardOutput = new List<string>();
            if (_settings.UseNewTestExecutionFramework)
            {
                return NewCreateTestcases(reportTestCase, standardOutput);
            }

            try
            {
                var launcher = new ProcessLauncher(_logger, _settings.GetPathExtension(_executable), null);
                int processExitCode;
                string workingDir = new FileInfo(_executable).DirectoryName;
                standardOutput = launcher.GetOutputOfCommand(workingDir, null, _executable, GoogleTestConstants.ListTestsOption,
                    false, false, out processExitCode);

                if (!CheckProcessExitCode(processExitCode, standardOutput))
                    return new List<TestCase>();
            }
            catch (Exception e)
            {
                SequentialTestRunner.LogExecutionError(_logger, _executable, Path.GetFullPath(""),
                    GoogleTestConstants.ListTestsOption, e);
                return new List<TestCase>();
            }

            IList<TestCaseDescriptor> testCaseDescriptors = new ListTestsParser(_settings.TestNameSeparator).ParseListTestsOutput(standardOutput);
            var testCaseLocations = GetTestCaseLocations(testCaseDescriptors, _settings.GetPathExtension(_executable));

            IList<TestCase> testCases = new List<TestCase>();
            IDictionary<string, ISet<TestCase>> suite2TestCases = new Dictionary<string, ISet<TestCase>>();
            foreach (var descriptor in testCaseDescriptors)
            {
                var testCase = _settings.ParseSymbolInformation 
                    ? CreateTestCase(descriptor, testCaseLocations) 
                    : CreateTestCase(descriptor);
                ISet<TestCase> testCasesInSuite;
                if (!suite2TestCases.TryGetValue(descriptor.Suite, out testCasesInSuite))
                    suite2TestCases.Add(descriptor.Suite, testCasesInSuite = new HashSet<TestCase>());
                testCasesInSuite.Add(testCase);
                testCases.Add(testCase);
            }

            foreach (var suiteTestCasesPair in suite2TestCases)
            {
                foreach (var testCase in suiteTestCasesPair.Value)
                {
                    testCase.Properties.Add(new TestCaseMetaDataProperty(suiteTestCasesPair.Value.Count, testCases.Count));
                }
            }

            return testCases;
        }

        private IList<TestCase> NewCreateTestcases(Action<TestCase> reportTestCase, List<string> standardOutput)
        {
            var testCases = new List<TestCase>();

            var resolver = new NewTestCaseResolver(
                _executable,
                _settings.GetPathExtension(_executable),
                _diaResolverFactory,
                _settings.ParseSymbolInformation,
                _logger);

            var suite2TestCases = new Dictionary<string, ISet<TestCase>>();
            var parser = new StreamingListTestsParser(_settings.TestNameSeparator);
            parser.TestCaseDescriptorCreated += (sender, args) =>
            {
                TestCase testCase;
                if (_settings.ParseSymbolInformation)
                {
                    TestCaseLocation testCaseLocation =
                        resolver.FindTestCaseLocation(
                            _signatureCreator.GetTestMethodSignatures(args.TestCaseDescriptor).ToList());
                    testCase = CreateTestCase(args.TestCaseDescriptor, testCaseLocation);
                }
                else
                {
                    testCase = CreateTestCase(args.TestCaseDescriptor);
                }
                testCases.Add(testCase);

                ISet<TestCase> testCasesOfSuite;
                if (!suite2TestCases.TryGetValue(args.TestCaseDescriptor.Suite, out testCasesOfSuite))
                    suite2TestCases.Add(args.TestCaseDescriptor.Suite, testCasesOfSuite = new HashSet<TestCase>());
                testCasesOfSuite.Add(testCase);
            };

            Action<string> lineAction = s =>
            {
                standardOutput.Add(s);
                parser.ReportLine(s);
            };

            try
            {
                string workingDir = new FileInfo(_executable).DirectoryName;
                int processExitCode = ProcessExecutor.ExecutionFailed;
                ProcessExecutor executor = null;
                var listAndParseTestsTask = new Task(() =>
                {
                    executor = new ProcessExecutor(null, _logger);
                    processExitCode = executor.ExecuteCommandBlocking(
                        _executable,
                        GoogleTestConstants.ListTestsOption,
                        workingDir,
                        null,
                        _settings.GetPathExtension(_executable),
                        lineAction);
                }, TaskCreationOptions.AttachedToParent);
                listAndParseTestsTask.Start();

                if (!listAndParseTestsTask.Wait(TimeSpan.FromSeconds(_settings.TestDiscoveryTimeoutInSeconds)))
                {
                    executor?.Cancel();

                    string dir = Path.GetDirectoryName(_executable);
                    string file = Path.GetFileName(_executable);
                    string command = $@"cd ""{dir}""{Environment.NewLine}{file} {GoogleTestConstants.ListTestsOption}";

                    _logger.LogError(String.Format(Resources.TestDiscoveryCancelled, _settings.TestDiscoveryTimeoutInSeconds, _executable));
                    _logger.DebugError(String.Format(Resources.TestCommandCanBeRun, Environment.NewLine, command));
                    
                    return new List<TestCase>();
                }

                foreach (var suiteTestCasesPair in suite2TestCases)
                {
                    foreach (var testCase in suiteTestCasesPair.Value)
                    {
                        testCase.Properties.Add(new TestCaseMetaDataProperty(suiteTestCasesPair.Value.Count, testCases.Count));
                        reportTestCase?.Invoke(testCase);
                    }
                }

                if (!CheckProcessExitCode(processExitCode, standardOutput))
                    return new List<TestCase>();
            }
            catch (Exception e)
            {
                SequentialTestRunner.LogExecutionError(_logger, _executable, Path.GetFullPath(""),
                    GoogleTestConstants.ListTestsOption, e);
                return new List<TestCase>();
            }
            return testCases;
        }

        private bool CheckProcessExitCode(int processExitCode, ICollection<string> standardOutput)
        {
            if (processExitCode != 0)
            {
                string messsage = String.Format(Resources.CouldNotListTestCases, _executable, processExitCode);
                messsage += Environment.NewLine + String.Format(Resources.CommandExecuted, _executable, GoogleTestConstants.ListTestsOption, Path.GetDirectoryName(_executable));
                if (standardOutput.Count(s => !string.IsNullOrEmpty(s)) > 0)
                    messsage += Environment.NewLine + Resources.OutputOfCommand + Environment.NewLine + string.Join(Environment.NewLine, standardOutput);
                else
                    messsage += Environment.NewLine + Resources.NoOutput;

                _logger.LogError(messsage);
                return false;
            }
            return true;
        }

        private Dictionary<string, TestCaseLocation> GetTestCaseLocations(IList<TestCaseDescriptor> testCaseDescriptors, string pathExtension)
        {
            var testMethodSignatures = new HashSet<string>();
            foreach (var descriptor in testCaseDescriptors)
            {
                foreach (var signature in _signatureCreator.GetTestMethodSignatures(descriptor))
                {
                    testMethodSignatures.Add(signature);
                }
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

        private TestCase CreateTestCase(TestCaseDescriptor descriptor, Dictionary<string, TestCaseLocation> testCaseLocations)
        {
            var signature = _signatureCreator.GetTestMethodSignatures(descriptor)
                .Select(StripTestSymbolNamespace)
                .FirstOrDefault(s => testCaseLocations.ContainsKey(s));
            TestCaseLocation location = null;
            if (signature != null)
                testCaseLocations.TryGetValue(signature, out location);

            return CreateTestCase(descriptor, location);
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor, TestCaseLocation location)
        {
            if (location != null)
            {
                var testCase = new TestCase(
                    descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, location.Sourcefile, (int)location.Line);
                testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, location.Traits));
                return testCase;
            }

            _logger.LogWarning(String.Format(Resources.LocationNotFoundError, descriptor.FullyQualifiedName));
            return new TestCase(
                descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, "", 0);
        }

        internal static string StripTestSymbolNamespace(string symbol)
        {
            var suffixLength = GoogleTestConstants.TestBodySignature.Length;
            var namespaceEnd = symbol.LastIndexOf("::", symbol.Length - suffixLength - 1, StringComparison.Ordinal);
            var nameStart = namespaceEnd >= 0 ? namespaceEnd + 2 : 0;
            return symbol.Substring(nameStart);
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

            var beforeTraits = _settings.TraitsRegexesBefore
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
