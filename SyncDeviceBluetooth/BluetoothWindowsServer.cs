using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsServer : BluetoothWindows
    {
        private StreamSocket socket;
        private RfcommServiceProvider rfcommProvider;
        private StreamSocketListener socketListener;

        public override async Task StartAsync(string reason)
        {
            Logger?.LogInformation(reason);
            InitializeRfcommServer();
        }

        public override Task StopAsync(string reason)
        {
            Disconnect(reason);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the server using RfcommServiceProvider to advertise the Chat Service UUID and start listening
        /// for incoming connections.
        /// </summary>
        private async void InitializeRfcommServer()
        {
            try
            {
                rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(RfcommChatServiceUuid));
            }
            // Catch exception HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE).
            catch (Exception ex) when ((uint)ex.HResult == 0x800710DF)
            {
                // The Bluetooth radio may be off.
                Logger?.LogError("Make sure your Bluetooth Radio is on: " + ex.Message);
                Status = SyncDeviceStatus.Stopped;
                return;
            }

            // Create a listener for this service and start listening
            socketListener = new StreamSocketListener();
            socketListener.ConnectionReceived += OnConnectionReceived;

            var rfcomm = rfcommProvider.ServiceId.AsString();

            await socketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(rfcommProvider);

            try
            {
                rfcommProvider.StartAdvertising(socketListener, true);
            }
            catch (Exception e)
            {
                // If you aren't able to get a reference to an RfcommServiceProvider, tell the user why.  Usually throws an exception if user changed their privacy settings to prevent Sync w/ Devices.  
                Logger?.LogError(e.Message);
                return;
            }

            Logger?.LogInformation("Listening for incoming connections");

            Status = SyncDeviceStatus.Started;
        }

        /// <summary>
        /// Creates the SDP record that will be revealed to the Client device when pairing occurs.  
        /// </summary>
        /// <param name="rfcommProvider">The RfcommServiceProvider that is being used to initialize the server</param>
        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
            sdpWriter.WriteString(SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        /// <summary>
        /// Invoked when the socket listener accepts an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accepted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private async void OnConnectionReceived(
            StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Don't need the listener anymore
            //socketListener.Dispose();
            //socketListener = null;

            try
            {
                socket = args.Socket;
            }
            catch (Exception e)
            {
                Logger?.LogError(e.Message);
                Disconnect("exception?");
                return;
            }
            
            

            // Note - this is the supported way to get a Bluetooth device from a given socket
            var remoteDevice = await BluetoothDevice.FromHostNameAsync(socket.Information.RemoteHostName);

            var writer = new DataWriter(socket.OutputStream);
            if (!Writers.TryAdd(remoteDevice.DeviceId, writer))
            {
                Logger?.LogError("Can't add writer to dictionary?");
                return;
            }

            var reader = new DataReader(socket.InputStream);
            bool remoteDisconnection = false;

            Logger?.LogInformation("Connected to Client: " + remoteDevice.Name);

            RaiseOnDeviceConnected(remoteDevice.DeviceId);

            // Infinite read buffer loop
            while (true)
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
            if (remoteDisconnection)
            {
                if (!Writers.TryRemove(remoteDevice.DeviceId, out _))
                {
                    RaiseOnDeviceConnected(remoteDevice.DeviceId);
                    Logger?.LogError("Can't remove writer from dictionary?");
                }
                // Disconnect("Remote disconnection?");
                Logger?.LogInformation($"Client {remoteDevice.Name} disconnected");
            }
        }

        protected void Disconnect(string disconnectReason)
        {
            if (rfcommProvider != null)
            {
                rfcommProvider.StopAdvertising();
                rfcommProvider = null;
            }

            if (socketListener != null)
            {
                socketListener.Dispose();
                socketListener = null;
            }

            ClearWriters();

            if (socket != null)
            {
                socket.Dispose();
                socket = null;
            }

            Logger?.LogInformation(disconnectReason);

            Status = SyncDeviceStatus.Stopped;
        }
    }
}
