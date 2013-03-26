using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace CopyUtils
{
    public class Win32
    {
        #region Constants

        public const int SECTOR_SIZE = 512;
        public const int PAGE_SIZE = 4096;

        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

        public const FileOptions FileFlagNoBuffering = (FileOptions)FILE_FLAG_NO_BUFFERING;

        #endregion

        #region Public Methods

        [DllImport("kernel32", SetLastError = true)]
        public static extern SafeFileHandle ReOpenFile(SafeFileHandle hOriginalFile, uint dwAccess, uint dwShareMode, uint dwFlags);

        #endregion
    }
}
