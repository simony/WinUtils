using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Threading;
using CopyUtils;

namespace LocalCopy
{
    public class LocalCopy
    {
        #region Members

        protected int _sectorSize = Win32.SECTOR_SIZE;

        #endregion

        #region Constructors

        public LocalCopy(int sectorSize)
        {
            this._sectorSize = sectorSize;
        }

        public LocalCopy()
            : this(Win32.SECTOR_SIZE)
        {
        }

        #endregion

        #region Public Methods

        public void CopyFile(string inputfile, string outputfile, FileMode fileMode, int bufferSize)
        {
            var buffer = new byte[bufferSize * this._sectorSize];
            using (FileStream sourceFileStream = new FileStream(inputfile,
                FileMode.Open, FileAccess.Read, FileShare.None, buffer.Length,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                using (FileStream targetFileStream = new FileStream(outputfile, fileMode, FileAccess.Write, FileShare.Write, 8,
                    FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
                {
                    this.SetFileInitializeLength(targetFileStream, sourceFileStream.Length);
                    this.CopyFile(sourceFileStream, targetFileStream, buffer);
                }
            }
        }

        public void CopyResumableFile(string inputfile, string outputfile, FileMode fileMode, int bufferSize)
        {
            var buffer = new byte[bufferSize * this._sectorSize];
            using (FileStream sourceFileStream = new FileStream(inputfile,
                FileMode.Open, FileAccess.Read, FileShare.None, buffer.Length,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                using (FileStream targetFileStream = new FileStream(outputfile, fileMode, FileAccess.Write, FileShare.Write, 8,
                    FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
                {
                    if (targetFileStream.Length == sourceFileStream.Length)
                    {
                        return;
                    }
                    this.SetResumablePositions(sourceFileStream, targetFileStream);
                    if (targetFileStream.Length > sourceFileStream.Length)
                    {
                        this.SetFileLength(targetFileStream.SafeFileHandle, sourceFileStream.Length);
                    }
                    else
                    {
                        this.CopyFile(sourceFileStream, targetFileStream, buffer);
                    }
                }
            }
        }

        public void CopyFile(FileStream sourceFileStream, FileStream targetFileStream, byte[] buffer)
        {
            while (true)
            {
                int bytesRead = sourceFileStream.Read(buffer, 0, buffer.Length);
                int length = this.CalculateAlignedLength(bytesRead);
                targetFileStream.Write(buffer, 0, length);
                if (bytesRead < buffer.Length)
                {
                    SetFileLength(targetFileStream.SafeFileHandle, sourceFileStream.Length);
                    return;
                }
            }
        }

        public void ThreadedCopyFile(string inputfile, string outputfile, FileMode fileMode, int bufferCount, int bufferSize)
        {
            var bufferManager = new BufferManager<byte>(bufferCount, bufferSize * this._sectorSize);
            using (FileStream sourceFileStream = new FileStream(inputfile,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferManager.BufferLength,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                using (FileStream targetFileStream = new FileStream(outputfile,
                        fileMode, FileAccess.Write, FileShare.Write, 8, FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
                {
                    this.SetFileInitializeLength(targetFileStream, sourceFileStream.Length);
                    this.ThreadedCopyFile(sourceFileStream, targetFileStream, sourceFileStream.Length, bufferManager);
                }
            }
        }

        public void ThreadedCopyResumableFile(string inputfile, string outputfile, FileMode fileMode, int bufferCount, int bufferSize)
        {
            var bufferManager = new BufferManager<byte>(bufferCount, bufferSize * this._sectorSize);
            using (FileStream sourceFileStream = new FileStream(inputfile,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferManager.BufferLength,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                using (FileStream targetFileStream = new FileStream(outputfile,
                        fileMode, FileAccess.Write, FileShare.Write, 8, FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
                {
                    if (targetFileStream.Length == sourceFileStream.Length)
                    {
                        return;
                    }
                    this.SetResumablePositions(sourceFileStream, targetFileStream);
                    if (targetFileStream.Length > sourceFileStream.Length)
                    {
                        this.SetFileLength(targetFileStream.SafeFileHandle, sourceFileStream.Length);
                    }
                    else
                    {
                        long length = sourceFileStream.Length - sourceFileStream.Position;
                        this.ThreadedCopyFile(sourceFileStream, targetFileStream, length, bufferManager);
                    }
                }
            }
        }

        public void ThreadedCopyFile(FileStream sourceFileStream, FileStream targetFileStream, long length, BufferManager<byte> bufferManager)
        {
            var readThread = new Thread(delegate(object thread)
            {
                this.Execute(this.ReadFile, bufferManager, sourceFileStream, length, (Thread)thread);
            });
            var writeThread = new Thread(delegate(object thread)
            {
                this.Execute(this.WriteFile, bufferManager, targetFileStream, length, (Thread)thread);
            });
            readThread.Start(writeThread);
            writeThread.Start(readThread);
            readThread.Join();
            writeThread.Join();
            if ((ThreadState.Aborted == readThread.ThreadState) ||
                (ThreadState.Aborted == writeThread.ThreadState))
            {
                throw new CopyAbortException("Read/Write threads were aborted");
            }
        }

        public void ReadFile(BufferManager<byte> bufferManager, string filename, long length)
        {
            using (FileStream sourceFileStream = new FileStream(filename,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferManager.BufferLength,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                this.ReadFile(bufferManager, sourceFileStream, length);
            }
        }

        public void ReadFile(BufferManager<byte> bufferManager, FileStream sourceFileStream, long length)
        {
            long totalRead = 0;
            while (length > totalRead)
            {
                totalRead += this.ReadBuffer(bufferManager, sourceFileStream);
            }
        }

        public int ReadBuffer(BufferManager<byte> bufferManager, FileStream sourceFileStream)
        {
            int bytesRead = 0;
            var buffer = bufferManager.Allocate();
            try
            {
                bytesRead = sourceFileStream.Read(buffer.Data, 0, buffer.Length);
                buffer.UsedLength = bytesRead;
            }
            catch
            {
                bufferManager.Free(buffer);
                throw;
            }
            bufferManager.Enqueue(buffer);
            return bytesRead;
        }

        public void WriteFile(BufferManager<byte> bufferManager, string filename, long length)
        {
            using (FileStream targetFileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write, 8,
                FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
            {
                this.SetFileInitializeLength(targetFileStream, length);

                this.WriteFile(bufferManager, targetFileStream, length);
            }
        }

        public void WriteFile(BufferManager<byte> bufferManager, FileStream targetFileStream, long length)
        {
            long initialFileLength = targetFileStream.Position;
            long totalWriten = 0;
            while (length > totalWriten)
            {
                totalWriten += this.WriteBuffer(bufferManager, targetFileStream);
            }
            if (length < totalWriten)
            {
                this.SetFileLength(targetFileStream.SafeFileHandle, initialFileLength + length);
            }
        }

        public int WriteBuffer(BufferManager<byte> bufferManager, FileStream targetFileStream)
        {
            var buffer = bufferManager.Dequeue();
            try
            {
                int length = this.CalculateAlignedLength(buffer.UsedLength);
                targetFileStream.Write(buffer.Data, 0, length);
                return length;
            }
            finally
            {
                bufferManager.Free(buffer);
            }
        }

        #endregion

        #region Protected Methods

        protected int CalculateAlignedLength(int length)
        {
            return (int)this.CalculateAlignedLength((long)length);
        }

        protected long CalculateAlignedLength(long length)
        {
            return (long)Math.Ceiling((decimal)length / this._sectorSize) * this._sectorSize;
        }

        protected void SetFileInitializeLength(FileStream fileStream, long length)
        {
            fileStream.SetLength(this.CalculateAlignedLength(length));
        }

        protected void SetResumablePositions(FileStream sourceFileStream, FileStream targetFileStream)
        {
            if ((targetFileStream.Length >= sourceFileStream.Length) ||
                (0 == targetFileStream.Length))
            {
                return;
            }

            long resumablePosition = (targetFileStream.Length / this._sectorSize) * this._sectorSize;
            targetFileStream.Seek(resumablePosition, SeekOrigin.Begin);
            sourceFileStream.Seek(resumablePosition, SeekOrigin.Begin);
        }

        protected void SetFileLength(SafeFileHandle safeFileHandle, long length)
        {
            using (FileStream fileStream = new FileStream(Win32.ReOpenFile(safeFileHandle,
                (uint)FileAccess.Write, (uint)FileShare.Write, 0), FileAccess.Write))
            {
                fileStream.SetLength(length);
            }
        }

        protected void Execute(Action<BufferManager<byte>, FileStream, long> action,
            BufferManager<byte> bufferManager, FileStream sourceFileStream, long length, Thread dependentThread)
        {
            try
            {
                action(bufferManager, sourceFileStream, length);
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Copy Thread Failure: {0}", ex.Message));
                Console.WriteLine(ex.StackTrace);
                dependentThread.Abort();
                Thread.CurrentThread.Abort();
            }
        }

        #endregion
    }
}
