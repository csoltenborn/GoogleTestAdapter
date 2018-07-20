// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using MethodSignature = GoogleTestAdapter.TestCases.MethodSignatureCreator.MethodSignature;

namespace GoogleTestAdapter.TestCases
{

    public class TestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        private readonly string _executable;
        private readonly string _pathExtension;
        private readonly IEnumerable<string> _additionalPdbs;
        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly ILogger _logger;

        private readonly List<SourceFileLocation> _allTestMethodSymbols = new List<SourceFileLocation>();
        private readonly List<SourceFileLocation> _allTraitSymbols = new List<SourceFileLocation>();

        private bool _loadedSymbolsFromAdditionalPdbs;
        private bool _loadedSymbolsFromImports;

        public TestCaseResolver(string executable, string pathExtension, IEnumerable<string> additionalPdbs, IDiaResolverFactory diaResolverFactory, bool parseSymbolInformation, ILogger logger)
        {
            _executable = executable;
            _pathExtension = pathExtension;
            _additionalPdbs = additionalPdbs;
            _diaResolverFactory = diaResolverFactory;
            _logger = logger;

            if (parseSymbolInformation)
            {
                AddSymbolsFromBinary(executable);
            }
            else
            {
                _loadedSymbolsFromAdditionalPdbs = true;
                _loadedSymbolsFromImports = true;
            }
        }

        public TestCaseLocation FindTestCaseLocation(List<MethodSignature> testMethodSignatures)
        {
            TestCaseLocation result = DoFindTestCaseLocation(testMethodSignatures);
            if (result == null && !_loadedSymbolsFromAdditionalPdbs)
            {
                LoadSymbolsFromAdditionalPdbs();
                _loadedSymbolsFromAdditionalPdbs = true;
                result = DoFindTestCaseLocation(testMethodSignatures);
            }
            if (result == null && !_loadedSymbolsFromImports)
            {
                LoadSymbolsFromImports();
                _loadedSymbolsFromImports = true;
                result = DoFindTestCaseLocation(testMethodSignatures);
            }
            return result;
        }

        private void LoadSymbolsFromAdditionalPdbs()
        {
            foreach (var pdbPattern in _additionalPdbs)
            {
                var matchingFiles = Utils.GetMatchingFiles(pdbPattern, _logger);
                if (matchingFiles.Length == 0)
                {
                    _logger.LogWarning($"Additional PDB pattern '{pdbPattern}' does not match any files");
                }
                else
                {
                    _logger.DebugInfo($"Additional PDB pattern '{pdbPattern}' matches {matchingFiles.Length} files");
                    foreach (string pdbCandidate in matchingFiles)
                    {
                        AddSymbolsFromBinary(_executable, pdbCandidate);
                    }
                }
            }
        }

        private void LoadSymbolsFromImports()
        {
            List<string> imports = PeParser.ParseImports(_executable, _logger);
            string moduleDirectory = Path.GetDirectoryName(_executable);
            foreach (string import in imports)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                string importedBinary = Path.Combine(moduleDirectory, import);
                if (File.Exists(importedBinary))
                    AddSymbolsFromBinary(importedBinary);
            }
        }

        private void AddSymbolsFromBinary(string binary)
        {
            string pdb = PdbLocator.FindPdbFile(binary, _pathExtension, _logger);
            if (pdb == null)
            {
                _logger.DebugWarning($"No .pdb file found for '{binary}'");
                return;
            }

            AddSymbolsFromBinary(binary, pdb);
        }

        private void AddSymbolsFromBinary(string binary, string pdb)
        {
            using (IDiaResolver diaResolver = _diaResolverFactory.Create(binary, pdb, _logger))
            {
                try
                {
                    var testMethodSymbols = diaResolver.GetFunctions("*" + GoogleTestConstants.TestBodySignature);
                    var traitSymbols = diaResolver.GetFunctions("*" + TraitAppendix);

                    _allTestMethodSymbols.AddRange(testMethodSymbols);
                    _allTraitSymbols.AddRange(traitSymbols);

                    _logger.DebugInfo(String.Format(Resources.FoundTestMethod, testMethodSymbols.Count, traitSymbols.Count, binary, pdb));
                }
                catch (Exception e)
                {
                    _logger.DebugError(String.Format(Resources.ExceptionResolving, binary, e));
                }
            }
        }

        private TestCaseLocation DoFindTestCaseLocation(List<MethodSignature> testMethodSignatures)
        {
            var sourceFileLocation =  _allTestMethodSymbols
                .FirstOrDefault(nsfl => testMethodSignatures.Any(tms => IsMatch(nsfl, tms))); 
            return sourceFileLocation != null
                ? ToTestCaseLocation(sourceFileLocation)
                : null;
        }

        private bool IsMatch(SourceFileLocation sourceFileLocation, MethodSignature methodSignature)
        {
            string signature = methodSignature.Signature;

            bool generalCheck = methodSignature.IsRegex
                ? Regex.IsMatch(sourceFileLocation.Symbol, signature)
                : sourceFileLocation.Symbol.Contains(signature);

            return generalCheck && Regex.IsMatch(sourceFileLocation.Symbol, GetPreciseRegex(signature));
        }

        private string GetPreciseRegex(string signature)
        {
            return $@"^(?:(?:(?:\w+)|(?:`anonymous namespace'))::)*{signature}";
        }

        private TestCaseLocation ToTestCaseLocation(SourceFileLocation location)
        {
            var testCaseLocation = new TestCaseLocation(location.Symbol, location.Sourcefile, location.Line);
            testCaseLocation.Traits.AddRange(GetTraits(location));
            return testCaseLocation;
        }

        private List<Trait> GetTraits(SourceFileLocation nativeSymbol)
        {
            var traits = new List<Trait>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (SourceFileLocation nativeTraitSymbol in _allTraitSymbols)
            {
                // TODO bring down to logarithmic complexity (binary search for finding a symbol, collect all matching symbols after and before)
                if (nativeSymbol.Symbol.StartsWith(nativeTraitSymbol.TestClassSignature))
                {
                    int lengthOfSerializedTrait = nativeTraitSymbol.Symbol.Length - nativeTraitSymbol.IndexOfSerializedTrait - TraitAppendix.Length;
                    string serializedTrait = nativeTraitSymbol.Symbol.Substring(nativeTraitSymbol.IndexOfSerializedTrait, lengthOfSerializedTrait);
                    string[] data = serializedTrait.Split(new[] { TraitSeparator }, StringSplitOptions.None);
                    traits.Add(new Trait(data[0], data[1]));
                }
            }

            return traits;
        }

    }

}