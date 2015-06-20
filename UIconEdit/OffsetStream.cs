﻿#region BSD license
/*
Copyright © 2015, KimikoMuffin.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.
3. The names of its contributors may not be used to endorse or promote 
   products derived from this software without specific prior written 
   permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using System;
using System.IO;

namespace UIconEdit
{
    internal class OffsetStream : Stream
    {
        private bool _leaveOpen;
        private Stream _stream;

        private int _offset, _length, _remainingLength;

        internal OffsetStream(Stream stream, int offset, int length, bool leaveOpen)
        {
            _stream = stream;
            _offset = offset;
            _leaveOpen = leaveOpen;
            try
            {
                _length = _remainingLength = (int)Math.Min(length, stream.Length - (_offset + stream.Position));
            }
            catch
            {
                _length = _remainingLength = length;
            }
        }

        public override bool CanRead
        {
            get { return _stream != null; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get
            {
                if (_stream == null) throw new ObjectDisposedException(null);
                return _stream.Position - _offset;
            }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] array, int offset, int count)
        {
            if (_stream == null) throw new ObjectDisposedException(null);
            if (array == null) throw new ArgumentNullException("buffer");
            new ArraySegment<byte>(array, offset, count);

            int readBytes = _stream.Read(array, offset, Math.Min(count, _remainingLength));
            if (readBytes == 0)
                _remainingLength = 0;
            else
                _remainingLength -= readBytes;
            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !_leaveOpen)
                    _stream.Dispose();
                _stream = null;
                _leaveOpen = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public void CopyTo(BinaryWriter writer, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];

            int read = Read(buffer, 0, bufferSize);

            while (read != 0)
            {
                writer.Write(buffer, 0, read);
                read = Read(buffer, 0, bufferSize);
            }
        }

        public void CopyTo(BinaryWriter writer)
        {
            CopyTo(writer, 8192);
        }
    }
}
