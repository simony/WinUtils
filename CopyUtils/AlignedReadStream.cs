using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CopyUtils;

namespace CopyUtils
{
    public class AlignedReadStream : AlignedStreamBase
    {
        #region Constructors

        public AlignedReadStream(Stream stream)
            : this(stream, DEFAULT_ALIGNMENT_SIZE)
        {
        }

        public AlignedReadStream(Stream stream, int alignmentSize)
            : base(stream, alignmentSize)
        {
            if (false == stream.CanRead)
            {
                throw new ArgumentException("Excepected a readble stream.", "stream");
            }
        }

        #endregion

        #region Public Properties

        public override bool CanWrite
        {
            get { return false; }
        }

        #endregion

        #region Public Methods

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ValidateReadable();
            this.Validate(buffer, offset, count);
            if (0 == count)
            {
                return 0;
            }
            int totalRead = 0;
            if ((0 != this._alignmentOffset) &&
                (false == this.ReadUnaligned(buffer, ref offset, ref count, ref totalRead)))
            {
                return totalRead;
            }
            if ((this._alignmentSize <= count) &&
                (this.IsValueAligned(offset)) &&
                (false == this.ReadAligned(buffer, ref offset, ref count, ref totalRead)))
            {
                return totalRead;
            }
            while (0 < count)
            {
                if (false == this.ReadUnaligned(buffer, ref offset, ref count, ref totalRead))
                {
                    return totalRead;
                }
            }
            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ValidateSeekable();

            long beginOffset = this.GetBeginOffset(offset, origin);
            if ((beginOffset < this._stream.Position) &&
                (beginOffset >= this._stream.Position - this._alignmentLength))
            {
                this._alignmentOffset = (int)(beginOffset - this._stream.Position);
            }
            else if (beginOffset == this._stream.Position)
            {
                this._alignmentOffset = 0;
                this._alignmentLength = 0;
            }
            else
            {
                long alignedOffset = this.GetAlignedValue(beginOffset);
                if (this._stream.Position != alignedOffset)
                {
                    this._stream.Seek(alignedOffset, SeekOrigin.Begin);
                }
                this._alignmentOffset = (int)(beginOffset - alignedOffset);
                this._alignmentLength = 0;
            }

            return beginOffset;
        }

        #endregion

        #region Protected Methods

        protected bool ReadAligned(byte[] buffer, ref int offset, ref int count, ref int totalRead)
        {
            int alignedCount = this.GetAlignedValue(count);
            int bytesRead = this._stream.Read(buffer, offset, alignedCount);
            totalRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
            if (bytesRead < alignedCount)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        protected bool ReadUnaligned(byte[] buffer, ref int offset, ref int count, ref int totalRead)
        {
            int bytesRead = this.ReadUnaligned(buffer, offset, count);
            if (0 == bytesRead)
            {
                return false;
            }
            totalRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
            return true;
        }

        protected int ReadUnaligned(byte[] buffer, int offset, int count)
        {
            if ((this._alignmentOffset >= this._alignmentLength) &&
                (false == this.ReadToAlignmentBuffer()))
            {
                return 0;
            }
            return this.ReadFromAlignmentBuffer(buffer, offset, count);
        }

        protected bool ReadToAlignmentBuffer()
        {
            this._alignmentLength = 0;
            while (true)
            {
                int bytesRead = this._stream.Read(this._alignmentBuffer, 0, this._alignmentBuffer.Length);
                if (0 == bytesRead)
                {
                    return false;
                }
                if (bytesRead <= this._alignmentOffset)
                {
                    this._alignmentOffset -= bytesRead;
                    continue;
                }
                this._alignmentLength = bytesRead;
                return true;
            }
        }

        protected int ReadFromAlignmentBuffer(byte[] buffer, int offset, int count)
        {
            int alignmentOffset = 0;
            int alignmentCount = 0;
            if (0 <= this._alignmentOffset)
            {
                alignmentCount = this._alignmentLength - this._alignmentOffset;
                alignmentOffset = this._alignmentOffset;
            }
            else
            {
                alignmentCount = -1 * this._alignmentOffset;
                alignmentOffset = this._alignmentLength - alignmentCount;
            }

            int bytesCopied = Math.Min(alignmentCount, count);
            Array.Copy(this._alignmentBuffer, alignmentOffset, buffer, offset, bytesCopied);
            if (alignmentCount > count)
            {
                this._alignmentOffset = count - alignmentCount;
            }
            else
            {
                this._alignmentOffset = 0;
                this._alignmentLength = 0;
            }

            return bytesCopied;
        }

        #endregion
    }
}
