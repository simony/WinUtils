using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CopyUtils
{
    public class AlignedStreamBase : ProxyStream
    {
        #region Constants

        public const int DEFAULT_ALIGNMENT_SIZE = Win32.SECTOR_SIZE;

        #endregion

        #region Members

        protected int _alignmentSize = DEFAULT_ALIGNMENT_SIZE;
        protected int _alignmentOffset = 0;
        protected int _alignmentLength = 0;
        protected byte[] _alignmentBuffer = null;

        #endregion

        #region Constructors

        public AlignedStreamBase(Stream stream)
            : this(stream, DEFAULT_ALIGNMENT_SIZE)
        {
        }

        public AlignedStreamBase(Stream stream, int alignmentSize)
            : base(stream)
        {
            if (0 >= alignmentSize)
            {
                throw new ArgumentException("Alignment size has to be greater than zero.", "alignmentSize");
            }
            this._alignmentSize = alignmentSize;
            this._alignmentBuffer = new byte[this._alignmentSize];
        }

        #endregion

        #region Public Properties

        public int AlignmentSize
        {
            get { return this._alignmentSize; }
        }

        #endregion

        #region Protected Methods

        protected long GetAlignedValue(long value)
        {
            return value - (value % this._alignmentSize);
        }

        protected int GetAlignedValue(int value)
        {
            return value - (value % this._alignmentSize);
        }

        protected bool IsValueAligned(long value)
        {
            return (0 == value % this._alignmentSize);
        }

        protected bool IsValueAligned(int value)
        {
            return (0 == value % this._alignmentSize);
        }

        protected void Validate(byte[] buffer, int offset, int count)
        {
            if (0 > offset)
            {
                throw new ArgumentException("offset has to be greater or euqal to zero.", "offset");
            }
            if (buffer.Length < offset)
            {
                throw new ArgumentException("offset is out of range.", "offset");
            }
            if (0 > count)
            {
                throw new ArgumentException("count has to be greater or euqal to zero.", "count");
            }
            if (buffer.Length < offset + count)
            {
                throw new ArgumentException("count is out of range.", "count");
            }
        }

        protected override long GetPosition()
        {
            return base.GetPosition() + this._alignmentOffset;
        }

        protected long GetBeginOffset(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return offset;
                case SeekOrigin.Current:
                    return this.Position + offset;
                case SeekOrigin.End:
                    return this.Length + offset;
                default:
                    throw new ArgumentException(string.Format("Invalid seek origin {0}.", origin));
            }
        }

        #endregion
    }
}
