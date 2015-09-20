using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Dia;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Discovery
{

    class NativeSourceFileLocation
    {
        public string Symbol;
        public uint AddressSection;
        public uint AddressOffset;
        public uint Length;
    }

    class DiaResolver
    {

        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        internal static List<GoogleTestDiscoverer.SourceFileLocation> ResolveAllMethods(string executable, List<string> symbols, string symbolFilterString, IMessageLogger logger)
        {
            List<GoogleTestDiscoverer.SourceFileLocation> foundSourceFileLocations = FindSymbolsFromExecutable(symbols, symbolFilterString, logger, executable);

            if (foundSourceFileLocations.Count == 0)
            {
                NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(executable, logger);
                string moduleDirectory = Path.GetDirectoryName(executable);
                logger.SendMessage(TestMessageLevel.Warning, "GTA: Couldn't find " + symbols.Count + " symbols in " + executable + ", looking from DllImports in module directory " + moduleDirectory);
                List<string> foundSymbols = parser.Imports;
                foreach (string symbol in foundSymbols)
                {
                    if (moduleDirectory != null)
                    {
                        string symbolFileName = Path.Combine(moduleDirectory, symbol);
                        if (File.Exists(symbolFileName))
                        {
                            foundSourceFileLocations.AddRange(FindSymbolsFromExecutable(symbols, symbolFilterString, logger, symbolFileName));
                        }
                    }
                }
            }
            return foundSourceFileLocations;
        }

        private static List<GoogleTestDiscoverer.SourceFileLocation> FindSymbolsFromExecutable(List<string> symbols, string symbolFilterString, IMessageLogger logger, string executable)
        {
            DiaSourceClass diaDataSource = new DiaSourceClass();
            string path = ReplaceExtension(executable, ".pdb");
            try
            {
                Stream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                IStream memoryStream = new DiaMemoryStream(fileStream);
                diaDataSource.loadDataFromIStream(memoryStream);

                IDiaSession diaSession;
                diaDataSource.openSession(out diaSession);
                try
                {
                    List<NativeSourceFileLocation> allTestMethodSymbols = ExecutableSymbols(diaSession, symbolFilterString);
                    List<NativeSourceFileLocation> allTraitSymbols = ExecutableSymbols(diaSession, "*" + TraitAppendix);
                    List<NativeSourceFileLocation> foundSymbols = new List<NativeSourceFileLocation>();
                    foreach (string s in symbols)
                    {
                        foundSymbols.AddRange(allTestMethodSymbols.Where(symbol => symbol.Symbol.Contains(s)));
                    }

                    return foundSymbols.Select(symbol => GetSourceFileLocation(diaSession, logger, executable, symbol, allTraitSymbols)).ToList();
                }
                finally
                {
                    NativeMethods.ReleaseCom(diaSession);
                    NativeMethods.ReleaseCom(diaDataSource);
                    fileStream.Close();
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
            List<Trait> traits = GetTraits(nativeSymbol, allTraitSymbols);
            IDiaEnumLineNumbers lineNumbers = diaSession.GetLineNumbers(nativeSymbol.AddressSection, nativeSymbol.AddressOffset, nativeSymbol.Length);
            try
            {
                if (lineNumbers.count > 0)
                {
                    GoogleTestDiscoverer.SourceFileLocation result = null;
                    foreach (IDiaLineNumber lineNumber in lineNumbers)
                    {
                        if (result == null)
                        {
                            result = new GoogleTestDiscoverer.SourceFileLocation()
                            {
                                Symbol = nativeSymbol.Symbol,
                                Sourcefile = lineNumber.sourceFile.fileName,
                                Line = lineNumber.lineNumber,
                                Traits = traits
                            };
                        }
                        NativeMethods.ReleaseCom(lineNumber);
                    }
                    return result;
                }
                else
                {
                    logger.SendMessage(TestMessageLevel.Error, "GTA: Failed to locate line number for " + nativeSymbol);
                    return new GoogleTestDiscoverer.SourceFileLocation()
                    {
                        Symbol = executable,
                        Sourcefile = "",
                        Line = 0,
                        Traits = traits
                    };
                }
            }
            finally
            {
                NativeMethods.ReleaseCom(lineNumbers);
            }
        }

        private static List<Trait> GetTraits(NativeSourceFileLocation nativeSymbol, List<NativeSourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = new List<Trait>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (NativeSourceFileLocation nativeTraitSymbol in allTraitSymbols)
            {
                int indexOfColons = nativeTraitSymbol.Symbol.LastIndexOf("::", StringComparison.Ordinal);
                string testIdentifier = nativeTraitSymbol.Symbol.Substring(0, indexOfColons);
                if (nativeSymbol.Symbol.StartsWith(testIdentifier))
                {
                    string trait = nativeTraitSymbol.Symbol.Substring(indexOfColons + 2, nativeTraitSymbol.Symbol.Length - indexOfColons - TraitAppendix.Length - 2);
                    string[] data = trait.Split(new[] { TraitSeparator }, StringSplitOptions.None);
                    traits.Add(new Trait(data[0], data[1]));
                }
            }

            return traits;
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
                NativeMethods.ReleaseCom(diaSymbols);
            }
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static string ReplaceExtension(string executable, string newExtension)
        {
            return Path.Combine(Path.GetDirectoryName(executable),
                     Path.GetFileNameWithoutExtension(executable)) + newExtension;
        }

    }

}