using GoogleTestAdapter.TestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.Helpers
{
    public class RunSettingsServiceUnderTest : RunSettingsService
    {
        private readonly string _solutionRunSettingsFile;

        internal VisualStudioConfiguration VisualStudioConfiguration { get; set; }

        internal RunSettingsServiceUnderTest(IGlobalRunSettings globalRunSettings, string solutionRunSettingsFile) 
            : base(globalRunSettings)
        {
            _solutionRunSettingsFile = solutionRunSettingsFile;
        }

        protected override VisualStudioConfiguration GetVisualStudioConfiguration(Microsoft.VisualStudio.TestWindow.Extensibility.ILogger logger)
        {
            return VisualStudioConfiguration;
        }

        protected override string GetSolutionSettingsXmlFile()
        {
            return _solutionRunSettingsFile;
        }
    }
}