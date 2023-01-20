using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows
{
    //Based on
    //https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/WiFiDirectServices/cs/WiFiDirectServiceManager.cs
    //
    // Generic wrapper for TCP (Stream) or UDP (Datagram) socket
    // For this sample, this just allows messages to be sent between connected peers,
    // real application would handle higher level logic over the connected socket(s)
    public class SocketWrapper : IDisposable
    {
        private readonly StreamSocket streamSocket = null;
        private readonly DatagramSocket datagramSocket = null;
        private readonly ILogger logger = null;

        private readonly DataReader reader = null;
        private readonly DataWriter writer;

        public SocketWrapper(
            ILogger logger,
            StreamSocket streamSocket = null,
            DatagramSocket datagramSocket = null,
            bool canSend = true)
        {
            this.streamSocket = streamSocket;
            this.datagramSocket = datagramSocket;
            this.logger = logger;

            if (streamSocket == null && datagramSocket == null)
            {
                throw new Exception("Bad SocketWrapper parameters, must provide a TCP or UDP socket");
            }
            else if (streamSocket != null && datagramSocket != null)
            {
                throw new Exception("Bad SocketWrapper parameters, got both TCP and UDP socket, expected only one");
            }
            else if (streamSocket != null)
            {
                reader = new DataReader(streamSocket.InputStream);
                if (canSend)
                {
                    writer = new DataWriter(streamSocket.OutputStream);
                }
            }
            else
            {
                datagramSocket.MessageReceived += OnUDPMessageReceived;
                if (canSend)
                {
                    writer = new DataWriter(datagramSocket.OutputStream);
                }
            }

            if (reader != null)
            {
                reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;
            }

            if (writer != null)
            {
                writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                writer.ByteOrder = ByteOrder.LittleEndian;
            }
        }

        public string Protocol
        {
            get
            {
                if (streamSocket != null)
                {
                    return "TCP";
                }
                else if (datagramSocket != null)
                {
                    return "UDP";
                }
                else
                {
                    return "???";
                }
            }
        }

        public string Port
        {
            get
            {
                if (streamSocket != null)
                {
                    return streamSocket.Information.LocalPort;
                }
                else if (datagramSocket != null)
                {
                    return datagramSocket.Information.LocalPort;
                }
                else
                {
                    return "???";
                }
            }
        }

        /// <summary>
        /// Send a string over the socket (TCP or UDP)
        /// Will send as a uint32 size followed by the message
        /// </summary>
        /// <param name="message">Message to send</param>
        public async Task SendMessageAsync(string message)
        {
            try
            {
                ThrowIfDisposed();

                if (writer == null)
                {
                    logger?.LogError("Socket is unable to send messages (receive only socket).");
                }
                else
                {
                    writer.WriteUInt32(writer.MeasureString(message));
                    writer.WriteString(message);
                    await writer.StoreAsync();

                    uint bytesSent = writer.MeasureString(message);

                    //logger?.LogInformation(String.Format("Sent Message: \"{0}\", {1} bytes",
                    //        (message.Length > 32) ? message.Substring(0, 32) + "..." : message,
                    //        bytesSent
                    //        )
                    //    );
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("SendMessageAsync Failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Read strings sent over the TCP socket
        /// Reads the uint32 size then the actual string
        /// </summary>
        public async Task<string> ReceiveMessageAsync()
        {
            return await HandleReceivedMessage(this.reader);
        }

        /// <summary>
        /// Handle both TCP receive call and UDP event handler
        /// </summary>
        /// <param name="datareader"></param>
        /// <param name="load">Set to true when the datareader.LoadAsync needs to be called for TCP</param>
        /// <returns></returns>
        private async Task<string> HandleReceivedMessage(DataReader datareader, bool load = true)
        {
            try
            {
                ThrowIfDisposed();
                uint bytesRead = 0;

                if (load)
                {
                    bytesRead = await datareader.LoadAsync(sizeof(uint));

                    if (bytesRead != sizeof(uint))
                    {
                        logger?.LogError("The underlying socket was closed before we were able to read the whole data.");
                        return null;
                    }
                }

                if (!load || bytesRead > 0)
                {
                    // Determine how long the string is.
                    uint messageLength = (uint)datareader.ReadUInt32();

                    if (load)
                    {
                        bytesRead = await datareader.LoadAsync(messageLength);
                        if (bytesRead != messageLength)
                        {
                            logger?.LogError("The underlying socket was closed before we were able to read the whole data.");
                            return null;
                        }
                    }

                    if ((!load && messageLength > 0) || bytesRead > 0)
                    {
                        // Decode the string.
                        string currentMessage = datareader.ReadString(messageLength);
                        uint bytesReceived = messageLength;

                        // Just print a message for now
                        logger?.LogInformation(String.Format("Received Message: \"{0}\", {1} bytes",
                                (currentMessage.Length > 32) ? currentMessage.Substring(0, 32) + "..." : currentMessage,
                                bytesReceived
                                )
                            );

                        // TCP will need to call this again, UDP will get callbacks
                        if (load)
                        {
                            return await HandleReceivedMessage(datareader, load);
                        }
                    }
                    else
                    {
                        logger?.LogError("ReceiveMessage 0 bytes loaded for message content.");
                    }
                }
                else
                {
                    logger?.LogError("ReceiveMessage 0 bytes loaded for message content.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("HandleReceivedMessage Failed: " + ex.Message);
            }
            return null;
        }

        private async void OnUDPMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                DataReader udpReader = args.GetDataReader();
                udpReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                udpReader.ByteOrder = ByteOrder.LittleEndian;

                await HandleReceivedMessage(udpReader, false);
            }
            catch (Exception ex)
            {
                logger?.LogError("OnUDPMessageReceived Failed: " + ex.Message);
            }
        }

        bool disposed = false;

        ~SocketWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                streamSocket?.Dispose();

                if (datagramSocket != null)
                {
                    datagramSocket.Dispose();
                    datagramSocket.MessageReceived -= OnUDPMessageReceived;
                }

                writer?.Dispose();

                reader?.Dispose();
            }

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("SocketWrapper");
            }
        }
     
    }
}
