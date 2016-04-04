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

    internal sealed class DiaResolver : IDiaResolver
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

        internal DiaResolver(string binary, string pathExtension)
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


        private class SectionHeader
        {
            public long VirtualAddress;
            public long PointerToRawData;
            public long SizeOfRawData;
        }

        private long getRealAddress(long VirtualAdress, SectionHeader[] sections)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                if (VirtualAdress > sections[i].VirtualAddress &&
                    VirtualAdress < sections[i].VirtualAddress + sections[i].SizeOfRawData)
                {
                    return VirtualAdress - sections[i].VirtualAddress + sections[i].PointerToRawData;
                }
            }

            return 0;
        }

        private int readDWORD(FileStream stream)
        {
            byte[] buffer = new byte[4];

            if (stream.Read(buffer, 0, 4) != 4)
                return 0;

            // seems to be network (big endian) byte order
            return (buffer[3] << 24) + (buffer[2] << 16) +
                (buffer[1] << 8) + buffer[0];
        }

        private int readWORD(FileStream stream)
        {
            byte[] buffer = new byte[2];

            if (stream.Read(buffer, 0, 2) != 2)
                return 0;

            // seems to be network (big endian) byte order
            return (buffer[1] << 8) + buffer[0];
        }

        // Most windows executables contain the path to their PDB
        // in the header. This should be the most stable way to
        // determine the location and the name of the PDB.
        // 
        // This is inspired by 
        // https://deplinenoise.wordpress.com/2013/06/14/getting-your-pdb-name-from-a-running-executable-windows/
        //
        // In contrast to this blog, the virtual addresses must 
        // be converted to an offset in the executable and 32bit and
        // 64bit executables must be treated diffently
        public string ExtractPdbPath(string binary)
        {
            const int offsetof_NumberOfSections = 2;
            const int sizeof_IMAGE_NT_HEADERS32 = 248;
            const int sizeof_IMAGE_NT_HEADERS64 = 264;
            int sizeof_IMAGE_NT_HEADERS = sizeof_IMAGE_NT_HEADERS32;
            const int sizeof_IMAGE_SECTION_HEADER = 40;

            const int offsetof_DataDirectory32 = 96;
            const int offsetof_DataDirectory64 = 112;
            int offsetof_DataDirectory = offsetof_DataDirectory32;

            const int sizeof_IMAGE_DATA_DIRECTORY = 8;

            FileStream executable = File.Open(binary, FileMode.Open);

            // the offset to the PE is at offeset 60 (0x3c) and contains
            // four bytes
            executable.Seek(60, SeekOrigin.Begin);

            byte[] buffer = new byte[8];

            long peOffset = readDWORD(executable);
            if (peOffset == 0)
                return null;

            executable.Seek(peOffset, SeekOrigin.Begin);

            // read the next four bytes. They must be 'PE\0\0'
            if (executable.Read(buffer, 0, 4) != 4)
                return null;

            if (buffer[0] != 'P' || buffer[1] != 'E' ||
                buffer[2] != 0 || buffer[3] != 0)
                return null;

            // get the magic in the optional header to determine whether
            // the binary is 32 bit or 64 bit.
            // get the magic
            executable.Seek(peOffset + 4 + 20, SeekOrigin.Begin);

            long magic = readWORD(executable);
            if (magic == 0)
                return null;
            
            // magic can be used to test if the executable is
            // a 32bit binary or a 64bit binary
            if (magic == 0x20b)
            {
                sizeof_IMAGE_NT_HEADERS = sizeof_IMAGE_NT_HEADERS64;
                offsetof_DataDirectory = offsetof_DataDirectory64;
            }

            // get the number of sections in this executable,
            // to map virtual addresses to offsets in the binary
            // the table of sections is required.
            executable.Seek(peOffset + 4 + offsetof_NumberOfSections,
                            SeekOrigin.Begin);

            int numberOfSections = readWORD(executable);
            if (numberOfSections == 0)
                return null;

            SectionHeader[] sections = new SectionHeader[numberOfSections];
            for (int i = 0; i < numberOfSections; i++)
            {
                sections[i] = new SectionHeader();
                byte[] shBuffer = new byte[sizeof_IMAGE_SECTION_HEADER];
                executable.Seek(peOffset + sizeof_IMAGE_NT_HEADERS +
                     i * sizeof_IMAGE_SECTION_HEADER, SeekOrigin.Begin);

                if (executable.Read(shBuffer,0,40) != 40)
                    return null;

                // offsetof VirtualAdress: 12;
                sections[i].VirtualAddress = (shBuffer[15] << 24) +
                    (shBuffer[14] << 16) +(shBuffer[13] << 8) + shBuffer[12];
                // offsetof SizeOfRawData: 16;
                sections[i].SizeOfRawData = (shBuffer[19] << 24) +
                    (shBuffer[18] << 16) + (shBuffer[17] << 8) + shBuffer[16];
                // offsetof PointerToRawData: 20;
                sections[i].PointerToRawData = (shBuffer[23] << 24) +
                    (shBuffer[22] << 16) + (shBuffer[21] << 8) + shBuffer[20];

            }

            // get the size of the optional header
            executable.Seek(peOffset + 4 + 16, SeekOrigin.Begin);
            if (executable.Read(buffer, 0, 2) != 2)
                return null;

            long sizeOfOptionalHeader = (buffer[1] << 8) + buffer[0];

            if (sizeOfOptionalHeader == 0)
                return null;

            // the DEBUG_DATA_DIRECTORY is at index 7 in the
            // DataDirectory member of the optional header
            int offset = offsetof_DataDirectory + 6 * sizeof_IMAGE_DATA_DIRECTORY;

            executable.Seek(peOffset + 4 + 20 + offset, SeekOrigin.Begin);
            if (executable.Read(buffer, 0, 8) != 8)
                return null;

            // get the size and the address of the DEBUG_DATA_DIRECTORY
            long tableSize = (buffer[7] << 24) + (buffer[6] << 16) +
                (buffer[5] << 8) + buffer[4];
            long tableOffset = (buffer[3] << 24) + (buffer[2] << 16) +
                (buffer[1] << 8) + buffer[0];

            tableOffset = getRealAddress(tableOffset, sections);

            // get the AddressOfRawData member of the 
            // DEBUG_DATA_DIRECTORY
            executable.Seek(tableOffset + 20, SeekOrigin.Begin);

            long AddressOfRawData = readDWORD(executable);
            if (AddressOfRawData == 0)
                return null;

            AddressOfRawData = getRealAddress(AddressOfRawData, sections);

            // the pointer to the pdb path starts at offset 24

            string pdbPath = "";

            executable.Seek(AddressOfRawData + 24, SeekOrigin.Begin);
            if (executable.Read(buffer, 0, 1) != 1)
                return null;

            while (buffer[0] != 0)
            {
                pdbPath += Convert.ToChar(buffer[0]);

                if (executable.Read(buffer, 0, 1) != 1)
                    return null;
            }

            executable.Close();

            return pdbPath;
        }

        private string FindPdbFile(string binary, string pathExtension)
        {
            string pdb = ExtractPdbPath(binary);
            if (pdb != null && File.Exists(pdb))
                return pdb;

            pdb = Path.ChangeExtension(binary, ".pdb");
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
