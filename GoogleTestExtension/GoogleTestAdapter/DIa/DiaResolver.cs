using System;
using System.Collections.Generic;
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
        /*
        Symbol=[<namespace>::]<test_case_name>_<test_name>_Test::TestBody
        */
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


        internal List<SourceFileLocation> ResolveAllMethods(string executable, List<string> testMethodSignatures, string symbolFilterString)
        {
            List<SourceFileLocation> foundSourceFileLocations =
                FindSymbolsFromBinary(executable, testMethodSignatures, symbolFilterString).ToList();

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
                        foundSourceFileLocations.AddRange(FindSymbolsFromBinary(importedBinary, testMethodSignatures, symbolFilterString));
                    }
                }
            }
            return foundSourceFileLocations;
        }


        private IEnumerable<SourceFileLocation> FindSymbolsFromBinary(string binary, List<string> testMethodSignatures, string symbolFilterString)
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
                    IEnumerable<NativeSourceFileLocation> allTestMethodSymbols = ExecutableSymbols(diaSession, symbolFilterString);
                    IEnumerable<NativeSourceFileLocation> allTraitSymbols = ExecutableSymbols(diaSession, "*" + TraitAppendix);
                    return
                        allTestMethodSymbols.Where(
                            nsfl => testMethodSignatures.Any(
                                tms => nsfl.Symbol.Contains(tms))) // Contains() instead of == because nsfl might contain namespace
                        .Select(
                            nsfl => GetSourceFileLocation(diaSession, binary, nsfl, allTraitSymbols));
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
                TestEnvironment.LogError("Exception while looking for testMethodSignatures: " + e);
                throw;
            }
        }

        private SourceFileLocation GetSourceFileLocation(IDiaSession diaSession, string executable, NativeSourceFileLocation nativeSymbol, IEnumerable<NativeSourceFileLocation> allTraitSymbols)
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

        private List<Trait> GetTraits(NativeSourceFileLocation nativeSymbol, IEnumerable<NativeSourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = new List<Trait>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (NativeSourceFileLocation nativeTraitSymbol in allTraitSymbols)
            {
                int indexOfSerializedTrait = nativeTraitSymbol.Symbol.LastIndexOf("::", StringComparison.Ordinal) + "::".Length;
                string testClassSignature = nativeTraitSymbol.Symbol.Substring(0, indexOfSerializedTrait - "::".Length);
                if (nativeSymbol.Symbol.StartsWith(testClassSignature))
                {
                    int lengthOfSerializedTrait = nativeTraitSymbol.Symbol.Length - indexOfSerializedTrait - TraitAppendix.Length;
                    string serializedTrait = nativeTraitSymbol.Symbol.Substring(indexOfSerializedTrait, lengthOfSerializedTrait);
                    string[] data = serializedTrait.Split(new[] { TraitSeparator }, StringSplitOptions.None);
                    traits.Add(new Trait(data[0], data[1]));
                }
            }

            return traits;
        }

        private IEnumerable<NativeSourceFileLocation> ExecutableSymbols(IDiaSession diaSession, string symbolFilterString)
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