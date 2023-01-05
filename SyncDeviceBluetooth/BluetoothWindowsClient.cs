using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.Runtime.Remoting.Messaging;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsClient : BluetoothWindows
    {
        private DeviceWatcher deviceWatcher = null;
        private BluetoothDevice bluetoothDevice = null;

        public override Task StartAsync(string reason)
        {
            Logger?.LogInformation(reason);
            Status = SyncDeviceStatus.Created;
            FindDevices();
            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            Disconnect(reason);
            return Task.CompletedTask;
        }

        public ConcurrentDictionary<string, DeviceInformation> ResultCollection
        {
            get;
            private set;
        } = new ConcurrentDictionary<string, DeviceInformation>();

        public void FindDevices()
        {
            Logger?.LogInformation($"Enumeration started. Scanning for devices...");

            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>((watcher, deviceInfo) =>
            {
                if (deviceInfo.Name != "")
                {
                    ResultCollection.TryAdd(deviceInfo.Id, deviceInfo);

                    Logger?.LogInformation($"{deviceInfo.Id}, {deviceInfo.Name} (device info added)");
                }
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (ResultCollection.TryGetValue(deviceInfoUpdate.Id, out var rfcommInfoDisp))
                {
                    rfcommInfoDisp.Update(deviceInfoUpdate);

                    Logger?.LogInformation($"{rfcommInfoDisp.Id}, {rfcommInfoDisp.Name} (device info updated)");
                }
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (ResultCollection.TryRemove(deviceInfoUpdate.Id, out var rfcommInfoDisp))
                {
                    Logger?.LogInformation($"{rfcommInfoDisp.Id}, {rfcommInfoDisp.Name} (device info removed)");
                }
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>((watcher, obj) =>
            {
                Logger?.LogInformation($"{ResultCollection.Count} devices found. Enumeration completed. Scanning for service...");

                BluetoothWindowsChannel channel = null;

                foreach (var deviceInfo in ResultCollection.Values)
                {
                    channel = Task.Run(() => Connect(deviceInfo)).Result;

                    if (channel != null)
                    {
                        Logger?.LogInformation($"Connected to {deviceInfo.Name}...");

                        Status = channel.Status;

                        RaiseOnDeviceConnected(channel);
                        break;
                    }
                }

                if (channel == null)
                {
                    Logger?.LogInformation($"Could not discover the {SdpServiceName}");
                    // ResetMainUI();
                    Status = SyncDeviceStatus.Stopped;
                }
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>((watcher, obj) =>
            {
                ResultCollection.Clear();
            });

            deviceWatcher.Start();
        }

        public async Task<BluetoothWindowsChannel> Connect(DeviceInformation deviceInfoDisp)
        {
            // Make sure user has selected a device first
            if (deviceInfoDisp != null)
            {
                Logger?.LogInformation($"Testing remote device {deviceInfoDisp.Name} for presence of {SdpServiceName}...");
            }
            else
            {
                Logger?.LogError("Please select an item to connect to");
                return null;
            }

            // RfcommChatDeviceDisplay deviceInfoDisp = resultsListView.SelectedItem as RfcommChatDeviceDisplay;

            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(deviceInfoDisp.Id).CurrentStatus;
            if (accessStatus == DeviceAccessStatus.DeniedByUser)
            {
                Logger?.LogError("This app does not have access to connect to the remote device (please grant access in Settings > Privacy > Other Devices");
                return null;
            }
            // If not, try to get the Bluetooth device
            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfoDisp.Id);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
                //   ResetMainUI();
                return null;
            }
            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.
            if (bluetoothDevice == null)
            {
                Logger?.LogError("Bluetooth Device returned null. Access Status = " + accessStatus.ToString());
            }

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServicesTask = bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(RfcommChatServiceUuid), BluetoothCacheMode.Uncached).AsTask();

            rfcommServicesTask.Wait();

            var rfcommServices = rfcommServicesTask.Result;
            RfcommDeviceService rfcommDeviceService;

            if (rfcommServices.Services.Count > 0)
            {
                rfcommDeviceService = rfcommServices.Services[0];

                if (rfcommDeviceService == null)
                {
                    Logger?.LogError(
                        "The Chat service is null?");
                    return null;
                }
            }
            else
            {
                return null;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await rfcommDeviceService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(SdpServiceNameAttributeId))
            {
                Logger?.LogError(
                    "The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
                    "Please verify that you are running the BluetoothRfcommChat server.");
                return null;
            }
            var attributeReader = DataReader.FromBuffer(attributes[SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != SdpServiceNameAttributeType)
            {
                Logger?.LogError(
                    "The Chat service is using an unexpected format for the Service Name attribute. " +
                    "Please verify that you are running the BluetoothRfcommChat server.");
                // ResetMainUI();
                return null;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;

            StopWatcher();

            //var streamSocket = new StreamSocket();
            //await streamSocket.ConnectAsync(rfcommDeviceService.ConnectionHostName, rfcommDeviceService.ConnectionServiceName);

            var channel = new BluetoothWindowsChannel(this, deviceInfoDisp.Id, rfcommDeviceService) {  Logger = Logger };

            if (!Channels.TryAdd(deviceInfoDisp.Id, channel))
            {
                Logger?.LogError("Can't add channel to dictionary?");
                return null;
            }
            else
            {

                Logger?.LogInformation("Channel added to dictionary?");
                return channel;
            }
            //catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            //{
            //    Logger?.LogError("Please verify that you are running the BluetoothRfcommChat server.");
            //    //   ResetMainUI();
            //}
            //catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            //{
            //    Logger?.LogError("Please verify that there is no other RFCOMM connection to the same device.");
            //    //  ResetMainUI();
            //}
            //  return null;
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                if ((DeviceWatcherStatus.Started == deviceWatcher.Status ||
                     DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status))
                {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }
        /*
        private async void ReceiveStringLoop(DataReader chatReader)
        {
            try
            {
                uint size = await chatReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint))
                {
                    Disconnect("Remote device terminated connection - make sure only one instance of server is running on remote device");
                    return;
                }

                uint stringLength = chatReader.ReadUInt32();
                uint actualStringLength = await chatReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength)
                {
                    // The underlying socket was closed before we were able to read the whole data
                    return;
                }

                RaiseOnMessage(chatReader.ReadString(stringLength));

                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (chatSocket == null)
                    {
                        // Do not print anything here -  the user closed the socket.
                        if ((uint)ex.HResult == 0x80072745)
                            Logger?.LogInformation("Disconnect triggered by remote device");
                        else if ((uint)ex.HResult == 0x800703E3)
                            Logger?.LogInformation("The I/O operation has been aborted because of either a thread exit or an application request.");
                    }
                    else
                    {
                        Disconnect("Read stream failed with error: " + ex.Message);
                    }
                }
            }
        }
        */
        /// <summary>
        /// Cleans up the socket and DataWriter and reset the UI
        /// </summary>
        /// <param name="disconnectReason"></param>
        public void Disconnect(string disconnectReason)
        {
            ClearChannels();

            //if (chatService != null)
            //{
            //    chatService.Dispose();
            //    chatService = null;
            //}
            //lock (this)
            //{
            //    if (chatSocket != null)
            //    {
            //        chatSocket.Dispose();
            //        chatSocket = null;
            //    }
            //}

            Logger?.LogInformation(disconnectReason);

            RaiseOnDeviceDisconnected(this);
        }
    }

}
