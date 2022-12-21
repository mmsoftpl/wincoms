﻿using SDKTemplate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;

namespace WindowsFormsApp1
{
    public partial class Scenario1_Advertiser : Form
    {
        //private MainPage rootPage = null;// MainPage.Current;
        private ObservableCollection<ConnectedDevice> ConnectedDevices = new ObservableCollection<ConnectedDevice>();
        WiFiDirectAdvertisementPublisher _publisher;
        WiFiDirectConnectionListener _listener;
        List<WiFiDirectInformationElement> _informationElements = new List<WiFiDirectInformationElement>();
        ConcurrentDictionary<StreamSocketListener, WiFiDirectDevice> _pendingConnections = new ConcurrentDictionary<StreamSocketListener, WiFiDirectDevice>();

        public Scenario1_Advertiser()
        {
            this.InitializeComponent();

            cmbListenState.Items.Add(WiFiDirectAdvertisementListenStateDiscoverability.Intensive);
            cmbListenState.Items.Add(WiFiDirectAdvertisementListenStateDiscoverability.Normal);
            cmbListenState.SelectedIndex = 0;
        }

        //protected override void OnNavigatedFrom(NavigationEventArgs e)
        //{
        //    if (btnStopAdvertisement.IsEnabled)
        //    {
        //        StopAdvertisement();
        //    }
        //}

        private void btnStartAdvertisement_Click(object sender, EventArgs e)
        {
            _publisher = new WiFiDirectAdvertisementPublisher();
            _publisher.StatusChanged += OnStatusChanged;

            _listener = new WiFiDirectConnectionListener();

            if (chkListener.Checked)
            {
                try
                {
                    // This can raise an exception if the machine does not support WiFi. Sorry.
                    _listener.ConnectionRequested += OnConnectionRequested;
                }
                catch (Exception ex)
                {
                    MainPage.Log($"Error preparing Advertisement: {ex}", NotifyType.ErrorMessage);
                    return;
                }
            }

            var discoverability = WiFiDirectAdvertisementListenStateDiscoverability.Normal;// Utils.GetSelectedItemTag<WiFiDirectAdvertisementListenStateDiscoverability>(cmbListenState);
            _publisher.Advertisement.ListenStateDiscoverability = discoverability;

            _publisher.Advertisement.IsAutonomousGroupOwnerEnabled = chkPreferGroupOwnerMode.Checked;

            // Legacy settings are meaningful only if IsAutonomousGroupOwnerEnabled is true.
            if (_publisher.Advertisement.IsAutonomousGroupOwnerEnabled && chkLegacySetting.Checked)
            {
                _publisher.Advertisement.LegacySettings.IsEnabled = true;
                if (!String.IsNullOrEmpty(txtPassphrase.Text))
                {
                    var creds = new Windows.Security.Credentials.PasswordCredential();
                    creds.Password = txtPassphrase.Text;
                    _publisher.Advertisement.LegacySettings.Passphrase = creds;
                }

                if (!String.IsNullOrEmpty(txtSsid.Text))
                {
                    _publisher.Advertisement.LegacySettings.Ssid = txtSsid.Text;
                }
            }

            // Add the information elements.
            foreach (WiFiDirectInformationElement informationElement in _informationElements)
            {
                _publisher.Advertisement.InformationElements.Add(informationElement);
            }

            _publisher.Start();

            if (_publisher.Status == WiFiDirectAdvertisementPublisherStatus.Started)
            {
                btnStartAdvertisement.Enabled = false;
                btnStopAdvertisement.Enabled = true;
                MainPage.Log("Advertisement started.", NotifyType.StatusMessage);
            }
            else
            {
                MainPage.Log($"Advertisement failed to start. Status is {_publisher.Status}", NotifyType.ErrorMessage);
            }
        }

        private void btnAddIe_Click(object sender, EventArgs e)
        {
            WiFiDirectInformationElement informationElement = new WiFiDirectInformationElement();

            // Information element blob
            DataWriter dataWriter = new DataWriter();
            dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            dataWriter.ByteOrder = ByteOrder.LittleEndian;
            dataWriter.WriteUInt32(dataWriter.MeasureString(txtInformationElement.Text));
            dataWriter.WriteString(txtInformationElement.Text);
            informationElement.Value = dataWriter.DetachBuffer();

            // Organizational unit identifier (OUI)
            informationElement.Oui = CryptographicBuffer.CreateFromByteArray(Globals.CustomOui);

            // OUI Type
            informationElement.OuiType = Globals.CustomOuiType;

            // Save this information element so we can add it when we advertise.
            _informationElements.Add(informationElement);

            txtInformationElement.Text = "";
            MainPage.Log("IE added successfully", NotifyType.StatusMessage);
        }

        private void btnStopAdvertisement_Click(object sender, EventArgs e)
        {
            StopAdvertisement();
            MainPage.Log("Advertisement stopped successfully", NotifyType.StatusMessage);
        }

        private void StopAdvertisement()
        {
            _publisher.Stop();
            _publisher.StatusChanged -= OnStatusChanged;

            _listener.ConnectionRequested -= OnConnectionRequested;

            connectionSettingsPanel.Reset();
            _informationElements.Clear();

            btnStartAdvertisement.Enabled = true;
            btnStopAdvertisement.Enabled = false;
        }

        private async Task<bool> HandleConnectionRequestAsync(WiFiDirectConnectionRequest connectionRequest)
        {
            string deviceName = connectionRequest.DeviceInformation.Name;

            bool isPaired = (connectionRequest.DeviceInformation.Pairing?.IsPaired == true) ||
                            (await IsAepPairedAsync(connectionRequest.DeviceInformation.Id));

            // Show the prompt only in case of WiFiDirect reconnection or Legacy client connection.
            if (isPaired || _publisher.Advertisement.LegacySettings.IsEnabled)
            {
                var messageDialog = new MessageDialog($"Connection request received from {deviceName}", "Connection Request");

                // Add two commands, distinguished by their tag.
                // The default command is "Decline", and if the user cancels, we treat it as "Decline".
                messageDialog.Commands.Add(new UICommand("Accept", null, true));
                messageDialog.Commands.Add(new UICommand("Decline", null, null));
                messageDialog.DefaultCommandIndex = 1;
                messageDialog.CancelCommandIndex = 1;

                // Show the message dialog
                var commandChosen = await messageDialog.ShowAsync();

                if (commandChosen.Id == null)
                {
                    return false;
                }
            }

            MainPage.Log($"Connecting to {deviceName}...", NotifyType.StatusMessage);

            // Pair device if not already paired and not using legacy settings
            if (!isPaired && !_publisher.Advertisement.LegacySettings.IsEnabled)
            {
                if (!await ConnectionSettingsPanel.RequestPairDeviceAsync(connectionRequest.DeviceInformation.Pairing))
                {
                    return false;
                }
            }

            WiFiDirectDevice wfdDevice = null;
            try
            {
                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                wfdDevice = await WiFiDirectDevice.FromIdAsync(connectionRequest.DeviceInformation.Id);
            }
            catch (Exception ex)
            {
                MainPage.Log($"Exception in FromIdAsync: {ex}", NotifyType.ErrorMessage);
                return false;
            }

            // Register for the ConnectionStatusChanged event handler
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            var listenerSocket = new StreamSocketListener();

            // Save this (listenerSocket, wfdDevice) pair so we can hook it up when the socket connection is made.
            _pendingConnections[listenerSocket] = wfdDevice;

            var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();

            listenerSocket.ConnectionReceived += OnSocketConnectionReceived;
            try
            {
                await listenerSocket.BindEndpointAsync(EndpointPairs[0].LocalHostName, Globals.strServerPort);
            }
            catch (Exception ex)
            {
                MainPage.Log($"Connect operation threw an exception: {ex.Message}", NotifyType.ErrorMessage);
                return false;
            }

            MainPage.Log($"Devices connected on L2, listening on IP Address: {EndpointPairs[0].LocalHostName}" +
                                $" Port: {Globals.strServerPort}", NotifyType.StatusMessage);
            return true;
        }

        private async void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs connectionEventArgs)
        {
            WiFiDirectConnectionRequest connectionRequest = connectionEventArgs.GetConnectionRequest();
         //   bool success = await Dispatcher.RunTaskAsync(async () =>
          //  {
                var success = await HandleConnectionRequestAsync(connectionRequest);
           // }
            //);

            if (!success)
            {
                // Decline the connection request
                MainPage.Log($"Connection request from {connectionRequest.DeviceInformation.Name} was declined", NotifyType.ErrorMessage);
                connectionRequest.Dispose();
            }
        }

        private async Task<bool> IsAepPairedAsync(string deviceId)
        {
            List<string> additionalProperties = new List<string>();
            additionalProperties.Add("System.Devices.Aep.DeviceAddress");
            String deviceSelector = $"System.Devices.Aep.AepId:=\"{deviceId}\"";
            DeviceInformation devInfo = null;

            try
            {
                devInfo = await DeviceInformation.CreateFromIdAsync(deviceId, additionalProperties);
            }
            catch (Exception ex)
            {
                MainPage.Log("DeviceInformation.CreateFromIdAsync threw an exception: " + ex.Message, NotifyType.ErrorMessage);
            }

            if (devInfo == null)
            {
                MainPage.Log("Device Information is null", NotifyType.ErrorMessage);
                return false;
            }

            deviceSelector = $"System.Devices.Aep.DeviceAddress:=\"{devInfo.Properties["System.Devices.Aep.DeviceAddress"]}\"";
            DeviceInformationCollection pairedDeviceCollection = await DeviceInformation.FindAllAsync(deviceSelector, null, DeviceInformationKind.Device);
            return pairedDeviceCollection.Count > 0;
        }

        private async void OnStatusChanged(WiFiDirectAdvertisementPublisher sender, WiFiDirectAdvertisementPublisherStatusChangedEventArgs statusEventArgs)
        {
            if (statusEventArgs.Status == WiFiDirectAdvertisementPublisherStatus.Started)
            {
               // await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
               await Task.Run(()=>
                {
                    if (sender.Advertisement.LegacySettings.IsEnabled)
                    {
                        // Show the autogenerated passphrase and SSID.
                        if (String.IsNullOrEmpty(txtPassphrase.Text))
                        {
                            txtPassphrase.Text = _publisher.Advertisement.LegacySettings.Passphrase.Password;
                        }

                        if (String.IsNullOrEmpty(txtSsid.Text))
                        {
                            txtSsid.Text = _publisher.Advertisement.LegacySettings.Ssid;
                        }
                    }
                }
                );
            }

            MainPage.Log($"Advertisement: Status: {statusEventArgs.Status}, Error: {statusEventArgs.Error}", NotifyType.StatusMessage);
            return;
        }

        private void OnSocketConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            _ = Task.Run(() => SocketConnectionReceivedTask(sender, args));

        }


        public async Task SocketConnectionReceivedTask(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        //var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            MainPage.Log("Connecting to remote side on L4 layer...", NotifyType.StatusMessage);
            StreamSocket serverSocket = args.Socket;

            // Look up the WiFiDirectDevice associated with this StreamSocketListener.
            WiFiDirectDevice wfdDevice;
            if (!_pendingConnections.TryRemove(sender, out wfdDevice))
            {
                MainPage.Log("Unexpected connection ignored.", NotifyType.ErrorMessage);
                serverSocket.Dispose();
                return;
            }

            SocketReaderWriter socketRW = new SocketReaderWriter(serverSocket, null);

            // The first message sent is the name of the connection.
            string message = await socketRW.ReadMessageAsync();

            // Add this connection to the list of active connections.
            ConnectedDevices.Add(new ConnectedDevice(message ?? "(unnamed)", wfdDevice, socketRW));

            while (message != null)
            {
                message = await socketRW.ReadMessageAsync();
            }
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            MainPage.Log($"Connection status changed: {sender.ConnectionStatus}", NotifyType.StatusMessage);

            if (sender.ConnectionStatus == WiFiDirectConnectionStatus.Disconnected)
            {
                // TODO: Should we remove this connection from the list?
                // (Yes, probably.)
            }
        }

        private async void btnSendMessage_Click(object sender, EventArgs e)
        {
            var connectedDevice = (ConnectedDevice)lvConnectedDevices.SelectedItem;
            await connectedDevice.SocketRW.WriteMessageAsync(txtSendMessage.Text);
        }

        private void btnCloseDevice_Click(object sender, EventArgs e)
        {
            var connectedDevice = (ConnectedDevice)lvConnectedDevices.SelectedItem;
            ConnectedDevices.Remove(connectedDevice);

            // Close socket and WiFiDirect object
            connectedDevice.Dispose();
        }
    }

}
