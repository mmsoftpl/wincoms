using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsChannel : BluetoothWindows
    {
        public override Task StartAsync(string reason)
        {
            if (Writer == null)
            {
                _ = ListenOnChannel();
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            Status = SyncDeviceStatus.Aborted;
            Writer.DetachStream();


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

        public async Task ListenOnChannel()
        {
            // Note - this is the supported way to get a Bluetooth device from a given socket
           // var remoteDevice = await BluetoothDevice.FromHostNameAsync(Socket.Information.RemoteHostName);

            if (ChatService != null)
            {
                Socket = new StreamSocket();
                await Socket.ConnectAsync(ChatService.ConnectionHostName, ChatService.ConnectionServiceName);
            }

            Writer = new DataWriter(Socket.OutputStream);

            var reader = new DataReader(Socket.InputStream);
            bool remoteDisconnection = false;

            Logger?.LogInformation("Connected to Client: " + DeviceId);

            RaiseOnConnectionStarted(DeviceId);
            Status = SyncDeviceStatus.Started;

            // Infinite read buffer loop
            while (Status == SyncDeviceStatus.Started)
            {
                try
                {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint))
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);
                    RaiseOnMessage(message);
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
                    remoteDisconnection = true;
                    break;
                }
            }

            reader.DetachStream();
            RaiseOnDeviceDisconnected(this);
            Logger?.LogInformation($"Client {DeviceId} disconnected");
        }

        public DataWriter Writer { get; set; }
        public StreamSocket Socket { get; set; }
        public string DeviceId { get; set; }

        public RfcommDeviceService ChatService { get; set; }

        public BluetoothWindows Creator { get; set; }

        internal override void RaiseOnMessage(string message)
        {
            base.RaiseOnMessage(message);
            Creator?.RaiseOnMessage(message);
        }

        internal override void RaiseOnDeviceConnected(ISyncDevice device)
        {
            base.RaiseOnDeviceConnected(device);
            Creator?.RaiseOnDeviceConnected(device);
        }

        internal override void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            base.RaiseOnDeviceDisconnected(device);
            Creator?.RaiseOnDeviceDisconnected(device);
        }

        internal override void RaiseOnConnectionStarted(string deviceId)
        {
            base.RaiseOnConnectionStarted(deviceId);
            Creator?.RaiseOnConnectionStarted(deviceId);
        }

        public BluetoothWindowsChannel(BluetoothWindows creator, string deviceId, StreamSocket streamSocket)
        {
            Creator = creator;
            Socket = streamSocket;
            DeviceId = deviceId;
            Status = SyncDeviceStatus.Created;
        }

        public BluetoothWindowsChannel(BluetoothWindows creator, string deviceId, RfcommDeviceService chatService)
        {
            Creator = creator;
            ChatService = chatService;
            DeviceId = deviceId;
            Status = SyncDeviceStatus.Created;
        }

        public override Task SendMessageAsync(string message)
        {
            return WriteMessageAsync(Writer, message);
        }


    }
}
