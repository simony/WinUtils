using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SevenZip;
using System.IO;
using CopyUtils;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using ArgsUtils;

namespace LocalArch
{
    public class Program
    {
        public static readonly OptionDescriptorSet<ArchOptions> ArchOptionDescriptorSet = new OptionDescriptorSet<ArchOptions>
        {
            { "-f", "--format", "format", "archive format the options are: " + string.Join(", ", Enum.GetNames(typeof(OutArchiveFormat))),
                (ArchOptions o, string v) => o.ArchiveFormat = (OutArchiveFormat)Enum.Parse(typeof(OutArchiveFormat), v),
                (string parameterName, string value) => OptionValidators.ValidateContains(parameterName, value, Enum.GetNames(typeof(OutArchiveFormat))) },
            { "-a", "--archive", "algorithm", "archive algorithm the options are: " + string.Join(", ", Enum.GetNames(typeof(CompressionMethod))),
                (ArchOptions o, string v) => o.CompressionMethod = (CompressionMethod)Enum.Parse(typeof(CompressionMethod), v),
                (string parameterName, string value) => OptionValidators.ValidateContains(parameterName, value, Enum.GetNames(typeof(CompressionMethod))) },
            { "-l", "--level", "level", "archive level the options are: " + string.Join(", ", Enum.GetNames(typeof(CompressionLevel))),
                (ArchOptions o, string v) => o.CompressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), v),
                (string parameterName, string value) => OptionValidators.ValidateContains(parameterName, value, Enum.GetNames(typeof(CompressionLevel))) },
            { "-e", "--encrypt", "method:password", "encryption method the options are: " + string.Join(", ", Enum.GetNames(typeof(ZipEncryptionMethod))),
                (ArchOptions o, string v) => SetEncryption(o, v) },
            { "-d", "--decompress", "password", "decompress with optional password, required if the archive is encryped",
                (ArchOptions o, string v) => SetDecompress(o, v) },
            { "-k", "--check", "check the compressed file",
                (ArchOptions o, bool v) => o.Check = v },
            { "-o", "--output", "path", "output file or path",
                (ArchOptions o, string v) => o.OutputFilename = v, OptionValidators.ValidatePath },
            { "-w", "--overwrite", "make sures that the target file will be overwriten, same as deleting the file",
                (ArchOptions o, bool v) => o.Overwrite = v },
            { "-p", "--precent", "preallocation percent", "percent of the source file size to pre allocate",
                (ArchOptions o, int number) => o.PreallocationPercent = number, OptionValidators.ValidateGraterThanZero },
            { "-s", "--size", "buffer size", string.Format(
                "size of the buffer to be used for compress/decompress, in units of sector size ({0})", Win32.SECTOR_SIZE),
                (ArchOptions o, int number) => o.BufferSize = number, OptionValidators.ValidateGraterThanZero },
            { "-m", "--measure", "measure the time of the archive",
                (ArchOptions o, bool v) => o.MeasureTime = v },
            { "-c", "--custom", "name=value", "custom parameter - for advanced users only",
                (ArchOptions o, string v) => SetCustomParameter(o, v) },
        };

        private static void SetDecompress(ArchOptions archOptions, string password)
        {
            archOptions.Decompress = true;
            archOptions.Password = password;
        }

        private static void SetCustomParameter(ArchOptions archOptions, string value)
        {
            var parts = value.Split('=');
            if (2 != parts.Length)
            {
                throw new ArgumentException(value);
            }
            archOptions.CustomParameters[parts[0]] = parts[1];
        }

        private static void SetEncryption(ArchOptions archOptions, string value)
        {
            var parts = value.Split(':');
            if (2 != parts.Length)
            {
                throw new ArgumentException(value);
            }
            string method = parts[0];
            OptionValidators.ValidateContains("method", method, Enum.GetNames(typeof(ZipEncryptionMethod)));
            string password = parts[1];
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Invalid password.");
            }
            archOptions.EncryptionMethod =  (ZipEncryptionMethod)Enum.Parse(typeof(ZipEncryptionMethod), parts[0]);
            archOptions.Password = password;
        }

        public static int Main(string[] args)
        {
            ArchOptions archOptions = null;
            try
            {
                archOptions = ParseArgs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Invalid Usage: {0}", ex.Message));
                PrintUsage();
                return -1;
            }

            try
            {
                Archive(archOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Archive Failure: {0}", ex.Message));
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            return 0;
        }

        private static void Archive(ArchOptions archOptions)
        {
            if (false == File.Exists(archOptions.SourceFilename))
            {
                throw new FileNotFoundException("Archive source was not found", archOptions.SourceFilename);
            }

            var localArch = new LocalArch();
            var stopwatch = new Stopwatch();
            if (archOptions.MeasureTime)
            {
                stopwatch.Start();
            }

            ArchiveFile(localArch, archOptions);

            if (archOptions.MeasureTime)
            {
                stopwatch.Stop();
                Console.WriteLine(string.Format("Archive took {0} minutes ({1} seconds)",
                    stopwatch.Elapsed.TotalMinutes, stopwatch.Elapsed.TotalSeconds));
            }
        }

        private static void ArchiveFile(LocalArch localArch, ArchOptions archOptions)
        {
            FileMode fileMode = FileMode.Create;
            if (false == archOptions.Overwrite)
            {
                fileMode = FileMode.CreateNew;
            }

            if (archOptions.Decompress)
            {
                if (archOptions.Check)
                {
                    CheckFile(localArch, archOptions);
                }
                else
                {
                    DecompressFile(localArch, archOptions, fileMode);
                }
            }
            else
            {
                CompressFile(localArch, archOptions, fileMode);
            }
        }

        private static void CheckFile(LocalArch localArch, ArchOptions archOptions)
        {
            localArch.Check(archOptions.SourceFilename, archOptions.Password, archOptions.BufferSize);
        }

        private static void DecompressFile(LocalArch localArch, ArchOptions archOptions, FileMode fileMode)
        {
            if ((false == string.IsNullOrEmpty(archOptions.OutputFilename)) &&
                (false == Directory.Exists(archOptions.OutputFilename)))
            {
                throw new DirectoryNotFoundException(archOptions.OutputFilename);
            }
            localArch.Decompress(archOptions.SourceFilename, archOptions.OutputFilename, fileMode,
                archOptions.Password, archOptions.BufferSize, archOptions.PreallocationPercent);
        }

        private static void CompressFile(LocalArch localArch, ArchOptions archOptions, FileMode fileMode)
        {
            string targetFilename = archOptions.OutputFilename;
            string extension = archOptions.ArchiveFormat.GetExtension();
            if (string.IsNullOrEmpty(targetFilename))
            {
                targetFilename = Path.ChangeExtension(Path.GetFileName(archOptions.SourceFilename), extension);
            }
            else if (Directory.Exists(targetFilename))
            {
                targetFilename = Path.Combine(targetFilename, Path.ChangeExtension(
                    Path.GetFileName(archOptions.SourceFilename), extension));
            }
            localArch.Compress(archOptions.SourceFilename, targetFilename, fileMode,
                archOptions.ArchiveFormat, archOptions.CompressionMethod, archOptions.CompressionLevel,
                archOptions.EncryptionMethod, archOptions.Password, archOptions.BufferSize,
                archOptions.PreallocationPercent, archOptions.Check, archOptions.CustomParameters);
        }

        private static ArchOptions ParseArgs(string[] args)
        {
            var hashOptions = new ArchOptions();
            hashOptions.SourceFilename = args[0];
            OptionValidators.ValidatePath("source filename", hashOptions.SourceFilename);
            foreach (var option in args.Skip(1))
            {
                if (false == Program.ArchOptionDescriptorSet.Apply(hashOptions, option))
                {
                    throw new ArgumentException("Invalid argument", option);
                }
            }
            return hashOptions;
        }

        public static void PrintUsage()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("LocalArch.exe <source filename> [OPTIONS]");
            builder.AppendLine(Program.ArchOptionDescriptorSet.ToString());
            Console.Write(builder.ToString());
        }
    }
}
