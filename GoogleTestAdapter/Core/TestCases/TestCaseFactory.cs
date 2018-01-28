// This file has been modified by Microsoft on 7/2017.

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
                int processExitCode = 0;
                ProcessLauncher launcher = null;
                var listTestsTask = new Task(() =>
                {
                    launcher = new ProcessLauncher(_logger, _settings.GetPathExtension(_executable), null);
                    standardOutput = launcher.GetOutputOfCommand("", _executable, GoogleTestConstants.ListTestsOption,
                        false, false, out processExitCode);
                }, TaskCreationOptions.AttachedToParent);
                listTestsTask.Start();

                if (!listTestsTask.Wait(TimeSpan.FromSeconds(_settings.TestDiscoveryTimeoutInSeconds)))
                {
                    launcher?.Cancel();

                    string dir = Path.GetDirectoryName(_executable);
                    string file = Path.GetFileName(_executable);
                    string cdToWorkingDir = $@"cd ""{dir}""";
                    string listTestsCommand = $"{file} {GoogleTestConstants.ListTestsOption}";

                    _logger.LogError($"Test discovery was cancelled after {_settings.TestDiscoveryTimeoutInSeconds}s for executable {_executable}");
                    _logger.DebugError($"Test whether the following commands can be executed sucessfully on the command line (make sure all required binaries are on the PATH):{Environment.NewLine}{cdToWorkingDir}{Environment.NewLine}{listTestsCommand}");

                    return new List<TestCase>();
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

            IList<TestCaseDescriptor> testCaseDescriptors = new ListTestsParser(_settings.TestNameSeparator).ParseListTestsOutput(standardOutput);
            var resolver = new TestCaseResolver(_executable, _settings.GetPathExtension(_executable), _diaResolverFactory, _settings.ParseSymbolInformation, _logger);

            IList<TestCase> testCases = new List<TestCase>();
            IDictionary<string, ISet<TestCase>> suite2TestCases = new Dictionary<string, ISet<TestCase>>();
            foreach (var descriptor in testCaseDescriptors)
            {
                var testCase = _settings.ParseSymbolInformation 
                    ? CreateTestCase(descriptor, resolver) 
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

            if (reportTestCase != null)
            {
                foreach (var testCase in testCases)
                {
                    reportTestCase(testCase);
                }
            }

            return testCases;
        }

        private IList<TestCase> NewCreateTestcases(Action<TestCase> reportTestCase, List<string> standardOutput)
        {
            var testCases = new List<TestCase>();

            var resolver = new TestCaseResolver(
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
                int processExitCode = ProcessExecutor.ExecutionFailed;
                ProcessExecutor executor = null;
                var listAndParseTestsTask = new Task(() =>
                {
                    executor = new ProcessExecutor(null, _logger);
                    processExitCode = executor.ExecuteCommandBlocking(
                        _executable,
                        GoogleTestConstants.ListTestsOption,
                        "",
                        _settings.GetPathExtension(_executable),
                        lineAction);
                }, TaskCreationOptions.AttachedToParent);
                listAndParseTestsTask.Start();

                if (!listAndParseTestsTask.Wait(TimeSpan.FromSeconds(_settings.TestDiscoveryTimeoutInSeconds)))
                {
                    executor?.Cancel();

                    string dir = Path.GetDirectoryName(_executable);
                    string file = Path.GetFileName(_executable);
                    string cdToWorkingDir = $@"cd ""{dir}""";
                    string listTestsCommand = $"{file} {GoogleTestConstants.ListTestsOption}";

                    _logger.LogError($"Test discovery was cancelled after {_settings.TestDiscoveryTimeoutInSeconds}s for executable {_executable}");
                    _logger.DebugError($"Test whether the following commands can be executed sucessfully on the command line (make sure all required binaries are on the PATH):{Environment.NewLine}{cdToWorkingDir}{Environment.NewLine}{listTestsCommand}");

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
                string messsage =
                    $"Could not list test cases of executable '{_executable}': executing process failed with return code {processExitCode}";
                messsage +=
                    $"\nCommand executed: '{_executable} {GoogleTestConstants.ListTestsOption}', working directory: '{Path.GetDirectoryName(_executable)}'";
                if (standardOutput.Count(s => !string.IsNullOrEmpty(s)) > 0)
                    messsage += $"\nOutput of command:\n{string.Join("\n", standardOutput)}";
                else
                    messsage += "\nCommand produced no output";

                _logger.LogError(messsage);
                return false;
            }
            return true;
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor)
        {
            var testCase = new TestCase(
                descriptor.FullyQualifiedName, _executable, descriptor.DisplayName, "", 0);
            testCase.Traits.AddRange(GetFinalTraits(descriptor.DisplayName, new List<Trait>()));
            return testCase;
        }

        private TestCase CreateTestCase(TestCaseDescriptor descriptor, TestCaseResolver resolver)
        {
            TestCaseLocation location =
                resolver.FindTestCaseLocation(_signatureCreator.GetTestMethodSignatures(descriptor).ToList());
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
