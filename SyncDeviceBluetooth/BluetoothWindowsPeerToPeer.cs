using Microsoft.Extensions.Logging;
using System.Threading;
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
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger, GroupName = GroupName };
                bluetoothWindowsClient.OnDeviceConnecting += BluetoothWindowsClient_OnDeviceConnecting;
                bluetoothWindowsClient.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
                bluetoothWindowsClient.OnError += BluetoothWindowsClient_OnError;
                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothWindowsClient_OnDeviceDisconnected;
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Starting client");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsClient_OnDeviceConnecting(object sender, ConnectingEventArgs e)
        {
            foreach(var c in Channels)
            {
                if (c.Value is BluetoothWindowsChannel  bluetoothWindowsChannel)
                {
                    if (bluetoothWindowsChannel.NetworkId == e.SyncDevice.NetworkId)
                    {
                        e.Cancel = true;
                        break;
                    }
                }

            }
        }

        CancellationTokenSource StartConnectToHostCancelationTokenSource;
        private async Task ReConnectToHostHosting()
        {
            StartConnectToHostCancelationTokenSource?.Cancel();
            await DisconnectFromHost("Restarting client after disconnected");

            StartConnectToHostCancelationTokenSource = new CancellationTokenSource();
            var token = StartConnectToHostCancelationTokenSource.Token;

            await Task.Delay(3000, token);
            if (!token.IsCancellationRequested)
                await ConnectToHost();
        }


        private void BluetoothWindowsClient_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            _ = ReConnectToHostHosting();
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

                    RaiseOnConnectionStarted(syncDevice);
                }
                else
                    syncDevice.StopAsync("Already connected");
            }
        }

        internal override void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            if (Channels.TryRemove(ConnectionId.Create(SessionName, device.SessionName).SessionName, out var bluetoothWindowsChannel))
            {
                RaiseOnStatus(Status);
            }
            else
                Logger.LogWarning($"Unable to remove channel from list? {device.SessionName}");
        }

        private Task DisconnectFromHost(string reason)
        {
            var _bluetoothWindowsClient = bluetoothWindowsClient;
            bluetoothWindowsClient = null;

            if (_bluetoothWindowsClient != null)
            {
                _bluetoothWindowsClient.OnConnectionStarted -= BluetoothPeerToPeer_OnConnectionStarted;
                _bluetoothWindowsClient.OnDeviceConnecting -= BluetoothWindowsClient_OnDeviceConnecting; 
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
                bluetoothWindowsServer = new BluetoothWindowsServer() { Logger = Logger, GroupName = GroupName };
                bluetoothWindowsServer.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
                bluetoothWindowsServer.OnDeviceConnecting += BluetoothWindowsClient_OnDeviceConnecting;
                bluetoothWindowsServer.OnError += BluetoothWindowsServer_OnError;
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"Starting server");
            }
            return Task.CompletedTask;
        }
        
        CancellationTokenSource StartHostingCancelationTokenSource;
        private async Task ReStartHosting()
        {
            StartHostingCancelationTokenSource?.Cancel();
            await StopHosting("Restarting server after disconnected");
            
            StartHostingCancelationTokenSource = new CancellationTokenSource();
            var token = StartHostingCancelationTokenSource.Token;

            await Task.Delay(3000, token);
            if (!token.IsCancellationRequested)
                await StartHosting();
        }

        private void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            _ = ReStartHosting();
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
                _bluetoothWindowsServer.OnDeviceConnecting -= BluetoothWindowsClient_OnDeviceConnecting;
                _bluetoothWindowsServer.OnError += BluetoothWindowsServer_OnError;
                _bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
                return _bluetoothWindowsServer?.StopAsync(reason);
            }
            return Task.CompletedTask;
        }

        #endregion  

        public override async Task StartAsync(string sessionName, string pin, string reason)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                Connections.Clear();
                SessionName = sessionName;
                Pin = pin;

                await StartHosting();
                await ConnectToHost();
                Status = SyncDeviceStatus.Started;
            }
        }

        public override async Task StopAsync(string reason)
        {
            Status = SyncDeviceStatus.Stopped;
            await StopHosting(reason);
            await DisconnectFromHost(reason);
            foreach(var c in Channels)
            {
                c.Value.Creator = null;
                await c.Value.StopAsync(reason);
            }
            ClearChannels();
        }

        public override async Task RestartAsync(string reason)
        {
            await StopAsync(reason);
            await StartAsync(SessionName, Pin, reason);
        }

        public override async Task SendMessageAsync(string message, string[] recipients = null)
        {
            foreach(var connection in Connections)
                await connection.SendMessageAsync(message, recipients);
        }

    }
}
