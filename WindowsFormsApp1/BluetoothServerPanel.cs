using SyncDevice;
using SyncDevice.Windows.Bluetooth;

namespace WindowsFormsApp1
{
    public partial class BluetoothServerPanel : ComsPanel
    {
        readonly BluetoothWindowsServer server = null;

        public override ISyncDevice SyncDevice => server;

        public BluetoothServerPanel()
        {
            InitializeComponent();

            server = new BluetoothWindowsServer() { Logger = SDKTemplate.MainPage.mainPage };
            server.OnStatus += Server_OnStatus;
            server.OnMessage += Server_OnMessage;
            server.OnDeviceConnected += Server_OnDeviceConnected;
            server.OnDeviceDisconnected += Server_OnDeviceDisconnected;
        }

        private void Server_OnDeviceDisconnected(object sender, string deviceId)
        {
        }

        private void Server_OnDeviceConnected(object sender, string deviceId)
        {
            KeepWriting();
        }

        private void Server_OnMessage(object sender, MessageEventArgs e)
        {
            RecordReciveMessage(e.Message);
        }

        private void Server_OnStatus(object sender, SyncDeviceStatus status)
        {
            Status = status;
        }
    }
}
