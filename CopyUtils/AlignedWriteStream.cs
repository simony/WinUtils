using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CopyUtils
{
    public class AlignedWriteStream : AlignedStreamBase
    {
        #region Members

        protected bool _canSeek = false;
        protected byte[] _copyBuffer = null;
        protected byte[] _clearBuffer = null;

        #endregion

        #region Constructors

        public AlignedWriteStream(Stream stream)
            : this(stream, DEFAULT_ALIGNMENT_SIZE)
        {
        }

        public AlignedWriteStream(Stream stream, int alignmentSize)
            : base(stream, alignmentSize)
        {
            if (false == stream.CanWrite)
            {
                throw new ArgumentException("Excepected a writable stream.", "stream");
            }
            this._canSeek = (stream.CanSeek) && (stream.CanRead);
            this._copyBuffer = new byte[this.AlignmentSize];
            this._clearBuffer = new byte[this.AlignmentSize];
        }

        #endregion

        #region Public Properties

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return this._canSeek; }
        }

        #endregion

        #region Public Methods

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.ValidateWritable();
            this.Validate(buffer, offset, count);
            if (0 == count)
            {
                return;
            }
            if (false == this.IsValueAligned(this.Position))
            {
                this.WriteUnaligned(buffer, ref offset, ref count);
            }
            if ((this.AlignmentSize <= count) &&
                (this.IsValueAligned(offset)))
            {
                this.WriteAligned(buffer, ref offset, ref count);
            }
            while (0 < count)
            {
                this.WriteUnaligned(buffer, ref offset, ref count);
            }
        }

        public override void Flush()
        {
            if (this.CanSeek)
            {
                long position = this.Position;
                if (position <= this.Length)
                {
                    if (this.WriteAlignmentBuffer())
                    {
                        this.Seek(position, SeekOrigin.Begin);
                    }
                }
            }
            base.Flush();
        }

        public override void SetLength(long length)
        {
            this.ValidateWritable();
            if (0 > length)
            {
                throw new ArgumentException("length has to be greater or euqal to zero.", "value");
            }

            long position = this._stream.Position;
            long alignedLength = this.GetAlignedValue(length);
            int lengthOffset = (int)(length - alignedLength);
            if (length < position)
            {
                this._alignmentOffset = lengthOffset;
                if (false == this.IsValueAligned(length))
                {
                    this._stream.Seek(alignedLength, SeekOrigin.Begin);
                    this.ReadAlignmentBuffer(this._alignmentOffset);
                }
                else
                {
                    this.ClearAlignmentBuffer();
                }
            }
            else if (length < position + this._alignmentOffset)
            {
                this._alignmentOffset = (int)(length - position);
                this.ClearAlignmentBuffer(this._alignmentOffset);
            }
            else if (length < position + this.AlignmentSize)
            {
                this.ClearAlignmentBuffer(this._alignmentOffset);
            }
            else if (false == this.IsValueAligned(length))
            {
                if (length < this.Length)
                {
                    this.Clear(length);
                }
                alignedLength += this.AlignmentSize;
            }
            this._stream.SetLength(alignedLength);
            if (this._stream.Length != alignedLength)
            {
                //Some underling streams fail to set the length so we making a hack to fix that
                this._stream.SetLength(this._stream.Length);
                this._stream.SetLength(alignedLength);
            }
            this._alignmentLength = (int)(length - this._stream.Length);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ValidateSeekable();

            long beginOffset = this.GetBeginOffset(offset, origin);
            if ((beginOffset > this._stream.Position) &&
                (beginOffset < this._stream.Position + this._alignmentOffset))
            {
                this._alignmentOffset = (int)(beginOffset - this._stream.Position);
            }
            else
            {
                if (this.Position <= this.Length)
                {
                    this.WriteAlignmentBuffer();
                }
                long alignedOffset = this.GetAlignedValue(beginOffset);
                this._stream.Seek(alignedOffset, SeekOrigin.Begin);
                this._alignmentOffset = (int)(beginOffset - alignedOffset);
                if (false == this.IsValueAligned(beginOffset))
                {
                    this.ReadAlignmentBuffer();
                    this._stream.Seek(alignedOffset, SeekOrigin.Begin);
                }
            }

            return beginOffset;
        }

        #endregion

        #region Protected Methods

        protected override long GetLength()
        {
            return base.GetLength() + this._alignmentLength;
        }

        protected bool IsAfterEnd(long offset)
        {
            if (offset > this.Length)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void WriteUnaligned(byte[] buffer, ref int offset, ref int count)
        {
            int alignmentCount = this.AlignmentSize - this._alignmentOffset;
            int bytesCopied = Math.Min(alignmentCount, count);
            bool isWriteEnd = this.IsAfterEnd(this.Position + bytesCopied);
            if ((false == isWriteEnd) && (0 == this._alignmentOffset))
            {
                long position = this._stream.Position;
                this.ReadAlignmentBuffer();
                this._stream.Seek(position, SeekOrigin.Begin);
            }
            Array.Copy(buffer, offset, this._alignmentBuffer, this._alignmentOffset, bytesCopied);
            this._alignmentOffset += bytesCopied;
            if (count >= alignmentCount)
            {
                this.WriteAlignmentBuffer();
            }
            else if (isWriteEnd)
            {
                this._alignmentLength = (int)(this.Position - this._stream.Length);
            }
            offset += bytesCopied;
            count -= bytesCopied;
        }

        protected void ClearAlignmentBuffer(int offset)
        {
            Array.Clear(this._alignmentBuffer, offset, this._alignmentBuffer.Length - offset);
        }

        protected void ClearAlignmentBuffer()
        {
            this.ClearAlignmentBuffer(0);
        }

        protected bool WriteAlignmentBuffer()
        {
            if (0 == this._alignmentOffset)
            {
                return false;
            }
            long length = Math.Max(this.Length, this.Position);
            this._stream.Write(this._alignmentBuffer, 0, this._alignmentBuffer.Length);
            this.ClearAlignmentBuffer();
            this._alignmentLength = (int)(length - this._stream.Length);
            this._alignmentOffset = 0;
            return true;
        }

        protected void WriteAligned(byte[] buffer, ref int offset, ref int count)
        {
            int alignedCount = this.GetAlignedValue(count);
            bool isWriteEnd = this.IsAfterEnd(this.Position + alignedCount);
            this._stream.Write(buffer, offset, alignedCount);
            offset += alignedCount;
            count -= alignedCount;
            if (isWriteEnd)
            {
                this._alignmentLength = 0;
            }
        }

        protected void ReadAlignmentBuffer()
        {
            this.ReadAlignmentBuffer(this.AlignmentSize);
        }

        protected void ReadAlignmentBuffer(int count)
        {
            this.ReadAligned(this._alignmentBuffer, count);
        }

        protected void ReadAligned(byte[] buffer, int count)
        {
            int offset = 0;
            int bytesRead = this._stream.Read(buffer, offset, this.AlignmentSize);
            bytesRead = Math.Min(bytesRead, count);
            offset += bytesRead;
            count -= bytesRead;
            while (0 < count)
            {
                bytesRead = this._stream.Read(this._copyBuffer, 0, this.AlignmentSize);
                if (0 == bytesRead)
                {
                    break;
                }
                bytesRead = Math.Min(bytesRead, count);
                Array.Copy(this._copyBuffer, 0, buffer, offset, bytesRead);
                offset += bytesRead;
                count -= bytesRead;
            }
            Array.Clear(buffer, offset, this.AlignmentSize - offset);
        }

        protected void Clear(long possition)
        {
            long alignedPosition = this.GetAlignedValue(possition);
            long currentPosition = this._stream.Position;
            this._stream.Seek(alignedPosition, SeekOrigin.Begin);
            this.ReadAligned(this._clearBuffer, (int)(possition - alignedPosition));
            this._stream.Seek(alignedPosition, SeekOrigin.Begin);
            this._stream.Write(this._clearBuffer, 0, this.AlignmentSize);
            this._stream.Seek(currentPosition, SeekOrigin.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            if (this.CanSeek)
            {
                this.Flush();
            }
            else if (this.Position <= this.Length)
            {
                this.WriteAlignmentBuffer();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
