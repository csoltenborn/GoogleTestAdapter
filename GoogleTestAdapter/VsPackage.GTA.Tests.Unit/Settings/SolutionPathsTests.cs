using System;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.Settings
{
    [TestClass]
    public class SolutionPathsTests
    {
        private string _folder;

        [TestInitialize]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GTA_SolutionPathsTests_" + Guid.NewGuid().ToString("N"), "MyCMakeProject");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void TearDown()
        {
            Directory.Delete(Path.GetDirectoryName(_folder), true);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionDir_Solution_DirectoryOfSolutionIsReturned()
        {
            SolutionPaths.GetSolutionDir(@"C:\src\MySolution.sln").Should().Be(@"C:\src");
            SolutionPaths.GetSolutionDir(@"C:\src\MySolution.slnx").Should().Be(@"C:\src");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionDir_Folder_FolderIsReturned()
        {
            SolutionPaths.GetSolutionDir(_folder).Should().Be(_folder);
            SolutionPaths.GetSolutionDir(_folder + @"\").Should().Be(_folder);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionDir_NoSolution_NullIsReturned()
        {
            SolutionPaths.GetSolutionDir(null).Should().BeNull();
            SolutionPaths.GetSolutionDir("").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionSettingsFile_Solution_FileNextToSolutionIsReturned()
        {
            SolutionPaths.GetSolutionSettingsFile(@"C:\src\MySolution.sln").Should().Be(@"C:\src\MySolution.gta.runsettings");
            SolutionPaths.GetSolutionSettingsFile(@"C:\src\MySolution.slnx").Should().Be(@"C:\src\MySolution.gta.runsettings");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionSettingsFile_NoSolution_NullIsReturned()
        {
            SolutionPaths.GetSolutionSettingsFile(null).Should().BeNull();
            SolutionPaths.GetSolutionSettingsFile(" ").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionSettingsFile_FolderWithoutSettings_FileNamedAfterFolderIsReturned()
        {
            SolutionPaths.GetSolutionSettingsFile(_folder)
                .Should().Be(Path.Combine(_folder, "MyCMakeProject.gta.runsettings"));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionSettingsFile_FolderWithSingleSettingsFile_ThatFileIsReturned()
        {
            string settingsFile = Path.Combine(_folder, "Tests.gta.runsettings");
            File.WriteAllText(settingsFile, "");

            SolutionPaths.GetSolutionSettingsFile(_folder + @"\").Should().Be(settingsFile);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSolutionSettingsFile_FolderWithSeveralSettingsFiles_FileNamedAfterFolderIsReturned()
        {
            string settingsFile = Path.Combine(_folder, "MyCMakeProject.gta.runsettings");
            File.WriteAllText(settingsFile, "");
            File.WriteAllText(Path.Combine(_folder, "Other.gta.runsettings"), "");

            SolutionPaths.GetSolutionSettingsFile(_folder).Should().Be(settingsFile);
        }
    }
}
