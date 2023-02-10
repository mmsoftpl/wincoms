using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsChannel : BluetoothWindows
    {
        private readonly bool isHost;
        public override bool IsHost { get => isHost; }

        public override Task StartAsync(string sessionName, string pin, string reason)
        {
            Pin = pin;
            SessionName = sessionName;
            if (Writer == null)
            {
                return ListenOnChannel();
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            Status = SyncDeviceStatus.Stopped;
            Writer?.DetachStream();


            if (ChatService != null)
            {
                ChatService.Dispose();
                ChatService = null;
            }

            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }        

            return Task.CompletedTask;
        }

        private static async Task<string> WaitForMessageAsync(DataReader reader)
        {
            // Based on the protocol we've defined, the first uint is the size of the message
            uint readLength = await reader.LoadAsync(sizeof(uint));

            // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
            if (readLength < sizeof(uint))
            {
                return null;
            }
            uint currentLength = reader.ReadUInt32();

            // Load the rest of the message since you already know the length of the data expected.  
            readLength = await reader.LoadAsync(currentLength);

            // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
            if (readLength < currentLength)
            {
                return null;
            }
            return reader.ReadString(currentLength);
        }

        public async Task<bool> WaitForHandshakeMessage(DataReader reader)
        {
            if (IsHost)
            {
                await WriteMessageAsync(Writer, HandshakeMessage.EncodeMessage(null, SessionName), false);

                var handshakeMessage = HandshakeMessage.DecodeMessage(await WaitForMessageAsync(reader));
                if (handshakeMessage != null && handshakeMessage.Pin == Pin)
                {
                    SessionName = handshakeMessage.SessionName;
                    Creator.SessionName = SessionName;
                    return true;
                }
                else
                {
                    Logger?.LogInformation($"Handshake message not correct. Expected {Pin} but recived {handshakeMessage?.Pin}");
                    return false;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Pin))
                {
                    var serverHandshakeMessage = HandshakeMessage.DecodeMessage(await WaitForMessageAsync(reader));
                    if (serverHandshakeMessage != null)
                    {
                        SessionName = serverHandshakeMessage.SessionName;
                     //   Creator.SessionName = SessionName;
                        _ = WriteMessageAsync(Writer, HandshakeMessage.EncodeMessage(Pin, Creator.SessionName), false);
                        return true;
                    }
                    else
                        return false;
                }
                return true;
            }            
        }

        public async Task ListenOnChannel()
        {
            if (ChatService != null)
            {
                Socket = new StreamSocket();
                await Socket.ConnectAsync(ChatService.ConnectionHostName, ChatService.ConnectionServiceName);
            }

            Writer = new DataWriter(Socket.OutputStream);
            var reader = new DataReader(Socket.InputStream);

            if (await WaitForHandshakeMessage(reader))
            {
                Logger?.LogInformation("Connection accepted, " + DeviceId);
                RaiseOnConnectionStarted(this);
            }
            else
            {
                await StopAsync("Connection not accepted, " + DeviceId);
                return;
            }

                // Infinite read buffer loop
            while (Status == SyncDeviceStatus.Started)
            {
                try
                {                    
                    string message = await WaitForMessageAsync(reader);
                    if (message != null)
                        RaiseOnMessageReceived(message, this);
                    else break;
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
                {
                    Logger?.LogInformation("Client Disconnected Successfully");
                    break;
                }
                catch (Exception e)
                {
                    Logger?.LogError("Read error", e);
                    break;
                }
            }

            reader.DetachStream();
            RaiseOnDeviceDisconnected(this);
            Logger?.LogInformation($"Client {DeviceId} disconnected");
        }

        public DataWriter Writer { get; set; }
        public StreamSocket Socket { get; set; }
        public RfcommDeviceService ChatService { get; set; }

        public BluetoothWindows Creator { get; set; }

        internal override void RaiseOnMessageReceived(string message, ISyncDevice device)
        {
            base.RaiseOnMessageReceived(message, device);
            Creator?.RaiseOnMessageReceived(message, device);
        }

        internal override void RaiseOnMessageSent(string message)
        {
            base.RaiseOnMessageSent(message);
            Creator?.RaiseOnMessageSent(message);
        }

        internal override void RaiseOnDeviceConnecting(ISyncDevice device, out ConnectingEventArgs e)
        {
            base.RaiseOnDeviceConnecting(device, out e);
            if (!e.Cancel)
                Creator?.RaiseOnDeviceConnecting(device, out e);
        }

        internal override void RaiseOnDeviceConnected(ISyncDevice device)
        {
            base.RaiseOnDeviceConnected(device);
            Creator?.RaiseOnDeviceConnected(device);
        }

        internal override void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            Status = SyncDeviceStatus.Aborted;
            base.RaiseOnDeviceDisconnected(device);
            Creator?.RaiseOnDeviceDisconnected(device);            
        }

        internal override void RaiseOnConnectionStarted(ISyncDevice device)
        {
            Status = SyncDeviceStatus.Started;
            base.RaiseOnConnectionStarted(device);
            Creator?.RaiseOnConnectionStarted(device);
        }

        public BluetoothWindowsChannel(BluetoothWindows creator, string deviceId, StreamSocket streamSocket)
        {
            isHost = creator.IsHost;
            Creator = creator;
            Socket = streamSocket;
            DeviceId = deviceId;
            Status = SyncDeviceStatus.Created;
        }

        public BluetoothWindowsChannel(BluetoothWindows creator, string deviceId, RfcommDeviceService chatService)
        {
            isHost = creator.IsHost;
            Creator = creator;
            ChatService = chatService;
            DeviceId = deviceId;
            Status = SyncDeviceStatus.Created;
        }

        public override Task SendMessageAsync(string message, string[] recipients= null)
        {
            if (recipients == null || recipients.Contains(SessionName))
                return WriteMessageAsync(Writer, message, true);
            return Task.CompletedTask;
        }


    }
}
