using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalHash
{
    public class HashMismatchException : Exception
    {
        public HashMismatchException(string hash, string expectedHash)
            : base(string.Format("Hash mismatch expected {0} while got {1}.", expectedHash, hash))
        {
        }
    }
}
