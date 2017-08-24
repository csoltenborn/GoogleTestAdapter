// This file has been modified by Microsoft on 8/2017.

using GoogleTestAdapter.Common;
using Microsoft.Dia;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

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

    internal sealed class DiaResolver : IDiaResolver
    {
        private readonly string _binary;
        private readonly ILogger _logger;
        private readonly Stream _fileStream;
        private readonly IDiaSession _diaSession;

        private IDiaDataSource _diaDataSource;

        private bool TryCreateDiaInstance()
        {
            try
            {
                _diaDataSource = DiaFactory.CreateInstance();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal DiaResolver(string binary, string pathExtension, ILogger logger)
        {
            _binary = binary;
            _logger = logger;

            string pdb = FindPdbFile(binary, pathExtension);
            if (pdb == null)
            {
                _logger.LogWarning(String.Format(Resources.PdbFileNotFoundError, binary));
                return;
            }

            if (!TryCreateDiaInstance())
            {
                _logger.LogError(Resources.msdiaError);
                return;
            }

            _logger.DebugInfo(String.Format(Resources.ParsingFile, pdb));

            _fileStream = File.Open(pdb, FileMode.Open, FileAccess.Read, FileShare.Read);
            _diaDataSource.loadDataFromIStream(new DiaMemoryStream(_fileStream));
            _diaDataSource.openSession(out _diaSession);
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
            if (diaSymbols == null)
                return new SourceFileLocation[0];
            return GetSymbolNamesAndAddresses(diaSymbols).Select(ToSourceFileLocation).ToList();
        }

        private string FindPdbFile(string binary, string pathExtension)
        {
            IList<string> attempts = new List<string>();
            string pdb = PeParser.ExtractPdbPath(binary, _logger);
            if (pdb != null && File.Exists(pdb))
                return pdb;
            attempts.Add($"\"{binary}\"");

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

            _logger.DebugInfo(String.Format(Resources.AttemptsToFind, string.Join("::", attempts)));

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
                _logger.LogWarning(String.Format(Resources.LineNumberError, nativeSymbol));
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
            IDiaEnumSymbols result = null;
            try
            {
                _diaSession.globalScope.findChildren(SymTagEnum.SymTagFunction, pattern, (uint)NameSearchOptions.NsfRegularExpression, out result);
            }
            catch (NotImplementedException)
            {
                // https://developercommunity.visualstudio.com/content/problem/4631/dia-sdk-still-doesnt-support-debugfastlink.html
                _logger.LogWarning(Resources.GenerateFullPDBMessage);
                _logger.LogWarning(Resources.UseLinkerOption);
            }
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
