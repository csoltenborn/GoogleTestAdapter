using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using GoogleTestAdapter.TestAdapter;
using GoogleTestAdapter.Tests.Common.ResultChecker;

namespace GoogleTestAdapter.Tests.Common
{
    public abstract class AbstractConsoleTests
    {
        protected readonly string SolutionFile;
        protected readonly string TestAdapterDir;

        private string _envVarStorage;

        protected AbstractConsoleTests()
        {
            AbstractConsoleIntegrationTests.GetDirectories(out TestAdapterDir, out SolutionFile);
        }

        protected void Setup(string settingsFile)
        {
            if (_envVarStorage != null)
            {
                throw new InvalidOperationException();
            }

            _envVarStorage = Environment.GetEnvironmentVariable(CommonFunctions.GtaSettingsEnvVariable);
            Environment.SetEnvironmentVariable(CommonFunctions.GtaSettingsEnvVariable, settingsFile);
        }

        protected void Teardown()
        {
            Environment.SetEnvironmentVariable(CommonFunctions.GtaSettingsEnvVariable, _envVarStorage);
            _envVarStorage = null;
        }

        protected void RunTestsAndCheckOutput(string typeName, string arguments, [CallerMemberName] string testCaseName = null)
        {
            TrxResultChecker resultChecker = new TrxResultChecker(SolutionFile);
            resultChecker.RunTestsAndCheckOutput(typeName, arguments, testCaseName);
        }

        protected void ListTestsOf(string testExecutable, [CallerMemberName] string testCaseName = null)
        {
            testExecutable = Path.GetFullPath(testExecutable);
            string arguments = GetAdapterIntegration() + @" /ListTests:""" + testExecutable + @"""";
            if (!testExecutable.Contains("_x86") && !testExecutable.Contains("_x64"))
                arguments += " /Settings:\"\"" + TestResources.UserTestSettingsForListingTests + "\"\"";
            string resultString = AbstractConsoleIntegrationTests.RunExecutableAndGetOutput(SolutionFile, arguments);
            string[] resultLines = resultString.Split('\n');
            resultLines = resultLines.Where(l => l.StartsWith("    ")).Select(l => l.Trim()).ToArray();
            for (int i = 0; i < resultLines.Length; i++)
            {
                resultLines[i] = Regex.Replace(resultLines[i], "Information:.*", "");
            }
            resultString = string.Join("\n", resultLines);
            // ReSharper disable once AssignNullToNotNullAttribute
            string projectDir = Path.Combine(Path.GetDirectoryName(SolutionFile), @"..\GoogleTestAdapter\VsPackage.Tests.Generated");
            new ResultChecker.ResultChecker(Path.Combine(projectDir, "GoldenFiles"), Path.Combine(projectDir, "TestErrors"), ".txt")
                // ReSharper disable once ExplicitCallerInfoArgument
                .CheckResults(resultString, GetType().Name, testCaseName);
        }

        protected string GetLogger()
        {
            return AbstractConsoleIntegrationTests.GetLogger();
        }

        protected string GetAdapterIntegration()
        {
            return GetLogger() + @"/TestAdapterPath:" + TestAdapterDir;
        }

    }

}
