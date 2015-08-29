using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Dia;
using System;
using System.Diagnostics;
using static GoogleTestAdapter.Native;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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

        private const string TRAIT_SEPARATOR = "__GTA__";
        private const string TRAIT_APPENDIX = "_GTA_TRAIT";

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
                    List<NativeSourceFileLocation> allTestMethodSymbols = ExecutableSymbols(diaSession, symbolFilterString);
                    List<NativeSourceFileLocation> allTraitSymbols = ExecutableSymbols(diaSession, "*_TRAIT");
                    List<NativeSourceFileLocation> foundSymbols = new List<NativeSourceFileLocation>();
                    foreach (string s in symbols)
                    {
                        foreach (NativeSourceFileLocation symbol in allTestMethodSymbols)
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
                        SourceFileLocations.Add(GetSourceFileLocation(diaSession, logger, executable, symbol, allTraitSymbols));
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

        private static GoogleTestDiscoverer.SourceFileLocation GetSourceFileLocation(IDiaSession diaSession, IMessageLogger logger, string executable, NativeSourceFileLocation nativeSymbol, List<NativeSourceFileLocation> allTraitSymbols)
        {
            List<Trait> Traits = GetTraits(nativeSymbol, allTraitSymbols);
            IDiaEnumLineNumbers lineNumbers = diaSession.GetLineNumbers(nativeSymbol.addressSection, nativeSymbol.addressOffset, nativeSymbol.length);
            try
            {
                if (lineNumbers.count > 0)
                {
                    return new GoogleTestDiscoverer.SourceFileLocation()
                    {
                        symbol = nativeSymbol.symbol,
                        sourcefile = lineNumbers.Item(0).sourceFile.fileName,
                        line = lineNumbers.Item(0).lineNumber,
                        traits = Traits
                    };
                }
                else
                {
                    logger.SendMessage(TestMessageLevel.Error, "Failed to locate line number for " + nativeSymbol);
                    return new GoogleTestDiscoverer.SourceFileLocation()
                    {
                        symbol = executable,
                        sourcefile = "",
                        line = 0,
                        traits = Traits
                    };
                }
            }
            finally
            {
                ReleaseCom(lineNumbers);
            }
        }

        private static List<Trait> GetTraits(NativeSourceFileLocation nativeSymbol, List<NativeSourceFileLocation> allTraitSymbols)
        {
            List<Trait> Traits = new List<Trait>();
            foreach (NativeSourceFileLocation nativeTraitSymbol in allTraitSymbols)
            {
                int IndexOfColons = nativeTraitSymbol.symbol.LastIndexOf("::");
                string TestIdentifier = nativeTraitSymbol.symbol.Substring(0, IndexOfColons);
                if (nativeSymbol.symbol.StartsWith(TestIdentifier))
                {
                    string Trait = nativeTraitSymbol.symbol.Substring(IndexOfColons + 2, nativeTraitSymbol.symbol.Length - IndexOfColons - TRAIT_APPENDIX.Length - 2);
                    string[] Data = Trait.Split(new string[] { TRAIT_SEPARATOR }, StringSplitOptions.None);
                    Traits.Add(new Trait(Data[0], Data[1]));
                }
            }

            return Traits;
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
