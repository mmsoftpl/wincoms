using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
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
        private BluetoothDevice BluetoothDevice = null;
        public override bool IsHost { get => false; }

        private BluetoothWatcher bluetoothWatcherServices = null;
        private BluetoothWatcher bluetoothWatcheroDevices = null;

        private async Task StartWatchers(string sessionName)
        {
            if (bluetoothWatcheroDevices == null)
            {
                bluetoothWatcheroDevices = new BluetoothWatcher() { ConnectStrategy = ConnectStrategy.ScanDevices, Logger = Logger };

                await bluetoothWatcheroDevices.StartAsync(sessionName, null, "Starting devices scanner");
                bluetoothWatcheroDevices.OnChanged += BluetoothWatcherServices_OnChanged;
            }

            if (bluetoothWatcherServices == null)
            {
                bluetoothWatcherServices = new BluetoothWatcher() { ConnectStrategy = ConnectStrategy.ScanServices, Logger = Logger };

                await bluetoothWatcherServices.StartAsync(sessionName, null, "Starting services scanner");
                bluetoothWatcherServices.OnChanged += BluetoothWatcherServices_OnChanged;
            }
        }

        private DateTime? LastCheck = null;

        private void BluetoothWatcherServices_OnChanged(object sender, System.Collections.Generic.IEnumerable<DeviceInformationDetails> e)
        {
            foreach (var deviceInfo in e)
            {
                if (LastCheck.HasValue && deviceInfo.LastStamp < LastCheck)
                    continue;

                RaiseOnDeviceDetected(deviceInfo.DeviceInformation.Name, deviceInfo.DeviceInformation.Id, "?", out var detectedArgs);
                if (!detectedArgs.Cancel)
                    Connect(deviceInfo.DeviceInformation).Wait();

            }
            LastCheck = DateTime.UtcNow;
        }

        private async Task StopWatchers(string reason)
        {
            await bluetoothWatcheroDevices?.StopAsync(reason);
            bluetoothWatcheroDevices = null;
            await bluetoothWatcherServices?.StopAsync(reason);
            bluetoothWatcherServices = null;
        }

        public override async Task StartAsync(string sessionName, string pin, string reason)
        {
            Pin = pin;
            SessionName = sessionName;

            await StartWatchers(sessionName);
            Status = SyncDeviceStatus.Started;
        }

        public override Task StopAsync(string reason)
        {
            return Disconnect(reason);
        }

        internal override void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            //Status = SyncDeviceStatus.Stopped;
            base.RaiseOnDeviceDisconnected(device); 
        }

        internal override void RaiseOnConnectionStarted(ISyncDevice device)
        {
            base.RaiseOnConnectionStarted(device);
        } 

        private async Task<string> GetServiceNameAsync(RfcommDeviceService rfcommDeviceService)
        {
            if (rfcommDeviceService != null)
            {
                // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
                var attributes = await rfcommDeviceService.GetSdpRawAttributesAsync();
                if (!attributes.ContainsKey(SdpServiceNameAttributeId))
                    return null;

                var attributeReader = DataReader.FromBuffer(attributes[SdpServiceNameAttributeId]);
                var attributeType = attributeReader.ReadByte();
                if (attributeType != SdpServiceNameAttributeType)
                    return null;

                var serviceNameLength = attributeReader.ReadByte();
                // The Service Name attribute requires UTF-8 encoding.
                attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                return attributeReader.ReadString(serviceNameLength);
            }
            return null;
        }

        private async Task<Tuple<RfcommDeviceService,string>> GetServiceAsync(BluetoothDevice bluetoothDevice)
        {
            if (bluetoothDevice == null)
                return null;

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServicesTask = bluetoothDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached).AsTask();

            rfcommServicesTask.Wait();
            
            var rfcommServices = rfcommServicesTask.Result;

            foreach (var rfcommDeviceService in rfcommServices.Services)
            {
                var serviceName = await GetServiceNameAsync(rfcommDeviceService);
                
                if (HasServiceName(serviceName))
                    return new Tuple<RfcommDeviceService, string>(rfcommDeviceService, serviceName);
            }
            
            return new Tuple<RfcommDeviceService, string>(null,null);
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
            if (ChannelCreated(deviceInfoDisp?.Id))
            {
                Logger?.LogInformation($"Skipping remote device {deviceInfoDisp.Name}, channel already created");
                return null;
            }

            BluetoothDevice = await CreateBluetoothDevice(deviceInfoDisp);

            if (BluetoothDevice != null)
            {
                if (Channels.ContainsKey(FormatDeviceName(BluetoothDevice.HostName.DisplayName))) 
                    return null;

                var s = await GetServiceAsync(BluetoothDevice);

                RfcommDeviceService rfcommDeviceService = s?.Item1;

                if (rfcommDeviceService != null)
                {
                    var channel = new BluetoothWindowsChannel(this, FormatDeviceName( rfcommDeviceService.ConnectionHostName.DisplayName), rfcommDeviceService) 
                    { 
                        Logger = Logger, 
                        SessionName = GetSessionName(s?.Item2)
                    };

                    RaiseOnDeviceConnecting(channel, out var e);

                    if (!e.Cancel)
                    {
                        RegisterChannel(channel, Pin);
                        return channel;
                    }
                }
            }
            else
            {
                Logger?.LogError($"Bluetooth Device is null? {deviceInfoDisp}");
            }
            return null;
        }

        public async Task Disconnect(string disconnectReason)
        {
            await StopWatchers(disconnectReason);

            ClearChannels();

            BluetoothDevice?.Dispose();
            BluetoothDevice = null;

            Logger?.LogInformation(disconnectReason);
            Status = SyncDeviceStatus.Stopped;
            RaiseOnDeviceDisconnected(this);
        }
    }

}
