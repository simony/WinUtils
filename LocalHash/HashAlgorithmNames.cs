using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace LocalHash
{
    public static class HashAlgorithmNames
    {
        #region Constants

        public const string MD5 = "MD5";
        public const string SHA1 = "SHA1";
        public const string SH256 = "SH256";
        public const string SH384 = "SH384";
        public const string SHA512 = "SHA512";
        public static readonly string[] ALL = new string[] { MD5, SHA1, SH256, SH384, SHA512 };

        #endregion

        #region Public Methods

        public static HashAlgorithm GetHashAlgorithm(string algorithmName)
        {
            switch (algorithmName)
            {
                case HashAlgorithmNames.MD5:
                    return System.Security.Cryptography.MD5.Create();
                case HashAlgorithmNames.SHA1:
                    return SHA1Managed.Create();
                case HashAlgorithmNames.SH256:
                    return SHA256Managed.Create();
                case HashAlgorithmNames.SH384:
                    return SHA384Managed.Create();
                case HashAlgorithmNames.SHA512:
                    return SHA512Managed.Create();
                default:
                    throw new ArgumentException(string.Format("Invalid hash algorithm {0}.", algorithmName));
            }
        }

        #endregion
    }
}
