using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
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
            foreach (Version version in History.Versions)
            {
                Stream stream = null;
                Assembly.GetAssembly(typeof(History))
                    .Invoking(a => stream = a.GetManifestResourceStream(History.GetResourceName(version)))
                    .ShouldNotThrow();
                stream.Should().NotBeNull($"release notes for version {version} should be located in folder VsPackage\\Resources\\ReleaseNotes and have build action 'Embedded Resource'");

                using (var reader = new StreamReader(stream))
                {
                    stream = null;
                    reader.ReadToEnd().Trim().Should().NotBeNullOrEmpty($"release notes for version {version} should have some content");
                }
                stream?.Dispose();
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