using System;
using System.IO;
using System.Runtime.InteropServices;
using Dia;

namespace GoogleTestAdapter.Helpers
{
    class DiaMemoryStream : IStream
    {
        public DiaMemoryStream(Stream pdbFile)
        {
            this.pdbFile = pdbFile;
        }

        private Stream pdbFile;

        unsafe void IStream.RemoteRead(out byte buffer, uint bufferSize, out uint bytesRead)
        {
            fixed (byte* addressOfBuffer = &buffer)
            {
                for(bytesRead = 0; bytesRead < bufferSize; bytesRead++)
                {
                    var nextByte = pdbFile.ReadByte();
                    if (nextByte == -1)
                        return;
                    addressOfBuffer[bytesRead] = (byte)nextByte;
                }
            }
        }

        void IStream.RemoteSeek(_LARGE_INTEGER offset, uint seekOrigin, out _ULARGE_INTEGER newPosition)
        {
            newPosition.QuadPart = (ulong)pdbFile.Seek(offset.QuadPart, (SeekOrigin)seekOrigin);
        }

        void IStream.Stat(out tagSTATSTG pstatstg, uint grfStatFlag)
        {
            pstatstg = new tagSTATSTG();
            pstatstg.cbSize.QuadPart = (ulong)pdbFile.Length;
        }




        void IStream.Clone(out IStream ppstm)
        {
            throw new NotImplementedException();
        }

        void IStream.Commit(uint grfCommitFlags)
        {
            throw new NotImplementedException();
        }

        void IStream.LockRegion(_ULARGE_INTEGER libOffset, _ULARGE_INTEGER cb, uint dwLockType)
        {
            throw new NotImplementedException();
        }

        void IStream.RemoteCopyTo(IStream pstm, _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten)
        {
            throw new NotImplementedException();
        }

        void ISequentialStream.RemoteRead(out byte pv, uint cb, out uint pcbRead)
        {
            throw new NotImplementedException();
        }

        void ISequentialStream.RemoteWrite(ref byte pv, uint cb, out uint pcbWritten)
        {
            throw new NotImplementedException();
        }

        void IStream.RemoteWrite(ref byte pv, uint cb, out uint pcbWritten)
        {
            throw new NotImplementedException();
        }

        void IStream.Revert()
        {
            throw new NotImplementedException();
        }

        void IStream.SetSize(_ULARGE_INTEGER libNewSize)
        {
            throw new NotImplementedException();
        }

        void IStream.UnlockRegion(_ULARGE_INTEGER libOffset, _ULARGE_INTEGER cb, uint dwLockType)
        {
            throw new NotImplementedException();
        }
    }
}
