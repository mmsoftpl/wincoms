using SDKTemplate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking.Sockets;
using Windows.Networking;
using System.IO;
using Windows.Devices.I2c;

namespace WindowsFormsApp1
{

    public partial class WiFiDirectClientPanel : ComsPanel
    {
        public WiFiDirectClientPanel()
        {
            InitializeComponent();
        }

        private WiFiDirectConnectorSettings Settings = new WiFiDirectConnectorSettings();

    //   private WiFiDirectAdvertisementPublisher _publisher = new WiFiDirectAdvertisementPublisher();
        DeviceWatcher _deviceWatcher = null;

        public override void OnUpdateControls()
        {
            base.OnUpdateControls();
            if (DDevice != null)
                _ = DoConnectToDevice(DDevice);
        }
        public ConcurrentDictionary<string, DiscoveredDevice> DiscoveredDevices { get; private set; } = new ConcurrentDictionary<string, DiscoveredDevice>();

        public ConnectedDevice CDevice { get; set;} = null;
        public DiscoveredDevice DDevice { get; set;} = null;
        public override void FindDevices()
        {
            //_publisher.Start();

            //if (_publisher.Status != WiFiDirectAdvertisementPublisherStatus.Started)
            //{
            //    MainPage.NotifyUser("Failed to start advertisement.", NotifyType.ErrorMessage);
            //    return;
            //}

            MainPage.Log("Finding Devices...", NotifyType.StatusMessage);
            DDevice = null;
            CDevice = null;
            DiscoveredDevices = new ConcurrentDictionary<string, DiscoveredDevice>();

            String deviceSelector = WiFiDirectDevice.GetDeviceSelector(Settings.wiFiDirectDeviceSelectorType);

            _deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector, new string[] { "System.Devices.WiFiDirect.InformationElements" });

            _deviceWatcher.Added += OnDeviceAdded;
            _deviceWatcher.Removed += OnDeviceRemoved;
            _deviceWatcher.Updated += OnDeviceUpdated;
            _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
            _deviceWatcher.Stopped += OnStopped;

            _deviceWatcher.Start();

            Status = WiFiDirectAdvertisementPublisherStatus.Created;

            UpdateControls();
        }

        public async Task Disconnect()
        {
            await Task.Delay(1000);

            Status = WiFiDirectAdvertisementPublisherStatus.Stopped;
            UpdateControls();
        }

        #region DeviceWatcherEvents
        private void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            if (DiscoveredDevices.TryAdd(deviceInfo.Id, new DiscoveredDevice(deviceInfo)))
            {
                MainPage.Log($"Added device [{deviceInfo.Id}]", NotifyType.StatusMessage);
            }
            UpdateControls();
        }

        private void OnDeviceRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            if (DiscoveredDevices.TryRemove(deviceInfoUpdate.Id, out _))
            {
                MainPage.Log($"Removed device [{deviceInfoUpdate.Id}]", NotifyType.StatusMessage);
            }
            UpdateControls();
        }

        private void OnDeviceUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            if (DiscoveredDevices.TryGetValue(deviceInfoUpdate.Id, out var discoveredDevice))
            {
                discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                MainPage.Log($"Updated device [{deviceInfoUpdate.Id}]", NotifyType.StatusMessage);
            }
            UpdateControls();
        }

        private void OnEnumerationCompleted(DeviceWatcher deviceWatcher, object o)
        {
            MainPage.Log("DeviceWatcher enumeration completed", NotifyType.StatusMessage);

            if (DiscoveredDevices.Count > 0)
            {
                DDevice = DiscoveredDevices.Values.FirstOrDefault();
            }
            else
            {
                MainPage.Log("No devices found?", NotifyType.StatusMessage);
                _ = Disconnect();
            }

            UpdateControls();

        }

        private void OnStopped(DeviceWatcher deviceWatcher, object o)
        {
            MainPage.Log("DeviceWatcher stopped", NotifyType.StatusMessage);
        }
        #endregion

        private ConnectedDevice DoConnectToDevice(DiscoveredDevice discoveredDevice)
        {
            if (discoveredDevice == null)
            {
                MainPage.Log("No device selected, please select one.", NotifyType.ErrorMessage);
                return null;
            }

            MainPage.Log($"Connecting to {discoveredDevice.DeviceInfo.Name}...", NotifyType.StatusMessage);

            if (!discoveredDevice.DeviceInfo.Pairing.IsPaired)
            {
                var requestResult = Task.Run(() => ConnectionSettingsPanel.RequestPairDeviceAsync(discoveredDevice.DeviceInfo.Pairing)).Result;

                if (!requestResult)
                {
                    return null;
                }
            }

            WiFiDirectDevice wfdDevice = null;

            try
            {
                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                var t = WiFiDirectDevice.FromIdAsync(discoveredDevice.DeviceInfo.Id).AsTask();
                t.Wait();
                wfdDevice = t.Result;
            }
            catch (TaskCanceledException)
            {
                MainPage.Log("FromIdAsync was canceled by user", NotifyType.ErrorMessage);
                return null;
            }

            return Task.Run(() => ConnectDevice(wfdDevice)).Result;
        }

            private async Task<ConnectedDevice> ConnectDevice(WiFiDirectDevice wfdDevice)
        {
            // Register for the ConnectionStatusChanged event handler
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            IReadOnlyList<EndpointPair> endpointPairs = wfdDevice.GetConnectionEndpointPairs();
            HostName remoteHostName = endpointPairs[0].RemoteHostName;

            MainPage.Log($"Devices connected on L2 layer, connecting to IP Address: {remoteHostName} Port: {Globals.strServerPort}",
                NotifyType.StatusMessage);

            // Wait for server to start listening on a socket
            await Task.Delay(5000);


            var endpoints = StreamSocket.GetEndpointPairsAsync(remoteHostName, Globals.strServerPort).AsTask();
            endpoints.Wait();            

            foreach(var ed in endpoints.Result)
            {
                MainPage.Log($"RemoteHostName: {ed.RemoteHostName}, LocalServiceName: {ed.LocalServiceName}, LocalHostName: {ed.LocalHostName}, LocalHostName: {ed.LocalHostName}", NotifyType.StatusMessage);
            }
            
            // Connect to Advertiser on L4 layer
            DatagramSocket clientSocket = new DatagramSocket();
            try
            {
                var t = clientSocket.ConnectAsync(remoteHostName, Globals.strServerPort).AsTask();
                t.Wait();
                MainPage.Log("Connected with remote side on L4 layer", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                MainPage.Log($"Connect operation threw an exception: {ex.Message}", NotifyType.ErrorMessage);
                return null;
            }

            SocketWrapper socketWrapper = new SocketWrapper(MainPage, null, clientSocket, true);
            SocketReaderWriter socketRW = null;// new SocketReaderWriter(clientSocket, MainPage);

            string sessionId = Path.GetRandomFileName();
            ConnectedDevice connectedDevice = new ConnectedDevice(sessionId, wfdDevice, socketRW, socketWrapper);

            // The first message sent over the socket is the name of the connection.
            await connectedDevice.WriteMessageAsync(sessionId);

            while (await connectedDevice.ReadMessageAsync() != null)
            {
                // Keep reading messages
            }

            return connectedDevice;
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            MainPage.Log($"Connection status changed: {sender.ConnectionStatus}", NotifyType.StatusMessage);

            if (sender.ConnectionStatus == WiFiDirectConnectionStatus.Connected)
                Status = WiFiDirectAdvertisementPublisherStatus.Started;
            else
                if (sender.ConnectionStatus == WiFiDirectConnectionStatus.Disconnected)
                Status = WiFiDirectAdvertisementPublisherStatus.Stopped;

            UpdateControls();
        }
    }
}
