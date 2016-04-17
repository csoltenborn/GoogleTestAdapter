using System;
using System.IO;
using System.Linq;
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
            if (newVersion < oldVersion)
                throw new ArgumentException($"{nameof(newVersion)} must be > {nameof(oldVersion)}");

            OldVersion = Versions.LastOrDefault(v => v <= oldVersion) ?? Versions[0];
            NewVersion = Versions.FirstOrDefault(v => v >= newVersion) ?? Versions.Last();
        }

        private string CreateMarkdown()
        {
            if (OldVersion == NewVersion)
                return null;

            string releaseNotes = CreateHeader();
            for (int i = Versions.Length - 1; i > Array.IndexOf(Versions, OldVersion); i--)
            {
                releaseNotes += CreateEntry(Versions[i]);
            }
            releaseNotes += CreateFooter();

            return releaseNotes;
        }

        public string CreateHtml()
        {
            string html = "<!DOCTYPE html><html><body>";
            html += CommonMarkConverter.Convert(CreateMarkdown());
            html += "</body></html>";

            return html;
        }

        private string CreateHeader()
        {
            string header = "";

            header += $"### Google Test Adapter: Release notes{Environment.NewLine}";
            header += Environment.NewLine;
            header += $"Google Test Adapter has been updated from version {ToDisplayName(OldVersion)} to {ToDisplayName(NewVersion)}. " 
                + "The changes to your formerly installed version are listed below. " 
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

        private string CreateFooter()
        {
            return "";
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