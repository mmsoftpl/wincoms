using SyncDevice;
using SyncDevice.Windows.Bluetooth;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public partial class BluetoothPeerToPeerPanel : SyncPanel
    {
        protected override string StartText => "Start";

        protected override string StopText => "Stop";

        public BluetoothPeerToPeerPanel(bool alwaysConnected)
        {
            InitializeComponent();

            ISyncDevice peerToPeer = null;
            if (alwaysConnected)
                peerToPeer = new BluetoothWindowsPeerToPeer() { Logger = SDKTemplate.MainPage.mainPage };
            else
                peerToPeer = new BluetoothWindowsPeerToPeer2() { Logger = SDKTemplate.MainPage.mainPage };

            peerToPeer.OnStatus += Server_OnStatus;
            peerToPeer.OnMessageReceived += Server_OnMessage;
            peerToPeer.OnConnectionStarted += Server_OnConnectionStarted;
            peerToPeer.OnDeviceConnected += Server_OnDeviceConnected;
            peerToPeer.OnDeviceDisconnected += Server_OnDeviceDisconnected;

            SyncDevice = peerToPeer;
        }



        private void Server_OnConnectionStarted(object sender, ISyncDevice device)
        {
            _ = KeepWriting(); 
            UpdateControls();
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
