using System;
using System.IO;
using CommonMark;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public class ReleaseNotesCreator
    {
        private static Version[] Versions => History.Versions;

        private readonly Version _formerlyInstalledVersion;
        private readonly Version _currentVersion;

        public ReleaseNotesCreator(Version formerlyInstalledVersion, Version currentVersion)
        {
            _formerlyInstalledVersion = formerlyInstalledVersion;
            _currentVersion = currentVersion;
        }

        private string CreateMarkdown()
        {
            if (_formerlyInstalledVersion == _currentVersion)
                return "";

            string releaseNotes = CreateHeader();

            int startIndex = Array.IndexOf(Versions, _currentVersion);
            int endIndex = _formerlyInstalledVersion == null ? -1 : Array.IndexOf(Versions, _formerlyInstalledVersion);
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

            string html = "<!DOCTYPE html><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'><html><body>";
            html += CommonMarkConverter.Convert(CreateMarkdown());
            html += "</body></html>";

            return html;
        }

        private string CreateHeader()
        {
            string fromVersion = _formerlyInstalledVersion == null ? "" : $" from V{ToDisplayName(_formerlyInstalledVersion)}";

            string header = "";
            header += $"### Google Test Adapter: Release notes{Environment.NewLine}";
            header += Environment.NewLine;
            header += "Google Test Adapter has been updated";
            header += $"{fromVersion} to V{ToDisplayName(_currentVersion)}. ";

            if (_formerlyInstalledVersion == null)
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
                return File.ReadAllText(History.GetReleaseNotesFile(version));
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string ToDisplayName(Version version)
        {
            return version.ToString();
        }

    }

}