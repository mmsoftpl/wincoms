using SDKTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SDKTemplate
{
    // Generic wrapper for TCP (Stream) or UDP (Datagram) socket
    // For this sample, this just allows messages to be sent between connected peers,
    // real application would handle higher level logic over the connected socket(s)
    public class SocketWrapper : IDisposable
    {
        private StreamSocket streamSocket = null;
        private DatagramSocket datagramSocket = null;
        // Used to update main state
        private MainPage manager = null;

        private DataReader reader = null;
        private DataWriter writer;

        public SocketWrapper(
            MainPage manager,
            StreamSocket streamSocket = null,
            DatagramSocket datagramSocket = null,
            bool canSend = true)
        {
            this.streamSocket = streamSocket;
            this.datagramSocket = datagramSocket;
            this.manager = manager;

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
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;
            }

            if (writer != null)
            {
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
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
                    MainPage.Log("Socket is unable to send messages (receive only socket).", NotifyType.ErrorMessage);
                }
                else
                {
                    writer.WriteUInt32(writer.MeasureString(message));
                    writer.WriteString(message);
                    await writer.StoreAsync();

                    uint bytesSent = writer.MeasureString(message);

                    MainPage.Log(String.Format("Sent Message: \"{0}\", {1} bytes",
                            (message.Length > 32) ? message.Substring(0, 32) + "..." : message,
                            bytesSent
                            ),
                        NotifyType.StatusMessage
                        );
                }
            }
            catch (Exception ex)
            {
                MainPage.Log("SendMessageAsync Failed: " + ex.Message, NotifyType.ErrorMessage);
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
                        MainPage.Log("The underlying socket was closed before we were able to read the whole data.", NotifyType.ErrorMessage);
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
                            MainPage.Log("The underlying socket was closed before we were able to read the whole data.", NotifyType.ErrorMessage);
                            return null;
                        }
                    }

                    if ((!load && messageLength > 0) || bytesRead > 0)
                    {
                        // Decode the string.
                        string currentMessage = datareader.ReadString(messageLength);
                        uint bytesReceived = messageLength;

                        // Just print a message for now
                        MainPage.Log(String.Format("Received Message: \"{0}\", {1} bytes",
                                (currentMessage.Length > 32) ? currentMessage.Substring(0, 32) + "..." : currentMessage,
                                bytesReceived
                                ),
                            NotifyType.StatusMessage
                            );

                        // TCP will need to call this again, UDP will get callbacks
                        if (load)
                        {
                            return await HandleReceivedMessage(datareader, load);
                        }
                    }
                    else
                    {
                        MainPage.Log("ReceiveMessage 0 bytes loaded for message content.", NotifyType.ErrorMessage);
                    }
                }
                else
                {
                    MainPage.Log("ReceiveMessage 0 bytes loaded for message content.", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MainPage.Log("HandleReceivedMessage Failed: " + ex.Message, NotifyType.ErrorMessage);
            }
            return null;
        }

        private async void OnUDPMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                DataReader udpReader = args.GetDataReader();
                udpReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                udpReader.ByteOrder = ByteOrder.LittleEndian;

                await HandleReceivedMessage(udpReader, false);
            }
            catch (Exception ex)
            {
                MainPage.Log("OnUDPMessageReceived Failed: " + ex.Message, NotifyType.ErrorMessage);
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
                if (streamSocket != null)
                {
                    streamSocket.Dispose();
                }

                if (datagramSocket != null)
                {
                    datagramSocket.Dispose();
                    datagramSocket.MessageReceived -= OnUDPMessageReceived;
                }

                if (writer != null)
                {
                    writer.Dispose();
                }

                if (reader != null)
                {
                    reader.Dispose();
                }
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
