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

        // Matching each test against all symbols is quadratic in the number of tests, which makes discovery of
        // executables with many tests slow. Test method symbols are thus indexed by their test class name without
        // template arguments (see GetClassNameKey()), trait symbols by the test class they belong to. Values are
        // indices into the lists above, which keeps the order in which symbols are found.
        private readonly Dictionary<string, List<int>> _testMethodSymbolsByClassName = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        private readonly List<int> _unindexedTestMethodSymbols = new List<int>();
        private readonly Dictionary<string, List<SourceFileLocation>> _traitSymbolsByTestClass = new Dictionary<string, List<SourceFileLocation>>(StringComparer.Ordinal);
        // TearDownTestSuite() (and TearDownTestCase()) methods of fixtures by class name (see GetClassNameKey())
        private readonly Dictionary<string, List<SourceFileLocation>> _tearDownTestSuiteSymbolsByClassName = new Dictionary<string, List<SourceFileLocation>>(StringComparer.Ordinal);
        // locations of Google Test's empty default implementations, with which the linker folds empty methods of
        // fixtures (/OPT:ICF), which then have the location of the default implementation
        private readonly HashSet<string> _defaultTearDownTestSuiteLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                // without debug symbols, tests have neither traits nor namespaces, and source locations only if
                // Google Test provides them; without source locations, tests can neither be navigated to nor be
                // analyzed by e.g. Copilot
                if (binary == _executable)
                    _logger.LogWarning($"No .pdb file found for test executable '{binary}', tests will have neither traits nor namespaces, and source locations only with Google Test 1.8.1 or later (make sure the executable is built with debug information, e.g. linker option /DEBUG)");
                else
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
                    AddTestMethodSymbols(diaResolver.GetFunctions("*" + GoogleTestConstants.TestBodySignature));
                    AddTraitSymbols(diaResolver.GetFunctions("*" + TraitAppendix));
                    if (_settings.TearDownTestSuiteTestCases)
                    {
                        AddTearDownTestSuiteSymbols(diaResolver.GetFunctions("*::" + GoogleTestConstants.TearDownTestSuiteTestName));
                        AddTearDownTestSuiteSymbols(diaResolver.GetFunctions("*::" + GoogleTestConstants.TearDownTestCaseMethodName));
                    }
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

        private void AddTestMethodSymbols(IEnumerable<SourceFileLocation> symbols)
        {
            foreach (SourceFileLocation symbol in symbols)
            {
                int index = _allTestMethodSymbols.Count;
                _allTestMethodSymbols.Add(symbol);

                string key = GetClassNameKey(symbol.Symbol, symbol.Symbol.Length - GoogleTestConstants.TestBodySignature.Length);
                if (key == null)
                {
                    _unindexedTestMethodSymbols.Add(index);
                }
                else
                {
                    if (!_testMethodSymbolsByClassName.TryGetValue(key, out var indices))
                        _testMethodSymbolsByClassName.Add(key, indices = new List<int>());
                    indices.Add(index);
                }
            }
        }

        private void AddTraitSymbols(IEnumerable<SourceFileLocation> symbols)
        {
            foreach (SourceFileLocation symbol in symbols)
            {
                _allTraitSymbols.Add(symbol);
                if (symbol.TestClassSignature == null)
                    continue;

                if (!_traitSymbolsByTestClass.TryGetValue(symbol.TestClassSignature, out var traitSymbols))
                    _traitSymbolsByTestClass.Add(symbol.TestClassSignature, traitSymbols = new List<SourceFileLocation>());
                traitSymbols.Add(symbol);
            }
        }

        private void AddTearDownTestSuiteSymbols(IEnumerable<SourceFileLocation> symbols)
        {
            foreach (SourceFileLocation symbol in symbols)
            {
                // Google Test's empty default implementations of testing::Test
                if (symbol.Symbol.StartsWith("testing::", StringComparison.Ordinal))
                {
                    _defaultTearDownTestSuiteLocations.Add(GetLocationKey(symbol));
                    continue;
                }

                string key = GetClassNameKey(symbol.Symbol, symbol.Symbol.LastIndexOf("::", StringComparison.Ordinal));
                if (key == null)
                    continue;

                if (!_tearDownTestSuiteSymbolsByClassName.TryGetValue(key, out var tearDownSymbols))
                    _tearDownTestSuiteSymbolsByClassName.Add(key, tearDownSymbols = new List<SourceFileLocation>());
                tearDownSymbols.Add(symbol);
            }
        }

        /// <summary>
        /// The location of TearDownTestSuite() (or TearDownTestCase()) of the fixture of test suite
        /// <paramref name="suite"/>, which is the last part of the suite's name which is not a number (e.g. "Fixture" for
        /// suites "Fixture", "Fixture/0", "Prefix/Fixture", and "Prefix/Fixture/0"); if there are several fixtures of
        /// that name, the one in namespace <paramref name="testNamespace"/> (the namespace of the suite's tests) is
        /// preferred. Null if the fixture does not define the method (or no symbols are available); the location's
        /// source file is empty if the linker has folded the method with Google Test's empty default implementation.
        /// </summary>
        public TestCaseLocation FindTearDownTestSuiteLocation(string suite, string testNamespace)
        {
            string fixture = suite.Split('/').LastOrDefault(part => part.Length > 0 && !part.All(char.IsDigit));
            if (fixture == null || !_tearDownTestSuiteSymbolsByClassName.TryGetValue(fixture, out var symbols))
                return null;

            SourceFileLocation symbol = symbols.FirstOrDefault(s => GetNamespaceOfMethod(s.Symbol) == testNamespace) ?? symbols[0];
            TestCaseLocation location = _defaultTearDownTestSuiteLocations.Contains(GetLocationKey(symbol))
                ? new TestCaseLocation(symbol.Symbol, "", 0)
                : ToTestCaseLocation(symbol);
            location.Namespace = GetNamespaceOfMethod(symbol.Symbol);
            return location;
        }

        private static string GetLocationKey(SourceFileLocation location)
        {
            return $"{location.Sourcefile}:{location.Line}";
        }

        // e.g. "outer::inner" for "outer::inner::Fixture<int>::TearDownTestSuite"
        private static string GetNamespaceOfMethod(string symbol)
        {
            int endOfClass = symbol.LastIndexOf("::", StringComparison.Ordinal);
            string className = GetClassNameKey(symbol, endOfClass);
            if (className == null)
                return null;

            // as in GetClassNameKey(): skip the template arguments, which may contain "::"
            int end = endOfClass;
            if (symbol[end - 1] == '>')
            {
                int depth = 0;
                for (end--; end >= 0; end--)
                {
                    if (symbol[end] == '>')
                        depth++;
                    else if (symbol[end] == '<' && --depth == 0)
                        break;
                }
            }
            return GetNamespace(symbol.Substring(0, end - className.Length));
        }

        /// <summary>
        /// The name of the class containing the method ending at <paramref name="endOfClass"/>, without namespaces and
        /// template arguments, e.g. "Suite_Test_Test" for "ns::Suite_Test_Test::TestBody" and
        /// "Suite_Test_Test&lt;int&gt;::TestBody", and "Test" for "gtest_suite_Suite_::Test&lt;int&gt;::TestBody";
        /// null if the symbol can not be parsed
        /// </summary>
        public static string GetClassNameKey(string symbol, int endOfClass)
        {
            if (endOfClass <= 0 || endOfClass > symbol.Length)
                return null;

            int end = endOfClass;
            if (symbol[end - 1] == '>')
            {
                int depth = 0;
                int i = end - 1;
                for (; i >= 0; i--)
                {
                    if (symbol[i] == '>')
                        depth++;
                    else if (symbol[i] == '<' && --depth == 0)
                        break;
                }
                if (i <= 0)
                    return null;
                end = i;
            }

            int start = symbol.LastIndexOf("::", end - 1, end, StringComparison.Ordinal);
            start = start < 0 ? 0 : start + 2;
            return end > start ? symbol.Substring(start, end - start) : null;
        }

        // the key of the symbols matching the signature, see GetClassNameKey(string, int)
        private static string GetClassNameKey(MethodSignature methodSignature)
        {
            string signature = methodSignature.Signature;
            if (!methodSignature.IsRegex)
                return GetClassNameKey(signature, signature.Length - GoogleTestConstants.TestBodySignature.Length);

            // the regex signatures of typed tests contain the type parameter as regex "<.+>"
            int endOfClass = signature.IndexOf('<');
            if (endOfClass <= 0)
                return null;
            int start = signature.LastIndexOf("::", endOfClass - 1, endOfClass, StringComparison.Ordinal);
            start = start < 0 ? 0 : start + 2;
            return signature.Substring(start, endOfClass - start);
        }

        // symbols which might match one of the signatures, in the order they have been found
        private IEnumerable<SourceFileLocation> GetCandidateSymbols(List<MethodSignature> testMethodSignatures)
        {
            var indices = new SortedSet<int>(_unindexedTestMethodSymbols);
            foreach (MethodSignature methodSignature in testMethodSignatures)
            {
                string key = GetClassNameKey(methodSignature);
                if (key == null)
                    return _allTestMethodSymbols;
                if (_testMethodSymbolsByClassName.TryGetValue(key, out var symbolIndices))
                    indices.UnionWith(symbolIndices);
            }
            return indices.Select(i => _allTestMethodSymbols[i]);
        }

        private TestCaseLocation DoFindTestCaseLocation(List<MethodSignature> testMethodSignatures)
        {
            foreach (SourceFileLocation sourceFileLocation in GetCandidateSymbols(testMethodSignatures))
            {
                foreach (MethodSignature methodSignature in testMethodSignatures)
                {
                    string namespaces = GetMatch(sourceFileLocation, methodSignature);
                    if (namespaces != null)
                    {
                        TestCaseLocation testCaseLocation = ToTestCaseLocation(sourceFileLocation);
                        testCaseLocation.Namespace = GetNamespace(namespaces);
                        return testCaseLocation;
                    }
                }
            }
            return null;
        }

        private const string NamespaceGroup = "namespace";
        private const string AnonymousNamespaceSymbol = "`anonymous namespace'";
        public const string AnonymousNamespace = "(anonymous namespace)";
        // namespaces are C++ identifiers, which may contain characters \w does not match (see StreamingListTestsParser);
        // "<" and ">" are excluded, since enclosing template classes are not namespaces
        private const string NamespacesPattern = @"(?:(?:(?:[^\s:<>`']+)|(?:" + AnonymousNamespaceSymbol + "))::)*";

        private static readonly Regex NamespacesRegex = new Regex($"^{NamespacesPattern}$", RegexOptions.Compiled);

        /// <returns>The namespace part of the symbol (e.g. "outer::`anonymous namespace'::") if the symbol is the
        /// signature preceded by namespaces, null otherwise</returns>
        private static string GetMatch(SourceFileLocation sourceFileLocation, MethodSignature methodSignature)
        {
            string symbol = sourceFileLocation.Symbol;
            string signature = methodSignature.Signature;

            if (methodSignature.IsRegex)
            {
                if (!Regex.IsMatch(symbol, signature))
                    return null;

                Match match = Regex.Match(symbol, $"^(?<{NamespaceGroup}>{NamespacesPattern}){signature}");
                return match.Success ? match.Groups[NamespaceGroup].Value : null;
            }

            // same as the regex above, but without creating a regex per signature (which is expensive for executables
            // with many tests); the regex's greedy namespace group matches the last possible occurrence
            var occurrences = new List<int>();
            for (int index = symbol.IndexOf(signature, StringComparison.Ordinal); index >= 0;
                 index = symbol.IndexOf(signature, index + 1, StringComparison.Ordinal))
            {
                occurrences.Add(index);
            }
            for (int i = occurrences.Count - 1; i >= 0; i--)
            {
                string namespaces = symbol.Substring(0, occurrences[i]);
                if (NamespacesRegex.IsMatch(namespaces))
                    return namespaces;
            }
            return null;
        }

        // MSVC's internal name of an anonymous namespace, used by DIA e.g. for an anonymous namespace containing another one
        private static readonly Regex InternalAnonymousNamespaceRegex = new Regex(@"^A0x[0-9a-fA-F]{8}$");

        // e.g. "outer::`anonymous namespace'::" => "outer::(anonymous namespace)"
        private static string GetNamespace(string ns)
        {
            if (ns.Length == 0)
                return ns;

            var parts = ns.Substring(0, ns.Length - 2)
                .Split(new[] { "::" }, StringSplitOptions.None)
                .Select(part => part == AnonymousNamespaceSymbol || InternalAnonymousNamespaceRegex.IsMatch(part)
                    ? AnonymousNamespace
                    : part);
            return string.Join("::", parts);
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
            if (nativeSymbol.TestClassSignature == null
                || !_traitSymbolsByTestClass.TryGetValue(nativeSymbol.TestClassSignature, out var traitSymbols))
                return traits;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (SourceFileLocation nativeTraitSymbol in traitSymbols)
            {
                int lengthOfSerializedTrait = nativeTraitSymbol.Symbol.Length - nativeTraitSymbol.IndexOfSerializedTrait - TraitAppendix.Length;
                string serializedTrait = nativeTraitSymbol.Symbol.Substring(nativeTraitSymbol.IndexOfSerializedTrait, lengthOfSerializedTrait);
                string[] data = serializedTrait.Split(new[] { TraitSeparator }, StringSplitOptions.None);
                traits.Add(new Trait(data[0], data[1]));
            }

            return traits;
        }

    }

}