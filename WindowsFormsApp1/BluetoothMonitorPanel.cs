using SyncDevice;
using SyncDevice.Windows.Bluetooth;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public partial class BluetoothMonitorPanel : SyncPanel
    {
        protected override string StartText => "Start watching";

        protected override string StopText => "Stop watching";

        public BluetoothMonitorPanel()
        {
            InitializeComponent();

            var monitor = new BluetoothWindowsMonitor() { Logger = SDKTemplate.MainPage.mainPage };
            monitor.OnStatus += Server_OnStatus;
            monitor.OnMessageReceived += Server_OnMessageReceived;
           // monitor.OnMessageSent += Watcher_OnMessageSent;
            monitor.OnConnectionStarted += Server_OnConnectionStarted;
            monitor.OnDeviceConnected += Server_OnDeviceConnected;
            monitor.OnDeviceDisconnected += Server_OnDeviceDisconnected;

            SyncDevice = monitor;
        }

        private void Server_OnConnectionStarted(object sender, ISyncDevice device)
        {
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

        private void Server_OnMessageReceived(object sender, MessageEventArgs e)
        {
            RecordReciveMessage(e.Message);
        }

        private void Server_OnStatus(object sender, SyncDeviceStatus status)
        {
            Status = status;
            UpdateControls();
        }

        protected override void OnPingBackButtonClick()
        {
            SyncDevice.SendMessageAsync(System.DateTime.UtcNow.ToString());
        }
    }
}
