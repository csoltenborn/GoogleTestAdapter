using System;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.Tests.Common
{
    public static class TestMetadata
    {
        public const bool OverwriteTestResults = false;

        public const VsVersion VersionUnderTest = 
#if VERSION_UNDER_TEST_VS2015
        VsVersion.VS2015;
#else
        VsVersion.VS2017;
#endif

        public static class TestCategories
        {
            public const string Unit = "Unit";
            public const string Integration = "Integration";
            public const string EndToEnd = "End to end";
            public const string Ui = "UI";
            public const string Load = "Load";
        }

        public static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(25);
        public static readonly int ToleranceInMs = (int)Math.Ceiling(Tolerance.TotalMilliseconds);
    }
}