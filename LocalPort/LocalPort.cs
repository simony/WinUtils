using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace LocalPort
{
    public class LocalPort
    {
        #region Constants

        public const int DEFAULT_BUFFER_SIZE = 4096;

        #endregion

        #region Members

        protected int _bufferSize = DEFAULT_BUFFER_SIZE;

        #endregion

        #region Constructors

        public LocalPort()
            : this(DEFAULT_BUFFER_SIZE)
        {
        }

        public LocalPort(int bufferSize)
        {
            this._bufferSize = bufferSize;
        }

        #endregion

        public Thread StartPortForwarding(int localPort,
            IPAddress targetAddress, int targetPort, PortForwardingOptionEnum portForwardingOption)
        {
            return this.StartPortForwarding(IPAddress.Any, localPort, targetAddress, targetPort, portForwardingOption);
        }

        public Thread StartPortForwarding(IPAddress localAddress, int localPort,
            IPAddress targetAddress, int targetPort, PortForwardingOptionEnum portForwardingOption)
        {
            TcpListener server = new TcpListener(localAddress, localPort);
            server.Start();
            var portForwardingThread = new Thread(delegate()
            {
                try
                {
                    this.ForwardPort(server, targetAddress, targetPort, portForwardingOption);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Port foardwarding failure: {0}", ex.Message));
                    Console.WriteLine(ex.StackTrace);                    
                    Thread.CurrentThread.Abort();
                }
                finally
                {
                    server.Stop();
                }
            });
            portForwardingThread.Start();
            return portForwardingThread;
        }

        public void ForwardPort(TcpListener server,
            IPAddress targetAddress, int targetPort, PortForwardingOptionEnum portForwardingOption)
        {
            while (true)
            {
                TcpClient sourceClient = server.AcceptTcpClient();
                TcpClient targetClient = null;
                try
                {
                    targetClient = new TcpClient();
                    targetClient.Connect(new IPEndPoint(targetAddress, targetPort));
                }
                catch
                {
                    sourceClient.Close();
                    throw;
                }

                this.StartPortForwarding(sourceClient, targetClient, portForwardingOption);
            }
        }

        private Thread StartPortForwarding(TcpClient sourceClient, TcpClient targetClient, PortForwardingOptionEnum portForwardingOption)
        {
            var portForwardingThread = new Thread(delegate()
            {
                try
                {
                    this.ForwardPort(sourceClient, targetClient, portForwardingOption);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Port foardwarding failure: {0}", ex.Message));
                    Console.WriteLine(ex.StackTrace);
                    Thread.CurrentThread.Abort();
                }
                finally
                {
                    sourceClient.Close();
                    targetClient.Close();
                }
            });
            portForwardingThread.Start();
            return portForwardingThread;
        }

        private void ForwardPort(TcpClient sourceClient, TcpClient targetClient, PortForwardingOptionEnum portForwardingOption)
        {
            using (NetworkStream sourceStream = sourceClient.GetStream())
            {
                using (NetworkStream targetStream = targetClient.GetStream())
                {
                    Thread portForwardingThreadRead = null;
                    Thread portForwardingThreadWrite = null;
                    if ((sourceStream.CanRead) && (targetStream.CanWrite))
                    {
                        portForwardingThreadRead = this.StartPortForwarding(sourceStream, targetStream, portForwardingOption);
                    }
                    if ((sourceStream.CanWrite) && (targetStream.CanRead))
                    {
                        portForwardingThreadWrite = this.StartPortForwarding(targetStream, sourceStream,
                            this.GetInversePortForwardingOption(portForwardingOption));
                    }
                    if (null != portForwardingThreadRead)
                    {
                        portForwardingThreadRead.Join();
                    }
                    if (null != portForwardingThreadWrite)
                    {
                        portForwardingThreadWrite.Join();
                    }
                }
            }
        }

        private PortForwardingOptionEnum GetInversePortForwardingOption(PortForwardingOptionEnum portForwardingOption)
        {
            switch (portForwardingOption)
            {
                case PortForwardingOptionEnum.Compress:
                    return PortForwardingOptionEnum.Decompress;
                case PortForwardingOptionEnum.Decompress:
                    return PortForwardingOptionEnum.Compress;
                case PortForwardingOptionEnum.None:
                default:
                    return PortForwardingOptionEnum.None;
            }
        }                

        private Thread StartPortForwarding(NetworkStream sourceStream, NetworkStream targetStream, 
            PortForwardingOptionEnum portForwardingOption)
        {
            var portForwardingThread = new Thread(delegate()
            {
                try
                {
                    this.ForwardPort(sourceStream, targetStream, portForwardingOption);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Port foardwarding failure: {0}", ex.Message));
                    Console.WriteLine(ex.StackTrace);
                    Thread.CurrentThread.Abort();
                }                
            });
            portForwardingThread.Start();
            return portForwardingThread;
        }

        private void ForwardPort(NetworkStream sourceStream, NetworkStream targetStream, PortForwardingOptionEnum portForwardingOption)
        {
            switch (portForwardingOption)
            {
                case PortForwardingOptionEnum.Compress:
                    this.Compress(sourceStream, targetStream);                    
                    break;
                case PortForwardingOptionEnum.Decompress:
                    this.Decompress(sourceStream, targetStream);                    
                    break;
                case PortForwardingOptionEnum.None:
                default:
                    sourceStream.CopyTo(targetStream, this._bufferSize);
                    break;
            }
        }

        private void Decompress(NetworkStream sourceStream, NetworkStream targetStream)
        {
            while (sourceStream.CanRead)
            {
                using (GZipStream gZipSourceStream = new GZipStream(sourceStream, CompressionMode.Decompress, true))
                {
                    gZipSourceStream.CopyTo(targetStream);
                }
            }
        }

        private void Compress(NetworkStream sourceStream, NetworkStream targetStream)
        {
            byte[] buffer = new byte[this._bufferSize];
            while (true)
            {
                int byteRead = sourceStream.Read(buffer, 0, buffer.Length);
                if (0 == byteRead)
                {
                    break;
                }
                using (GZipStream gZipTargetStream = new GZipStream(targetStream, CompressionMode.Compress, true))
                {
                    gZipTargetStream.Write(buffer, 0, byteRead);
                }
            }
        }
    }
}
