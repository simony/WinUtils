using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LocalCopy
{
    [Serializable]
    public class CopyAbortException : Exception
    {
        #region Constructors

        public CopyAbortException()
            : base()
        {
        }

        public CopyAbortException(string message)
            : base(message)
        {
        }

        public CopyAbortException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CopyAbortException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}
