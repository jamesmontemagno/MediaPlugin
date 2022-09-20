using System;
using System.IO;
using Foundation;
using System.Runtime.InteropServices;

namespace Plugin.Media
{
    class NSDataStream : Stream
    {
        NSData theData;
        uint pos;

        public NSDataStream(NSData data) =>
            theData = data;


        protected override void Dispose(bool disposing)
        {
            if (theData != null)
            {
                theData.Dispose();
                theData = null;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (pos >= theData.Length)
            {
                return 0;
            }
            else
            {
                var len = (int)Math.Min(count, (double)(theData.Length - pos));
#if NET6_0_OR_GREATER
                Marshal.Copy(new IntPtr(Convert.ToInt64(theData.Bytes) + pos), buffer, offset, len);
#else
                Marshal.Copy(new IntPtr(theData.Bytes.ToInt64() + pos), buffer, offset, len);
#endif
                pos += (uint)len;
                return len;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();


        public override void SetLength(long value) =>
            throw new NotSupportedException();


        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();


        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => (long)theData.Length;

        public override long Position
        {
            get => pos;
            set
            {
            }
        }
    }
}
