﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsServer : BluetoothWindows
    {
        private RfcommServiceProvider rfcommProvider;
        private StreamSocketListener socketListener;
        private BluetoothLeWatcher bluetoothLeWatcher;
        public override bool IsHost { get => true; }

        public Task ScanForSignatures(string sessionName)
        {
            bluetoothLeWatcher = new BluetoothLeWatcher() { Logger = Logger };
            return bluetoothLeWatcher.StartAsync(sessionName, "Publishing LE signature");
        }

        public Task StopScanningForSignatures(string reason)
        {
            try
            {
                return bluetoothLeWatcher?.StopAsync(reason);
            }
            finally 
            {
                bluetoothLeWatcher = null; 
            }
        }

        public override async Task StartAsync(string sessionName, string reason)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                await ScanForSignatures(sessionName);

                SessionName = sessionName;
                Logger?.LogInformation(reason);
                await InitializeRfcommServer();
            }
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
        private async Task InitializeRfcommServer()
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
            InitializeServiceSdpAttributes(rfcommProvider.SdpRawAttributes);

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

            var machineName = System.Environment.MachineName;
            //var f = BluetoothDiagnostics.BluetoothComPort.FindAll();

            var serviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort), new string[] { "System.Devices.AepService.AepId" });

            foreach (var serviceInfo in serviceInfoCollection)
            {
                var deviceInfo = await DeviceInformation.CreateFromIdAsync((string)serviceInfo.Properties["System.Devices.AepService.AepId"]);

                Logger?.LogInformation($"This device name is: '{deviceInfo.Name}' and id is: '{deviceInfo.Id}'");
            }

            Logger?.LogInformation($"Advertising service name: {SdpServiceName} on {machineName}");

            Status = SyncDeviceStatus.Started;
        }

        /// <summary>
        /// Creates the SDP record that will be revealed to the Client device when pairing occurs.  
        /// </summary>
        /// <param name="rfcommProvider">The RfcommServiceProvider that is being used to initialize the server</param>
        private void InitializeServiceSdpAttributes(IDictionary<uint, IBuffer> SdpRawAttributes)
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
            SdpRawAttributes.Add(SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        /// <summary>
        /// Invoked when the socket listener accepts an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accepted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private async void OnConnectionReceived(
            StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            if (Status != SyncDeviceStatus.Started)
            {
                Disconnect("Can't accept connection. Server not started");
                return;
            }

            StreamSocket socket;                 
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

            var clientSignature = remoteDevice.HostName?.DisplayName?.Replace(":", "")?.TrimStart('(')?.TrimEnd(')');

            var watcherSignature = bluetoothLeWatcher.GetSignature(clientSignature);

            if (watcherSignature != null)
            {
                clientSignature = GetSessionName(watcherSignature);
            }
            else
            {
                Logger?.LogInformation($"Unregonized signature {clientSignature}");
            }

            var channel = new BluetoothWindowsChannel(this, socket.Information.RemoteHostName.ToString(), socket) 
            { 
                Logger = Logger, 
                SessionName = clientSignature
            };

            if (!Channels.TryAdd(channel.DeviceId, channel))
            {
                Logger?.LogError("Can't add channel to dictionary?");
            }
            else
            {
                Logger?.LogInformation("Channel added to dictionary");
                RaiseOnDeviceConnected(channel);
            }
        }

        protected void Disconnect(string disconnectReason)
        {
            _ = StopScanningForSignatures(disconnectReason);

            if (rfcommProvider != null)
            {
                rfcommProvider.StopAdvertising();
                rfcommProvider = null;
            }

            if (socketListener != null)
            {
                socketListener.ConnectionReceived -= OnConnectionReceived;
                socketListener.Dispose();
                socketListener = null;
            }

            ClearChannels();

            Logger?.LogInformation(disconnectReason);

            Status = SyncDeviceStatus.Stopped;
        }
    }
}
