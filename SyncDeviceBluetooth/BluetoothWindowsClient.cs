using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public enum ConnectStrategy
    {
        ScanDevices,
        ScanServices
    }

    public class BluetoothWindowsClient : BluetoothWindows
    {
        private DeviceWatcher deviceWatcher = null;
        private BluetoothDevice BluetoothDevice = null;

        private ConnectStrategy ConnectStrategy = ConnectStrategy.ScanServices;

        public override Task StartAsync(string sessionName, string reason)
        {
            SessionName = sessionName;
            ConnectStrategy = ConnectStrategy.ScanServices;
            return RestartAsync(reason);
        }

        private Task RestartAsync(string reason)
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
            StopWatcher();
            Logger?.LogInformation($"Enumeration started. Scanning for devices...");

            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            string asqFilter = $"(System.Devices.AepService.ProtocolId:=\"{BluetoothProtocolId}\" AND\r\nSystem.Devices.AepService.ServiceClassId:=\"{RfcommChatServiceUuid}\")";
            asqFilter = $"(System.Devices.AepService.ProtocolId:=\"{{{BluetoothProtocolId}}}\")";

            if (ConnectStrategy == ConnectStrategy.ScanDevices)
            {
                Logger?.LogInformation("Scaning for devices (slow)");
                deviceWatcher = DeviceInformation.CreateWatcher(asqFilter,
                                                                requestedProperties,
                                                                DeviceInformationKind.AssociationEndpoint);
            }
            else
            if (ConnectStrategy == ConnectStrategy.ScanServices)
            {
                Logger?.LogInformation("Scaning for services (quick)");
                deviceWatcher = DeviceInformation.CreateWatcher(asqFilter,
                                                                requestedProperties,
                                                                DeviceInformationKind.AssociationEndpointService);
            }

                // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                var serviceName = deviceInfo.Name;

                if (ConnectStrategy == ConnectStrategy.ScanDevices)
                {
                    var b = Task.Run(() => CreateBluetoothDevice(deviceInfo)).Result;
                    if (b != null)
                    {
                        var s = Task.Run(() => GetRfcommDeviceService(b)).Result;
                        serviceName = s?.Item2;
                    }
                }

                if (IsEFMserviceName(serviceName))
                {
                    if (serviceName.Contains(SessionName))
                    {
                        ResultCollection.TryAdd(deviceInfo.Id, deviceInfo);
                        Logger?.LogInformation($"{deviceInfo.Id}, {deviceInfo.Name} (device info added) for session {serviceName}");
                    }
                    else
                    {
                        Logger?.LogInformation($"{deviceInfo.Id}, {deviceInfo.Name} (device info NOT added) - different session {serviceName}");
                    }
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
                        StopWatcher();
                        Logger?.LogInformation($"Connected to {deviceInfo.Name}...");

                        Status = channel.Status;

                        RaiseOnDeviceConnected(channel);
                        break;
                    }
                }

                if (channel == null)
                {
                    if (ConnectStrategy == ConnectStrategy.ScanDevices)
                    {
                        Status = SyncDeviceStatus.Stopped;
                        Disconnect($"Could not discover {SdpServiceName(this)}");
                    }
                    else
                    {
                        ConnectStrategy = ConnectStrategy.ScanDevices;
                        RestartAsync("Switching to devices scan mode (slow)");
                    }
                }
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, object>((watcher, obj) =>
            {
                ResultCollection.Clear();
            });

            deviceWatcher.Start();
        }

        private async Task<Tuple<RfcommDeviceService,string>> GetRfcommDeviceService(BluetoothDevice bluetoothDevice)
        {
            if (bluetoothDevice == null)
                return null;

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServicesTask = bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(RfcommChatServiceUuid), BluetoothCacheMode.Uncached).AsTask();

            rfcommServicesTask.Wait();

            var rfcommServices = rfcommServicesTask.Result;
            RfcommDeviceService rfcommDeviceService = null;

            if (rfcommServices.Services.Count > 0)
                rfcommDeviceService = rfcommServices.Services[0];

            string serviceName = String.Empty;

            if (rfcommDeviceService != null)
            {
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
                serviceName = attributeReader.ReadString(serviceNameLength);

                if (!serviceName.Contains(SessionName))
                {
                    Logger?.LogError($"This is not proper service. Wrong session group {SessionName}");

                    return null;
                }
            }

            return new Tuple<RfcommDeviceService, string>(rfcommDeviceService, serviceName);
        }

        private async Task<BluetoothDevice> CreateBluetoothDevice(DeviceInformation deviceInfoDisp)
        {    
            // Make sure user has selected a device first
            if (deviceInfoDisp != null)
            {
                Logger?.LogInformation($"Testing remote device {deviceInfoDisp.Name} for presence of {SdpServiceName(this)}...");
            }
            else
            {
                Logger?.LogError("Please select an item to connect to");
                return null;
            }

            BluetoothDevice bluetoothDevice;
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
            return bluetoothDevice;
        }

        public async Task<BluetoothWindowsChannel> Connect(DeviceInformation deviceInfoDisp)
        {
            BluetoothDevice = await CreateBluetoothDevice(deviceInfoDisp);

            if (BluetoothDevice != null)
            {
                var s = await GetRfcommDeviceService(BluetoothDevice);

                RfcommDeviceService rfcommDeviceService = s?.Item1;

                if (rfcommDeviceService != null)
                {
                    var channel = new BluetoothWindowsChannel(this, deviceInfoDisp.Id, rfcommDeviceService) { Logger = Logger };

                    if (!Channels.TryAdd(deviceInfoDisp.Id, channel))
                    {
                        BluetoothDevice = null;
                        Logger?.LogError("Can't add channel to dictionary?");
                        return null;
                    }
                    else
                    {
                        Logger?.LogInformation("Channel added to dictionary?");
                        return channel;
                    }
                }
            }
            return null;
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

            StopWatcher();

            BluetoothDevice?.Dispose();
            BluetoothDevice = null;
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
