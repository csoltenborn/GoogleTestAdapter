using System;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{
    public static class PdbLocator
    {
        public static string FindPdbFile(string binary, string pathExtension, ILogger logger)
        {
            IList<string> attempts = new List<string>();
            string pdb = PeParser.ExtractPdbPath(binary, logger);
            if (pdb != null && File.Exists(pdb))
                return pdb;
            attempts.Add("parsing from executable");

            pdb = Path.ChangeExtension(binary, ".pdb");
            if (File.Exists(pdb))
                return pdb;
            attempts.Add($"\"{pdb}\"");

            pdb = Path.GetFileName(pdb);
            if (pdb == null || File.Exists(pdb))
                return pdb;
            attempts.Add($"\"{pdb}\"");

            string path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathExtension))
                path = $"{pathExtension};{path}";
            var pathElements = path?.Split(';');
            if (path != null)
            {
                foreach (string pathElement in pathElements)
                {
                    string file = Path.Combine(pathElement, pdb);
                    if (File.Exists(file))
                        return file;
                    attempts.Add($"\"{file}\"");
                }
            }

            logger.DebugInfo("Attempts to find pdb: " + string.Join("::", attempts));

            return null;
        }
    }
}