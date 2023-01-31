using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsMonitor : BluetoothWindows
    {
        private BluetoothLeWatcher bluetoothLeWatcher;
        private BluetoothLePublisher bluetoothLePublisher;

        private BluetoothWindowsClient bluetoothWindowsClient;
        private BluetoothWindowsServer bluetoothWindowsServer;

        public string Signature
        {
            get;
            private set;
        }

        public async Task SetSignatureAsync(string value, bool startHosting)
        {
            if (Signature != value)
            {
                Signature = value;

                await StopPublishingSignatureAsync("signature changed");
                if (startHosting)
                    await StartHosting();
                await StartPublishingSignatureAsync();
            }
        }

        public override bool IsHost => bluetoothWindowsServer?.IsHost == true;

        public override IList<ISyncDevice> Connections
        {
            get
            {
                Dictionary<ulong, ISyncDevice> connections = new System.Collections.Generic.Dictionary<ulong, ISyncDevice>();

                if (bluetoothWindowsServer?.Connections?.Count > 0)
                    foreach (var connection in bluetoothWindowsServer.Connections)
                        connections.Add(ToUlong( connection.DeviceId), connection);

                if (bluetoothWindowsServer?.Connections?.Count > 0)
                    foreach (var connection in bluetoothWindowsClient.Connections)
                        connections.Add(ToUlong(connection.DeviceId), connection);

                if (bluetoothLeWatcher != null)
                {
                    foreach (var signature in bluetoothLeWatcher.Signatures)
                    {
                        BluetoothLeSignature bluetoothLeSignature = new BluetoothLeSignature()
                        {
                            SessionName = signature.Value.Data,
                        };
                        connections.Add(signature.Key, bluetoothLeSignature);
                    }
                }
                return connections.Values.ToList();
            }
        }

        #region Bluetooth LE Watcher
        private Task ScanForSignatures()
        {
            if (bluetoothLeWatcher == null)
            {
                bluetoothLeWatcher = new BluetoothLeWatcher() { Logger = Logger };
                bluetoothLeWatcher.OnError += BluetoothLeWatcher_OnError;
                bluetoothLeWatcher.OnStatus += BluetoothLeWatcher_OnStatus;
                bluetoothLeWatcher.OnMessageReceived += BluetoothLeWatcher_OnMessage;
                return bluetoothLeWatcher.StartAsync(SessionName, null, $"Start scanning for '{ServiceName}' signatures");
            }
            return Task.CompletedTask;
        }

        private void BluetoothLeWatcher_OnStatus(object sender, SyncDeviceStatus status)
        {
            RaiseOnStatus(status);
        }

        private void BluetoothLeWatcher_OnMessage(object sender, MessageEventArgs e)
        {
            

            //if (msg?.Length > 2)
            {
                ConnectToHost();
            }
        }

        private void BluetoothLeWatcher_OnError(object sender, string error)
        {
            RaiseOnError(error);
        }

        private Task StopScanningForSignatures(string reason)
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

        #region Bluetooth LE Publisher

        private Task StartPublishingSignatureAsync()
        {
            var clientSignature = SessionName + "|" + Signature;

            if (bluetoothLePublisher == null)
            {
                bluetoothLePublisher = new BluetoothLePublisher() { Logger = Logger, ServiceName = ServiceName, SessionName = clientSignature };
                return bluetoothLePublisher.StartAsync(clientSignature, null, $"Start publishing signature");
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

        #region Bluetooth Client
        private Task ConnectToHost()
        {
            if (bluetoothWindowsClient == null)
            {
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger };
                bluetoothWindowsClient.OnMessageReceived += BluetoothWindowsClient_OnMessage;
                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothWindowsClient_OnDeviceDisconnected;
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Connecting to host");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsClient_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (sender==bluetoothWindowsClient)
            {
                bluetoothWindowsClient.OnMessageReceived -= BluetoothWindowsClient_OnMessage;
                bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothWindowsClient_OnDeviceDisconnected;
                bluetoothWindowsClient = null;
            }
        }

        private void BluetoothWindowsClient_OnMessage(object sender, MessageEventArgs e)
        {
            RaiseOnMessageReceived(e.Message);
        }

        private Task DisconnectFromHost(string reason)
        {
            try
            {
                if (bluetoothWindowsClient != null)
                {
                    bluetoothWindowsClient.OnMessageReceived -= BluetoothWindowsClient_OnMessage;
                    bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothWindowsClient_OnDeviceDisconnected;
                    return bluetoothWindowsClient?.StopAsync(reason);
                }
                return Task.CompletedTask;
            }
            finally
            {
                bluetoothWindowsClient = null;
            }
        }
        #endregion

        #region Bluetooth Server

        private Task StartHosting()
        {
            if (bluetoothWindowsServer == null)
            {
                bluetoothWindowsServer = new BluetoothWindowsServer() { Logger = Logger };
                bluetoothWindowsServer.OnConnectionStarted += BluetoothWindowsServer_OnConnectionStarted;
                bluetoothWindowsServer.OnMessageSent += BluetoothWindowsServer_OnMessageSent;
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"Connecting to host");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (sender == bluetoothWindowsServer)
            {
                bluetoothWindowsServer.OnConnectionStarted -= BluetoothWindowsServer_OnConnectionStarted;
                bluetoothWindowsServer.OnMessageSent -= BluetoothWindowsServer_OnMessageSent;
                bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
                bluetoothWindowsServer = null;
            }
        }

        private CancellationTokenSource CancellationTokenSource_cts;
        private async Task KillConnectionAfter5sec(ISyncDevice syncDevice, CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                await syncDevice?.StopAsync("Power save after 5 sec;)");
            }
        }

        private Task KillConnectionAfter5sec(ISyncDevice syncDevice)
        {
            CancellationTokenSource_cts?.Cancel();

            CancellationTokenSource_cts = new CancellationTokenSource();

            return KillConnectionAfter5sec(syncDevice, CancellationTokenSource_cts.Token);
        }


        private void BluetoothWindowsServer_OnMessageSent(object sender, MessageEventArgs e)
        {
            RaiseOnMessageSent(e.Message);
            _ = KillConnectionAfter5sec(e.SyncDevice);
        }

        private void BluetoothWindowsServer_OnConnectionStarted(object sender, ISyncDevice syncDevice)
        {            
            if (lastRecipients.TryRemove(syncDevice.SessionName, out var message))
            {
                RaiseOnConnectionStarted(syncDevice);
                syncDevice.SendMessageAsync(message);
            }
            else
            {
                _ = KillConnectionAfter5sec(syncDevice);
            }
        }

        private Task StopHosting(string reason)
        {
            try
            {
                if (bluetoothWindowsServer != null)
                {
                    bluetoothWindowsServer.OnConnectionStarted -= BluetoothWindowsServer_OnConnectionStarted;
                    bluetoothWindowsServer.OnMessageSent -= BluetoothWindowsServer_OnMessageSent;
                    bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
                    return bluetoothWindowsServer?.StopAsync(reason);
                }
                return Task.CompletedTask;
            }
            finally
            {
                bluetoothWindowsServer = null;
            }
        }

        #endregion  

        public override async Task StartAsync(string sessionName, string pin, string reason)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                SessionName = sessionName;
                Pin = pin;
                await ScanForSignatures();
                Status = SyncDeviceStatus.Started;
                
                await SetSignatureAsync(string.Empty, false);

            }
        }

        public override async Task StopAsync(string reason)
        {
            Status = SyncDeviceStatus.Stopped;
            await StopHosting(reason);
            await StopPublishingSignatureAsync(reason);
            await DisconnectFromHost(reason);
            await StopScanningForSignatures(reason);
        }

        private int SignatureId;
        private ConcurrentDictionary<string, string> lastRecipients = new ConcurrentDictionary<string, string>();
        public string LastMessage
        {
            get;set;
        }

        private void SetLastMessage(string message, string[] recipients)
        {
            lastRecipients = new ConcurrentDictionary<string, string>();
            if (recipients?.Length > 0)
            {
                foreach (var recipient in recipients)
                {
                    if (!string.IsNullOrEmpty(recipient))
                        lastRecipients.TryAdd(recipient, message);
                }
            }

            // if (lastMessage != value)
            {
                LastMessage = message;

                _ = SetSignatureAsync(Interlocked.Increment(ref SignatureId).ToString(), true);
            }
        }

        public override Task SendMessageAsync(string message, string[] recipients = null)
        {
            if (Status == SyncDeviceStatus.Started)
            {
                SetLastMessage(message, recipients);

                return bluetoothWindowsServer?.SendMessageAsync(message);
            }
            else
                RaiseOnError("Not started");
            return Task.CompletedTask;
        }

    }
}
