using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalCopy
{
    public class CopyOptions
    {
        #region Public Properties

        public string SourceFilename { get; set; }
        public string TargetFilename { get; set; }
        public bool Resumable { get; set; }
        public bool Overwrite { get; set; }
        public bool Threaded { get; set; }
        public int BufferSize { get; set; }
        public int BufferCount { get; set; }
        public bool MeasureTime { get; set; }

        #endregion

        #region Constructors

        public CopyOptions()
        {
            this.SourceFilename = string.Empty;
            this.TargetFilename = string.Empty;
            this.Resumable = false;
            this.Overwrite = false;
            this.Threaded = false;
            this.BufferSize = 4 * 1024;
            this.BufferCount = 6;
            this.MeasureTime = false;
        }

        #endregion
    }
}
