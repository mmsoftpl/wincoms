using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsPeerToPeer : BluetoothWindows
    {
        private BluetoothLeWatcher bluetoothLeWatcher;
        private BluetoothLePublisher bluetoothLePublisher;

        private BluetoothWindowsClient bluetoothWindowsClient;
        private BluetoothWindowsServer bluetoothWindowsServer;

        public override bool IsHost => bluetoothWindowsServer?.IsHost ?? bluetoothWindowsClient?.IsHost ?? false;

        public readonly ConcurrentDictionary<ConnectionId, ISyncDevice> PeerToPeerConnections = new ConcurrentDictionary<ConnectionId, ISyncDevice>();

        #region Bluetooth LE Watcher
        private Task ScanForServerSignatures()
        {
            if (bluetoothLeWatcher == null)
            {
                bluetoothLeWatcher = new BluetoothLeWatcher() { Logger = Logger };
                return bluetoothLeWatcher.StartAsync(SessionName, null, $"Start scanning for '{ServiceName}' server signatures");
            }
            return Task.CompletedTask;
        }
        private Task StopScanningForServerSignatures(string reason)
        {
            try
            {
                if (bluetoothLeWatcher != null)
                {
                    return bluetoothLeWatcher?.StopAsync(reason);
                }
                return Task.CompletedTask;
            }
            finally
            {
                bluetoothLeWatcher = null;
            }
        }

        #endregion

        public override IList<ISyncDevice> Connections => PeerToPeerConnections.Values.ToList();

        #region Bluetooth Client
        private Task ConnectToHost()
        {
            if (bluetoothWindowsClient == null)
            {
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsClient.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Other server found. Starting in client mode.");
            }
            return Task.CompletedTask;
        }

        private void BluetoothPeerToPeer_OnConnectionStarted(object sender, ISyncDevice syncDevice)
        {
            if (syncDevice != bluetoothWindowsClient && syncDevice != bluetoothWindowsServer)
            {
                if (PeerToPeerConnections.TryAdd(ConnectionId.Create(syncDevice), syncDevice))
                {
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
            if (syncDevice == bluetoothWindowsClient)
            {
                if (bluetoothWindowsClient?.Connections?.Count == 0 && bluetoothWindowsServer?.Connections?.Count == 0)
                {
                    _ = RestartAsync("Restarting client & server");
                }
                else
                    bluetoothWindowsClient?.RestartAsync("Restarting client");
            }
            else
            {
                if (PeerToPeerConnections.TryRemove(ConnectionId.Create(syncDevice), out var sd))
                {
                    sd.OnMessageReceived -= BluetoothPeerToPeer_OnMessageReceived;
                    sd.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                    RaiseOnDeviceDisconnected(sd);
                }
            }
        }

        private void BluetoothPeerToPeer_OnMessageReceived(object sender, MessageEventArgs e)
        {
            RaiseOnMessageReceived(e.Message);
        }

        private Task DisconnectFromHost(string reason)
        {
            var _bluetoothWindowsClient = bluetoothWindowsClient;
            bluetoothWindowsClient = null;

            if (_bluetoothWindowsClient != null)
            {
                _bluetoothWindowsClient.OnConnectionStarted -= BluetoothPeerToPeer_OnConnectionStarted;
             //   _bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                // bluetoothWindowsClient.OnStatus -= BluetoothPeerToPeer_OnStatus;
                return _bluetoothWindowsClient?.StopAsync(reason);
            }
            return Task.CompletedTask;
        }
        #endregion

        #region Bluetooth LE Publisher

        private Task StartPublishingSignatureAsync()
        {
            var clientSignature = SessionName;

            if (bluetoothLePublisher == null)
            {
                bluetoothLePublisher = new BluetoothLePublisher() { Logger = Logger, ServiceName = ServiceName, SessionName = clientSignature };
                return bluetoothLePublisher.StartAsync(clientSignature, null, $"Start publishing server signature");
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private Task StopPublishingSignatureAsync(string reason)
        {
            try
            {
                if (bluetoothLePublisher != null)
                {
                    return bluetoothLePublisher?.StopAsync(reason);
                }
                return Task.CompletedTask;
            }
            finally
            {
                bluetoothLePublisher = null;
            }
        }

        #endregion

        #region Bluetooth Server

        private Task StartHosting()
        {
            if (bluetoothWindowsServer == null)
            {
                bluetoothWindowsServer = new BluetoothWindowsServer() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsServer.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
              //  bluetoothWindowsServer.OnDeviceDisconnected += BluetoothPeerToPeer_OnDeviceDisconnected;
              //  bluetoothWindowsServer.OnStatus += BluetoothPeerToPeer_OnStatus;
                //bluetoothWindowsServer.OnConnectionStarted += BluetoothWindowsServer_OnConnectionStarted;
                //bluetoothWindowsServer.OnMessageSent += BluetoothWindowsServer_OnMessageSent;
                //bluetoothWindowsClient.OnStatus += BluetoothWindowsClient_OnStatus;
                //bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"No server found. Starting in server mode.");
            }
            return Task.CompletedTask;
        }

        //private void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        //{
        //    if (sender == bluetoothWindowsServer)
        //    {
        //        bluetoothWindowsServer.OnConnectionStarted -= BluetoothWindowsServer_OnConnectionStarted;
        //        bluetoothWindowsServer.OnMessageSent -= BluetoothWindowsServer_OnMessageSent;
        //        bluetoothWindowsServer.OnStatus -= BluetoothWindowsClient_OnStatus;
        //        bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
        //        bluetoothWindowsServer = null;
        //    }
        //}

        //private CancellationTokenSource CancellationTokenSource_cts;
        //private async Task KillConnectionAfter5sec(ISyncDevice syncDevice, CancellationToken cancellationToken)
        //{
        //    await Task.Delay(5000, cancellationToken);
        //    if (!cancellationToken.IsCancellationRequested)
        //    {
        //        await syncDevice?.StopAsync("Power save after 5 sec;)");
        //    }
        //}

        //private Task KillConnectionAfter5sec(ISyncDevice syncDevice)
        //{
        //    CancellationTokenSource_cts?.Cancel();

        //    CancellationTokenSource_cts = new CancellationTokenSource();

        //    return KillConnectionAfter5sec(syncDevice, CancellationTokenSource_cts.Token);
        //}


        //private void BluetoothWindowsServer_OnMessageSent(object sender, MessageEventArgs e)
        //{
        //    RaiseOnMessageSent(e.Message);
        //    _ = KillConnectionAfter5sec(e.SyncDevice);
        //}

        //private void BluetoothWindowsServer_OnConnectionStarted(object sender, ISyncDevice syncDevice)
        //{
        //    RaiseOnConnectionStarted(syncDevice);
        //}

        private Task StopHosting(string reason)
        {
            var _bluetoothWindowsServer = bluetoothWindowsServer;
            bluetoothWindowsServer = null;
            if (_bluetoothWindowsServer != null)
            {
                _bluetoothWindowsServer.OnConnectionStarted -= BluetoothPeerToPeer_OnConnectionStarted;
              //  _bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;

                //  bluetoothWindowsServer.OnStatus -= BluetoothPeerToPeer_OnStatus;

                //bluetoothWindowsServer.OnConnectionStarted -= BluetoothWindowsServer_OnConnectionStarted;
                //bluetoothWindowsServer.OnMessageSent -= BluetoothWindowsServer_OnMessageSent;

                //bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
                return _bluetoothWindowsServer?.StopAsync(reason);
            }
            return Task.CompletedTask;
        }

        #endregion  

        private async Task StartServerOrHost(CancellationToken cancellationToken)
        {
            await Task.Delay(2000, cancellationToken); //

            if (!cancellationToken.IsCancellationRequested)
            {
                if (bluetoothLeWatcher.Signatures.Count == 0)
                {
                    await StartPublishingSignatureAsync();
                    await StartHosting();
                }
                else
                {
                    await ConnectToHost();
                }

                startAsyncTokenSource = null;
            }
        }

        CancellationTokenSource startAsyncTokenSource = null;

        public override async Task StartAsync(string sessionName, string pin, string reason)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                startAsyncTokenSource = new CancellationTokenSource();

                Connections.Clear();
                SessionName = sessionName;
                Pin = pin;

                await ScanForServerSignatures();

                _ = StartServerOrHost(startAsyncTokenSource.Token);
                Status = SyncDeviceStatus.Started;
            }
        }

        public override async Task StopAsync(string reason)
        {
            startAsyncTokenSource?.Cancel();
            startAsyncTokenSource = null;
            Status = SyncDeviceStatus.Stopped;
            await StopHosting(reason);
            await DisconnectFromHost(reason);
            await StopScanningForServerSignatures(reason);
            await StopPublishingSignatureAsync(reason);
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
