using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CopyUtils
{
    public class BufferManager<T>
    {
        #region Constants

        public const int DEFAULT_BUFFER_LENGTH = 4096 * 1024;

        #endregion

        #region Members

        protected int _bufferLength = DEFAULT_BUFFER_LENGTH;
        protected NonEmptySyncQueue<Buffer<T>> _freeBufferQueue = new NonEmptySyncQueue<Buffer<T>>();
        protected NonEmptySyncQueue<Buffer<T>> _fullBufferQueue = new NonEmptySyncQueue<Buffer<T>>();

        #endregion

        #region Constructors

        public BufferManager()
            : this(1)
        {
        }

        public BufferManager(int count)
            : this(count, DEFAULT_BUFFER_LENGTH)
        {
        }

        public BufferManager(int count, int bufferLength)
        {
            this._bufferLength = bufferLength;
            this.Create(count);
        }

        #endregion

        #region Public Properties

        public int BufferLength
        {
            get
            {
                return this._bufferLength;
            }
        }

        #endregion

        #region Public Methods

        public void Create(int count)
        {
            for (int i = 0; i < count; i++)
            {
                this._freeBufferQueue.Enqueue(new Buffer<T>(this._bufferLength));
            }
        }

        public Buffer<T> Allocate()
        {
            return this._freeBufferQueue.Dequeue();
        }

        public void Free(Buffer<T> buffer)
        {
            this._freeBufferQueue.Enqueue(buffer);
        }

        public void Enqueue(Buffer<T> buffer)
        {
            this._fullBufferQueue.Enqueue(buffer);
        }

        public Buffer<T> Dequeue()
        {
            return this._fullBufferQueue.Dequeue();
        }

        #endregion
    }
}
