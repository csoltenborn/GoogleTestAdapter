// This file has been modified by Microsoft on 7/2017.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitAppendix = "_GTA_TRAIT";

        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly ILogger _logger;

        internal TestCaseResolver(IDiaResolverFactory diaResolverFactory, ILogger logger)
        {
            _diaResolverFactory = diaResolverFactory;
            _logger = logger;
        }

        internal Dictionary<string, TestCaseLocation> ResolveAllTestCases(string executable, HashSet<string> testMethodSignatures, string symbolFilterString, string pathExtension, IEnumerable<string> additionalPdbs)
        {
            var testCaseLocationsFound = FindTestCaseLocationsInBinary(executable, testMethodSignatures, symbolFilterString, pathExtension);
            if (testCaseLocationsFound.Count == 0)
            {
                ResolveTestCasesFromAdditionalPdbs(testCaseLocationsFound, executable, additionalPdbs, testMethodSignatures, symbolFilterString);
                ResolveTestCasesFromImports(testCaseLocationsFound, executable, pathExtension, testMethodSignatures, symbolFilterString);
            }
            return testCaseLocationsFound;
        }

        private Dictionary<string, TestCaseLocation> FindTestCaseLocationsInBinary(
            string binary, HashSet<string> testMethodSignatures, string symbolFilterString, string pathExtension)
        {
            string pdb = PdbLocator.FindPdbFile(binary, pathExtension, _logger);
            if (pdb == null)
            {
                _logger.LogWarning($"Couldn't find the .pdb file of file '{binary}'. You might not get source locations for some or all of your tests.");
                return new Dictionary<string, TestCaseLocation>();
            }

            return FindTestCaseLocations(binary, pdb, testMethodSignatures, symbolFilterString);
        }

        private void ResolveTestCasesFromAdditionalPdbs(Dictionary<string, TestCaseLocation> testCaseLocationsFound, string executable,
            IEnumerable<string> additionalPdbs, HashSet<string> testMethodSignatures, string symbolFilterString)
        {
            foreach (string pdb in additionalPdbs)
            {
                if (!File.Exists(pdb))
                {
                    _logger.LogWarning($"Configured additional PDB file '{pdb}' does not exist");
                    continue;
                }
                foreach (var keyValuePair in FindTestCaseLocations(executable, pdb, testMethodSignatures, symbolFilterString))
                {
                    testCaseLocationsFound.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        private void ResolveTestCasesFromImports(Dictionary<string, TestCaseLocation> testCaseLocationsFound, string executable, string pathExtension,
            HashSet<string> testMethodSignatures, string symbolFilterString)
        {
            List<string> imports = PeParser.ParseImports(executable, _logger);

            string moduleDirectory = Path.GetDirectoryName(executable);

            foreach (string import in imports)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                string importedBinary = Path.Combine(moduleDirectory, import);
                if (File.Exists(importedBinary))
                {
                    foreach (var testCaseLocation in FindTestCaseLocationsInBinary(importedBinary, testMethodSignatures,
                        symbolFilterString, pathExtension))
                    {
                        testCaseLocationsFound.Add(testCaseLocation.Key, testCaseLocation.Value);
                    }
                }
            }
        }

        private Dictionary<string, TestCaseLocation> FindTestCaseLocations(string binary, string pdb, HashSet<string> testMethodSignatures,
            string symbolFilterString)
        {
            using (IDiaResolver diaResolver = _diaResolverFactory.Create(binary, pdb, _logger))
            {
                try
                {
                    IList<SourceFileLocation> allTestMethodSymbols = diaResolver.GetFunctions(symbolFilterString);
                    IList<SourceFileLocation> allTraitSymbols = diaResolver.GetFunctions("*" + TraitAppendix);
                    _logger.DebugInfo(
                        $"Found {allTestMethodSymbols.Count} test method symbols and {allTraitSymbols.Count} trait symbols in binary {binary}");

                    return allTestMethodSymbols
                        .Where(nsfl => testMethodSignatures.Contains(TestCaseFactory.StripTestSymbolNamespace(nsfl.Symbol)))
                        .Select(nsfl => ToTestCaseLocation(nsfl, allTraitSymbols))
                        .ToDictionary(nsfl => TestCaseFactory.StripTestSymbolNamespace(nsfl.Symbol));
                }
                catch (Exception e)
                {
                    _logger.DebugError($"Exception while resolving test locations and traits in {binary}\n{e}");
                    return new Dictionary<string, TestCaseLocation>();
                }
            }
        }

        private TestCaseLocation ToTestCaseLocation(SourceFileLocation location, IEnumerable<SourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = NewTestCaseResolver.GetTraits(location, allTraitSymbols);
            var testCaseLocation = new TestCaseLocation(location.Symbol, location.Sourcefile, location.Line);
            testCaseLocation.Traits.AddRange(traits);
            return testCaseLocation;
        }

    }

}
