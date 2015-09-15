using System;
using System.Collections.Generic;
using System.Linq;
using Dia;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{

    enum NameSearchOptions : uint
    {
        nsNone = 0x0u,
        nsfCaseSensitive = 0x1u,
        nsfCaseInsensitive = 0x2u,
        nsfFNameExt = 0x4u,
        nsfRegularExpression = 0x8u,
        nsfUndecoratedName = 0x10u
    }

    public static class AllKindsOfExtensions
    {

        public static TestCase FindTestcase(this IEnumerable<TestCase> testcases, string qualifiedName)
        {
            return testcases.FirstOrDefault(testcase => testcase.FullyQualifiedName.Split(' ')[0] == qualifiedName);
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static string AppendIfNotEmpty(this string theString, string appendix)
        {
            return string.IsNullOrWhiteSpace(theString) ? theString : theString + appendix;
        }

    }

    public static class IDiaSessionExtensions
    {
        /// Find all symbols from session's global scope which are tagged as functions
        public static IDiaEnumSymbols FindFunctions(this IDiaSession session)
        {
            IDiaEnumSymbols result;
            session.findChildren(session.globalScope, SymTagEnum.SymTagFunction, null, (uint)NameSearchOptions.nsNone, out result);
            return result;
        }

        /// Find all symbols matching from session's global scope which are tagged as functions
        public static IDiaEnumSymbols FindFunctionsByRegex(this IDiaSession session, string pattern)
        {
            IDiaEnumSymbols result;
            session.globalScope.findChildren(SymTagEnum.SymTagFunction, pattern, (uint)NameSearchOptions.nsfRegularExpression, out result);
            return result;
        }

        /// From given symbol enumeration, extract name, section, offset and length
        public static List<NativeSourceFileLocation> GetSymbolNamesAndAddresses(this IDiaSession session, IDiaEnumSymbols diaSymbols)
        {
            List<NativeSourceFileLocation> locations = new List<NativeSourceFileLocation>();
            foreach (IDiaSymbol diaSymbol in diaSymbols)
            {
                locations.Add(new NativeSourceFileLocation()
                {
                    symbol = diaSymbol.name,
                    addressSection = diaSymbol.addressSection,
                    addressOffset = diaSymbol.addressOffset,
                    length = (UInt32)diaSymbol.length
                });
                Native.ReleaseCom(diaSymbol);
            }
            return locations;
        }

        public class Location
        {
            public string sourcefile;
            public uint linenumner;
        }

        public static IDiaEnumLineNumbers GetLineNumbers(this IDiaSession session, uint addressSection, uint addressOffset, uint length)
        {
            IDiaEnumLineNumbers linenumbers;
            session.findLinesByAddr(addressSection, addressOffset, length, out linenumbers);
            return linenumbers;
        }

    }

}