namespace GoogleTestAdapter.Tests.Common
{
    public static class TestMetadata
    {
        public const bool OverwriteTestResults = false;
        public const bool GenerateVsixTests = true;

        public static class TestCategories
        {
            public const string Unit = "Unit";
            public const string Integration = "Integration";
            public const string EndToEnd = "End to end";
            public const string Ui = "UI";
            public const string Load = "Load";
        }

        public enum Versions { VS2012 = 11, VS2013 = 12, VS2015 = 14 }
    }
}