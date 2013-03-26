using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalHash
{
    public class HashOptions
    {
        #region Public Properties

        public string SourceFilename { get; set; }
        public string OutputFilename { get; set; }
        public string ExpectedHash { get; set; }
        public string HashAlgorithm { get; set; }
        public bool Overwrite { get; set; }
        public int BufferSize { get; set; }
        public bool MeasureTime { get; set; }

        #endregion

        #region Constructors

        public HashOptions()
        {
            this.SourceFilename = string.Empty;
            this.OutputFilename = string.Empty;
            this.ExpectedHash = string.Empty;
            this.HashAlgorithm = HashAlgorithmNames.SHA1;
            this.Overwrite = false;
            this.BufferSize = 4 * 1024;
            this.MeasureTime = false;
        }

        #endregion
    }
}
