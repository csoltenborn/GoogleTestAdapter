using System;
using System.IO;
using System.Reflection;
using CommonMark;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public class ReleaseNotesCreator
    {
        private static Version[] Versions => History.Versions;

        private Version OldVersion { get; }
        private Version NewVersion { get; }

        public ReleaseNotesCreator(Version oldVersion, Version newVersion)
        {
            OldVersion = oldVersion;
            NewVersion = newVersion;
        }

        private string CreateMarkdown()
        {
            if (OldVersion == NewVersion)
                return "";

            string releaseNotes = CreateHeader();

            int startIndex = Array.IndexOf(Versions, NewVersion);
            int endIndex = OldVersion == null ? -1 : Array.IndexOf(Versions, OldVersion);
            for (int i = startIndex; i > endIndex; i--)
            {
                releaseNotes += CreateEntry(Versions[i]);
            }

            return releaseNotes;
        }

        public string CreateHtml()
        {
            string markDown = CreateMarkdown();
            if (string.IsNullOrEmpty(markDown))
                return "";

            string html = "<!DOCTYPE html><html><body>";
            html += CommonMarkConverter.Convert(CreateMarkdown());
            html += "</body></html>";

            return html;
        }

        private string CreateHeader()
        {
            string fromVersion = OldVersion == null ? "" : $" from V{ToDisplayName(OldVersion)}";

            string header = "";
            header += $"### Google Test Adapter: Release notes{Environment.NewLine}";
            header += Environment.NewLine;
            header += "Google Test Adapter has been updated";
            header += $"{fromVersion} to V{ToDisplayName(NewVersion)}. ";

            if (OldVersion == null)
                header += $"The complete version history is listed below. Future updates will only list changes to the formerly installed version.{Environment.NewLine}";
            else
                header += "The changes to your formerly installed version are listed below. "
                    + $"A [complete revision history](https://github.com/csoltenborn/GoogleTestAdapter/releases) is available at GitHub.{Environment.NewLine}";

            return header;
        }

        private string CreateEntry(Version version)
        {
            string releaseNoteEntry = ReadReleaseNotesFile(version);
            if (string.IsNullOrEmpty(releaseNoteEntry))
                return "";

            string date = History.GetDate(version).ToShortDateString();

            string entry = "";

            entry += Environment.NewLine;
            entry += $"#### Version {ToDisplayName(version)} ({date}){Environment.NewLine}";
            entry += Environment.NewLine;
            entry += $"{releaseNoteEntry}{Environment.NewLine}";

            return entry;
        }

        private string ReadReleaseNotesFile(Version version)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ToResourceName(version)))
                // ReSharper disable once AssignNullToNotNullAttribute
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string ToResourceName(Version version)
        {
            string versionString = $"{version.Major}.{version.Minor}.{version.Revision}";
            return $"GoogleTestAdapter.VsPackage.Resources.ReleaseNotes.{versionString}.md";
        }

        private string ToDisplayName(Version version)
        {
            return $"{version.Major}.{version.Minor}.{version.Revision}";
        }

    }

}