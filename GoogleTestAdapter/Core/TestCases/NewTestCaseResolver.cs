// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class NewTestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        private readonly string _executable;
        private readonly string _pathExtension;
        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly ILogger _logger;

        private readonly List<SourceFileLocation> _allTestMethodSymbols = new List<SourceFileLocation>();
        private readonly List<SourceFileLocation> _allTraitSymbols = new List<SourceFileLocation>();

        private bool _loadedSymbolsFromImports;

        internal NewTestCaseResolver(string executable, string pathExtension, IDiaResolverFactory diaResolverFactory, bool parseSymbolInformation, ILogger logger)
        {
            _executable = executable;
            _pathExtension = pathExtension;
            _diaResolverFactory = diaResolverFactory;
            _logger = logger;

            if (parseSymbolInformation)
                AddSymbolsFromBinary(executable);
            else
                _loadedSymbolsFromImports = true;
        }

        internal TestCaseLocation FindTestCaseLocation(List<string> testMethodSignatures)
        {
            TestCaseLocation result = DoFindTestCaseLocation(testMethodSignatures);
            if (result == null && !_loadedSymbolsFromImports)
            {
                LoadSymbolsFromImports();
                _loadedSymbolsFromImports = true;
                result = DoFindTestCaseLocation(testMethodSignatures);
            }
            return result;
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
            using (IDiaResolver diaResolver = _diaResolverFactory.Create(binary, _pathExtension, _logger))
            {
                try
                {
                    _allTestMethodSymbols.AddRange(diaResolver.GetFunctions("*" + GoogleTestConstants.TestBodySignature));
                    _allTraitSymbols.AddRange(diaResolver.GetFunctions("*" + TraitAppendix));

                    _logger.DebugInfo(String.Format(Resources.FoundTestMethod, _allTestMethodSymbols.Count, _allTraitSymbols.Count, binary));
                }
                catch (Exception e)
                {
                    _logger.DebugError(String.Format(Resources.ExceptionResolving, binary, e));
                }
            }
        }

        private TestCaseLocation DoFindTestCaseLocation(List<string> testMethodSignatures)
        {
            return _allTestMethodSymbols
                .Where(nsfl => testMethodSignatures.Any(tms => Regex.IsMatch(nsfl.Symbol, tms))) // Contains() instead of == because nsfl might contain namespace
                .Select(nsfl => ToTestCaseLocation(nsfl, _allTraitSymbols))
                .FirstOrDefault(); // we need to force immediate query execution, otherwise our session object will already be released
        }

        private TestCaseLocation ToTestCaseLocation(SourceFileLocation location, IEnumerable<SourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = GetTraits(location, allTraitSymbols);
            var testCaseLocation = new TestCaseLocation(location.Symbol, location.Sourcefile, location.Line);
            testCaseLocation.Traits.AddRange(traits);
            return testCaseLocation;
        }

        public static List<Trait> GetTraits(SourceFileLocation nativeSymbol, IEnumerable<SourceFileLocation> allTraitSymbols)
        {
            var traits = new List<Trait>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (SourceFileLocation nativeTraitSymbol in allTraitSymbols)
            {
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