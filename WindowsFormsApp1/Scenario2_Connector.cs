using SDKTemplate;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace WindowsFormsApp1
{
    public partial class Scenario2_Connector : Form
    {
        private MainPage rootPage = null;// MainPage.Current;
        DeviceWatcher _deviceWatcher = null;
//        bool _fWatcherStarted = false;
      //  WiFiDirectAdvertisementPublisher _publisher = new WiFiDirectAdvertisementPublisher();

        public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; } = new ObservableCollection<DiscoveredDevice>();
        public ObservableCollection<ConnectedDevice> ConnectedDevices { get; } = new ObservableCollection<ConnectedDevice>();

        public Scenario2_Connector()
        {
            this.InitializeComponent();

            cmbDeviceSelector.Items.Add(WiFiDirectDeviceSelectorType.DeviceInterface);
            cmbDeviceSelector.Items.Add(WiFiDirectDeviceSelectorType.AssociationEndpoint);
            cmbDeviceSelector.SelectedIndex = 1;

        }

        //protected override void OnNavigatedFrom(NavigationEventArgs e)
        //{
        //    if (_deviceWatcher != null)
        //    {
        //        StopWatcher();
        //    }
        //}

        private void StopWatcher()
        {
            _deviceWatcher.Added -= OnDeviceAdded;
            _deviceWatcher.Removed -= OnDeviceRemoved;
            _deviceWatcher.Updated -= OnDeviceUpdated;
            _deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
            _deviceWatcher.Stopped -= OnStopped;

            _deviceWatcher.Stop();

            _deviceWatcher = null;
        }

        private void btnWatcher_Click(object sender, EventArgs e)
        {
            if (_deviceWatcher == null)
            {
                //_publisher.Start();

                //if (_publisher.Status != WiFiDirectAdvertisementPublisherStatus.Started)
                //{
                //    MainPage.NotifyUser("Failed to start advertisement.", NotifyType.ErrorMessage);
                //    return;
                //}

                DiscoveredDevices.Clear();
                MainPage.Log("Finding Devices...", NotifyType.StatusMessage);

                String deviceSelector = WiFiDirectDevice.GetDeviceSelector(
                    Utils.GetSelectedItemTag<WiFiDirectDeviceSelectorType>(cmbDeviceSelector));

                _deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector, new string[] { "System.Devices.WiFiDirect.InformationElements" });

                _deviceWatcher.Added += OnDeviceAdded;
                _deviceWatcher.Removed += OnDeviceRemoved;
                _deviceWatcher.Updated += OnDeviceUpdated;
                _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
                _deviceWatcher.Stopped += OnStopped;

                _deviceWatcher.Start();

                btnWatcher.Text = "Stop Watcher";
//                _fWatcherStarted = true;
            }
            else
            {
              //  _publisher.Stop();

                btnWatcher.Text = "Start Watcher";

                StopWatcher();

                MainPage.Log("Device watcher stopped.", NotifyType.StatusMessage);
            }

            updateForm();
        }

        private void updateForm()
        {
            lvDiscoveredDevices.Invoke((MethodInvoker)(() =>
            {
                lvDiscoveredDevices.Items.Clear();
                foreach (var i in DiscoveredDevices)
                    lvDiscoveredDevices.Items.Add(i);
            }));

            lvConnectedDevices.Invoke((MethodInvoker)(() =>
            {
                lvConnectedDevices.Items.Clear();
                foreach (var i in ConnectedDevices)
                    lvConnectedDevices.Items.Add(i);
            }));
        }

        #region DeviceWatcherEvents
        private async void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>

            await Task.Run(() =>
            {
                DiscoveredDevices.Add(new DiscoveredDevice(deviceInfo));
            }
            );

            updateForm();
        }

        private async void OnDeviceRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            await Task.Run(()=>
            {
                foreach (DiscoveredDevice discoveredDevice in DiscoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        DiscoveredDevices.Remove(discoveredDevice);
                        break;
                    }
                }
            }
            );
            updateForm();
        }

        private async void OnDeviceUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
          //  await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
          await Task.Run(()=>
            {
                foreach (DiscoveredDevice discoveredDevice in DiscoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                        break;
                    }
                }
            }
            );

            updateForm();
        }

        private void OnEnumerationCompleted(DeviceWatcher deviceWatcher, object o)
        {
            MainPage.Log("DeviceWatcher enumeration completed", NotifyType.StatusMessage);
        }

        private void OnStopped(DeviceWatcher deviceWatcher, object o)
        {
            MainPage.Log("DeviceWatcher stopped", NotifyType.StatusMessage);
        }
        #endregion

        private void btnIe_Click(object sender, EventArgs e)
        {
            var discoveredDevice = (DiscoveredDevice)lvDiscoveredDevices.SelectedItem;

            IList<WiFiDirectInformationElement> informationElements = null;
            try
            {
                informationElements = WiFiDirectInformationElement.CreateFromDeviceInformation(discoveredDevice.DeviceInfo);
            }
            catch (Exception ex)
            {
                MainPage.Log("No Information element found: " + ex.Message, NotifyType.ErrorMessage);
            }

            if (informationElements != null)
            {
                StringWriter message = new StringWriter();

                foreach (WiFiDirectInformationElement informationElement in informationElements)
                {
                    string ouiName = CryptographicBuffer.EncodeToHexString(informationElement.Oui);
                    string value = string.Empty;
                    Byte[] bOui = informationElement.Oui.ToArray();

                    if (bOui.SequenceEqual(Globals.MsftOui))
                    {
                        // The format of Microsoft information elements is documented here:
                        // https://msdn.microsoft.com/en-us/library/dn392651.aspx
                        // with errata here:
                        // https://msdn.microsoft.com/en-us/library/mt242386.aspx
                        ouiName += " (Microsoft)";
                    }
                    else if (bOui.SequenceEqual(Globals.WfaOui))
                    {
                        ouiName += " (WFA)";
                    }
                    else if (bOui.SequenceEqual(Globals.CustomOui))
                    {
                        ouiName += " (Custom)";

                        if (informationElement.OuiType == Globals.CustomOuiType)
                        {
                            DataReader dataReader = DataReader.FromBuffer(informationElement.Value);
                            dataReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                            dataReader.ByteOrder = ByteOrder.LittleEndian;

                            // Read the string.
                            try
                            {
                                string data = dataReader.ReadString(dataReader.ReadUInt32());
                                value = $"Data: {data}";
                            }
                            catch (Exception)
                            {
                                value = "(Unable to parse)";
                            }
                        }
                    }

                    message.WriteLine($"OUI {ouiName}, Type {informationElement.OuiType} {value}");
                }

                message.Write($"Information elements found: {informationElements.Count}");

                MainPage.Log(message.ToString(), NotifyType.StatusMessage);
            }

            updateForm();
        }

        private async void btnFromId_Click(object sender, EventArgs e)
        {
            var discoveredDevice = (DiscoveredDevice)lvDiscoveredDevices.SelectedItem;

            if (discoveredDevice == null)
            {
                MainPage.Log("No device selected, please select one.", NotifyType.ErrorMessage);
                return;
            }

            MainPage.Log($"Connecting to {discoveredDevice.DeviceInfo.Name}...", NotifyType.StatusMessage);

            if (!discoveredDevice.DeviceInfo.Pairing.IsPaired)
            {
                if (!await ConnectionSettingsPanel.RequestPairDeviceAsync(discoveredDevice.DeviceInfo.Pairing))
                {
                    return;
                }
            }

            WiFiDirectDevice wfdDevice = null;
            try
            {
                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                wfdDevice = await WiFiDirectDevice.FromIdAsync(discoveredDevice.DeviceInfo.Id);
            }
            catch (TaskCanceledException)
            {
                MainPage.Log("FromIdAsync was canceled by user", NotifyType.ErrorMessage);
                return;
            }

            // Register for the ConnectionStatusChanged event handler
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            IReadOnlyList<EndpointPair> endpointPairs = wfdDevice.GetConnectionEndpointPairs();
            HostName remoteHostName = endpointPairs[0].RemoteHostName;

            MainPage.Log($"Devices connected on L2 layer, connecting to IP Address: {remoteHostName} Port: {Globals.strServerPort}",
                NotifyType.StatusMessage);

            // Wait for server to start listening on a socket
            await Task.Delay(2000);

            // Connect to Advertiser on L4 layer
            StreamSocket clientSocket = new StreamSocket();
            try
            {
                await clientSocket.ConnectAsync(remoteHostName, Globals.strServerPort);
                MainPage.Log("Connected with remote side on L4 layer", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                MainPage.Log($"Connect operation threw an exception: {ex.Message}", NotifyType.ErrorMessage);
                return;
            }

            SocketReaderWriter socketRW = new SocketReaderWriter(clientSocket, rootPage);

            string sessionId = Path.GetRandomFileName();
            ConnectedDevice connectedDevice = new ConnectedDevice(sessionId, wfdDevice, socketRW);
            ConnectedDevices.Add(connectedDevice);

            // The first message sent over the socket is the name of the connection.
            await socketRW.WriteMessageAsync(sessionId);

            while (await socketRW.ReadMessageAsync() != null)
            {
                // Keep reading messages
            }

            updateForm();
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            MainPage.Log($"Connection status changed: {sender.ConnectionStatus}", NotifyType.StatusMessage);
        }

        private async void btnSendMessage_Click(object sender, EventArgs e)
        {
            var connectedDevice = (ConnectedDevice)lvConnectedDevices.SelectedItem;
            await connectedDevice.SocketRW.WriteMessageAsync(txtSendMessage.Text);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            var connectedDevice = (ConnectedDevice)lvConnectedDevices.SelectedItem;
            ConnectedDevices.Remove(connectedDevice);

            // Close socket and WiFiDirect object
            connectedDevice.Dispose();
        }

        private async void btnUnpair_Click(object sender, EventArgs e)
        {
            var discoveredDevice = (DiscoveredDevice)lvDiscoveredDevices.SelectedItem;

            DeviceUnpairingResult result = await discoveredDevice.DeviceInfo.Pairing.UnpairAsync();
            MainPage.Log($"Unpair result: {result.Status}", NotifyType.StatusMessage);

            updateForm();
        }

        
    }
}

