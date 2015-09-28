using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Dia;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Dia
{


    class DiaResolver
    {
        internal class SourceFileLocation
        {
            internal string Symbol { get; }
            internal string Sourcefile { get; }
            internal uint Line { get; }
            internal List<Trait> Traits { get; }

            internal SourceFileLocation(string symbol, string sourceFile, uint line, List<Trait> traits)
            {
                this.Symbol = symbol;
                this.Sourcefile = sourceFile;
                this.Line = line;
                this.Traits = traits;
            }
        }

        // see GTA_Traits.h
        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        private TestEnvironment TestEnvironment { get; }

        internal DiaResolver(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }

        internal List<SourceFileLocation> ResolveAllMethods(string executable, List<string> symbols, string symbolFilterString)
        {
            List<SourceFileLocation> foundSourceFileLocations = FindSymbolsFromBinary(executable, symbols, symbolFilterString);

            if (foundSourceFileLocations.Count == 0)
            {
                NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(executable, TestEnvironment);
                List<string> imports = parser.Imports;

                string moduleDirectory = Path.GetDirectoryName(executable);
                TestEnvironment.LogInfo("", TestEnvironment.LogType.UserDebug);

                foreach (string import in imports)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    string importedBinary = Path.Combine(moduleDirectory, import);
                    if (File.Exists(importedBinary))
                    {
                        foundSourceFileLocations.AddRange(FindSymbolsFromBinary(importedBinary, symbols, symbolFilterString));
                    }
                }
            }
            return foundSourceFileLocations;
        }

        private List<SourceFileLocation> FindSymbolsFromBinary(string binary, List<string> symbols, string symbolFilterString)
        {
            DiaSourceClass diaSourceClass = new DiaSourceClass();
            string pdb = ReplaceExtension(binary, ".pdb");
            try
            {
                Stream fileStream = File.Open(pdb, FileMode.Open, FileAccess.Read, FileShare.Read);
                IStream memoryStream = new DiaMemoryStream(fileStream);
                diaSourceClass.loadDataFromIStream(memoryStream);

                IDiaSession diaSession;
                diaSourceClass.openSession(out diaSession);
                try
                {
                    List<NativeSourceFileLocation> allTestMethodSymbols = ExecutableSymbols(diaSession, symbolFilterString);
                    List<NativeSourceFileLocation> allTraitSymbols = ExecutableSymbols(diaSession, "*" + TraitAppendix);
                    List<NativeSourceFileLocation> foundSymbols = new List<NativeSourceFileLocation>();
                    foreach (string symbol in symbols)
                    {
                        foundSymbols.AddRange(allTestMethodSymbols.Where(s => s.Symbol.Contains(symbol)));
                    }

                    return foundSymbols.Select(s => GetSourceFileLocation(diaSession, binary, s, allTraitSymbols)).ToList();
                }
                finally
                {
                    NativeMethods.ReleaseCom(diaSession);
                    NativeMethods.ReleaseCom(diaSourceClass);
                    fileStream.Close();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private SourceFileLocation GetSourceFileLocation(IDiaSession diaSession, string executable, NativeSourceFileLocation nativeSymbol, List<NativeSourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = GetTraits(nativeSymbol, allTraitSymbols);
            IDiaEnumLineNumbers lineNumbers = diaSession.GetLineNumbers(nativeSymbol.AddressSection, nativeSymbol.AddressOffset, nativeSymbol.Length);
            try
            {
                if (lineNumbers.count > 0)
                {
                    SourceFileLocation result = null;
                    foreach (IDiaLineNumber lineNumber in lineNumbers)
                    {
                        if (result == null)
                        {
                            result = new SourceFileLocation(
                                nativeSymbol.Symbol, lineNumber.sourceFile.fileName,
                                lineNumber.lineNumber, traits);
                        }
                        NativeMethods.ReleaseCom(lineNumber);
                    }
                    return result;
                }
                else
                {
                    TestEnvironment.LogError("Failed to locate line number for " + nativeSymbol);
                    return new SourceFileLocation(executable, "", 0, traits);
                }
            }
            finally
            {
                NativeMethods.ReleaseCom(lineNumbers);
            }
        }

        private List<Trait> GetTraits(NativeSourceFileLocation nativeSymbol, List<NativeSourceFileLocation> allTraitSymbols)
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

        private List<NativeSourceFileLocation> ExecutableSymbols(IDiaSession diaSession, string symbolFilterString)
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
        private string ReplaceExtension(string executable, string newExtension)
        {
            return Path.Combine(Path.GetDirectoryName(executable),
                     Path.GetFileNameWithoutExtension(executable)) + newExtension;
        }

    }

}