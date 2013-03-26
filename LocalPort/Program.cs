using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArgsUtils;
using System.Net.Sockets;
using System.Net;

namespace LocalPort
{
    public enum PortForwardingOptionEnum
    {
        None,
        Compress,
        Decompress
    }

    public class PortOptions
    {
        public string SourceAddress { get; set; }
        public int SourcePort { get; set; }
        public string TargetAddress { get; set; }
        public int TargetPort { get; set; }
        public PortForwardingOptionEnum PortForwardingOption { get; set; }
        public int BufferSize { get; set; }

        public PortOptions()
        {
            this.SourceAddress = IPAddress.Any.ToString();
            this.SourcePort = 0;
            this.TargetAddress = string.Empty;
            this.TargetPort = 0;
            this.PortForwardingOption = PortForwardingOptionEnum.None;
            this.BufferSize = LocalPort.DEFAULT_BUFFER_SIZE;
        }
    }
    public class Program
    {
        public static readonly OptionDescriptorSet<PortOptions> PortOptionDescriptorSet = new OptionDescriptorSet<PortOptions>
        {            
            { "-c", "--compress", "compress trafic",
                (PortOptions o, bool v) => o.PortForwardingOption = PortForwardingOptionEnum.Compress },
            { "-d", "--decompress", "decompress trafic",
                (PortOptions o, bool v) => o.PortForwardingOption = PortForwardingOptionEnum.Decompress },
            { "-i", "--ip", "source address", "source ip address",
                (PortOptions o, string v) => o.SourceAddress = v },
            { "-s", "--size", "buffer size", "size of the buffer to be used for data transfer",
                (PortOptions o, int number) => o.BufferSize = number, OptionValidators.ValidateGraterThanZero },
        };

        public static int Main(string[] args)
        {
            PortOptions portOptions = null;
            try
            {
                portOptions = ParseArgs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Invalid Usage: {0}", ex.Message));
                PrintUsage();
                return -1;
            }

            try
            {
                StartPortForwarding(portOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Port farwarding Failure: {0}", ex.Message));
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            return 0;
        }

        private static void StartPortForwarding(PortOptions portOptions)
        {            
            IPAddress localAddress = IPAddress.Parse(portOptions.SourceAddress);
            IPAddress targetAddress = IPAddress.Parse(portOptions.TargetAddress);

            LocalPort localPort = new LocalPort(portOptions.BufferSize);
            localPort.StartPortForwarding(localAddress, portOptions.SourcePort,
                targetAddress, portOptions.TargetPort, portOptions.PortForwardingOption);            
        }

        private static PortOptions ParseArgs(string[] args)
        {
            var portOptions = new PortOptions();
            portOptions.SourcePort = int.Parse(args[0]);
            portOptions.TargetAddress = args[1];
            portOptions.TargetPort = int.Parse(args[2]);
            
            foreach (var option in args.Skip(3))
            {
                if (false == Program.PortOptionDescriptorSet.Apply(portOptions, option))
                {
                    throw new ArgumentException("Invalid argument", option);
                }
            }
            return portOptions;
        }

        public static void PrintUsage()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("LocalPort.exe <source port> <target address> <target port> [OPTIONS]");
            builder.AppendLine(Program.PortOptionDescriptorSet.ToString());
            Console.Write(builder.ToString());
        }
    }
}
