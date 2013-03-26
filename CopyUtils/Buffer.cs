using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CopyUtils
{
    public class Buffer<T>
    {
        #region Members

        protected T[] _buffer = null;
        protected int _usedLength = 0;

        #endregion

        #region Constructors

        public Buffer(T[] buffer, int length)
        {
            this._buffer = buffer;
            this._usedLength = length;
        }

        public Buffer(int length)
            : this(new T[length], length)
        {
        }

        #endregion

        #region Public Properties

        public T[] Data
        {
            get
            {
                return this._buffer;
            }
        }

        public int Length
        {
            get
            {
                return this._buffer.Length;
            }
        }

        public int UsedLength
        {
            get
            {
                return this._usedLength;
            }
            set
            {
                //TODO: validate legnth between 0 and buffer.length
                this._usedLength = value;
            }
        }

        #endregion
    }
}
