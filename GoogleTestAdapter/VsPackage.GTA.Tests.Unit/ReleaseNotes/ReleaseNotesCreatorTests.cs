using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    [TestClass]
    public class ReleaseNotesCreatorTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void CreateHtml_Theme_PageHasColorsOfTheme()
        {
            var theme = new ReleaseNotesTheme
            {
                Background = System.Drawing.Color.FromArgb(0x1F, 0x1F, 0x1F),
                Text = System.Drawing.Color.FromArgb(0xF0, 0xF0, 0xF0),
                Link = System.Drawing.Color.FromArgb(0x33, 0x99, 0xFF)
            };

            string html = new ReleaseNotesCreator(History.Versions.First(), History.Versions.Last()).CreateHtml(theme);

            html.Should().Contain("content='IE=edge'");
            html.Should().Contain("body { background-color: #1F1F1F; color: #F0F0F0;");
            html.Should().Contain("a { color: #3399FF; }");
            // code is distinguishable from the background (10% of the text color)
            html.Should().Contain("background-color: #343434; font-family: Consolas");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateHtml_NoTheme_SystemColors()
        {
            string html = new ReleaseNotesCreator(History.Versions.First(), History.Versions.Last()).CreateHtml();

            html.Should().Contain($"background-color: {ReleaseNotesTheme.ToCss(System.Drawing.SystemColors.Window)};");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateHtml_MinAndMaxVersions_AllVersionsExceptMin()
        {
            var creator = new ReleaseNotesCreator(History.Versions.First(), History.Versions.Last());

            string html = creator.CreateHtml();

            // only in introduction
            Version version = History.Versions.First();
            MatchCollection firstVersionMatches = Regex.Matches(html, version.ToString());
            firstVersionMatches.Should().HaveCount(1);

            // in introduction and version list
            version = History.Versions.Last();
            MatchCollection lastVersionMatches = Regex.Matches(html, version.ToString());
            lastVersionMatches.Should().HaveCount(2);

            firstVersionMatches[0].Index.Should().BeLessThan(lastVersionMatches[0].Index);

            // only in version list
            for (int i = 1; i < History.Versions.Length - 1; i++)
            {
                version = History.Versions[i];
                html.Should().Contain(version.ToString(), $"Version {version} is missing in HTML file");
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
            MatchCollection firstVersionMatches = Regex.Matches(html, version.ToString());
            firstVersionMatches.Should().HaveCount(1);

            // in introduction and version list
            version = History.Versions.Last();
            MatchCollection lastVersionMatches = Regex.Matches(html, version.ToString());
            lastVersionMatches.Should().HaveCount(2);

            firstVersionMatches[0].Index.Should().BeGreaterThan(lastVersionMatches[0].Index);

            // only in version list
            for (int i = 1; i < History.Versions.Length - 1; i++)
            {
                version = History.Versions[i];
                html.Should().Contain(version.ToString(), $"Version {version} is missing in HTML file");
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