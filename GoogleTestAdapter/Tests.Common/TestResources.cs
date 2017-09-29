using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.Tests.Common
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class TestResources
    {
#if DEBUG
        private const string BuildConfig = "Debug";
#else
        private const string BuildConfig = "Release";
#endif

        public const string GtaSolutionDir = @"..\..\..\";
        public const string SampleTestsSolutionDir = GtaSolutionDir + @"..\SampleTests\";
        public const string TestdataDir = GtaSolutionDir + @"Tests.Common\bin\" + BuildConfig + @"\Resources\TestData\";

        // helpers
        public const string TenSecondsWaiter = GtaSolutionDir + @"TenSecondsWaiter\bin\" + BuildConfig + @"\TenSecondsWaiter.exe";
        public const string AlwaysCrashingExe = GtaSolutionDir + BuildConfig + @"\CrashingExe.exe";
        public const string AlwaysFailingExe = GtaSolutionDir + BuildConfig + @"\FailingExe.exe";
        public const string FakeGtestDllExe = GtaSolutionDir + BuildConfig + @"\FakeGtestDllApplication.exe";
        public const string FakeGtestDllExeX64 = GtaSolutionDir + @"x64\" + BuildConfig + @"\FakeGtestDllApplication-x64.exe";
        public const string SemaphoreExe = GtaSolutionDir + @"SemaphoreExe\SemaphoreExe.exe";

        public const string Tests_DebugX86 = SampleTestsSolutionDir + @"Debug\Tests_gta.exe";
        public const string Tests_ReleaseX86 = SampleTestsSolutionDir + @"Release\Tests_gta.exe";
        public const string Tests_DebugX86_Gtest170 = SampleTestsSolutionDir + @"Debug\Tests_1.7.0_gta.exe";
        public const string Tests_DebugX64 = SampleTestsSolutionDir + @"x64\Debug\Tests_gta.exe";
        public const string Tests_ReleaseX64 = SampleTestsSolutionDir + @"x64\Release\Tests_gta.exe";
        public const string Tests_ReleaseX64_Output = TestdataDir + @"Tests_gta_exe_output.txt";
        public const int NrOfTests = 98;
        public const int NrOfPassingTests = 44;
        public const int NrOfFailingTests = 54;
        public const int NrOfGtest170CompatibleTests = 94;

        public static readonly string LoadTests_ReleaseX86 = Path.Combine(SampleTestsSolutionDir, @"Release\LoadTests_gta.exe");

        public static readonly string LongRunningTests_ReleaseX86 = Path.Combine(SampleTestsSolutionDir, @"Release\LongRunningTests_gta.exe");

        public const string CrashingTests_DebugX86 = SampleTestsSolutionDir + @"Debug\CrashingTests_gta.exe";
        public const string CrashingTests_ReleaseX86 = SampleTestsSolutionDir + @"Release\CrashingTests_gta.exe";
        public const string CrashingTests_DebugX64 = SampleTestsSolutionDir + @"X64\Debug\CrashingTests_gta.exe";
        public const string CrashingTests_ReleaseX64 = SampleTestsSolutionDir + @"X64\Release\CrashingTests_gta.exe";

        public const string DllTests_ReleaseX86 = SampleTestsSolutionDir + @"Release\DllTests_gta.exe";
        public const string DllTestsDll_ReleaseX86 = SampleTestsSolutionDir + @"Release\DllProject.dll";
        public const string DllTests_ReleaseX64 = SampleTestsSolutionDir + @"X64\Release\DllTests_gta.exe";
        public const string DllTestsDll_ReleaseX64 = SampleTestsSolutionDir + @"X64\Release\DllProject.dll";
        public const int NrOfDllTests = 2;

        public const string SucceedingBatch = @"Tests\Returns0.bat";
        public const string FailingBatch = @"Tests\Returns1.bat";

        public const string XmlFile1 = TestdataDir + @"SampleResult1.xml";
        public const string XmlFile2 = TestdataDir + @"SampleResult2.xml";
        public const string XmlFileBroken = TestdataDir + @"SampleResult1_Broken.xml";
        // ReSharper disable once InconsistentNaming
        public const string XmlFileBroken_InvalidStatusAttibute = TestdataDir + @"SampleResult1 _Broken_InvalidStatusAttribute.xml";

        public const string SolutionTestSettings = TestdataDir + @"RunSettingsServiceTests\Solution.gta.runsettings";
        public const string UserTestSettings = TestdataDir + @"RunSettingsServiceTests\User.runsettings";
        public const string UserTestSettingsWithoutRunSettingsNode = TestdataDir + @"RunSettingsServiceTests\User_WithoutRunSettingsNode.runsettings";
        public const string UserTestSettingsForGeneratedTests_Project = TestdataDir + "Project.runsettings";
        public const string UserTestSettingsForGeneratedTests_Solution = TestdataDir + "Solution.runsettings";
        public const string UserTestSettingsForGeneratedTests_SolutionProject = TestdataDir + "SolutionProject.runsettings";
        public const string UserTestSettingsForListingTests = TestdataDir + "ListTests.runsettings";
        public const string ProviderDeliveredTestSettings = TestdataDir + @"RunSettingsServiceTests\Provider_delivered.runsettings";

        public static string GetGoldenFileName(string typeName, string testCaseName, string fileExtension)
        {
            return typeName + "__" + testCaseName + fileExtension;
        }

        public static string GetVsTestConsolePath(VsVersion version)
        {
            switch (version)
            {
                case VsVersion.VS2012_1:
                case VsVersion.VS2013:
                case VsVersion.VS2015:
                    return $@"C:\Program Files (x86)\Microsoft Visual Studio {version:d}.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
                case VsVersion.VS2017:
                    return $@"C:\Program Files (x86)\Microsoft Visual Studio\{version.Year()}\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
                default:
                    throw new InvalidOperationException();
            }
        }

        public static string NormalizePointerInfo(string text)
        {
            return Regex.Replace(text, "([0-9A-F]{8}){1,2} pointing to", "${MemoryLocation} pointing to");
        }

    }

}