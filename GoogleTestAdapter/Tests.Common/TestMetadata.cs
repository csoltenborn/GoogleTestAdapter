using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.Tests.Common
{
    public static class TestMetadata
    {
        public const bool OverwriteTestResults = false;
        public const bool GenerateVsixTests = false;

        public const VsVersion VersionUnderTest = VsVersion.VS2017;

        public static class TestCategories
        {
            public const string Unit = "Unit";
            public const string Integration = "Integration";
            public const string EndToEnd = "End to end";
            public const string Ui = "UI";
            public const string Load = "Load";
        }
    }
}