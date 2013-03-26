using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CopyUtils
{
    public class MaxPositionStream : ProxyStream
    {
        #region Members

        protected long _maxPosition = 0;

        #endregion

        #region Constructors

        public MaxPositionStream(Stream stream)
            : base(stream)
        {
            this._maxPosition = stream.Position;
        }

        #endregion

        #region Stream Methods

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = base.Read(buffer, offset, count);
            this._maxPosition = Math.Max(this._maxPosition, this.Position);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long position = base.Seek(offset, origin);
            this._maxPosition = Math.Max(this._maxPosition, position);
            return position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);
            this._maxPosition = Math.Max(this._maxPosition, this.Position);
        }

        #endregion

        #region Public Properties

        public long MaxPosition
        {
            get { return this._maxPosition; }
        }

        #endregion
    }
}
