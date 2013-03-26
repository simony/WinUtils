using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CopyUtils
{
    public class NonEmptySyncQueue<T>
    {
        #region Members

        protected Queue<T> _queue = new Queue<T>();
        protected AutoResetEvent _resetEvent = new AutoResetEvent(false);

        #endregion

        #region Public Methods

        public T Dequeue()
        {
            this._resetEvent.WaitOne();
            lock (this)
            {
                if (1 < this._queue.Count)
                {
                    this._resetEvent.Set();
                }
                return this._queue.Dequeue();
            }
        }

        public void Enqueue(T item)
        {
            lock (this)
            {
                if (0 == this._queue.Count)
                {
                    this._resetEvent.Set();
                }
                this._queue.Enqueue(item);
            }
        }

        #endregion
    }
}
