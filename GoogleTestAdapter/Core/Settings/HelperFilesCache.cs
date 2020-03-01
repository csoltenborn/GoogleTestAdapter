using System;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Settings
{
    public class HelperFilesCache
    {
        public const string HelperFileEnding = ".gta_settings_helper";
        public const string SettingsSeparator = "::GTA::";

        private readonly ILogger _logger;
        private readonly IDictionary<string, IDictionary<string, string>> _files2ReplacementMap = new Dictionary<string, IDictionary<string, string>>();

        // public for mocking
        // ReSharper disable once UnusedMember.Global
        public HelperFilesCache() {}

        public HelperFilesCache(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger Logger => _logger;

        // virtual for mocking
        public virtual IDictionary<string, string> GetReplacementsMap(string executable)
        {
            lock (this)
            {
                if (!_files2ReplacementMap.TryGetValue(executable, out IDictionary<string, string> replacementMap))
                {
                    replacementMap = LoadReplacementsMap(executable);
                    _files2ReplacementMap[executable] = replacementMap;
                }
                return replacementMap;
            }
        }

        public static string GetHelperFile(string executable)
        {
            return $"{executable}{HelperFileEnding}";
        }

        private IDictionary<string, string> LoadReplacementsMap(string executable)
        {
            string helperFile = GetHelperFile(executable);
            try
            {
                if (!File.Exists(helperFile))
                    return new Dictionary<string, string>();

                _logger.DebugInfo($"Parsing settings helper file at {helperFile} (executable: {executable})");
                return ParseHelperFile(File.ReadAllText(helperFile).Trim());
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Exception while loading settings from file '{helperFile}': {e}");
                return new Dictionary<string, string>();
            }
        }

        private IDictionary<string, string> ParseHelperFile(string content)
        {
            var replacementMap = new Dictionary<string, string>();
            foreach (string setting in content.Split(new []{ SettingsSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                int index = setting.IndexOf('=');
                if (index > 0)
                {
                    string placeholder = setting.Substring(0, index);
                    string value = setting.Substring(index + 1, setting.Length - index - 1);
                    replacementMap.Add(placeholder, value);
                    _logger.VerboseInfo($"Found placeholder {placeholder} with value '{value}'");
                }
            }
            return replacementMap;
        }
    }
}