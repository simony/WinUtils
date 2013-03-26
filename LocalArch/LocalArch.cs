using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CopyUtils;
using SevenZip;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace LocalArch
{
    public class LocalArch
    {
        #region Members

        protected int _sectorSize = Win32.SECTOR_SIZE;

        #endregion

        #region Constructors

        public LocalArch(int sectorSize)
        {
            this._sectorSize = sectorSize;
        }

        public LocalArch()
            : this(Win32.SECTOR_SIZE)
        {
        }

        #endregion

        #region Public Methods

        public void Compress(string sourceFilename, string targetFilename, FileMode fileMode, OutArchiveFormat archiveFormat,
            CompressionMethod compressionMethod, CompressionLevel compressionLevel, ZipEncryptionMethod zipEncryptionMethod,
            string password, int bufferSize, int preallocationPercent, bool check, Dictionary<string, string> customParameters)
        {
            bufferSize *= this._sectorSize;
            SevenZipCompressor compressor = new SevenZipCompressor();
            compressor.FastCompression = true;
            compressor.ArchiveFormat = archiveFormat;
            compressor.CompressionMethod = compressionMethod;
            compressor.CompressionLevel = compressionLevel;
            compressor.DefaultItemName = Path.GetFileName(sourceFilename);
            compressor.DirectoryStructure = false;
            compressor.ZipEncryptionMethod = zipEncryptionMethod;
            foreach (var pair in customParameters)
            {
                compressor.CustomParameters[pair.Key] = pair.Value;
            }
            using (FileStream sourceFileStream = new FileStream(sourceFilename,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferSize,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                using (FileStream targetFileStream = new FileStream(targetFilename,
                       fileMode, FileAccess.ReadWrite, FileShare.ReadWrite, 8,
                       FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
                {
                    this.Compress(compressor, sourceFileStream, targetFileStream,
                        password, preallocationPercent, check, bufferSize);
                }
            }
        }

        public void Decompress(string sourceFilename, string targetPath, FileMode fileMode, string password,
            int bufferSize, int preallocationPercent)
        {
            bufferSize *= this._sectorSize;
            using (FileStream sourceFileStream = new FileStream(sourceFilename,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferSize,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                this.Decompress(sourceFileStream, targetPath, fileMode, password, bufferSize, preallocationPercent);
            }
        }

        public void Check(string sourceFilename, string password, int bufferSize)
        {
            bufferSize *= this._sectorSize;
            using (FileStream sourceFileStream = new FileStream(sourceFilename,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferSize,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                this.Check(sourceFileStream, password, bufferSize);
            }
        }

        #endregion

        #region Protected Methods

        protected void Decompress(FileStream sourceFileStream, string targetPath, FileMode fileMode, string password,
            int bufferSize, int preallocationPercent)
        {
            using (AlignedReadStream readArchiveStream = new AlignedReadStream(sourceFileStream, bufferSize))
            {
                SevenZipExtractor extractor = this.CreateExtractor(readArchiveStream, password);
                this.Decompress(extractor, targetPath, fileMode, bufferSize, preallocationPercent);
            }
        }

        protected void Decompress(SevenZipExtractor extractor, string targetPath, FileMode fileMode,
            int bufferSize, int preallocationPercent)
        {
            if (1 != extractor.FilesCount)
            {
                throw new InvalidArchiveException("Archive contains more than one file");
            }
            string targetFilename = extractor.ArchiveFileNames.First();
            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = targetFilename;
            }
            else
            {
                targetPath = Path.Combine(targetPath, targetFilename);
            }
            using (FileStream targetFileStream = new FileStream(targetPath,
                   fileMode, FileAccess.ReadWrite, FileShare.ReadWrite, 8,
                   FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
            {
                this.Decompress(extractor, targetFileStream, bufferSize, preallocationPercent);
            }
        }

        protected void Decompress(SevenZipExtractor extractor, FileStream targetFileStream,
            int bufferSize, int preallocationPercent)
        {
            decimal preallocationSize = (((decimal)extractor.PackedSize * preallocationPercent) / 100);
            preallocationSize -= preallocationSize % this._sectorSize;
            if (targetFileStream.Length < preallocationSize)
            {
                targetFileStream.SetLength((long)preallocationSize);
            }
            long decompressedLength = 0;
            using (AlignedWriteStream writeStream = new AlignedWriteStream(targetFileStream, bufferSize))
            {
                extractor.ExtractFile(0, writeStream);
                decompressedLength = writeStream.Length;
            }
            if (targetFileStream.Length != decompressedLength)
            {
                this.SetFileLength(targetFileStream.SafeFileHandle, decompressedLength);
            }
        }

        protected void Compress(SevenZipCompressor compressor, FileStream sourceFileStream, FileStream targetFileStream,
            string password, int preallocationPercent, bool check, int bufferSize)
        {
            using (AlignedReadStream readStream = new AlignedReadStream(sourceFileStream, bufferSize))
            {
                decimal preallocationSize = (((decimal)readStream.Length * preallocationPercent) / 100);
                preallocationSize -= preallocationSize % this._sectorSize;
                if (targetFileStream.Length < preallocationSize)
                {
                    targetFileStream.SetLength((long)preallocationSize);
                }
                long compressedLength = 0;
                using (AlignedWriteStream writeStream = new AlignedWriteStream(targetFileStream, bufferSize))
                {
                    compressedLength = this.Compress(compressor, readStream, writeStream, password);
                }
                if (targetFileStream.Length != compressedLength)
                {
                    this.SetFileLength(targetFileStream.SafeFileHandle, compressedLength);
                }
                if (check)
                {
                    this.Check(targetFileStream.SafeFileHandle, password, bufferSize);
                }
            }
        }

        protected long Compress(SevenZipCompressor compressor, AlignedReadStream readStream, AlignedWriteStream writeStream,
            string password)
        {
            using (MaxPositionStream maxWriteStream = new MaxPositionStream(writeStream))
            {
                if (string.IsNullOrEmpty(password))
                {
                    compressor.CompressStream(readStream, maxWriteStream);
                }
                else
                {
                    compressor.CompressStream(readStream, maxWriteStream, password);
                }
                return maxWriteStream.MaxPosition;
            }
        }

        protected void Check(SafeFileHandle safeFileHandle, string password, int bufferSize)
        {
            using (FileStream fileStream = new FileStream(Win32.ReOpenFile(safeFileHandle,
                (uint)FileAccess.ReadWrite, (uint)FileShare.ReadWrite, 0), FileAccess.ReadWrite, bufferSize))
            {
                this.Check(fileStream, password, bufferSize);
            }
        }

        protected void Check(FileStream fileStream, string password, int bufferSize)
        {
            using (AlignedReadStream readArchiveStream = new AlignedReadStream(fileStream, bufferSize))
            {
                readArchiveStream.Seek(0, SeekOrigin.Begin);
                SevenZipExtractor extractor = this.CreateExtractor(readArchiveStream, password);
                if (extractor.Check())
                {
                    return;
                }
                throw new InvalidArchiveException();
            }
        }

        protected void SetFileLength(SafeFileHandle safeFileHandle, long length)
        {
            using (FileStream fileStream = new FileStream(Win32.ReOpenFile(safeFileHandle,
                (uint)FileAccess.ReadWrite, (uint)FileShare.ReadWrite, 0), FileAccess.ReadWrite))
            {
                fileStream.SetLength(length);
            }
        }

        protected SevenZipExtractor CreateExtractor(AlignedReadStream readArchiveStream, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new SevenZipExtractor(readArchiveStream);
            }
            else
            {
                return new SevenZipExtractor(readArchiveStream, password);
            }
        }

        #endregion
    }
}
