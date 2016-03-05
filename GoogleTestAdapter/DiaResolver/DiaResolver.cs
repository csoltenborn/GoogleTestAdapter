using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Dia;
// ReSharper disable InconsistentNaming

namespace GoogleTestAdapter.DiaResolver
{
    internal interface IDiaSession
    {
        IDiaSymbol globalScope { get; }
        void findLinesByAddr(uint seg, uint offset, uint length, out IDiaEnumLineNumbers ppResult);
    }

    internal class IDiaSessionAdapter : IDiaSession
    {
        private readonly IDiaSession140 DiaSession140;
        private readonly IDiaSession110 DiaSession110;

        public IDiaSessionAdapter(IDiaSession140 diaSession)
        {
            DiaSession140 = diaSession;
        }
        public IDiaSessionAdapter(IDiaSession110 diaSession)
        {
            DiaSession110 = diaSession;
        }

        public IDiaSymbol globalScope => DiaSession140?.globalScope ?? DiaSession110?.globalScope;

        public void findLinesByAddr(uint seg, uint offset, uint length, out IDiaEnumLineNumbers ppResult)
        {
            ppResult = null;
            DiaSession140?.findLinesByAddr(seg, offset, length, out ppResult);
            DiaSession110?.findLinesByAddr(seg, offset, length, out ppResult);
        }
    }

    public sealed class DiaResolver
    {
        private static readonly Guid Dia140 = new Guid("e6756135-1e65-4d17-8576-610761398c3c");
        private static readonly Guid Dia120 = new Guid("3bfcea48-620f-4b6b-81f7-b9af75454c7d");
        private static readonly Guid Dia110 = new Guid("761D3BCD-1304-41D5-94E8-EAC54E4AC172");

        private string Binary { get; }

        private Stream FileStream { get; }
        private IDiaDataSource DiaDataSource { get; set; }
        private IDiaSession DiaSession { get; }

        public List<string> ErrorMessages { get; } = new List<string>();

        private bool TryCreateDiaInstance(Guid clsid)
        {
            try
            {
                Type comType = Type.GetTypeFromCLSID(clsid);
                DiaDataSource = (IDiaDataSource)Activator.CreateInstance(comType);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public DiaResolver(string binary, string pathExtension)
        {
            Binary = binary;

            if (!TryCreateDiaInstance(Dia140) && !TryCreateDiaInstance(Dia120) && !TryCreateDiaInstance(Dia110))
            {
                ErrorMessages.Add("Couldn't find the msdia.dll to parse *.pdb files. You will not get any source locations for your tests.");
                return;
            }

            string pdb = FindPdbFile(binary, pathExtension);
            if (pdb == null)
            {
                ErrorMessages.Add($"Couldn't find the .pdb file of file '{binary}'. You will not get any source locations for your tests.");
                return;
            }

            FileStream = File.Open(pdb, FileMode.Open, FileAccess.Read, FileShare.Read);
            DiaDataSource.loadDataFromIStream(new DiaMemoryStream(FileStream));

            dynamic diaSession110or140;
            DiaDataSource.openSession(out diaSession110or140);
            DiaSession = new IDiaSessionAdapter(diaSession110or140);
        }

        public void Dispose()
        {
            FileStream?.Dispose();
        }


        public IEnumerable<SourceFileLocation> GetFunctions(string symbolFilterString)
        {
            if (DiaDataSource == null) // Silently return when DIA failed to load
                return new SourceFileLocation[0];

            IDiaEnumSymbols diaSymbols = FindFunctionsByRegex(symbolFilterString);
            return GetSymbolNamesAndAddresses(diaSymbols).Select(ToSourceFileLocation);
        }


        private string FindPdbFile(string binary, string pathExtension)
        {
            string pdb = Path.ChangeExtension(binary, ".pdb");
            if (File.Exists(pdb))
                return pdb;

            pdb = Path.GetFileName(pdb);
            if (pdb == null || File.Exists(pdb))
                return pdb;

            string path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathExtension))
                path = $"{pathExtension};{path}";
            var pathElements = path?.Split(';');
            return pathElements?.Select(pe => Path.Combine(pe, pdb)).FirstOrDefault(File.Exists);
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
            }
            return locations;
        }

        private SourceFileLocation ToSourceFileLocation(NativeSourceFileLocation nativeSymbol)
        {
            IDiaEnumLineNumbers lineNumbers = GetLineNumbers(nativeSymbol.AddressSection, nativeSymbol.AddressOffset, nativeSymbol.Length);
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
                }
                return result;
            }
            else
            {
                ErrorMessages.Add("Failed to locate line number for " + nativeSymbol);
                return new SourceFileLocation(Binary, "", 0);
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