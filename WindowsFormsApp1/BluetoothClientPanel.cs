using SyncDevice;
using SyncDevice.Windows.Bluetooth;

namespace WindowsFormsApp1
{
    public partial class BluetoothClientPanel : SyncPanel
    {
        public BluetoothClientPanel()
        {
            InitializeComponent();

            var client = new BluetoothWindowsClient() { Logger = SDKTemplate.MainPage.mainPage };
            client.OnStatus += Client_OnStatus;
            client.OnMessage += Client_OnMessage;
            client.OnDeviceConnected += Client_OnDeviceConnected;
            client.OnConnectionStarted += Client_OnConnectionStarted;
            client.OnDeviceDisconnected += Client_OnDeviceDisconnected;

            SyncDevice = client;
        }

        private void Client_OnConnectionStarted(object sender, string deviceId)
        {
            _ = KeepWriting();
        }

        private void Client_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            UpdateControls();
        }

        private void Client_OnDeviceConnected(object sender, ISyncDevice syncDevice)
        {
            UpdateControls();
            //_ = syncDevice.StartAsync(syncDevice.SessionName, "client auto starting device");
        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {
            RecordReciveMessage(e.Message);
        }

        private void Client_OnStatus(object sender, SyncDeviceStatus status)
        {
            Status = status;
            UpdateControls();
        }

        public override void OnUpdateControls()
        {
            base.OnUpdateControls();

            if (rescanButton != null)
                rescanButton.Enabled = (SyncDevice?.Status == SyncDeviceStatus.Started) ||
                    (SyncDevice?.Status == SyncDeviceStatus.Created);


        }

        private void rescanButton_Click(object sender, System.EventArgs e)
        {
            SyncDevice.StopAsync("Rescan");
            Reset();
            (SyncDevice as BluetoothWindowsClient).ConnectStrategy = ConnectStrategy.ScanDevices;
            SyncDevice.RestartAsync("Rescan");
        }
    }
}
