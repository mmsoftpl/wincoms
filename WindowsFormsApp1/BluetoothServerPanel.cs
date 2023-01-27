using SyncDevice;
using SyncDevice.Windows.Bluetooth;

namespace WindowsFormsApp1
{
    public partial class BluetoothServerPanel : SyncPanel
    {
        protected override string StartText => "Start hosting";

        protected override string StopText => "Stop hosting";

        public BluetoothServerPanel()
        {
            InitializeComponent();

            var server = new BluetoothWindowsServer() { Logger = SDKTemplate.MainPage.mainPage };
            server.OnStatus += Server_OnStatus;
            server.OnMessageReceived += Server_OnMessage;
            server.OnConnectionStarted += Server_OnConnectionStarted;
            server.OnDeviceConnected += Server_OnDeviceConnected;
            server.OnDeviceDisconnected += Server_OnDeviceDisconnected;

            SyncDevice = server;
        }

        private void Server_OnConnectionStarted(object sender, ISyncDevice device)
        {
            _ = KeepWriting();
        }

        private void Server_OnDeviceDisconnected(object sender, ISyncDevice device)
        {
            UpdateControls();
        }

        private void Server_OnDeviceConnected(object sender, ISyncDevice device)
        {
            UpdateControls();
            //_ = device.StartAsync(device.SessionName, "server auto starting device");
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
    }
}
