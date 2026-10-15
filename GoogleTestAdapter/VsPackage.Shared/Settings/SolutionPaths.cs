using System.IO;
using System.Linq;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    /// <summary>
    /// Derives the solution directory and the solution settings file from Solution.FullName. If Visual Studio
    /// has opened a folder rather than a solution (e.g. a CMake project), Solution.FullName is the folder's path.
    /// </summary>
    public static class SolutionPaths
    {
        public static string GetSolutionDir(string solutionFullName)
        {
            if (string.IsNullOrWhiteSpace(solutionFullName))
                return null;

            return IsFolder(solutionFullName)
                ? GetFolder(solutionFullName)
                : Path.GetDirectoryName(solutionFullName);
        }

        /// <summary>
        /// For an opened folder, the settings file is &lt;FolderName&gt;.gta.runsettings within that folder
        /// or, if that does not exist, the only *.gta.runsettings file within that folder.
        /// </summary>
        public static string GetSolutionSettingsFile(string solutionFullName)
        {
            if (string.IsNullOrWhiteSpace(solutionFullName))
                return null;

            if (!IsFolder(solutionFullName))
                return Path.ChangeExtension(solutionFullName, GoogleTestConstants.SettingsExtension);

            string folder = GetFolder(solutionFullName);
            string settingsFile = Path.Combine(folder, Path.GetFileName(folder) + GoogleTestConstants.SettingsExtension);
            if (File.Exists(settingsFile))
                return settingsFile;

            var settingsFiles = Directory.GetFiles(folder, "*" + GoogleTestConstants.SettingsExtension);
            return settingsFiles.Length == 1 ? settingsFiles.Single() : settingsFile;
        }

        private static bool IsFolder(string solutionFullName)
        {
            return Directory.Exists(solutionFullName);
        }

        private static string GetFolder(string solutionFullName)
        {
            string folder = Path.GetFullPath(solutionFullName);
            string root = Path.GetPathRoot(folder);
            return folder.Length > root.Length
                ? folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : folder;
        }
    }
}
