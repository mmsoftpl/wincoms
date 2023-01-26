﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
       // private BluetoothLePublisher bluetoothLePublisher = null;
        public override bool IsHost { get => false; }

        public ConnectStrategy ConnectStrategy = ConnectStrategy.ScanServices;

        //private Task StartLePublisherAsync(string sessionName)
        //{
        //    var clientMacAddress = string.Join(",", BluetoothAdapters().Select(a => a.GetPhysicalAddress().ToString().Replace(":", "")));
        //    var clientSignature = clientMacAddress + "|" + sessionName;

        //    bluetoothLePublisher = new BluetoothLePublisher() { Logger = Logger, ServiceName = ServiceName, SessionName = clientSignature };
        //    return bluetoothLePublisher.StartAsync(clientSignature, null, "Publishing LE signature");
        //}

        public override async Task StartAsync(string sessionName, string pin, string reason)
        {
            Pin = pin;
           // await StartLePublisherAsync(sessionName);

            SessionName = sessionName;
            ConnectStrategy = ConnectStrategy.ScanServices;
            await RestartAsync(reason);
        }

        public override Task RestartAsync(string reason)
        {
            Logger?.LogInformation(reason);
            ClearChannels();
            Status = SyncDeviceStatus.Created;
            FindDevices();
            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            Disconnect(reason);
            return Task.CompletedTask;
        }

        internal override void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            Status = SyncDeviceStatus.Stopped;
            base.RaiseOnDeviceDisconnected(device); 
        }

        internal override void RaiseOnConnectionStarted(ISyncDevice device)
        {
            //StopLePublisher();
            StopWatcher();
            base.RaiseOnConnectionStarted(device);
        }

        public ConcurrentDictionary<string, DeviceInformation> ResultCollection
        {
            get;
            private set;
        } = new ConcurrentDictionary<string, DeviceInformation>();

        public void FindDevices()
        {
            StopWatcher();

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
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>((watcher, deviceInfo) =>
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

                if (HasServiceName(serviceName) && deviceInfo.Id.Contains("RFCOMM"))
                {
                    ResultCollection.TryAdd(deviceInfo.Id, deviceInfo);
                    Logger?.LogInformation($"[Device added] {deviceInfo.Id}, {deviceInfo.Name}");
                }
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (ResultCollection.TryGetValue(deviceInfoUpdate.Id, out var rfcommInfoDisp))
                {
                    rfcommInfoDisp.Update(deviceInfoUpdate);

                    Logger?.LogInformation($"[Device updated] {rfcommInfoDisp.Id}, {rfcommInfoDisp.Name}");
                }
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (ResultCollection.TryRemove(deviceInfoUpdate.Id, out var rfcommInfoDisp))
                {
                    Logger?.LogInformation($"[Device removed] {rfcommInfoDisp.Id}, {rfcommInfoDisp.Name}");
                }
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, object>((watcher, obj) =>
            {
                Logger?.LogInformation($"[Enumeration completed] {ResultCollection.Count} devices found");

                foreach (var deviceInfo in ResultCollection.Values)
                {
                    var channel = Task.Run(() => Connect(deviceInfo)).Result;

                    if (channel != null)
                    {
                        StopWatcher();
                        Logger?.LogInformation($"Connected to {deviceInfo.Name}...");

                        Status = channel.Status;

                        RaiseOnDeviceConnected(this);
                    }
                }

                if (Channels.Count == 0)
                {
                    //if (bluetoothLePublisher?.LastError == BluetoothError.RadioNotAvailable)
                    //{
                    //    RaiseOnError($"Make sure your Bluetooth Radio is on; '{bluetoothLePublisher.LastError}'");
                    //    Disconnect(null);
                    //}
                    //else
                    if (ConnectStrategy == ConnectStrategy.ScanDevices)
                        {
                            Status = SyncDeviceStatus.Stopped;
                            Disconnect($"Could not discover {SdpServiceName}");
                            RaiseOnError("No hosting sessions in range?");
                        }
                        else
                    if (ConnectStrategy == ConnectStrategy.ScanServices)
                    {
                        ConnectStrategy = ConnectStrategy.ScanDevices;
                        RestartAsync("Restart in scan mode");
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

            string serviceName = string.Empty;

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
            }

            return new Tuple<RfcommDeviceService, string>(rfcommDeviceService, serviceName);
        }

        private async Task<BluetoothDevice> CreateBluetoothDevice(DeviceInformation deviceInfoDisp)
        {    
            // Make sure user has selected a device first
            if (deviceInfoDisp != null)
            {
                Logger?.LogInformation($"Testing remote device {deviceInfoDisp.Name} for presence of {SdpServiceName}...");
            }
            else
                return null;

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
                    var channel = new BluetoothWindowsChannel(this, rfcommDeviceService.ConnectionHostName.DisplayName, rfcommDeviceService) 
                    { 
                        Logger = Logger, 
                        SessionName = GetSessionName(s?.Item2)
                    };

                    RegisterChannel(channel, Pin);

                    return channel;
                }
            }
            return null;
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                Logger?.LogTrace("Stopping DeviceWatcher watcher"); 
                if ((DeviceWatcherStatus.Started == deviceWatcher.Status ||
                     DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status))
                {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }

        //private void StopLePublisher()
        //{
        //    if (bluetoothLePublisher != null)
        //    {
        //        Logger?.LogTrace("Stopping Bluetooth LE Publisher watcher");
        //        bluetoothLePublisher?.StopAsync(null);
        //        bluetoothLePublisher = null;
        //    }
        //}

        public void Disconnect(string disconnectReason)
        {
            StopWatcher();
            
          //  StopLePublisher();

            ClearChannels();

            BluetoothDevice?.Dispose();
            BluetoothDevice = null;

            Logger?.LogInformation(disconnectReason);

            RaiseOnDeviceDisconnected(this);
        }
    }

}
