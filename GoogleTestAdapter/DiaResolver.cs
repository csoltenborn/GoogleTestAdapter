using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Dia;
using System;
using System.Diagnostics;
using static GoogleTestAdapter.Native;

namespace GoogleTestAdapter
{
    public class NativeSourceFileLocation
    {
        public string symbol;
        public uint addressSection;
        public uint addressOffset;
        public uint length;
    }

    class DiaResolver
    {

        internal static List<GoogleTestDiscoverer.SourceFileLocation> ResolveAllMethods(string executable, List<string> symbols, string symbolFilterString, IMessageLogger logger)
        {
            List<GoogleTestDiscoverer.SourceFileLocation> foundSymbols = FindSymbolsFromExecutable(symbols, symbolFilterString, logger, executable);
            if (foundSymbols.Count == 0)
            {
                ImportsParser Parser = new ImportsParser(executable, logger);
                string ModuleDirectory = Path.GetDirectoryName(executable);
                logger.SendMessage(TestMessageLevel.Warning, "Couldn't find " + symbols.Count + " symbols in " + executable + ", looking from DllImports in module directory " + ModuleDirectory);
                List<string> FoundSymbols = Parser.Imports;
                foreach (string symbol in FoundSymbols)
                {
                    string symbolFileName = Path.Combine(ModuleDirectory, symbol);
                    if (File.Exists(symbolFileName))
                    {
                        foundSymbols.AddRange(FindSymbolsFromExecutable(symbols, symbolFilterString, logger, symbolFileName));
                    }
                }
            }
            return foundSymbols;
        }

        private static List<GoogleTestDiscoverer.SourceFileLocation> FindSymbolsFromExecutable(List<string> symbols, string symbolFilterString, IMessageLogger logger, string executable)
        {
            DiaSourceClass diaDataSource = new DiaSourceClass();
            string path = DiaResolver.ReplaceExtension(executable, ".pdb");
            logger.SendMessage(TestMessageLevel.Informational, "Loading PDB: " + path);
            try
            {
                IStream memoryStream = new DiaUtils.DiaMemoryStream(path);
                diaDataSource.loadDataFromIStream(memoryStream);

                IDiaSession diaSession = null;
                diaDataSource.openSession(out diaSession);
                try
                {
                    List<NativeSourceFileLocation> allSymbols = ExecutableSymbols(diaSession, symbolFilterString);
                    List<NativeSourceFileLocation> foundSymbols = new List<NativeSourceFileLocation>();
                    foreach (NativeSourceFileLocation symbol in allSymbols)
                    {
                        foreach (string s in symbols)
                        {
                            if (symbol.symbol.Contains(s))
                            {
                                foundSymbols.Add(symbol);
                            }
                        }
                    }

                    List<GoogleTestDiscoverer.SourceFileLocation> SourceFileLocations = new List<GoogleTestDiscoverer.SourceFileLocation>();
                    foreach (NativeSourceFileLocation symbol in foundSymbols)
                    {
                        SourceFileLocations.Add(GetSourceFileLocation(diaSession, logger, executable, symbol));
                    }
                    logger.SendMessage(TestMessageLevel.Informational, "From " + executable + ", found " + foundSymbols.Count + " symbols");
                    return SourceFileLocations;
                }
                finally
                {
                    ReleaseCom(diaSession);
                    ReleaseCom(diaDataSource);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private static GoogleTestDiscoverer.SourceFileLocation GetSourceFileLocation(IDiaSession diaSession, IMessageLogger logger, string executable, NativeSourceFileLocation symbol)
        {
            IDiaEnumLineNumbers lineNumbers = diaSession.GetLineNumbers(symbol.addressSection, symbol.addressOffset, symbol.length);
            try
            {
                if (lineNumbers.count > 0)
                {
                    return new GoogleTestDiscoverer.SourceFileLocation()
                    {
                        symbol = symbol.symbol,
                        sourcefile = lineNumbers.Item(0).sourceFile.fileName,
                        line = lineNumbers.Item(0).lineNumber
                    };
                }
                else
                {
                    logger.SendMessage(TestMessageLevel.Error, "Failed to locate line number for " + symbol);
                    return new GoogleTestDiscoverer.SourceFileLocation()
                    {
                        symbol = executable,
                        sourcefile = "",
                        line = 0
                    };
                }
            }
            finally
            {
                ReleaseCom(lineNumbers);
            }
        }

        private static List<NativeSourceFileLocation> ExecutableSymbols(IDiaSession diaSession, string symbolFilterString)
        {
            IDiaEnumSymbols diaSymbols = diaSession.FindFunctionsByRegex(symbolFilterString);
            try
            {
                return diaSession.GetSymbolNamesAndAddresses(diaSymbols);
            }
            finally
            {
                ReleaseCom(diaSymbols);
            }
        }

        private static string ReplaceExtension(string executable, string newExtension)
        {
            return Path.Combine(Path.GetDirectoryName(executable),
                     Path.GetFileNameWithoutExtension(executable)) + newExtension;
        }

    }
}
