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
            if (_settings.UseNewTestExecutionFramework)
            {
                return NewCreateTestcases(reportTestCase);
            }

           string workingDir = _settings.GetWorkingDirForDiscovery(_executable);
           string finalParams = GetDiscoveryParams();
            List<string> standardOutput = new List<string>();
            try
            {
                int processExitCode = 0;
                ProcessLauncher launcher = null;
                var listTestsTask = new Task(() =>
                {
                    launcher = new ProcessLauncher(_logger, _settings.GetPathExtension(_executable));
                    processExitCode = launcher.GetOutputOfCommand(workingDir, _executable, finalParams,
                        false, false, standardOutput);
                }, TaskCreationOptions.AttachedToParent);
                listTestsTask.Start();

                if (!listTestsTask.Wait(TimeSpan.FromSeconds(_settings.TestDiscoveryTimeoutInSeconds)))
                {
                    launcher?.Cancel();
                    LogTimeoutError(workingDir, finalParams, standardOutput);
                    return new List<TestCase>();
                }

                if (!CheckProcessExitCode(processExitCode, standardOutput, workingDir, finalParams))
                    return new List<TestCase>();
            }
            catch (Exception e)
            {
                SequentialTestRunner.LogExecutionError(_logger, _executable, workingDir, finalParams, e);
                return new List<TestCase>();
            }

            var testCaseDescriptors = new ListTestsParser(_settings.TestNameSeparator).ParseListTestsOutput(standardOutput);
            var resolver = new TestCaseResolver(_executable, _settings.GetPathExtension(_executable), _settings.GetAdditionalPdbs(_executable), _diaResolverFactory, _settings.ParseSymbolInformation, _logger);

            var testCases = new List<TestCase>();
            var suite2TestCases = new Dictionary<string, ISet<TestCase>>();
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

        private IList<TestCase> NewCreateTestcases(Action<TestCase> reportTestCase)
        {
            var standardOutput = new List<string>();
            var testCases = new List<TestCase>();

            var resolver = new TestCaseResolver(
                _executable,
                _settings.GetPathExtension(_executable),
                _settings.GetAdditionalPdbs(_executable),
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

           string workingDir = _settings.GetWorkingDirForDiscovery(_executable);
           var finalParams = GetDiscoveryParams();
            try
            {
                int processExitCode = ProcessExecutor.ExecutionFailed;
                ProcessExecutor executor = null;
                var listAndParseTestsTask = new Task(() =>
                {
                    executor = new ProcessExecutor(null, _logger);
                    processExitCode = executor.ExecuteCommandBlocking(
                        _executable,
                        finalParams,
                        workingDir,
                        _settings.GetPathExtension(_executable),
                        lineAction);
                }, TaskCreationOptions.AttachedToParent);
                listAndParseTestsTask.Start();

                if (!listAndParseTestsTask.Wait(TimeSpan.FromSeconds(_settings.TestDiscoveryTimeoutInSeconds)))
                {
                    executor?.Cancel();
                    LogTimeoutError(workingDir, finalParams, standardOutput);
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

                if (!CheckProcessExitCode(processExitCode, standardOutput, workingDir, finalParams))
                    return new List<TestCase>();
            }
            catch (Exception e)
            {
                SequentialTestRunner.LogExecutionError(_logger, _executable, workingDir, finalParams, e);
                return new List<TestCase>();
            }
            return testCases;
        }

        private string GetDiscoveryParams()
        {
            string finalParams = GoogleTestConstants.ListTestsOption;
            string userParams = _settings.GetUserParametersForDiscovery(_executable);
            if (!string.IsNullOrWhiteSpace(userParams))
            {
                finalParams += $" {userParams}";
            }

            return finalParams;
        }

        private void LogTimeoutError(string workingDir, string finalParams, IList<string> outputSoFar)
        {
            string file = Path.GetFileName(_executable);
            string cdToWorkingDir = $@"cd ""{workingDir}""";
            string listTestsCommand = $"{file} {finalParams}";

            string message =
                $"Test discovery was cancelled after {_settings.TestDiscoveryTimeoutInSeconds}s for executable '{_executable}'";
            string output = outputSoFar.Any()
                ? $"Output of {_executable} so far:{Environment.NewLine}" + 
                  $"{string.Join(Environment.NewLine, outputSoFar)}"
                : $"Executable {_executable}produced no output.";
            string hint =
                $"Hint: test whether the following commands can be executed sucessfully on the command line (make sure all required binaries are on the PATH):{Environment.NewLine}" + 
                $"{cdToWorkingDir}{Environment.NewLine}" + 
                listTestsCommand;

            _logger.LogError(message);
            _logger.DebugError(output);
            _logger.DebugError(hint);
        }

        private bool CheckProcessExitCode(int processExitCode, ICollection<string> standardOutput, string workingDir, string parameters)
        {
            if (processExitCode != 0)
            {
                string messsage =
                    $"Could not list test cases of executable '{_executable}': executing process failed with return code {processExitCode}";
                messsage +=
                    $"\nCommand executed: '{_executable} {parameters}', working directory: '{workingDir}'";
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
