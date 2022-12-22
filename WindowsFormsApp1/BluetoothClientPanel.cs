using SDKTemplate;
using System;
using System.Collections.Concurrent;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace WindowsFormsApp1
{
    public partial class BluetoothClientPanel : BluetoothPanel
    {
        public BluetoothClientPanel()
        {
            InitializeComponent();
        }
        
        private DeviceWatcher deviceWatcher = null;
        private BluetoothDevice bluetoothDevice = null;
        private RfcommDeviceService chatService = null;
        private StreamSocket chatSocket = null;

        public ConcurrentDictionary<string, DeviceInformation> ResultCollection
        {
            get;
            private set;
        } = new ConcurrentDictionary<string, DeviceInformation>();

        public override void FindDevices()
        {
            MainPage.Log($"Enumeration started. Scanning for devices...", NotifyType.StatusMessage);

            Status = Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisherStatus.Created;
            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>( (watcher, deviceInfo) =>
            {
                if (deviceInfo.Name != "")
                {
                    ResultCollection.TryAdd(deviceInfo.Id, deviceInfo);
                    
                    MainPage.Log($"{deviceInfo.Id}, {deviceInfo.Name} (device info added)", NotifyType.StatusMessage);
                }
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>( (watcher, deviceInfoUpdate) =>
            {
                if (ResultCollection.TryGetValue(deviceInfoUpdate.Id, out var rfcommInfoDisp))
                {
                    rfcommInfoDisp.Update(deviceInfoUpdate);

                    MainPage.Log($"{rfcommInfoDisp.Id}, {rfcommInfoDisp.Name} (device info updated)", NotifyType.StatusMessage);
                }
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (ResultCollection.TryRemove(deviceInfoUpdate.Id, out var rfcommInfoDisp))
                {
                    MainPage.Log($"{rfcommInfoDisp.Id}, {rfcommInfoDisp.Name} (device info removed)", NotifyType.StatusMessage);
                }
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>( (watcher, obj) =>
            {
                MainPage.Log($"{ResultCollection.Count} devices found. Enumeration completed. Scanning for service...", NotifyType.StatusMessage);

                DataReader chatReader = null;

                foreach(var deviceInfo in ResultCollection.Values)
                {
                    chatReader = Task.Run(() => Connect(deviceInfo)).Result;
                    
                    if (chatReader != null)
                    {
                        MainPage.Log($"Connecting to {deviceInfo.Name}...", NotifyType.StatusMessage);

                        ReceiveStringLoop(chatReader);
                        Status = WiFiDirectAdvertisementPublisherStatus.Started;

                        _ = KeepWriting();
                        break;
                    }
                }

                if (chatReader == null)
                {
                    MainPage.Log($"Could not discover the {SdpServiceName}", NotifyType.StatusMessage);
                    // ResetMainUI();
                    Status = WiFiDirectAdvertisementPublisherStatus.Stopped;
                }
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>( (watcher, obj) =>
            {
                ResultCollection.Clear();
            });

            deviceWatcher.Start();
        }

        public async Task<DataReader> Connect(DeviceInformation deviceInfoDisp)
        {
            // Make sure user has selected a device first
            if (deviceInfoDisp != null)
            {
                MainPage.Log($"Testing remote device {deviceInfoDisp.Name} for presence of {SdpServiceName}...", NotifyType.StatusMessage);
            }
            else
            {
                MainPage.Log("Please select an item to connect to", NotifyType.ErrorMessage);
                return null;
            }

           // RfcommChatDeviceDisplay deviceInfoDisp = resultsListView.SelectedItem as RfcommChatDeviceDisplay;

            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(deviceInfoDisp.Id).CurrentStatus;
            if (accessStatus == DeviceAccessStatus.DeniedByUser)
            {
                MainPage.Log("This app does not have access to connect to the remote device (please grant access in Settings > Privacy > Other Devices", NotifyType.ErrorMessage);
                return null;
            }
            // If not, try to get the Bluetooth device
            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfoDisp.Id);
            }
            catch (Exception ex)
            {
                MainPage.Log(ex.Message, NotifyType.ErrorMessage);
                //   ResetMainUI();
                return null;
            }
            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.
            if (bluetoothDevice == null)
            {
                MainPage.Log("Bluetooth Device returned null. Access Status = " + accessStatus.ToString(), NotifyType.ErrorMessage);
            }

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(RfcommChatServiceUuid), BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count > 0)
            {
                chatService = rfcommServices.Services[0];
            }
            else
            {
                return null;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await chatService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(SdpServiceNameAttributeId))
            {
                MainPage.Log(
                    "The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                //  ResetMainUI();
                return null;
            }
            var attributeReader = DataReader.FromBuffer(attributes[SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != SdpServiceNameAttributeType)
            {
                MainPage.Log(
                    "The Chat service is using an unexpected format for the Service Name attribute. " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                // ResetMainUI();
                return null;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

            StopWatcher();

            lock (this)
            {
                chatSocket = new StreamSocket();
            }
            try
            {
                await chatSocket.ConnectAsync(chatService.ConnectionHostName, chatService.ConnectionServiceName);

               // SetChatUI(attributeReader.ReadString(serviceNameLength), bluetoothDevice.Name);
                var writer = new DataWriter(chatSocket.OutputStream);
                if (Writers.TryAdd(deviceInfoDisp.Id, writer))
                {
                    DataReader chatReader = new DataReader(chatSocket.InputStream);
                    return chatReader;
                }
                else
                    MainPage.Log("Can't add writer to dictionary?", NotifyType.ErrorMessage);

                return null;
               
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
                MainPage.Log("Please verify that you are running the BluetoothRfcommChat server.", NotifyType.ErrorMessage);
             //   ResetMainUI();
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                MainPage.Log("Please verify that there is no other RFCOMM connection to the same device.", NotifyType.ErrorMessage);
              //  ResetMainUI();
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

                RecordReciveMessage(chatReader.ReadString(stringLength));

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
                            MainPage.Log("Disconnect triggered by remote device", NotifyType.StatusMessage);
                        else if ((uint)ex.HResult == 0x800703E3)
                            MainPage.Log("The I/O operation has been aborted because of either a thread exit or an application request.", NotifyType.StatusMessage);
                    }
                    else
                    {
                        Disconnect("Read stream failed with error: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up the socket and DataWriter and reset the UI
        /// </summary>
        /// <param name="disconnectReason"></param>
        public override void Disconnect(string disconnectReason)
        {
            ClearWriters();

            if (chatService != null)
            {
                chatService.Dispose();
                chatService = null;
            }
            lock (this)
            {
                if (chatSocket != null)
                {
                    chatSocket.Dispose();
                    chatSocket = null;
                }
            }

            MainPage.Log(disconnectReason, NotifyType.StatusMessage);
            //ResetMainUI();
        
            Status = WiFiDirectAdvertisementPublisherStatus.Stopped;
        }
    }
}
