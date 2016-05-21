using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    [TestClass]
    public class ReleaseNotesCreatorTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void CreateHtml_MinAndMaxVersions_AllVersionsExceptMin()
        {
            var creator = new ReleaseNotesCreator(History.Versions.First(), History.Versions.Last());

            string html = creator.CreateHtml();

            // only in introduction
            Version version = History.Versions.First();
            MatchCollection firstVersionMatches = Regex.Matches(html,
                $"{version.Major}.{version.Minor}.{version.Revision}");
            firstVersionMatches.Count.Should().Be(1);

            // in introduction and version list
            version = History.Versions.Last();
            MatchCollection lastVersionMatches = Regex.Matches(html, 
                $"{version.Major}.{version.Minor}.{version.Revision}");
            lastVersionMatches.Count.Should().Be(2);

            firstVersionMatches[0].Index.Should().BeLessThan(lastVersionMatches[0].Index);

            // only in version list
            for (int i = 1; i < History.Versions.Length - 1; i++)
            {
                version = History.Versions[i];
                html.Should().Contain($"{version.Major}.{version.Minor}.{version.Revision}", $"Version {version} is missing in HTML file");
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateHtml_NullAndMaxVersions_AllVersions()
        {
            var creator = new ReleaseNotesCreator(null, History.Versions.Last());

            string html = creator.CreateHtml();

            // only in version list
            Version version = History.Versions.First();
            MatchCollection firstVersionMatches = Regex.Matches(html,
                $"{version.Major}.{version.Minor}.{version.Revision}");
            firstVersionMatches.Count.Should().Be(1);

            // in introduction and version list
            version = History.Versions.Last();
            MatchCollection lastVersionMatches = Regex.Matches(html, 
                $"{version.Major}.{version.Minor}.{version.Revision}");
            lastVersionMatches.Count.Should().Be(2);

            firstVersionMatches[0].Index.Should().BeGreaterThan(lastVersionMatches[0].Index);

            // only in version list
            for (int i = 1; i < History.Versions.Length - 1; i++)
            {
                version = History.Versions[i];
                html.Should().Contain($"{version.Major}.{version.Minor}.{version.Revision}", $"Version {version} is missing in HTML file");
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateHtml_MaxVersions_EmptyString()
        {
            var creator = new ReleaseNotesCreator(History.Versions.Last(), History.Versions.Last());

            string content = creator.CreateHtml();

            content.Should().BeEmpty();
        }

    }

}