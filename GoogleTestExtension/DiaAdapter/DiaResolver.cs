using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Dia;

namespace DiaAdapter
{
    public sealed class DiaResolver : IDisposable
    {
        private string Binary { get; }

        private Stream FileStream { get; }
        private DiaSourceClass DiaSourceClass { get; set; }
        private IDiaSession DiaSession { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();


        public DiaResolver(string binary)
        {
            Binary = binary;

            DiaSourceClass = new DiaSourceClass();

            string pdb = Path.ChangeExtension(binary, ".pdb");
            FileStream = File.Open(pdb, FileMode.Open, FileAccess.Read, FileShare.Read);
            IStream memoryStream = new DiaMemoryStream(FileStream);
            DiaSourceClass.loadDataFromIStream(memoryStream);

            IDiaSession diaSession;
            DiaSourceClass.openSession(out diaSession);
            DiaSession = diaSession;
        }

        ~DiaResolver()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isCalledFromDispose)
        {
            if (DiaSession != null)
            {
                NativeMethods.ReleaseCom(DiaSession);
                DiaSession = null;
            }
            if (DiaSourceClass != null)
            {
                NativeMethods.ReleaseCom(DiaSourceClass);
                DiaSourceClass = null;
            }
            if (isCalledFromDispose)
            {
                FileStream.Dispose();
            }
        }


        public IEnumerable<SourceFileLocation> GetFunctions(string symbolFilterString)
        {
            IDiaEnumSymbols diaSymbols = FindFunctionsByRegex(symbolFilterString);
            try
            {
                return GetSymbolNamesAndAddresses(diaSymbols).Select(ToSourceFileLocation);
            }
            finally
            {
                NativeMethods.ReleaseCom(diaSymbols);
            }
        }


        /// From given symbol enumeration, extract name, section, offset and length
        private List<NativeSourceFileLocation> GetSymbolNamesAndAddresses(IDiaEnumSymbols diaSymbols)
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

        private SourceFileLocation ToSourceFileLocation(NativeSourceFileLocation nativeSymbol)
        {
            IDiaEnumLineNumbers lineNumbers = GetLineNumbers(nativeSymbol.AddressSection, nativeSymbol.AddressOffset, nativeSymbol.Length);
            try
            {
                if (lineNumbers.count > 0)
                {
                    SourceFileLocation result = null;
                    foreach (IDiaLineNumber lineNumber in lineNumbers)
                    {
                        if (result == null)
                        {
                            result = new SourceFileLocation(
                                nativeSymbol.Symbol, lineNumber.sourceFile.fileName,
                                lineNumber.lineNumber);
                        }
                        NativeMethods.ReleaseCom(lineNumber);
                    }
                    return result;
                }
                else
                {
                    ErrorMessages.Add("Failed to locate line number for " + nativeSymbol);
                    return new SourceFileLocation(Binary, "", 0);
                }
            }
            finally
            {
                NativeMethods.ReleaseCom(lineNumbers);
            }
        }

        private IDiaEnumSymbols FindFunctionsByRegex(string pattern)
        {
            IDiaEnumSymbols result;
            DiaSession.globalScope.findChildren(SymTagEnum.SymTagFunction, pattern, (uint)NameSearchOptions.NsfRegularExpression, out result);
            return result;
        }

        private IDiaEnumLineNumbers GetLineNumbers(uint addressSection, uint addressOffset, uint length)
        {
            IDiaEnumLineNumbers linenumbers;
            DiaSession.findLinesByAddr(addressSection, addressOffset, length, out linenumbers);
            return linenumbers;
        }

    }

}