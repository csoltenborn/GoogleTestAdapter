using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    [TestClass]
    public class ReleaseNotesCreatorTests
    {
        private const string EmptyHtmlPage = "<!DOCTYPE html><html><body></body></html>";


        [TestMethod]
        public void WriteHtmlToTempFile_MinAndMaxVersions_AllVersionsExceptMin()
        {
            var creator = new ReleaseNotesCreator(History.Versions.First(), History.Versions.Last());

            string content = creator.CreateHtml();

            // only in introduction
            Version version = History.Versions.First();
            Assert.AreEqual(1, Regex.Matches(content, $"{version.Major}.{version.Minor}.{version.Revision}").Count);

            // in introduction and version list
            version = History.Versions.Last();
            Assert.AreEqual(2, Regex.Matches(content, $"{version.Major}.{version.Minor}.{version.Revision}").Count);

            // only in version list
            for (int i = 1; i < History.Versions.Length - 1; i++)
            {
                version = History.Versions[i];
                Assert.IsTrue(content.Contains($"{version.Major}.{version.Minor}.{version.Revision}"), $"Version {version} is missing in HTML file");
            }
        }

        [TestMethod]
        public void WriteHtmlToTempFile_MaxVersions_EmptyString()
        {
            var creator = new ReleaseNotesCreator(History.Versions.Last(), History.Versions.Last());

            string content = creator.CreateHtml();

            Assert.AreEqual(EmptyHtmlPage, content);
        }

    }

}