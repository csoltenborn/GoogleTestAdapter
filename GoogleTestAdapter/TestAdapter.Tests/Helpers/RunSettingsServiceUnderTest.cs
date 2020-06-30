using GoogleTestAdapter.TestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    public class RunSettingsServiceUnderTest : RunSettingsService
    {
        private readonly string _solutionRunSettingsFile;

        internal RunSettingsServiceUnderTest(IGlobalRunSettings2 globalRunSettings, string solutionRunSettingsFile) 
            : base(globalRunSettings)
        {
            _solutionRunSettingsFile = solutionRunSettingsFile;
        }

        protected override string GetSolutionSettingsXmlFile()
        {
            return _solutionRunSettingsFile;
        }
    }
}