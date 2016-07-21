using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dia;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{
    internal class NativeSourceFileLocation
    {
        internal string Symbol;
        internal uint AddressSection;
        internal uint AddressOffset;
        internal uint Length;

        public override string ToString()
        {
            return Symbol;
        }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal interface IDiaSession
    {
        IDiaSymbol globalScope { get; }
        void findLinesByAddr(uint seg, uint offset, uint length, out IDiaEnumLineNumbers ppResult);
    }

    internal class DiaSessionAdapter : IDiaSession
    {
        private readonly IDiaSession140 _diaSession140;
        private readonly IDiaSession110 _diaSession110;

        public DiaSessionAdapter(IDiaSession140 diaSession)
        {
            _diaSession140 = diaSession;
        }
        public DiaSessionAdapter(IDiaSession110 diaSession)
        {
            _diaSession110 = diaSession;
        }

        public IDiaSymbol globalScope => _diaSession140?.globalScope ?? _diaSession110?.globalScope;

        public void findLinesByAddr(uint seg, uint offset, uint length, out IDiaEnumLineNumbers ppResult)
        {
            ppResult = null;
            _diaSession140?.findLinesByAddr(seg, offset, length, out ppResult);
            _diaSession110?.findLinesByAddr(seg, offset, length, out ppResult);
        }
    }

    internal sealed class DiaResolver : IDiaResolver
    {
        private static readonly Guid Dia140 = new Guid("e6756135-1e65-4d17-8576-610761398c3c");
        private static readonly Guid Dia120 = new Guid("3bfcea48-620f-4b6b-81f7-b9af75454c7d");
        private static readonly Guid Dia110 = new Guid("761D3BCD-1304-41D5-94E8-EAC54E4AC172");

        private readonly string _binary;
        private readonly ILogger _logger;
        private readonly bool _debugMode;
        private readonly Stream _fileStream;
        private readonly IDiaSession _diaSession;

        private IDiaDataSource _diaDataSource;

        private bool TryCreateDiaInstance(Guid clsid)
        {
            try
            {
                Type comType = Type.GetTypeFromCLSID(clsid);
                _diaDataSource = (IDiaDataSource)Activator.CreateInstance(comType);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal DiaResolver(string binary, string pathExtension, ILogger logger, bool debugMode)
        {
            _binary = binary;
            _logger = logger;
            _debugMode = debugMode;

            string pdb = FindPdbFile(binary, pathExtension);
            if (pdb == null)
            {
                _logger.LogWarning($"Couldn't find the .pdb file of file '{binary}'. You will not get any source locations for your tests.");
                return;
            }

            if (!TryCreateDiaInstance(Dia140) && !TryCreateDiaInstance(Dia120) && !TryCreateDiaInstance(Dia110))
            {
                _logger.LogError("Couldn't find the msdia.dll to parse *.pdb files. You will not get any source locations for your tests.");
                return;
            }

            if (_debugMode)
                _logger.LogInfo($"Parsing pdb file \"{pdb}\"");

            _fileStream = File.Open(pdb, FileMode.Open, FileAccess.Read, FileShare.Read);
            _diaDataSource.loadDataFromIStream(new DiaMemoryStream(_fileStream));

            dynamic diaSession110Or140;
            _diaDataSource.openSession(out diaSession110Or140);
            _diaSession = new DiaSessionAdapter(diaSession110Or140);
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }


        public IList<SourceFileLocation> GetFunctions(string symbolFilterString)
        {
            if (_diaDataSource == null) // Silently return when DIA failed to load
                return new SourceFileLocation[0];

            IDiaEnumSymbols diaSymbols = FindFunctionsByRegex(symbolFilterString);
            return GetSymbolNamesAndAddresses(diaSymbols).Select(ToSourceFileLocation).ToList();
        }

        private string FindPdbFile(string binary, string pathExtension)
        {
            IList<string> attempts = new List<string>();
            string pdb = PeParser.ExtractPdbPath(binary, _logger);
            if (pdb != null && File.Exists(pdb))
                return pdb;
            attempts.Add("parsing from executable");

            pdb = Path.ChangeExtension(binary, ".pdb");
            if (File.Exists(pdb))
                return pdb;
            attempts.Add($"\"{pdb}\"");

            pdb = Path.GetFileName(pdb);
            if (pdb == null || File.Exists(pdb))
                return pdb;
            attempts.Add($"\"{pdb}\"");

            string path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathExtension))
                path = $"{pathExtension};{path}";
            var pathElements = path?.Split(';');
            if (path != null)
            {
                foreach (string pathElement in pathElements)
                {
                    string file = Path.Combine(pathElement, pdb);
                    if (File.Exists(file))
                        return file;
                    attempts.Add($"\"{file}\"");
                }
            }

            if (_debugMode)
                _logger.LogInfo("Attempts to find pdb: " + string.Join("::", attempts));

            return null;
        }

        /// From given symbol enumeration, extract name, section, offset and length
        private IList<NativeSourceFileLocation> GetSymbolNamesAndAddresses(IDiaEnumSymbols diaSymbols)
        {
            var locations = new List<NativeSourceFileLocation>();
            foreach (IDiaSymbol diaSymbol in diaSymbols)
            {
                locations.Add(new NativeSourceFileLocation()
                {
                    Symbol = diaSymbol.name,
                    AddressSection = diaSymbol.addressSection,
                    AddressOffset = diaSymbol.addressOffset,
                    Length = (uint)diaSymbol.length
                });
            }
            return locations;
        }

        private SourceFileLocation ToSourceFileLocation(NativeSourceFileLocation nativeSymbol)
        {
            IDiaEnumLineNumbers lineNumbers = GetLineNumbers(nativeSymbol.AddressSection, nativeSymbol.AddressOffset, nativeSymbol.Length);
            if (lineNumbers.count <= 0)
            {
                _logger.LogWarning("Failed to locate line number for " + nativeSymbol);
                return new SourceFileLocation(_binary, "", 0);
            }

            SourceFileLocation result = null;
            foreach (IDiaLineNumber lineNumber in lineNumbers)
            {
                if (result == null)
                {
                    result = new SourceFileLocation(
                        nativeSymbol.Symbol, lineNumber.sourceFile.fileName,
                        lineNumber.lineNumber);
                    // do not break to make sure all lineNumbers are enumerated - not sure if this is necessary
                }
            }
            return result;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum NameSearchOptions : uint
        {
            NsNone = 0x0u,
            NsfCaseSensitive = 0x1u,
            NsfCaseInsensitive = 0x2u,
            NsfFNameExt = 0x4u,
            NsfRegularExpression = 0x8u,
            NsfUndecoratedName = 0x10u
        }

        private IDiaEnumSymbols FindFunctionsByRegex(string pattern)
        {
            IDiaEnumSymbols result;
            _diaSession.globalScope.findChildren(SymTagEnum.SymTagFunction, pattern, (uint)NameSearchOptions.NsfRegularExpression, out result);
            return result;
        }

        private IDiaEnumLineNumbers GetLineNumbers(uint addressSection, uint addressOffset, uint length)
        {
            IDiaEnumLineNumbers linenumbers;
            _diaSession.findLinesByAddr(addressSection, addressOffset, length, out linenumbers);
            return linenumbers;
        }

    }

}
