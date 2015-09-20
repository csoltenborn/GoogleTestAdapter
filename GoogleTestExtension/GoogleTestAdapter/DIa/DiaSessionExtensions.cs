using System;
using System.Collections.Generic;
using Dia;

namespace GoogleTestAdapter.DIa
{
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
                NativeMethods.ReleaseCom(diaSymbol);
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