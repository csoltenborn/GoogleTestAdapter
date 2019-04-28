using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using MethodSignature = GoogleTestAdapter.TestCases.MethodSignatureCreator.MethodSignature;

namespace GoogleTestAdapter.TestCases
{

    public class TestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        private readonly string _executable;
        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly SettingsWrapper _settings;
        private readonly ILogger _logger;

        private readonly List<SourceFileLocation> _allTestMethodSymbols = new List<SourceFileLocation>();
        private readonly List<SourceFileLocation> _allTraitSymbols = new List<SourceFileLocation>();

        private bool _loadedSymbolsFromAdditionalPdbs;
        private bool _loadedSymbolsFromImports;

        public TestCaseResolver(string executable, IDiaResolverFactory diaResolverFactory, SettingsWrapper settings, ILogger logger)
        {
            _executable = executable;
            _diaResolverFactory = diaResolverFactory;
            _settings = settings;
            _logger = logger;

            if (_settings.ParseSymbolInformation)
            {
                AddSymbolsFromBinary(executable, true);
            }
            else
            {
                _loadedSymbolsFromAdditionalPdbs = true;
                _loadedSymbolsFromImports = true;
            }
        }

        public TestCaseLocation MainMethodLocation { get; private set; }

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
            foreach (var pdbPattern in _settings.GetAdditionalPdbs(_executable))
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

        private void AddSymbolsFromBinary(string binary, bool resolveMainMethod = false)
        {
            string pdb = PdbLocator.FindPdbFile(binary, _settings.GetPathExtension(_executable), _logger);
            if (pdb == null)
            {
                _logger.DebugWarning($"No .pdb file found for '{binary}'");
                return;
            }

            AddSymbolsFromBinary(binary, pdb, resolveMainMethod);
        }

        private void AddSymbolsFromBinary(string binary, string pdb, bool resolveMainMethod = false)
        {
            using (IDiaResolver diaResolver = _diaResolverFactory.Create(binary, pdb, _logger))
            {
                try
                {
                    _allTestMethodSymbols.AddRange(diaResolver.GetFunctions("*" + GoogleTestConstants.TestBodySignature));
                    _allTraitSymbols.AddRange(diaResolver.GetFunctions("*" + TraitAppendix));
                    _logger.DebugInfo($"Found {_allTestMethodSymbols.Count} test method symbols and {_allTraitSymbols.Count} trait symbols in binary {binary}, pdb {pdb}");

                    if (resolveMainMethod)
                    {
                        MainMethodLocation = ResolveMainMethod(diaResolver);
                    }
                }
                catch (Exception e)
                {
                    _logger.DebugError($"Exception while resolving test locations and traits in '{binary}':{Environment.NewLine}{e}");
                }
            }
        }

        private TestCaseLocation ResolveMainMethod(IDiaResolver diaResolver)
        {
            var mainSymbols = new List<SourceFileLocation>();
            mainSymbols.AddRange(diaResolver.GetFunctions("main"));

            if (!string.IsNullOrWhiteSpace(_settings.ExitCodeTestCase))
            {
                if (mainSymbols.Count == 0)
                {
                    _logger.DebugWarning(
                        $"Could not find any main method for executable {_executable} - exit code test will not have source location");
                }
                else if (mainSymbols.Count > 1)
                {
                    _logger.DebugWarning(
                        $"Found more than one potential main method in executable {_executable} - exit code test might have wrong source location");
                }
            }

            var location = mainSymbols.FirstOrDefault();
            return location != null ? ToTestCaseLocation(location) : null;
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