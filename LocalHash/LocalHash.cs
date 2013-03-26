using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CopyUtils;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;

namespace LocalHash
{
    public class LocalHash
    {
        #region Members

        protected int _sectorSize = Win32.SECTOR_SIZE;

        #endregion

        #region Constructors

        public LocalHash(int sectorSize)
        {
            this._sectorSize = sectorSize;
        }

        public LocalHash()
            : this(Win32.SECTOR_SIZE)
        {
        }

        #endregion

        #region Public Methods

        public void ValidateHash(string inputFilename, string outputFilename, FileMode fileMode,
            HashAlgorithm hashAlgorithm, int bufferSize, byte[] hash)
        {
            this.ValidateHash(inputFilename, hashAlgorithm, bufferSize, hash);
            this.WriteHash(outputFilename, fileMode, hash);
        }

        public void ValidateHash(string inputFilename, HashAlgorithm hashAlgorithm, int bufferSize, byte[] hash)
        {
            var calculatedHash = this.ComputeHash(inputFilename, hashAlgorithm, bufferSize);
            if (false == calculatedHash.SequenceEqual(hash))
            {
                throw new HashMismatchException(this.GetHashString(calculatedHash), this.GetHashString(hash));
            }
        }

        public void WriteHash(string outputFilename, FileMode fileMode, byte[] hash)
        {
            using (FileStream fileStream = new FileStream(outputFilename, fileMode, FileAccess.Write))
            {
                byte[] hashString = Encoding.ASCII.GetBytes(this.GetHashString(hash));
                fileStream.Write(hashString, 0, hashString.Length);
            }
        }

        public void ComputeHash(string inputFilename, string outputFilename, FileMode fileMode, HashAlgorithm hashAlgorithm, int bufferSize)
        {
            byte[] hash = this.ComputeHash(inputFilename, hashAlgorithm, bufferSize);
            this.WriteHash(outputFilename, fileMode, hash);
        }

        public byte[] ComputeHash(string inputFilename, HashAlgorithm hashAlgorithm, int bufferSize)
        {
            var buffer = new byte[bufferSize * this._sectorSize];
            using (FileStream fileStream = new FileStream(inputFilename,
                FileMode.Open, FileAccess.Read, FileShare.None, buffer.Length,
                Win32.FileFlagNoBuffering | FileOptions.SequentialScan))
            {
                return this.ComputeHash(fileStream, hashAlgorithm, buffer);
            }
        }

        public byte[] ComputeHash(FileStream fileStream, HashAlgorithm hashAlgorithm, byte[] buffer)
        {
            while (true)
            {
                int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == buffer.Length)
                {
                    hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                }
                else
                {
                    hashAlgorithm.TransformFinalBlock(buffer, 0, bytesRead);
                    return hashAlgorithm.Hash;
                }
            }
        }

        public string GetHashString(byte[] hash)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.AppendFormat("{0:X2}", hash[i]);
            }
            return builder.ToString();
        }

        public byte[] GetHashArray(string hashString)
        {
            if (0 != hashString.Length % 2)
            {
                throw new ArgumentException(string.Format("Invalidate hash string ({0}).", hashString));
            }

            byte[] hash = new byte[hashString.Length / 2];
            for (int index = 0; index < hash.Length; index++)
            {
                string byteValue = hashString.Substring(index * 2, 2);
                hash[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return hash;
        }

        #endregion
    }
}
