using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly TestEnvironment _testEnvironment;

        internal TestCaseResolver(IDiaResolverFactory diaResolverFactory, TestEnvironment testEnvironment)
        {
            _diaResolverFactory = diaResolverFactory;
            _testEnvironment = testEnvironment;
        }

        internal List<TestCaseLocation> ResolveAllTestCases(string executable, List<string> testMethodSignatures, string symbolFilterString, string pathExtension)
        {
            List<TestCaseLocation> testCaseLocationsFound =
                FindTestCaseLocationsInBinary(executable, testMethodSignatures, symbolFilterString, pathExtension).ToList();

            if (testCaseLocationsFound.Count == 0)
            {
                List<string> imports = PeParser.ParseImports(executable, _testEnvironment);

                string moduleDirectory = Path.GetDirectoryName(executable);

                foreach (string import in imports)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    string importedBinary = Path.Combine(moduleDirectory, import);
                    if (File.Exists(importedBinary))
                    {
                        testCaseLocationsFound.AddRange(FindTestCaseLocationsInBinary(importedBinary, testMethodSignatures, symbolFilterString, pathExtension));
                    }
                }
            }
            return testCaseLocationsFound;
        }


        private IEnumerable<TestCaseLocation> FindTestCaseLocationsInBinary(
            string binary, List<string> testMethodSignatures, string symbolFilterString, string pathExtension)
        {
            using (IDiaResolver diaResolver = _diaResolverFactory.Create(binary, pathExtension, _testEnvironment, _testEnvironment.Options.DebugMode))
            {
                try
                {
                    IList<SourceFileLocation> allTestMethodSymbols = diaResolver.GetFunctions(symbolFilterString);
                    IList<SourceFileLocation> allTraitSymbols = diaResolver.GetFunctions("*" + TraitAppendix);
                    _testEnvironment.DebugInfo($"Found {allTestMethodSymbols.Count} test method symbols and {allTraitSymbols.Count} trait symbols in binary {binary}");

                    return allTestMethodSymbols
                        .Where(nsfl => testMethodSignatures.Any(tms => Regex.IsMatch(nsfl.Symbol, tms))) // Contains() instead of == because nsfl might contain namespace
                        .Select(nsfl => ToTestCaseLocation(nsfl, allTraitSymbols))
                        .ToList(); // we need to force immediate query execution, otherwise our session object will already be released
                }
                catch (Exception e)
                {
                    if (_testEnvironment.Options.DebugMode)
                        _testEnvironment.LogError($"Exception while resolving test locations and traits in {binary}\n{e}");
                    return new TestCaseLocation[0];
                }
            }
        }

        private TestCaseLocation ToTestCaseLocation(SourceFileLocation location, IEnumerable<SourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = GetTraits(location, allTraitSymbols);
            var testCaseLocation = new TestCaseLocation(location.Symbol, location.Sourcefile, location.Line);
            testCaseLocation.Traits.AddRange(traits);
            return testCaseLocation;
        }

        private List<Trait> GetTraits(SourceFileLocation nativeSymbol, IEnumerable<SourceFileLocation> allTraitSymbols)
        {
            var traits = new List<Trait>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (SourceFileLocation nativeTraitSymbol in allTraitSymbols)
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

    }

}