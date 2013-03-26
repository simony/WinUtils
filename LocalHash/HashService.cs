using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace LocalHash
{
    public class HashService
    {
        #region Members

        protected LocalHash _localHash = new LocalHash();

        #endregion

        #region Public Methods

        public void ValidateHash(string inputFilename, string outputFilename, FileMode fileMode,
            string hashAlgorithmName, int bufferSize, byte[] hash)
        {
            HashAlgorithm hashAlgorithm = HashAlgorithmNames.GetHashAlgorithm(hashAlgorithmName);
            this._localHash.ValidateHash(inputFilename, outputFilename, fileMode, hashAlgorithm, bufferSize, hash);
        }

        public void ValidateHash(string inputFilename, string hashAlgorithmName, int bufferSize, byte[] hash)
        {
            HashAlgorithm hashAlgorithm = HashAlgorithmNames.GetHashAlgorithm(hashAlgorithmName);
            this._localHash.ValidateHash(inputFilename, hashAlgorithm, bufferSize, hash);
        }

        public void ComputeHash(string inputFilename, string outputFilename, FileMode fileMode, string hashAlgorithmName, int bufferSize)
        {
            HashAlgorithm hashAlgorithm = HashAlgorithmNames.GetHashAlgorithm(hashAlgorithmName);
            this._localHash.ComputeHash(inputFilename, outputFilename, fileMode, hashAlgorithm, bufferSize);
        }

        public byte[] ComputeHash(string inputFilename, string hashAlgorithmName, int bufferSize)
        {
            HashAlgorithm hashAlgorithm = HashAlgorithmNames.GetHashAlgorithm(hashAlgorithmName);
            return this._localHash.ComputeHash(inputFilename, hashAlgorithm, bufferSize);
        }

        #endregion
    }
}
