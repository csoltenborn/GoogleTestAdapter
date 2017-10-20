using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.Assertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{

    [TestClass]
    public class HistoryTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void History_Versions_ShouldAllHaveReleaseNotes()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // ReSharper disable once AssignNullToNotNullAttribute
            string releaseNotesDir = Path.Combine(assemblyDir, "Resources", "ReleaseNotes");

            foreach (Version version in History.Versions)
            {
                string releaseNotesFile = Path.Combine(releaseNotesDir, History.GetReleaseNotesFile(version));
                releaseNotesFile.AsFileInfo().Should().Exist($"release notes for version {version} should be located in folder VsPackage\\Resources\\ReleaseNotes and have same properties as other release notes files");

                File.ReadAllText(releaseNotesFile).Should()
                    .NotBeNullOrEmpty($"release notes for version {version} should have some content");
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void History_Versions_ShouldIncrease()
        {
            History.Versions.Should().BeInAscendingOrder();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void History_Versions_DatesShouldIncrease()
        {
            History.Versions.Select(History.GetDate).Should().BeInAscendingOrder();
        }

    }

}