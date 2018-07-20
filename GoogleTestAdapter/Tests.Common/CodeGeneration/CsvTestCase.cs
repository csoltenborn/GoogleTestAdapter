namespace GoogleTestAdapter.Tests.Common.CodeGeneration
{
    public class CsvTestCase
    {
        public string TestExecutablePlaceholder;
        public string TestExecutable;
        public string SettingsFilePlaceholder;
        public string SettingsFile;
		public string TestCaseFilter;
		public bool EnableCodeCoverage;
		public bool InIsolation;
		public bool Ignore;

		public string MethodName
		{
			get
			{
			    string settings = SettingsFilePlaceholder;
				string codeCoverage = EnableCodeCoverage ? "_Coverage" : "";
				string isolation = InIsolation ? "_Isolation" : "";
				string filter = TestCaseFilter == "none" ? "" : "_" + TestCaseFilter;

				string result = TestExecutablePlaceholder + settings + codeCoverage + isolation + filter;

				result = result.Replace(".", "_");
				result = result.Replace("=", "_EQ_");
				result = result.Replace("|", "_OR_");
				result = result.Replace("&", "_AND_");

				if (result.Contains("*"))
					result = "NEG_" + result.Replace("*", "");

				return result;
			}
		}

		public string ExecutableAndArguments
		{
			get
			{
				string arguments = " \"\"" + TestExecutable + "\"\"";

//				if (!string.IsNullOrWhiteSpace(ActualSettingsFile))
//					arguments += " /Settings:\"\"" + ActualSettingsFile + "\"\"";

				if (!"none".Equals(TestCaseFilter))
					arguments += " /TestCaseFilter:\"\"" + TestCaseFilter + "\"\"";

				if (EnableCodeCoverage)
					arguments += " /EnableCodeCoverage";

				if (InIsolation)
					arguments += " /InIsolation";

				return arguments;
			}
		}
    }
}
