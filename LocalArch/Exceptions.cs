using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalArch
{
    public class InvalidArchiveException : Exception
    {
        public InvalidArchiveException()
            : this("Invliad Archive")
        {
        }

        public InvalidArchiveException(string message)
            : base(message)
        {
        }
    }
}
