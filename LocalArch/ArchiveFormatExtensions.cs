using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SevenZip;

namespace LocalArch
{
    public static class ArchiveFormatExtensions
    {
        public static string GetExtension(this OutArchiveFormat format)
        {
            switch (format)
            {
                case OutArchiveFormat.BZip2:
                    return "bz2";
                case OutArchiveFormat.GZip:
                    return "gz";
                case OutArchiveFormat.SevenZip:
                default:
                    return "7z";
                case OutArchiveFormat.Tar:
                    return "tar";
                case OutArchiveFormat.XZ:
                    return "xz";
                case OutArchiveFormat.Zip:
                    return "zip";
            }
        }
    }
}
