// This file has been modified by Microsoft on 6/2017.

using Microsoft.Dia;
using System;
using System.IO;

namespace GoogleTestAdapter.DiaResolver
{

    class DiaMemoryStream : StubMemoryStream
    {
        private readonly Stream _pdbFile;

        internal DiaMemoryStream(Stream pdbFile)
        {
            _pdbFile = pdbFile;
        }

        public override unsafe void RemoteRead(out byte buffer, uint bufferSize, out uint bytesRead)
        {
            fixed (byte* addressOfBuffer = &buffer)
            {
                for (bytesRead = 0; bytesRead < bufferSize; bytesRead++)
                {
                    var nextByte = _pdbFile.ReadByte();
                    if (nextByte == -1)
                        return;
                    addressOfBuffer[bytesRead] = (byte)nextByte;
                }
            }
        }

        public override void RemoteSeek(_LARGE_INTEGER offset, uint seekOrigin, out _ULARGE_INTEGER newPosition)
        {
            newPosition.QuadPart = (ulong)_pdbFile.Seek(offset.QuadPart, (SeekOrigin)seekOrigin);
        }

        public override void Stat(out tagSTATSTG pstatstg, uint grfStatFlag)
        {
            pstatstg = new tagSTATSTG
            {
                cbSize = new _ULARGE_INTEGER
                {
                    QuadPart = (ulong)_pdbFile.Length
                }
            };
        }

    }


    abstract class StubMemoryStream : IStream
    {
        public abstract void RemoteRead(out byte pv, uint cb, out uint pcbRead);
        public abstract void RemoteSeek(_LARGE_INTEGER dlibMove, uint dwOrigin, out _ULARGE_INTEGER plibNewPosition);
        public abstract void Stat(out tagSTATSTG pstatstg, uint grfStatFlag);

        void IStream.Clone(out IStream ppstm)
        {
            throw new NotImplementedException("IStream.Clone");
        }

        void IStream.Commit(uint grfCommitFlags)
        {
            throw new NotImplementedException("IStream.Commit");
        }

        void IStream.LockRegion(_ULARGE_INTEGER libOffset, _ULARGE_INTEGER cb, uint dwLockType)
        {
            throw new NotImplementedException("IStream.LockRegion");
        }

        void IStream.RemoteCopyTo(IStream pstm, _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten)
        {
            throw new NotImplementedException("IStream.RemoteCopyTo");
        }

        void ISequentialStream.RemoteRead(out byte pv, uint cb, out uint pcbRead)
        {
            throw new NotImplementedException("ISequentialStream.RemoteRead");
        }

        void ISequentialStream.RemoteWrite(ref byte pv, uint cb, out uint pcbWritten)
        {
            throw new NotImplementedException("ISequentialStream.RemoteWrite");
        }

        void IStream.RemoteWrite(ref byte pv, uint cb, out uint pcbWritten)
        {
            throw new NotImplementedException("IStream.RemoteWrite");
        }

        void IStream.Revert()
        {
            throw new NotImplementedException("IStream.Revert");
        }

        void IStream.SetSize(_ULARGE_INTEGER libNewSize)
        {
            throw new NotImplementedException("IStream.SetSize");
        }

        void IStream.UnlockRegion(_ULARGE_INTEGER libOffset, _ULARGE_INTEGER cb, uint dwLockType)
        {
            throw new NotImplementedException("IStream.UnlockRegion");
        }

    }

}