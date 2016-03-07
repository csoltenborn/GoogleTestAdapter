using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitSeparator = "__GTA__";
        private const string TraitAppendix = "_GTA_TRAIT";

        internal List<TestCaseLocation> ResolveAllTestCases(string executable, List<string> testMethodSignatures, string symbolFilterString, string pathExtension, List<string> errorMessages)
        {
            List<TestCaseLocation> testCaseLocationsFound =
                FindTestCaseLocationsInBinary(executable, testMethodSignatures, symbolFilterString, pathExtension, errorMessages).ToList();

            if (testCaseLocationsFound.Count == 0)
            {
                var parser = new NativeMethods.ImportsParser(executable, errorMessages);
                List<string> imports = parser.Imports;

                string moduleDirectory = Path.GetDirectoryName(executable);

                foreach (string import in imports)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    string importedBinary = Path.Combine(moduleDirectory, import);
                    if (File.Exists(importedBinary))
                    {
                        testCaseLocationsFound.AddRange(FindTestCaseLocationsInBinary(importedBinary, testMethodSignatures, symbolFilterString, pathExtension, errorMessages));
                    }
                }
            }
            return testCaseLocationsFound;
        }


        private IEnumerable<TestCaseLocation> FindTestCaseLocationsInBinary(
            string binary, List<string> testMethodSignatures, string symbolFilterString, string pathExtension, List<string> errorMessages)
        {
            var resolver = new DiaResolver.DiaResolver(binary, pathExtension);
            IEnumerable<SourceFileLocation> allTestMethodSymbols = resolver.GetFunctions(symbolFilterString);
            IEnumerable<SourceFileLocation> allTraitSymbols = resolver.GetFunctions("*" + TraitAppendix);

            IEnumerable<TestCaseLocation> result = allTestMethodSymbols
                .Where(nsfl => testMethodSignatures.Any(tms => nsfl.Symbol.Contains(tms))) // Contains() instead of == because nsfl might contain namespace
                .Select(nsfl => ToTestCaseLocation(nsfl, allTraitSymbols))
                .ToList(); // we need to force immediate query execution, otherwise our session object will already be released

            errorMessages.AddRange(resolver.ErrorMessages);
            resolver.Dispose();

            return result;
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