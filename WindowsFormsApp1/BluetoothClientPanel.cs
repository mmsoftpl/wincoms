using SyncDevice;
using SyncDevice.Windows.Bluetooth;

namespace WindowsFormsApp1
{
    public partial class BluetoothClientPanel : SyncPanel
    {
        readonly BluetoothWindowsClient client = null;
        public override ISyncDevice SyncDevice => client;

        public BluetoothClientPanel()
        {
            InitializeComponent();

            client = new BluetoothWindowsClient() { Logger = SDKTemplate.MainPage.mainPage };
            client.OnStatus += Server_OnStatus;
            client.OnMessage += Server_OnMessage;
            client.OnDeviceConnected += Server_OnDeviceConnected;
            client.OnConnectionStarted += Client_OnConnectionStarted;
            client.OnDeviceDisconnected += Server_OnDeviceDisconnected;
        }

        private void Client_OnConnectionStarted(object sender, string deviceId)
        {
            _ = KeepWriting();
        }

        private void Server_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            UpdateControls();
        }

        private void Server_OnDeviceConnected(object sender, ISyncDevice syncDevice)
        {
            UpdateControls();
            //_ = syncDevice.StartAsync(syncDevice.SessionName, "client auto starting device");
        }

        private void Server_OnMessage(object sender, MessageEventArgs e)
        {
            RecordReciveMessage(e.Message);
        }

        private void Server_OnStatus(object sender, SyncDeviceStatus status)
        {
            Status = status;
            UpdateControls();
        }

        public override void OnUpdateControls()
        {
            base.OnUpdateControls();

            if (rescanButton != null)
                rescanButton.Enabled = (client?.Status == SyncDeviceStatus.Started) ||
                    (client?.Status == SyncDeviceStatus.Created);


        }

        private void rescanButton_Click(object sender, System.EventArgs e)
        {
            client.StopAsync("Rescan");
            Reset();
            client.ConnectStrategy = ConnectStrategy.ScanDevices;
            client.RestartAsync("Rescan");
        }
    }
}
