using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using CopyUtils;

namespace UnitTest
{
    [TestClass]
    public class LocalArchTest
    {
        #region Members

        public static readonly Random Random = new Random();

        #endregion

        #region Write Stream Tests

        #region Random Option Tests

        public enum WriteSreamOperations
        {
            SetLength,
            Write,
            Seek,
            Flush,
            Count
        }

        [TestMethod]
        public void TestWriteStreamRandomOperations()
        {
            int count1 = 100;
            int count2 = 200;
            string filename = @"SmallData.bin";
            string filename1 = @"WriteData1.bin";
            string filename2 = @"WriteData2.bin";

            while (0 < count1)
            {
                this.TestWriteStreamRandomOperations(count2, filename, filename1, filename2, Win32.SECTOR_SIZE, this.TestRandomOperation);
                this.TestWriteStreamRandomOperations(count2, filename, filename1, filename2, Win32.PAGE_SIZE, this.TestRandomOperation);

                this.TestWriteStreamRandomOperations(count2, filename, filename1, filename2, Win32.SECTOR_SIZE, this.TestRandomWriteOperation);
                this.TestWriteStreamRandomOperations(count2, filename, filename1, filename2, Win32.PAGE_SIZE, this.TestRandomWriteOperation);
                count1--;
            }
        }

        protected byte[] ReadFile(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[stream.Length];
                int count = buffer.Length;
                int offset = 0;
                while (0 < count)
                {
                    int byteRead = stream.Read(buffer, offset, count);
                    if (0 == byteRead)
                    {
                        break;
                    }
                    offset += byteRead;
                    count -= byteRead;
                }
                return buffer;
            }
        }

        protected int CompareBuffers(byte[] buffer1, byte[] buffer2, int offset, int length)
        {
            for (int i = offset; i < offset + length; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return i;
                }
            }
            return -1;
        }

        protected int FindNoneZero(byte[] buffer, int offset)
        {
            int length = buffer.Length - offset;
            for (int i = offset; i < length; i++)
            {
                if (0 != buffer[i])
                {
                    return i;
                }
            }
            return -1;
        }

        private void TestWriteStreamRandomOperations(int count, string inputfile, string outputfile1, string outputfile2, int alignmentSize,
            Func<FileStream, AlignedWriteStream, byte[], WriteSreamOperations> testOperation)
        {
            byte[] data = File.ReadAllBytes(inputfile);
            List<Tuple<WriteSreamOperations, long, long, long, long, long, long>> operations =
                new List<Tuple<WriteSreamOperations, long, long, long, long, long, long>>();
            using (FileStream targetFileStream1 = new FileStream(outputfile1, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (FileStream targetFileStream2 = new FileStream(outputfile2, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 8,
                    FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
                {
                    using (AlignedWriteStream writeStream = new AlignedWriteStream(targetFileStream2, alignmentSize))
                    {
                        while (0 < count)
                        {
                            long position = targetFileStream1.Position;
                            long length = targetFileStream1.Length;
                            WriteSreamOperations operation = testOperation(targetFileStream1, writeStream, data);
                            targetFileStream1.Flush();
                            byte[] data1 = this.ReadFile(outputfile1);
                            byte[] data2 = this.ReadFile(outputfile2);
                            if (Math.Abs(data1.Length - data2.Length) > alignmentSize * 2)
                            {
                                writeStream.Flush();
                                data2 = this.ReadFile(outputfile2);
                            }
                            Assert.IsTrue(Math.Abs(data1.Length - data2.Length) < alignmentSize*2, "Length mismatch");
                            int i = 0;
                            if (data1.Length < data2.Length)
                            {
                                i = this.FindNoneZero(data2, data1.Length);
                                Assert.IsTrue(-1 == i, "Junk found");
                            }
                            i = this.CompareBuffers(data1, data2, 0, Math.Min(data1.Length, data2.Length));
                            if (-1 != i)
                            {
                                writeStream.Flush();
                                data2 = this.ReadFile(outputfile2);
                                if (data1.Length < data2.Length)
                                {
                                    i = this.FindNoneZero(data2, data1.Length);
                                    Assert.IsTrue(-1 == i, "Junk found");
                                }
                                i = this.CompareBuffers(data1, data2, 0, Math.Min(data1.Length, data2.Length));
                                Assert.IsTrue(-1 == i, "Data mismatch");
                            }
                            count--;
                            operations.Add(new Tuple<WriteSreamOperations, long, long, long, long, long, long>(
                                operation, position, length, targetFileStream1.Position, targetFileStream1.Length,
                                targetFileStream2.Position, targetFileStream2.Length));
                        }
                    }
                }
            }
            byte[] fdata1 = this.ReadFile(outputfile1);
            byte[] fdata2 = this.ReadFile(outputfile2);
            Assert.IsTrue(Math.Abs(fdata1.Length - fdata2.Length) < alignmentSize * 2, "Length mismatch");
            Assert.IsTrue(-1 == this.CompareBuffers(fdata1, fdata2, 0, fdata1.Length), "Data mismatch");
        }

        private WriteSreamOperations TestRandomOperation(FileStream targetStream, AlignedWriteStream writeStream, byte[] data)
        {
            WriteSreamOperations operation = (WriteSreamOperations)Random.Next((int)WriteSreamOperations.Count);
            switch (operation)
            {
                case WriteSreamOperations.SetLength:
                    this.TestRandomSetLengthOperation(targetStream, writeStream, data);
                    break;
                case WriteSreamOperations.Write:
                    this.TestRandomWriteOperation(targetStream, writeStream, data);
                    break;
                case WriteSreamOperations.Seek:
                    this.TestRandomSeekOperation(writeStream, targetStream, data.Length);
                    break;
                case WriteSreamOperations.Flush:
                    this.TestRandomFlushOperation(writeStream, targetStream);
                    break;
                default:
                    Assert.Fail("Invalid operation");
                    break;
            }
            return operation;
        }

        private void TestRandomFlushOperation(AlignedWriteStream writeStream, FileStream targetStream)
        {
            writeStream.Flush();
            targetStream.Flush();

            Assert.AreEqual(targetStream.Position, writeStream.Position, "Position mismatch");
            Assert.AreEqual(targetStream.Length, writeStream.Length, "Length mismatch");
        }

        private WriteSreamOperations TestRandomWriteOperation(FileStream targetStream, AlignedWriteStream writeStream, byte[] data)
        {
            int offsetUpperBound = data.Length;
            int offset = Random.Next(offsetUpperBound);
            if (0 == Random.Next(2))
            {
                offset -= (offset % writeStream.AlignmentSize);
            }
            int countUpperBound = Math.Min((int)(data.Length - targetStream.Position), data.Length - offset);
            int count = Random.Next(countUpperBound);
            if (0 == Random.Next(2))
            {
                count -= (count % writeStream.AlignmentSize);
            }
            this.TestRandomWriteOperation(targetStream, writeStream, data, offset, count);
            return WriteSreamOperations.Write;
        }

        private void TestRandomWriteOperation(FileStream targetStream, AlignedWriteStream writeStream,
            byte[] data, int offset, int count)
        {
            long possition = targetStream.Position;
            long length = targetStream.Length;

            targetStream.Write(data, offset, count);
            writeStream.Write(data, offset, count);

            Assert.AreEqual(targetStream.Position, writeStream.Position, string.Format("Position mismatch (Original {0})", possition));
            Assert.AreEqual(targetStream.Length, writeStream.Length, string.Format("Length mismatch (Original {0})", length));
        }

        private void TestRandomSetLengthOperation(FileStream targetStream, AlignedWriteStream writeStream, byte[] data)
        {
            int lengthUpperBound = (int)data.Length;
            int length = Random.Next(lengthUpperBound);
            if (0 == Random.Next(2))
            {
                length -= (length % writeStream.AlignmentSize);
            }
            this.TestRandomSetLengthOperation(targetStream, writeStream, length);
        }

        private void TestRandomSetLengthOperation(FileStream targetStream, AlignedWriteStream writeStream, long length)
        {
            long position = targetStream.Position;

            targetStream.SetLength(length);
            writeStream.SetLength(length);

            Assert.AreEqual(targetStream.Position, writeStream.Position, string.Format("Position mismatch (Original: {0})", position));
            Assert.AreEqual(targetStream.Length, writeStream.Length, "Length mismatch");
        }

        #endregion

        #region Position Tests

        [TestMethod]
        public void TestAlignedWriteStreamWritePosition()
        {
            this.TestAlignedWriteStreamWritePosition(@"WriteData.bin");
        }

        private void TestAlignedWriteStreamWritePosition(string targetFilename)
        {
            var buffer = new byte[1024];
            using (FileStream targetFileStream = new FileStream(targetFilename, FileMode.Create, FileAccess.Write, FileShare.Write, 8,
                FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
            {
                using (AlignedWriteStream writeStream = new AlignedWriteStream(targetFileStream))
                {
                    writeStream.Write(buffer, 0, 500);
                    Assert.AreEqual(500, writeStream.Position, "Invlid position");
                    Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(500, writeStream.Length, "Invlid length");
                    Assert.AreEqual(0, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 500, 524);
                    Assert.AreEqual(1024, writeStream.Position, "Invlid position");
                    Assert.AreEqual(writeStream.Position, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1024, writeStream.Length, "Invlid length");
                    Assert.AreEqual(writeStream.Length, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 19, 512);
                    Assert.AreEqual(1536, writeStream.Position, "Invlid position");
                    Assert.AreEqual(writeStream.Position, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1536, writeStream.Length, "Invlid length");
                    Assert.AreEqual(writeStream.Length, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 0, 1000);
                    Assert.AreEqual(2536, writeStream.Position, "Invlid position");
                    Assert.AreEqual(2048, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2536, writeStream.Length, "Invlid length");
                    Assert.AreEqual(2048, targetFileStream.Length, "Invlid length");
                }
                Assert.AreEqual(2560, targetFileStream.Position, "Invlid position");
                Assert.AreEqual(2560, targetFileStream.Length, "Invlid position");
            }
        }

        [TestMethod]
        public void TestAlignedWriteStreamSeekPosition()
        {
            this.TestAlignedWriteStreamSeekPosition(@"WriteData.bin");
        }

        private void TestAlignedWriteStreamSeekPosition(string targetFilename)
        {
            var buffer = new byte[1024];
            using (FileStream targetFileStream = new FileStream(targetFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.Write, 8,
                FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
            {
                using (AlignedWriteStream writeStream = new AlignedWriteStream(targetFileStream))
                {
                    writeStream.Write(buffer, 0, 500);
                    Assert.AreEqual(500, writeStream.Position, "Invlid position");
                    Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(500, writeStream.Length, "Invlid length");
                    Assert.AreEqual(0, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(400, SeekOrigin.Begin);
                    Assert.AreEqual(400, writeStream.Position, "Invlid position");
                    Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(500, writeStream.Length, "Invlid length");
                    Assert.AreEqual(0, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 0, 650);
                    Assert.AreEqual(1050, writeStream.Position, "Invlid position");
                    Assert.AreEqual(1024, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1024, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(1000, SeekOrigin.Begin);
                    Assert.AreEqual(1000, writeStream.Position, "Invlid position");
                    Assert.AreEqual(512, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1536, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 19, 24);
                    Assert.AreEqual(1024, writeStream.Position, "Invlid position");
                    Assert.AreEqual(writeStream.Position, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1536, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(12, SeekOrigin.Begin);
                    Assert.AreEqual(12, writeStream.Position, "Invlid position");
                    Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1536, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 19, 500);
                    Assert.AreEqual(512, writeStream.Position, "Invlid position");
                    Assert.AreEqual(writeStream.Position, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1536, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(-2, SeekOrigin.End);
                    Assert.AreEqual(1048, writeStream.Position, "Invlid position");
                    Assert.AreEqual(1024, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1536, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 0, 1000);
                    Assert.AreEqual(2048, writeStream.Position, "Invlid position");
                    Assert.AreEqual(2048, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2048, writeStream.Length, "Invlid length");
                    Assert.AreEqual(writeStream.Length, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(-1000, SeekOrigin.Current);
                    Assert.AreEqual(1048, writeStream.Position, "Invlid position");
                    Assert.AreEqual(1024, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2048, writeStream.Length, "Invlid length");
                    Assert.AreEqual(writeStream.Length, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(0, SeekOrigin.End);
                    Assert.AreEqual(2048, writeStream.Position, "Invlid position");
                    Assert.AreEqual(2048, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2048, writeStream.Length, "Invlid length");
                    Assert.AreEqual(writeStream.Length, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 11, 2);
                    Assert.AreEqual(2050, writeStream.Position, "Invlid position");
                    Assert.AreEqual(2048, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(2048, targetFileStream.Length, "Invlid length");
                    writeStream.Seek(4000, SeekOrigin.Begin);
                    Assert.AreEqual(4000, writeStream.Position, "Invlid position");
                    Assert.AreEqual(3584, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2050, writeStream.Length, "Invlid length");
                    Assert.AreEqual(2560, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 40, 95);
                    Assert.AreEqual(4095, writeStream.Position, "Invlid position");
                    Assert.AreEqual(3584, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(4095, writeStream.Length, "Invlid length");
                    Assert.AreEqual(2560, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 40, 1);
                    Assert.AreEqual(4096, writeStream.Position, "Invlid position");
                    Assert.AreEqual(4096, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(4096, writeStream.Length, "Invlid length");
                    Assert.AreEqual(4096, targetFileStream.Length, "Invlid length");
                }
                Assert.AreEqual(4096, targetFileStream.Position, "Invlid position");
                Assert.AreEqual(4096, targetFileStream.Length, "Invlid length");
            }
        }

        #endregion

        #region Length Tests

        [TestMethod]
        public void TestAlignedWriteStreamLength()
        {
            this.TestAlignedWriteStreamLength(@"WriteData.bin");
        }

        private void TestAlignedWriteStreamLength(string targetFilename)
        {
            var buffer = new byte[1024];
            using (FileStream targetFileStream = new FileStream(targetFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.Write, 8,
                FileOptions.WriteThrough | Win32.FileFlagNoBuffering))
            {
                using (AlignedWriteStream writeStream = new AlignedWriteStream(targetFileStream))
                {
                    writeStream.Write(buffer, 0, 1000);
                    Assert.AreEqual(1000, writeStream.Position, "Invlid position");
                    Assert.AreEqual(512, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1000, writeStream.Length, "Invlid length");
                    Assert.AreEqual(512, targetFileStream.Length, "Invlid length");
                    writeStream.SetLength(1024);
                    Assert.AreEqual(1000, writeStream.Position, "Invlid position");
                    Assert.AreEqual(512, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1024, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1024, targetFileStream.Length, "Invlid length");
                    writeStream.SetLength(512);
                    Assert.AreEqual(512, writeStream.Position, "Invlid position");
                    Assert.AreEqual(512, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(512, writeStream.Length, "Invlid length");
                    Assert.AreEqual(512, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 0, 1000);
                    Assert.AreEqual(1512, writeStream.Position, "Invlid position");
                    Assert.AreEqual(1024, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(1512, writeStream.Length, "Invlid length");
                    Assert.AreEqual(1024, targetFileStream.Length, "Invlid length");
                    writeStream.SetLength(2048);
                    Assert.AreEqual(1512, writeStream.Position, "Invlid position");
                    Assert.AreEqual(1024, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2048, writeStream.Length, "Invlid length");
                    Assert.AreEqual(2048, targetFileStream.Length, "Invlid length");
                    writeStream.Write(buffer, 345, 38);
                    Assert.AreEqual(1550, writeStream.Position, "Invlid position");
                    Assert.AreEqual(1536, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(2048, writeStream.Length, "Invlid length");
                    Assert.AreEqual(2048, targetFileStream.Length, "Invlid length");
                    writeStream.SetLength(0);
                    Assert.AreEqual(0, writeStream.Position, "Invlid position");
                    Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(0, writeStream.Length, "Invlid length");
                    Assert.AreEqual(0, targetFileStream.Length, "Invlid length");
                    writeStream.Flush();
                    Assert.AreEqual(0, writeStream.Position, "Invlid position");
                    Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                    Assert.AreEqual(0, writeStream.Length, "Invlid length");
                    Assert.AreEqual(0, targetFileStream.Length, "Invlid length");

                }
                Assert.AreEqual(0, targetFileStream.Position, "Invlid position");
                Assert.AreEqual(0, targetFileStream.Length, "Invlid position");
            }
        }

        #endregion

        #endregion

        #region Read Stream Tests

        #region Random Option Tests

        public enum ReadSreamOperations
        {
            Seek,
            Read,
            Count
        }

        [TestMethod]
        public void TestReadStreamRandomOperations()
        {
            int count = 1000;
            string filename = @"SmallData.bin";

            this.TestReadStreamRandomOperations(count, filename, Win32.PAGE_SIZE);
            this.TestReadStreamRandomOperations(count, filename, Win32.SECTOR_SIZE);
        }

        private void TestReadStreamRandomOperations(int count, string inputfile, int bufferSize)
        {
            byte[] data = File.ReadAllBytes(inputfile);
            using (MemoryStream dataStream = new MemoryStream(data))
            {
                using (FileStream sourceFileStream = new FileStream(inputfile,
                    FileMode.Open, FileAccess.Read, FileShare.None, bufferSize,
                    Win32.FileFlagNoBuffering))
                {
                    using (AlignedReadStream readStream = new AlignedReadStream(sourceFileStream, bufferSize))
                    {
                        while (0 < count)
                        {
                            this.TestRandomOperation(readStream, dataStream);
                            count--;
                        }
                    }
                }
            }
        }

        private void TestRandomOperation(AlignedReadStream readStream, Stream dataStream)
        {
            ReadSreamOperations operation = (ReadSreamOperations)Random.Next((int)ReadSreamOperations.Count);
            switch (operation)
            {
                case ReadSreamOperations.Seek:
                    this.TestRandomSeekOperation(readStream, dataStream, (int)dataStream.Length);
                    break;
                case ReadSreamOperations.Read:
                    this.TestRandomReadOperation(readStream, dataStream);
                    break;
                default:
                    Assert.Fail("Invalid operation");
                    break;
            }
        }

        private void TestRandomReadOperation(AlignedReadStream readStream, Stream dataStream)
        {
            int offsetUpperBound = (int)(dataStream.Length - dataStream.Position);
            int offset = Random.Next(offsetUpperBound);
            if (0 == Random.Next(2))
            {
                offset -= (offset % readStream.AlignmentSize);
            }
            int countUpperBound = (int)(dataStream.Length - dataStream.Position - offset);
            int count = Random.Next(countUpperBound);
            if (0 == Random.Next(2))
            {
                count -= (count % readStream.AlignmentSize);
            }
            this.TestRandomReadOperation(readStream, dataStream, offset, count);
        }

        private void TestRandomReadOperation(AlignedReadStream readStream, Stream dataStream, int offset, int count)
        {
            var buffer1 = new byte[offset + count];
            int bytesRead1 = readStream.Read(buffer1, offset, count);
            var buffer2 = new byte[offset + count];
            int bytesRead2 = dataStream.Read(buffer2, offset, count);

            Assert.AreEqual(readStream.Position, dataStream.Position, "Position mismatch");
            Assert.AreEqual(bytesRead1, bytesRead2, "Bytes read mismatch");
            Assert.IsTrue(buffer1.SequenceEqual(buffer2), "Data mismatch");
        }

        #endregion

        #region Random Read Data Tests

        [TestMethod]
        public void TestRandomReadData()
        {
            int count = 100;
            string filename = @"SmallData.bin";

            this.TestRandomReadData(count, filename, Win32.PAGE_SIZE, this.TestRandomReadData);
            this.TestRandomReadData(count, filename, Win32.SECTOR_SIZE, this.TestRandomReadData);

            this.TestRandomReadData(count, filename, Win32.PAGE_SIZE, this.TestRandomReadDataAligned);
            this.TestRandomReadData(count, filename, Win32.SECTOR_SIZE, this.TestRandomReadDataAligned);

            this.TestRandomReadData(count, filename, Win32.PAGE_SIZE, this.TestRandomReadDataAlignedCount);
            this.TestRandomReadData(count, filename, Win32.SECTOR_SIZE, this.TestRandomReadDataAlignedCount);

            this.TestRandomReadData(count, filename, Win32.PAGE_SIZE, this.TestRandomReadDataAlignedOffset);
            this.TestRandomReadData(count, filename, Win32.SECTOR_SIZE, this.TestRandomReadDataAlignedOffset);
        }

        private void TestRandomReadData(int count, string inputfile, int bufferSize, Action<AlignedReadStream, byte[]> readTester)
        {
            byte[] data = File.ReadAllBytes(inputfile);
            using (FileStream sourceFileStream = new FileStream(inputfile,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferSize,
                Win32.FileFlagNoBuffering))
            {
                using (AlignedReadStream readStream = new AlignedReadStream(sourceFileStream, bufferSize))
                {
                    while (0 < count)
                    {
                        readTester(readStream, data);
                        count--;
                    }
                }
            }
        }

        private void TestRandomReadDataAligned(AlignedReadStream readStream, byte[] data)
        {
            int positionUpperBound = data.Length;
            int position = Random.Next(positionUpperBound);
            position -= (position % readStream.AlignmentSize);
            int countUpperBound = data.Length - position;
            int count = Random.Next(countUpperBound);
            count -= (count % readStream.AlignmentSize);

            this.TestReadData(readStream, data, position, count);
        }

        private void TestRandomReadDataAlignedCount(AlignedReadStream readStream, byte[] data)
        {
            int positionUpperBound = data.Length;
            int position = Random.Next(positionUpperBound);
            int countUpperBound = data.Length - position;
            int count = Random.Next(countUpperBound);
            count -= (count % readStream.AlignmentSize);

            this.TestReadData(readStream, data, position, count);
        }

        private void TestRandomReadDataAlignedOffset(AlignedReadStream readStream, byte[] data)
        {
            int positionUpperBound = data.Length;
            int position = Random.Next(positionUpperBound);
            position -= (position % readStream.AlignmentSize);
            int countUpperBound = data.Length - position;
            int count = Random.Next(countUpperBound);

            this.TestReadData(readStream, data, position, count);
        }

        private void TestRandomReadData(AlignedReadStream readStream, byte[] data)
        {
            int positionUpperBound = data.Length;
            int position = Random.Next(positionUpperBound);
            int countUpperBound = data.Length - position;
            int count = Random.Next(countUpperBound);

            this.TestReadData(readStream, data, position, count);
        }

        private void TestReadData(AlignedReadStream readStream, byte[] data, int offset, int count)
        {
            var buffer = new byte[count];
            readStream.Seek(offset, SeekOrigin.Begin);
            readStream.Read(buffer, 0, count);
            Assert.IsTrue(buffer.SequenceEqual(data.Skip(offset).Take(count)), "Data mismatch");
        }

        #endregion

        #region Position Tests

        [TestMethod]
        public void TestAlignedReadStreamPosition()
        {
            this.TestAlignedReadStreamPosition(@"SmallData.bin", 4096);
        }

        private void TestAlignedReadStreamPosition(string inputfile, int bufferSize)
        {
            var buffer = new byte[1000];
            using (FileStream sourceFileStream = new FileStream(inputfile,
                FileMode.Open, FileAccess.Read, FileShare.None, bufferSize,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                using (AlignedReadStream readStream = new AlignedReadStream(sourceFileStream))
                {
                    readStream.Seek(12, SeekOrigin.Begin);
                    Assert.AreEqual(12, readStream.Position, "Invlid position");
                    Assert.AreEqual(0, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(512, SeekOrigin.Begin);
                    Assert.AreEqual(512, readStream.Position, "Invlid position");
                    Assert.AreEqual(512, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(600, SeekOrigin.Begin);
                    Assert.AreEqual(600, readStream.Position, "Invlid position");
                    Assert.AreEqual(512, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(4096, SeekOrigin.Begin);
                    Assert.AreEqual(4096, readStream.Position, "Invlid position");
                    Assert.AreEqual(4096, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(-3000, SeekOrigin.Current);
                    Assert.AreEqual(1096, readStream.Position, "Invlid position");
                    Assert.AreEqual(1024, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(1024, SeekOrigin.Current);
                    Assert.AreEqual(2120, readStream.Position, "Invlid position");
                    Assert.AreEqual(2048, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(3385, SeekOrigin.Current);
                    Assert.AreEqual(5505, readStream.Position, "Invlid position");
                    Assert.AreEqual(5120, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(-100, SeekOrigin.End);
                    Assert.AreEqual(readStream.Length - 100, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length - 100 - (sourceFileStream.Length - 100) % 512, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(-512, SeekOrigin.End);
                    Assert.AreEqual(readStream.Length - 512, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length - 512 - (sourceFileStream.Length - 512) % 512, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(-2658, SeekOrigin.End);
                    Assert.AreEqual(readStream.Length - 2658, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length - 2658 - (sourceFileStream.Length - 2658) % 512, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(0, readStream.Position, "Invlid position");
                    Assert.AreEqual(0, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, buffer.Length);
                    Assert.AreEqual(buffer.Length, readStream.Position, "Invlid position");
                    Assert.AreEqual(buffer.Length + (512 - buffer.Length % 512), sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 4);
                    Assert.AreEqual(buffer.Length + 4, readStream.Position, "Invlid position");
                    Assert.AreEqual(buffer.Length + 4 + (512 - (buffer.Length + 4) % 512), sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 21);
                    Assert.AreEqual(buffer.Length + 4 + 21, readStream.Position, "Invlid position");
                    Assert.AreEqual(buffer.Length + 4 + 21 + (512 - (buffer.Length + 4 + 21) % 512), sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 1);
                    Assert.AreEqual(buffer.Length + 4 + 21 + 1, readStream.Position, "Invlid position");
                    Assert.AreEqual(buffer.Length + 4 + 21 + 1 + (512 - (buffer.Length + 4 + 21 + 1) % 512), sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 510);
                    Assert.AreEqual(buffer.Length + 4 + 21 + 1 + 510, readStream.Position, "Invlid position");
                    Assert.AreEqual(readStream.Position, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 24);
                    Assert.AreEqual(buffer.Length + 4 + 21 + 1 + 510 + 24, readStream.Position, "Invlid position");
                    Assert.AreEqual(buffer.Length + 4 + 21 + 1 + 510 + 24 + (512 - (buffer.Length + 4 + 21 + 1 + 510 + 24) % 512), sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, buffer.Length);
                    Assert.AreEqual(2 * buffer.Length + 4 + 21 + 1 + 510 + 24, readStream.Position, "Invlid position");
                    Assert.AreEqual(readStream.Position, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(0, SeekOrigin.End);
                    Assert.AreEqual(readStream.Length, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length - (sourceFileStream.Length % 512), sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, buffer.Length);
                    Assert.AreEqual(readStream.Length, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, buffer.Length);
                    Assert.AreEqual(readStream.Length, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(0, readStream.Position, "Invlid position");
                    Assert.AreEqual(0, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(2, SeekOrigin.Begin);
                    Assert.AreEqual(2, readStream.Position, "Invlid position");
                    Assert.AreEqual(0, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 10);
                    Assert.AreEqual(12, readStream.Position, "Invlid position");
                    Assert.AreEqual(512, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(500, SeekOrigin.Begin);
                    Assert.AreEqual(500, readStream.Position, "Invlid position");
                    Assert.AreEqual(512, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 12);
                    Assert.AreEqual(512, readStream.Position, "Invlid position");
                    Assert.AreEqual(readStream.Position, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(-1 * readStream.Length % 512, SeekOrigin.End);
                    Assert.AreEqual(readStream.Length - readStream.Length % 512, readStream.Position, "Invlid position");
                    Assert.AreEqual(readStream.Position, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 24);
                    Assert.AreEqual((readStream.Length - readStream.Length % 512) + 24, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(100, SeekOrigin.Current);
                    Assert.AreEqual((readStream.Length - readStream.Length % 512) + 24 + 100, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length, sourceFileStream.Position, "Invlid position");
                    readStream.Read(buffer, 0, 100);
                    Assert.AreEqual((readStream.Length - readStream.Length % 512) + 24 + 100 + 100, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length, sourceFileStream.Position, "Invlid position");
                    readStream.Seek(0, SeekOrigin.End);
                    Assert.AreEqual(readStream.Length, readStream.Position, "Invlid position");
                    Assert.AreEqual(sourceFileStream.Length, sourceFileStream.Position, "Invlid position");
                }
            }
        }

        #endregion

        #endregion

        #region Private Random Operation Methods

        private void TestRandomSeekOperation(AlignedStreamBase alignedStream, Stream dataStream, int offsetUpperBound)
        {
            int offset = Random.Next(offsetUpperBound);
            if (0 == Random.Next(2))
            {
                offset -= (offset % alignedStream.AlignmentSize);
            }
            SeekOrigin origin = (SeekOrigin)Random.Next(3);
            this.TestRandomSeekOperation(alignedStream, dataStream, offset, origin);
        }

        private void TestRandomSeekOperation(AlignedStreamBase alignedStream, Stream dataStream, long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;
                case SeekOrigin.Current:
                    offset -= dataStream.Position;
                    break;
                case SeekOrigin.End:
                    offset -= dataStream.Length;
                    break;
                default:
                    Assert.Fail("Invalid seek origin");
                    break;
            }

            long dataPosition = dataStream.Seek(offset, origin);
            long alignedPosition = alignedStream.Seek(offset, origin);

            Assert.AreEqual(alignedPosition, dataPosition, "Position mismatch");
            Assert.AreEqual(alignedStream.Position, dataStream.Position, "Position mismatch");
            Assert.AreEqual(alignedStream.Length, dataStream.Length, "Length mismatch");
        }

        #endregion
    }
}
