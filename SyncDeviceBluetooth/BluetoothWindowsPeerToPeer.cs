using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsPeerToPeer : BluetoothWindows
    {
        private BluetoothWindowsClient bluetoothWindowsClient;
        private BluetoothWindowsServer bluetoothWindowsServer;

        public override bool IsHost => bluetoothWindowsServer?.IsHost ?? bluetoothWindowsClient?.IsHost ?? false;
      
        #region Bluetooth Client
        private Task ConnectToHost()
        {
            if (bluetoothWindowsClient == null)
            {
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsClient.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
                bluetoothWindowsClient.OnError += BluetoothWindowsClient_OnError;
                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothWindowsClient_OnDeviceDisconnected;
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Other server found. Starting in client mode.");
            }
            return Task.CompletedTask;
        }

        private async void BluetoothWindowsClient_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            await DisconnectFromHost("Restarting after client disconnected");
            await ConnectToHost();         
        }

        private void BluetoothWindowsClient_OnError(object sender, string error)
        {
            RaiseOnError(error);
            _ = StopAsync("Error while connecting");
        }

        private void BluetoothPeerToPeer_OnConnectionStarted(object sender, ISyncDevice syncDevice)
        {
            if (syncDevice != bluetoothWindowsClient && syncDevice != bluetoothWindowsServer)
            {
                BluetoothWindowsChannel bluetoothWindowsChannel = syncDevice as BluetoothWindowsChannel;
                
                if (Channels.TryAdd(ConnectionId.Create(SessionName, syncDevice.SessionName).SessionName, bluetoothWindowsChannel))
                {
                    bluetoothWindowsChannel.Creator.UnRegisterChannel(bluetoothWindowsChannel);
                    bluetoothWindowsChannel.Creator = this;                   

                    syncDevice.OnMessageReceived += BluetoothPeerToPeer_OnMessageReceived;
                    syncDevice.OnDeviceDisconnected += BluetoothPeerToPeer_OnDeviceDisconnected;
                    RaiseOnConnectionStarted(syncDevice);
                }
                else
                    syncDevice.StopAsync("Already connected");
            }
        }

        private void BluetoothPeerToPeer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            string k = null;
            foreach(var c in Channels)
                if (c.Value== syncDevice)
                {
                    k = c.Key;
                }
            if (k != null)
            {
                if (Channels.TryRemove(k, out var bluetoothWindowsChannel))
                {
                    bluetoothWindowsChannel.OnMessageReceived -= BluetoothPeerToPeer_OnMessageReceived;
                    bluetoothWindowsChannel.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                    RaiseOnDeviceDisconnected(bluetoothWindowsChannel);
                }
            }
            else
                Logger.LogWarning($"Unable to remove channel from list? {syncDevice.SessionName}");
        }

        private void BluetoothPeerToPeer_OnMessageReceived(object sender, MessageEventArgs e)
        {
            RaiseOnMessageReceived(e.Message, sender as ISyncDevice);
        }

        private Task DisconnectFromHost(string reason)
        {
            var _bluetoothWindowsClient = bluetoothWindowsClient;
            bluetoothWindowsClient = null;

            if (_bluetoothWindowsClient != null)
            {
                _bluetoothWindowsClient.OnConnectionStarted -= BluetoothPeerToPeer_OnConnectionStarted;
                _bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothWindowsClient_OnDeviceDisconnected;
                _bluetoothWindowsClient.OnError -= BluetoothWindowsClient_OnError;
                return _bluetoothWindowsClient?.StopAsync(reason);
            }
            return Task.CompletedTask;
        }
        #endregion       

        #region Bluetooth Server

        private Task StartHosting()
        {
            if (bluetoothWindowsServer == null)
            {
                bluetoothWindowsServer = new BluetoothWindowsServer() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsServer.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
                bluetoothWindowsServer.OnError += BluetoothWindowsServer_OnError;
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"No server found. Starting in server mode.");
            }
            return Task.CompletedTask;
        }

        private async void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            await StopHosting("Restarting after server disconnected");
            await StartHosting();
        }

        private void BluetoothWindowsServer_OnError(object sender, string error)
        {
            RaiseOnError(error);
            _ = StopAsync("Error while hosting");
        }

        private Task StopHosting(string reason)
        {
            var _bluetoothWindowsServer = bluetoothWindowsServer;
            bluetoothWindowsServer = null;
            if (_bluetoothWindowsServer != null)
            {
                _bluetoothWindowsServer.OnConnectionStarted -= BluetoothPeerToPeer_OnConnectionStarted;
                _bluetoothWindowsServer.OnError += BluetoothWindowsServer_OnError;
                _bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
                return _bluetoothWindowsServer?.StopAsync(reason);
            }
            return Task.CompletedTask;
        }

        #endregion  

        public async Task startAsync(string sessionName, string pin, int delayMs = 2000)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                Connections.Clear();
                SessionName = sessionName;
                Pin = pin;

                await StartHosting();
                await ConnectToHost();
            }
        }

        public override Task StartAsync(string sessionName, string pin, string reason)
        {
            return startAsync(sessionName, pin);
        }

        public override async Task StopAsync(string reason)
        {

            ClearChannels();
            Status = SyncDeviceStatus.Stopped;
            await StopHosting(reason);
            await DisconnectFromHost(reason);
        }

        public override async Task RestartAsync(string reason)
        {
            bool wasHosting = IsHost;

            await StopAsync(reason);
            if (wasHosting)
                await startAsync(SessionName, Pin, 1000);
            else
                await startAsync(SessionName, Pin, 3000);
        }

        public override async Task SendMessageAsync(string message, string[] recipients = null)
        {
            foreach(var connection in Connections)
                await connection.SendMessageAsync(message, recipients);
        }

    }
}
