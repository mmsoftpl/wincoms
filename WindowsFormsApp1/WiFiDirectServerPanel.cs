using SyncDevice;
using SyncDevice.Windows.Bluetooth;
using SyncDevice.Windows.WifiDirect;

namespace WindowsFormsApp1
{
    public partial class WiFiDirectServerPanel : SyncPanel
    {
        readonly WifiDirectWindowsServer server = null;

        public override ISyncDevice SyncDevice => server;

        public WiFiDirectServerPanel()
        {
            InitializeComponent();

            server = new WifiDirectWindowsServer() { Logger = SDKTemplate.MainPage.mainPage };
            server.OnStatus += Server_OnStatus;
            server.OnMessage += Server_OnMessage;
            server.OnDeviceConnected += Server_OnDeviceConnected;
            server.OnDeviceDisconnected += Server_OnDeviceDisconnected;
        }

        private void Server_OnDeviceDisconnected(object sender, ISyncDevice device)
        {
        }

        private void Server_OnDeviceConnected(object sender, ISyncDevice device)
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
