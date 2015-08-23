using System;
using System.Collections.Generic;
using Dia;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{

    public class ConsoleLogger : IMessageLogger
    {
        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            Console.WriteLine(testMessageLevel.ToString() + ": " + message);
        }
    }

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
            foreach (TestCase testcase in testcases)
            {
                if (testcase.FullyQualifiedName.Split(' ')[0] == qualifiedName)
                {
                    return testcase;
                }
            }
            return null;
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
            List<Location> locations = new List<Location>();
            IDiaEnumLineNumbers linenumbers;
            session.findLinesByAddr(addressSection, addressOffset, length, out linenumbers);
            if (linenumbers.count > 0)
            {
                foreach (IDiaLineNumber linenumber in linenumbers)
                {
                    locations.Add(new Location()
                    {
                        sourcefile = linenumber.sourceFile.fileName,
                        linenumner = linenumber.lineNumber
                    });
                    Native.ReleaseCom(linenumber);
                }
            }
            return linenumbers;
        }

    }

    public static class DebugUtils
    {

        public static void CheckDebugModeForExecutionCode(IMessageLogger logger = null)
        {
            CheckDebugMode("Test execution code", logger);
        }

        public static void CheckDebugModeForDiscoverageCode(IMessageLogger logger = null)
        {
            CheckDebugMode("Test discoverage code", logger);
        }

        private static void CheckDebugMode(string codeType, IMessageLogger logger = null)
        {
            if (Constants.DEBUG_MODE)
            {
                string Message = codeType + " is running on the process with id " + Process.GetCurrentProcess().Id;
                if (logger != null)
                {
                    logger.SendMessage(TestMessageLevel.Informational, Message);
                }
                if (!Constants.UNIT_TEST_MODE)
                {
                    MessageBox.Show(Message + ". Attach debugger if necessary, then click ok.",
                        "Attach debugger", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                }
            }
        }

    }

}
