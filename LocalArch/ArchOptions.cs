using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SevenZip;

namespace LocalArch
{
    public class ArchOptions
    {
        #region Members

        protected Dictionary<string, string> _customParameters = new Dictionary<string, string>();

        #endregion

        #region Public Properties

        public string SourceFilename { get; set; }
        public string OutputFilename { get; set; }
        public bool Decompress { get; set; }
        public bool Check { get; set; }
        public OutArchiveFormat ArchiveFormat { get; set; }
        public CompressionMethod CompressionMethod { get; set; }
        public CompressionLevel CompressionLevel { get; set; }
        public ZipEncryptionMethod EncryptionMethod { get; set; }
        public string Password { get; set; }
        public bool Overwrite { get; set; }
        public int PreallocationPercent { get; set; }
        public int BufferSize { get; set; }
        public bool MeasureTime { get; set; }
        public Dictionary<string, string> CustomParameters
        {
            get
            {
                return this._customParameters;
            }
        }

        #endregion

        #region Constructors

        public ArchOptions()
        {
            this.SourceFilename = string.Empty;
            this.OutputFilename = string.Empty;
            this.Decompress = false;
            this.Check = false;
            this.ArchiveFormat = OutArchiveFormat.SevenZip;
            this.CompressionMethod = CompressionMethod.Default;
            this.CompressionLevel = CompressionLevel.Normal;
            this.EncryptionMethod = ZipEncryptionMethod.ZipCrypto;
            this.Password = string.Empty;
            this.PreallocationPercent = 0;
            this.Overwrite = false;
            this.BufferSize = 4 * 1024;
            this.MeasureTime = false;
        }

        #endregion
    }
}
