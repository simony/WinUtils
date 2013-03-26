using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using CopyUtils;
using ArgsUtils;

namespace LocalCopy
{
    public class Program
    {
        public static readonly OptionDescriptorSet<CopyOptions> CopyOptionDescriptorSet = new OptionDescriptorSet<CopyOptions>
        {
            { "-u", "--resumable", "copies a file in a resumable manar, i.e. can be resumed on failure",
                (CopyOptions o, bool v) => o.Resumable = v },
            { "-w", "--overwrite", "make sures that the target file will be overwriten, same as deleting the file",
                (CopyOptions o, bool v) => o.Overwrite = v },
            { "-t", "--threaded", "copies a file using read/write threads",
                (CopyOptions o, bool v) => o.Threaded = v },
            { "-s", "--size", "buffer size", string.Format(
                "size of the buffer to be used for copying, in units of sector size ({0})", Win32.SECTOR_SIZE),
                (CopyOptions o, int number) => o.BufferSize = number, OptionValidators.ValidateGraterThanZero },
            { "-c", "--count", "buffer count", "amount of buffers to be used by the read/write threads",
                (CopyOptions o, int number) => o.BufferCount = number, OptionValidators.ValidateGraterThanZero },
            { "-m", "--measure", "measure the time of the copy",
                (CopyOptions o, bool v) => o.MeasureTime = v },
        };

        public static int Main(string[] args)
        {
            CopyOptions copyOptions = null;
            try
            {
                copyOptions = ParseArgs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Invalid Usage: {0}", ex.Message));
                PrintUsage();
                return -1;
            }

            try
            {
                Copy(copyOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Copy Failure: {0}", ex.Message));
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            return 0;
        }

        private static void Copy(CopyOptions copyOptions)
        {
            if (false == File.Exists(copyOptions.SourceFilename))
            {
                throw new FileNotFoundException("Copy source was not found", copyOptions.SourceFilename);
            }

            var localCopy = new LocalCopy();
            var stopwatch = new Stopwatch();
            if (copyOptions.MeasureTime)
            {
                stopwatch.Start();
            }

            if (copyOptions.Resumable)
            {
                CopyResumableFile(localCopy, copyOptions);
            }
            else
            {
                CopyFile(localCopy, copyOptions);
            }

            if (copyOptions.MeasureTime)
            {
                stopwatch.Stop();
                Console.WriteLine(string.Format("Copy took {0} minutes ({1} seconds)",
                    stopwatch.Elapsed.TotalMinutes, stopwatch.Elapsed.TotalSeconds));
            }
        }

        private static void CopyFile(LocalCopy localCopy, CopyOptions copyOptions)
        {
            FileMode fileMode = FileMode.Create;
            if (false == copyOptions.Overwrite)
            {
                fileMode = FileMode.CreateNew;
            }
            if (copyOptions.Threaded)
            {
                localCopy.ThreadedCopyFile(copyOptions.SourceFilename, copyOptions.TargetFilename,
                    fileMode, copyOptions.BufferCount, copyOptions.BufferSize);
            }
            else
            {
                localCopy.CopyFile(copyOptions.SourceFilename, copyOptions.TargetFilename,
                    fileMode, copyOptions.BufferSize);
            }
        }

        private static void CopyResumableFile(LocalCopy localCopy, CopyOptions copyOptions)
        {
            FileMode fileMode = FileMode.OpenOrCreate;
            if (copyOptions.Overwrite)
            {
                fileMode = FileMode.Create;
            }
            if (copyOptions.Threaded)
            {
                localCopy.ThreadedCopyResumableFile(copyOptions.SourceFilename, copyOptions.TargetFilename,
                    fileMode, copyOptions.BufferCount, copyOptions.BufferSize);
            }
            else
            {
                localCopy.CopyResumableFile(copyOptions.SourceFilename, copyOptions.TargetFilename,
                    fileMode, copyOptions.BufferSize);
            }
        }

        private static CopyOptions ParseArgs(string[] args)
        {
            var copyOptions = new CopyOptions();
            copyOptions.SourceFilename = args[0];
            OptionValidators.ValidatePath("source filename", copyOptions.SourceFilename);
            copyOptions.TargetFilename = args[1];
            OptionValidators.ValidatePath("target filename", copyOptions.TargetFilename);
            foreach (var option in args.Skip(2))
            {
                if (false == Program.CopyOptionDescriptorSet.Apply(copyOptions, option))
                {
                    throw new ArgumentException("Invalid argument", option);
                }
            }
            return copyOptions;
        }

        public static void PrintUsage()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("LocalCopy.exe <source filename> <target filename> [OPTIONS]");
            builder.AppendLine(Program.CopyOptionDescriptorSet.ToString());
            Console.Write(builder.ToString());
        }
    }
}
