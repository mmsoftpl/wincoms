using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsChannel : BluetoothWindows
    {
        public override bool IsHost { get; set; }

        public override Task StartAsync(string sessionName, string reason)
        {
            SessionName = sessionName;
            if (Writer == null)
            {
                return ListenOnChannel();
            }

            return Task.CompletedTask;
        }

        public void Pause()
        {
            Status = SyncDeviceStatus.Stopped;
            Writer?.DetachStream();
            Writer = null;

            if (ChatService != null)
            {
                ChatService.Dispose();
                ChatService = null;
            }
        }

        public override Task StopAsync(string reason)
        {
            Pause();
            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }        

            return Task.CompletedTask;
        }

        internal async Task<string> WaitForMessageAsync(DataReader reader)
        {
            // Based on the protocol we've defined, the first uint is the size of the message
            uint readLength = await reader.LoadAsync(sizeof(uint));

            // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
            if (readLength < sizeof(uint))
            {
                await StopAsync("Remote has terminated the connection");
                return null;
            }
            uint currentLength = reader.ReadUInt32();

            // Load the rest of the message since you already know the length of the data expected.  
            readLength = await reader.LoadAsync(currentLength);

            // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
            if (readLength < currentLength)
            {
                await StopAsync("Remote has terminated the connection");
                return null;
            }
            return reader.ReadString(currentLength);
        }

        public async Task<bool> WaitForHostWelcomeMessage(DataReader reader)
        {
            if (IsHost)
            {
                // do nothing, as this is host
                return true;
            }
            else
            {
                string welcomeMessage = await WaitForMessageAsync(reader);
                return !string.IsNullOrEmpty(welcomeMessage);
            }            
        }

        private async Task<DataReader> EnsureReaderWriterCreated()
        {
            if (Socket == null)
            {
                if (ChatService != null)
                {
                    Socket = new StreamSocket();
                    await Socket.ConnectAsync(ChatService.ConnectionHostName, ChatService.ConnectionServiceName);
                }
            }
            if (Writer == null)
                Writer = new DataWriter(Socket.OutputStream);
            return new DataReader(Socket.InputStream);
        }

        public async Task WelcomeOnChannel()
        {
            var reader = await EnsureReaderWriterCreated();

            bool welcomed = false;
            OnMessageEventHandler onMsg = (a, b) =>
            {
                Logger?.LogInformation($"Received welcome message '{b.Message}'");
                welcomed = true;
            };

            OnMessage += onMsg;

            Logger?.LogInformation("Started broadcasting SessionName");
            // Infinite read buffer loop
            while (!welcomed)
            {
                try
                {
                    string message = SessionName;
                    Writer?.WriteUInt32((uint)message.Length);
                    Writer?.WriteString(message);

                    await Writer?.StoreAsync();
                    Logger?.LogInformation($"Broadcasted welcome message '{message}'");

                    await Task.Delay(1000);
                    
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
                {
                    //        Logger?.LogInformation("Client Disconnected Successfully");
                    break;
                }
                catch (Exception e)
                {
                    //  Logger?.LogError("Read error", e);
                    break;
                }
            }
            OnMessage -= onMsg; 
            Logger?.LogInformation("Stoped broadcasting SessionName");
            Pause();
        }

        public async Task<string> ReadWelcomeOnChannelAsync()
        {
            var reader = await EnsureReaderWriterCreated();

            string message;

            Logger?.LogInformation("Started reading SessionName");
            try
            {
                message = await WaitForMessageAsync(reader);
            }
            // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
            catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
            {
                Logger?.LogInformation("Client Disconnected Successfully");
                return null;
            }
            catch (Exception e)
            {
                Logger?.LogError("Read error", e);
                return null;
            }
            Logger?.LogInformation("Stopped reading SessionName");
            Pause();
            return message;
        }

        public async Task ListenOnChannel()
        {
            var reader = await EnsureReaderWriterCreated();

            if (await WaitForHostWelcomeMessage(reader))
            {
                Logger?.LogInformation("Connection accepted, " + DeviceId);
                RaiseOnConnectionStarted(DeviceId);
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

        public override string Id { get => SessionName + " ["+ DeviceId + "]"; }

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
            Status = SyncDeviceStatus.Aborted;
            base.RaiseOnDeviceDisconnected(device);
            Creator?.RaiseOnDeviceDisconnected(device);            
        }

        internal override void RaiseOnConnectionStarted(string deviceId)
        {
            Status = SyncDeviceStatus.Started;
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
            return WriteMessageAsync(Writer, message, Logger);
        }

        //public static async Task SendWelcomeMessageAsync(string msg, RfcommDeviceService rfcommDeviceService)
        //{
        //    var Socket = new StreamSocket();
        //    await Socket.ConnectAsync(rfcommDeviceService.ConnectionHostName, rfcommDeviceService.ConnectionServiceName);
        //    var Writer = new DataWriter(Socket.OutputStream);
        //    await WriteMessageAsync(Writer, msg, null);
        //    Writer?.DetachStream();            
        //    Socket.Dispose();
        //}

        //public async static Task<string> ReadWelcomeMessageAsync(StreamSocket socket)
        //{
        //    var reader = new DataReader(socket.InputStream);
        //    // Based on the protocol we've defined, the first uint is the size of the message
        //    uint readLength = await reader.LoadAsync(sizeof(uint));

        //    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
        //    if (readLength < sizeof(uint))
        //    {
        //        //
        //        return null;
        //    }
        //    uint currentLength = reader.ReadUInt32();

        //    // Load the rest of the message since you already know the length of the data expected.  
        //    readLength = await reader.LoadAsync(currentLength);

        //    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
        //    if (readLength < currentLength)
        //    {
        //        //
        //        return null;
        //    }

        //    var r = reader.ReadString(currentLength);
        //    reader.DetachStream();
        //    return r;
        //}

    }
}
