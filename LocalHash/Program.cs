using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using CopyUtils;
using System.Diagnostics;
using ArgsUtils;

namespace LocalHash
{
    public class Program
    {
        public static readonly OptionDescriptorSet<HashOptions> HashOptionDescriptorSet = new OptionDescriptorSet<HashOptions>
        {
            { "-h", "--hash", "algorithm", "hash algorithm the options are: " + string.Join(", ", HashAlgorithmNames.ALL),
                (HashOptions o, string v) => o.HashAlgorithm = v,
                (string parameterName, string value) => OptionValidators.ValidateContains(parameterName, value, HashAlgorithmNames.ALL) },
            { "-v", "--validate", "hash value", "validate source file hash equals hash value",
                (HashOptions o, string v) => o.ExpectedHash = v, OptionValidators.ValidateNotNullOrEmpty },
            { "-o", "--output", "filename", "write the hash to an output file",
                (HashOptions o, string v) => o.OutputFilename = v, OptionValidators.ValidatePath },
            { "-w", "--overwrite", "make sures that the output file will be overwriten, same as deleting the file",
                (HashOptions o, bool v) => o.Overwrite = v },
            { "-s", "--size", "buffer size", string.Format(
                "size of the buffer to be used for hashing, in units of sector size ({0})", Win32.SECTOR_SIZE),
                (HashOptions o, int number) => o.BufferSize = number, OptionValidators.ValidateGraterThanZero },
            { "-m", "--measure", "measure the time of the hash",
                (HashOptions o, bool v) => o.MeasureTime = v },
        };

        public static int Main(string[] args)
        {
            HashOptions hashOptions = null;
            try
            {
                hashOptions = ParseArgs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Invalid Usage: {0}", ex.Message));
                PrintUsage();
                return -1;
            }

            try
            {
                Hash(hashOptions);
            }
            catch (HashMismatchException ex)
            {
                Console.WriteLine(ex.Message);
                return -2;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Hash Failure: {0}", ex.Message));
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            return 0;
        }

        private static void Hash(HashOptions hashOptions)
        {
            if (false == File.Exists(hashOptions.SourceFilename))
            {
                throw new FileNotFoundException("Hash source was not found", hashOptions.SourceFilename);
            }

            var localHash = new LocalHash();
            var stopwatch = new Stopwatch();
            if (hashOptions.MeasureTime)
            {
                stopwatch.Start();
            }

            HashFile(localHash, hashOptions);

            if (hashOptions.MeasureTime)
            {
                stopwatch.Stop();
                Console.WriteLine(string.Format("Hash took {0} minutes ({1} seconds)",
                    stopwatch.Elapsed.TotalMinutes, stopwatch.Elapsed.TotalSeconds));
            }
        }

        private static void HashFile(LocalHash localHash, HashOptions hashOptions)
        {
            FileMode fileMode = FileMode.Create;
            if (false == hashOptions.Overwrite)
            {
                fileMode = FileMode.CreateNew;
            }
            var hashAlgorithm = HashAlgorithmNames.GetHashAlgorithm(hashOptions.HashAlgorithm);
            if (string.IsNullOrEmpty(hashOptions.OutputFilename))
            {
                if (string.IsNullOrEmpty(hashOptions.ExpectedHash))
                {
                    byte[] hash = localHash.ComputeHash(hashOptions.SourceFilename,
                        hashAlgorithm, hashOptions.BufferSize);
                    ReportHash(hashOptions.HashAlgorithm, localHash.GetHashString(hash));
                }
                else
                {
                    localHash.ValidateHash(hashOptions.SourceFilename,
                        hashAlgorithm, hashOptions.BufferSize, localHash.GetHashArray(hashOptions.ExpectedHash));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(hashOptions.ExpectedHash))
                {
                    localHash.ComputeHash(hashOptions.SourceFilename, hashOptions.OutputFilename,
                        fileMode, hashAlgorithm, hashOptions.BufferSize);
                }
                else
                {
                    localHash.ValidateHash(hashOptions.SourceFilename, hashOptions.OutputFilename, fileMode,
                        hashAlgorithm, hashOptions.BufferSize, localHash.GetHashArray(hashOptions.ExpectedHash));
                }
            }
        }

        private static void ReportHash(string hashName, string hashValue)
        {
            Console.WriteLine(string.Format("{0} Hash: {1}", hashName, hashValue));
        }

        private static HashOptions ParseArgs(string[] args)
        {
            var hashOptions = new HashOptions();
            hashOptions.SourceFilename = args[0];
            OptionValidators.ValidatePath("source filename", hashOptions.SourceFilename);
            foreach (var option in args.Skip(1))
            {
                if (false == Program.HashOptionDescriptorSet.Apply(hashOptions, option))
                {
                    throw new ArgumentException("Invalid argument", option);
                }
            }
            return hashOptions;
        }

        public static void PrintUsage()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("LocalHash.exe <source filename> [OPTIONS]");
            builder.AppendLine(Program.HashOptionDescriptorSet.ToString());
            Console.Write(builder.ToString());
        }
    }
}
