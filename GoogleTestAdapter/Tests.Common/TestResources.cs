using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.Tests.Common
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class TestResources
    {
#if DEBUG
        private const string BuildConfig = "Debug";
#else
        private const string BuildConfig = "Release";
#endif

        public const string GtaSolutionDir = @"..\..\..\";
        public const string SampleTestsSolutionDir = @"..\..\..\..\SampleTests\";
        public const string TestdataDir = @"..\..\..\Tests.Common\bin\" + BuildConfig + @"\Resources\TestData\";

        public const string Results0Batch = @"Tests\Returns0.bat";
        public const string Results1Batch = @"Tests\Returns1.bat";

        public const string TenSecondsWaiter = GtaSolutionDir + @"TenSecondsWaiter\bin\" + BuildConfig + @"\TenSecondsWaiter.exe";

        public const int NrOfSampleTests = 88;
        public const string SampleTests = SampleTestsSolutionDir + @"Debug\Tests_gta.exe";
        public const string SampleTestsRelease = SampleTestsSolutionDir + @"Release\Tests_gta.exe";
        public const string SampleTests170 = SampleTestsSolutionDir + @"Debug\Tests_1.7.0_gta.exe";

        public static readonly string LoadTests = Path.Combine(SampleTestsSolutionDir, @"Release\LoadTests_gta.exe");
        public static readonly string LongRunningTests = Path.Combine(SampleTestsSolutionDir, @"Release\LongRunningTests_gta.exe");

        public const string X86HardCrashingSampleTests = SampleTestsSolutionDir + @"Debug\CrashingTests_gta.exe";
        public const string X86HardCrashingSampleTestsRelease = SampleTestsSolutionDir + @"Release\CrashingTests_gta.exe";
        public const string X64HardCrashingSampleTests = SampleTestsSolutionDir + @"X64\Debug\CrashingTests_gta.exe";
        public const string X64HardCrashingSampleTestsRelease = SampleTestsSolutionDir + @"X64\Release\CrashingTests_gta.exe";

        public const string X86Dir = TestdataDir + @"_x86\";
        public const string X86StaticallyLinkedTests = X86Dir + @"StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe"; // TODO
        public const string X86ExternallyLinkedTests = X86Dir + @"ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe"; // TODO
        public const string X86ExternallyLinkedTestsDll = X86Dir + @"ExternallyLinkedGoogleTests\ExternalGoogleTestLibrary.dll"; // TODO
        public const string PathExtensionTestsExe = SampleTestsSolutionDir + @"Release\DllTests_gta.exe";
        public const string PathExtensionTestsDll = SampleTestsSolutionDir + @"Release\DllProject.dll";
        public const string AlwaysCrashingExe = GtaSolutionDir + BuildConfig + @"\CrashingExe.exe";
        public const string AlwaysFailingExe = GtaSolutionDir + BuildConfig + @"\FailingExe.exe";
        public const int NrOfPathExtensionTests = 2;

        public const string X64Dir = TestdataDir + @"_x64\";
        public const string X64StaticallyLinkedTests = X64Dir + @"StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe"; // TODO
        public const string X64ExternallyLinkedTests = X64Dir + @"ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe"; // TODO

        public const string XmlFile1 = TestdataDir + @"SampleResult1.xml";
        public const string XmlFile2 = TestdataDir + @"SampleResult2.xml";
        public const string XmlFileBroken = TestdataDir + @"SampleResult1_Broken.xml";
        // ReSharper disable once InconsistentNaming
        public const string XmlFileBroken_InvalidStatusAttibute = TestdataDir + @"SampleResult1 _Broken_InvalidStatusAttribute.xml";

        public const string SolutionTestSettings = TestdataDir + @"RunSettingsServiceTests\Solution.gta.runsettings";
        public const string UserTestSettings = TestdataDir + @"RunSettingsServiceTests\User.runsettings";
        public const string UserTestSettingsWithoutRunSettingsNode = TestdataDir + @"RunSettingsServiceTests\User_WithoutRunSettingsNode.runsettings";
        public const string UserTestSettingsForGeneratedTests = TestdataDir + "User.runsettings";
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