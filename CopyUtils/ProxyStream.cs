using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CopyUtils
{
    public class ProxyStream : Stream
    {
        #region Members

        protected Stream _stream = null;
        protected bool _isDisposed = false;

        #endregion

        #region Constructors

        public ProxyStream(Stream stream)
        {
            this._stream = stream;
        }

        #endregion

        #region Stream Properties

        public override bool CanRead
        {
            get { return (false == this._isDisposed) && this._stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return (false == this._isDisposed) && this._stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return (false == this._isDisposed) && this._stream.CanWrite; }
        }

        public override long Position
        {
            get
            {
                return this.GetPosition();
            }
            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public override long Length
        {
            get { return this.GetLength(); }
        }

        #endregion

        #region Stream Methods

        public override void Flush()
        {
            this._stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ValidateReadable();

            return this._stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ValidateSeekable();

            return this._stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.ValidateWritable();

            this._stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.ValidateWritable();

            this._stream.Write(buffer, offset, count);
        }

        #endregion

        #region Protected Methods

        protected virtual long GetLength()
        {
            return this._stream.Length;
        }

        protected virtual long GetPosition()
        {
            return this._stream.Position;
        }

        protected void ValidateReadable()
        {
            if (false == this.CanRead)
            {
                throw new NotSupportedException("Read not supported, stream is not readable.");
            }
        }

        protected void ValidateSeekable()
        {
            if (false == this.CanSeek)
            {
                throw new NotSupportedException("Seek not supported, stream is not seekable.");
            }
        }

        protected void ValidateWritable()
        {
            if (false == this.CanWrite)
            {
                throw new NotSupportedException("Stream is not writable.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            this._isDisposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}
