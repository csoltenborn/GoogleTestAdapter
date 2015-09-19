using System;
using System.Collections.Generic;
using System.Linq;
using Dia;
using GoogleTestAdapter.Discovery;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Helpers
{

    enum NameSearchOptions : uint
    {
        NsNone = 0x0u,
        NsfCaseSensitive = 0x1u,
        NsfCaseInsensitive = 0x2u,
        NsfFNameExt = 0x4u,
        NsfRegularExpression = 0x8u,
        NsfUndecoratedName = 0x10u
    }

    static class AllKindsOfExtensions
    {

        internal static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        internal static TestCase FindTestcase(this IEnumerable<TestCase> testcases, string qualifiedName)
        {
            return testcases.FirstOrDefault(testcase => testcase.FullyQualifiedName.Split(' ')[0] == qualifiedName);
        }

        internal static IDictionary<string, List<TestCase>> GroupByExecutable(this IEnumerable<TestCase> testcases)
        {
            Dictionary<string, List<TestCase>> groupedTestCases = new Dictionary<string, List<TestCase>>();
            foreach (TestCase testCase in testcases)
            {
                List<TestCase> group;
                if (groupedTestCases.ContainsKey(testCase.Source))
                {
                    group = groupedTestCases[testCase.Source];
                }
                else
                {
                    group = new List<TestCase>();
                    groupedTestCases.Add(testCase.Source, group);
                }
                group.Add(testCase);
            }
            return groupedTestCases;
        }


    }

    static class StringExtensions
    {

        internal static string AppendIfNotEmpty(this string theString, string appendix)
        {
            return string.IsNullOrWhiteSpace(theString) ? theString : theString + appendix;
        }

    }

    static class DiaSessionExtensions
    {
        /// Find all symbols from session's global scope which are tagged as functions
        internal static IDiaEnumSymbols FindFunctions(this IDiaSession session)
        {
            IDiaEnumSymbols result;
            session.findChildren(session.globalScope, SymTagEnum.SymTagFunction, null, (uint)NameSearchOptions.NsNone, out result);
            return result;
        }

        /// Find all symbols matching from session's global scope which are tagged as functions
        internal static IDiaEnumSymbols FindFunctionsByRegex(this IDiaSession session, string pattern)
        {
            IDiaEnumSymbols result;
            session.globalScope.findChildren(SymTagEnum.SymTagFunction, pattern, (uint)NameSearchOptions.NsfRegularExpression, out result);
            return result;
        }

        /// From given symbol enumeration, extract name, section, offset and length
        internal static List<NativeSourceFileLocation> GetSymbolNamesAndAddresses(this IDiaSession session, IDiaEnumSymbols diaSymbols)
        {
            List<NativeSourceFileLocation> locations = new List<NativeSourceFileLocation>();
            foreach (IDiaSymbol diaSymbol in diaSymbols)
            {
                locations.Add(new NativeSourceFileLocation()
                {
                    Symbol = diaSymbol.name,
                    AddressSection = diaSymbol.addressSection,
                    AddressOffset = diaSymbol.addressOffset,
                    Length = (UInt32)diaSymbol.length
                });
                Native.ReleaseCom(diaSymbol);
            }
            return locations;
        }

        internal static IDiaEnumLineNumbers GetLineNumbers(this IDiaSession session, uint addressSection, uint addressOffset, uint length)
        {
            IDiaEnumLineNumbers linenumbers;
            session.findLinesByAddr(addressSection, addressOffset, length, out linenumbers);
            return linenumbers;
        }

    }

}