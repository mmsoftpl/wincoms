using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
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
        private readonly ConcurrentDictionary<ConnectionId, ISyncDevice> PeerToPeerConnections = new ConcurrentDictionary<ConnectionId, ISyncDevice>();
        public override IList<ISyncDevice> Connections => PeerToPeerConnections.Values.ToList();

        #region Bluetooth LE Watcher
        private Task ScanForServerSignatures()
        {
            if (bluetoothLeWatcher == null)
            {
                bluetoothLeWatcher = new BluetoothLeWatcher() { Logger = Logger };
                bluetoothLeWatcher.OnError += BluetoothLeWatcher_OnError;
                return bluetoothLeWatcher.StartAsync(SessionName, null, $"Start scanning for '{ServiceName}' server signatures");
            }
            return Task.CompletedTask;
        }

        private void BluetoothLeWatcher_OnError(object sender, string error)
        {
            RaiseOnError(error);
            _= StopAsync("Error while watching for server signatures ");
        }

        private Task StopScanningForServerSignatures(string reason)
        {
            try
            {
                if (bluetoothLeWatcher != null)
                {
                    bluetoothLeWatcher.OnError -= BluetoothLeWatcher_OnError;
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

        private void BluetoothWindowsClient_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            _ = RestartAsync("Restarting after client disconnected");           
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
            if (PeerToPeerConnections.TryRemove(ConnectionId.Create(syncDevice), out var sd))
            {
                sd.OnMessageReceived -= BluetoothPeerToPeer_OnMessageReceived;
                sd.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                RaiseOnDeviceDisconnected(sd);
            }
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

        #region Bluetooth LE Publisher

        private Task StartPublishingSignatureAsync()
        {
            var clientSignature = SessionName;

            if (bluetoothLePublisher == null)
            {
                bluetoothLePublisher = new BluetoothLePublisher() { Logger = Logger, ServiceName = ServiceName, SessionName = clientSignature };
                bluetoothLePublisher.OnError += BluetoothLePublisher_OnError;
                return bluetoothLePublisher.StartAsync(clientSignature, null, $"Start publishing server signature");
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private void BluetoothLePublisher_OnError(object sender, string error)
        {
                RaiseOnError(error);
                _ = StopAsync("Error while publishing server signatures ");
        }

        private Task StopPublishingSignatureAsync(string reason)
        {
            try
            {
                if (bluetoothLePublisher != null)
                {
                    bluetoothLePublisher.OnError -= BluetoothLePublisher_OnError;
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
                bluetoothWindowsServer.OnError += BluetoothWindowsServer_OnError;
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"No server found. Starting in server mode.");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            _= RestartAsync("Restarting after server disconnected");
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

        private async Task StartServerOrHost(CancellationToken? cancellationToken, int delayMs = 2000)
        {
            if (delayMs <= 0)
                throw new System.ArgumentException("Must be greater than 0", nameof(delayMs));

            if (cancellationToken != null)
            {
                //wait two seconds before decide which mode to activate
                await Task.Delay(delayMs, cancellationToken.Value);

                if (!cancellationToken.Value.IsCancellationRequested)
                {
                    // No other servers signatures found, activate server mode
                    if (bluetoothLeWatcher.Signatures.Count == 0)
                    {
                        await StartPublishingSignatureAsync();
                        await StartHosting();
                        await StopScanningForServerSignatures("Starting in server mode, no need to scan for other servers");
                    }
                    else
                    {
                        // Some server singature found, activate client mode
                        await ConnectToHost();
                    }

                    startAsyncTokenSource = null;
                    Status = SyncDeviceStatus.Started;
                }
            }
        }

        CancellationTokenSource startAsyncTokenSource = null;

        public async Task startAsync(string sessionName, string pin, int delayMs = 2000)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                startAsyncTokenSource = new CancellationTokenSource();

                Connections.Clear();
                SessionName = sessionName;
                Pin = pin;

                await ScanForServerSignatures();

                await StartServerOrHost(startAsyncTokenSource?.Token, delayMs);
            }
        }

        public override Task StartAsync(string sessionName, string pin, string reason)
        {
            return startAsync(sessionName, pin);
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
