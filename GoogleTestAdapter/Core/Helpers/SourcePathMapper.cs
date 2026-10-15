using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleTestAdapter.Helpers
{
    /// <summary>
    /// Maps source paths of test executables built elsewhere (e.g. by CI or within a container) to local paths, as
    /// configured by option <see cref="Settings.SettingsWrapper.OptionSourcePathMapping"/>.
    /// </summary>
    public class SourcePathMapper
    {
        public const string MappingSeparator = "=>";
        public const string PairSeparator = "//||//";

        public static readonly SourcePathMapper Identity = new SourcePathMapper(new KeyValuePair<string, string>[0], false);

        private readonly IList<KeyValuePair<string, string>> _mappings;
        private readonly bool _mapOnlyMissingPaths;

        /// <param name="mappings">Build path prefixes and the local paths replacing them; the first matching prefix
        /// wins</param>
        /// <param name="mapOnlyMissingPaths">If true, paths of existing files are not mapped</param>
        public SourcePathMapper(IEnumerable<KeyValuePair<string, string>> mappings, bool mapOnlyMissingPaths)
        {
            _mappings = mappings.ToList();
            _mapOnlyMissingPaths = mapOnlyMissingPaths;
        }

        public bool IsIdentity => _mappings.Count == 0;

        /// <summary>
        /// Parses an option value of the form "buildPath1=>localPath1//||//buildPath2=>localPath2".
        /// </summary>
        /// <exception cref="ArgumentException">If the value can not be parsed</exception>
        public static IList<KeyValuePair<string, string>> Parse(string option)
        {
            var mappings = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(option))
                return mappings;

            foreach (string pair in option.Split(new[] { PairSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                int index = pair.IndexOf(MappingSeparator, StringComparison.Ordinal);
                string buildPath = index < 0 ? "" : pair.Substring(0, index).Trim();
                string localPath = index < 0 ? "" : pair.Substring(index + MappingSeparator.Length).Trim();
                if (buildPath.Length == 0 || localPath.Length == 0)
                    throw new ArgumentException($"Invalid source path mapping '{pair.Trim()}', expected '<build path>{MappingSeparator}<local path>'");

                mappings.Add(new KeyValuePair<string, string>(buildPath, localPath));
            }
            return mappings;
        }

        /// <returns>The local path of the source file, or the path itself if no mapping applies</returns>
        public string Map(string path)
        {
            if (IsIdentity || string.IsNullOrEmpty(path))
                return path;

            string normalizedPath = Normalize(path);
            foreach (var mapping in _mappings)
            {
                string buildPath = Normalize(mapping.Key).TrimEnd('\\');
                if (!IsPrefixOf(buildPath, normalizedPath))
                    continue;

                if (_mapOnlyMissingPaths && File.Exists(path))
                    return path;

                string remainder = normalizedPath.Substring(buildPath.Length).TrimStart('\\');
                string localPath = mapping.Value.TrimEnd('\\', '/');
                try
                {
                    return Path.GetFullPath(remainder.Length == 0 ? localPath : $"{localPath}\\{remainder}");
                }
                catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is PathTooLongException)
                {
                    return path;
                }
            }
            return path;
        }

        // "C:\src" is a prefix of "C:\src\foo.cpp", but not of "C:\src2\foo.cpp"
        private static bool IsPrefixOf(string prefix, string path)
        {
            return prefix.Length > 0
                && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (path.Length == prefix.Length || path[prefix.Length] == '\\');
        }

        private static string Normalize(string path)
        {
            return path.Trim().Replace('/', '\\');
        }
    }
}
