// This file has been modified by Microsoft on 9/2017.

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

        public const string RootDir = @"..\..\..\..\..\";
        public const string SampleTestsSolutionDir = RootDir + @"SampleTests\";
        public const string GoogleTestAdapterBuildDir = RootDir + @"out\binaries\GoogleTestAdapter\" + BuildConfig + @"\";
        public const string SampleTestsBuildDir = RootDir + @"out\binaries\SampleTests\";
        public const string TestdataDir = GoogleTestAdapterBuildDir + @"Tests.Common\Resources\TestData\";

        // helpers
        public const string TenSecondsWaiter = GoogleTestAdapterBuildDir + @"TenSecondsWaiter\TenSecondsWaiter.exe";
        public const string AlwaysCrashingExe = GoogleTestAdapterBuildDir + @"CrashingExe\CrashingExe.exe";
        public const string AlwaysFailingExe = GoogleTestAdapterBuildDir + @"FailingExe\FailingExe.exe";
        public const string FakeGtestDllExe = GoogleTestAdapterBuildDir + @"FakeGtestDllApplication\FakeGtestDllApplication.exe";
        public const string FakeGtestDllExeX64 = GoogleTestAdapterBuildDir + @"FakeGtestDllApplication-x64\FakeGtestDllApplication-x64.exe";
        public const string SemaphoreExe = GoogleTestAdapterBuildDir + @"SemaphoreExe\SemaphoreExe.exe";
        public const string TestDiscoveryParamExe = GoogleTestAdapterBuildDir + @"TestDiscoveryParam\TestDiscoveryParam.exe";
        public const string UnicodeNameExe = GoogleTestAdapterBuildDir + @"UnicodeNameExe\㐀㕵ExtAxCP936丂狛狜.exe";

        public const string Tests_DebugX86 = SampleTestsBuildDir + @"Debug\Tests_gta.exe";
        public const string Tests_ReleaseX86 = SampleTestsBuildDir + @"Release\Tests_gta.exe";
        public const string Tests_DebugX86_Gtest170 = SampleTestsBuildDir + @"Debug\Tests_1.7.0_gta.exe";
        public const string Tests_DebugX64 = SampleTestsBuildDir + @"Debug-x64\Tests_gta.exe";
        public const string Tests_ReleaseX64 = SampleTestsBuildDir + @"Release-x64\Tests_gta.exe";
        public const string Tests_ReleaseX64_Output = TestdataDir + @"Tests_gta_exe_output.txt";
        public const int NrOfTests = 94;
        public const int NrOfPassingTests = 40;
        public const int NrOfFailingTests = 54;

        public static readonly string LoadTests_ReleaseX86 = Path.Combine(SampleTestsBuildDir, @"Release\LoadTests_gta.exe");

        public static readonly string LongRunningTests_ReleaseX86 = Path.Combine(SampleTestsBuildDir, @"Release\LongRunningTests_gta.exe");

        public const string CrashingTests_DebugX86 = SampleTestsBuildDir + @"Debug\CrashingTests_gta.exe";
        public const string CrashingTests_ReleaseX86 = SampleTestsBuildDir + @"Release\CrashingTests_gta.exe";
        public const string CrashingTests_DebugX64 = SampleTestsBuildDir + @"Debug-x64\CrashingTests_gta.exe";
        public const string CrashingTests_ReleaseX64 = SampleTestsBuildDir + @"Release-x64\CrashingTests_gta.exe";

        public const string DllTests_ReleaseX86 = SampleTestsBuildDir + @"Release\DllTests_gta.exe";
        public const string DllTestsDll_ReleaseX86 = SampleTestsBuildDir + @"Release\DllProject.dll";
        public const string DllTests_ReleaseX64 = SampleTestsBuildDir + @"Release-x64\DllTests_gta.exe";
        public const string DllTestsDll_ReleaseX64 = SampleTestsBuildDir + @"Release-x64\DllProject.dll";
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

        private static string GetPathIfExists(string path)
        {
            if (Directory.Exists(path))
                return path;
            else
                return null;
        }

        private static readonly Lazy<string> VS2017Location = new Lazy<string>(() =>
        {
            return Environment.GetEnvironmentVariable("GTA_TESTS_VS2017") ??
                GetPathIfExists($@"C:\Program Files (x86)\Microsoft Visual Studio\{VsVersion.VS2017.Year()}\Enterprise") ??
                GetPathIfExists($@"C:\Program Files (x86)\Microsoft Visual Studio\{VsVersion.VS2017.Year()}\Community");
        });

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
                    return Path.Combine(VS2017Location.Value, @"Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe");
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