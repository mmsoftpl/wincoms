using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncDevice.Windows.Bluetooth
{
    public class PeerToPeerConnection : IComparable
    {
        private readonly string SessionName;
        public readonly string SessionNameA;
        public readonly string SessionNameB;

        protected PeerToPeerConnection(string sessionNameA, string sessionNameB)
        {
            if (string.Compare( sessionNameA, sessionNameB) <=0)
                SessionName = sessionNameA + " - " + sessionNameB;
            else
                SessionName = sessionNameB + " - " + sessionNameA;
            SessionNameA = sessionNameA;
            SessionNameB = sessionNameB;
        }

        public static PeerToPeerConnection Create(string sesionNameA,string SesionNameB)
        {
            if (string.IsNullOrEmpty(sesionNameA)) throw new ArgumentNullException(nameof(sesionNameA));
            if (string.IsNullOrEmpty(SesionNameB)) throw new ArgumentNullException(nameof(SesionNameB));
            return new PeerToPeerConnection(sesionNameA, SesionNameB);
        }

        public static PeerToPeerConnection Create(ISyncDevice syncDevice)
        {
            return PeerToPeerConnection.Create(
                    syncDevice.SessionName,
                    (syncDevice as BluetoothWindowsChannel).Creator.SessionName);
        }

        public override int GetHashCode() => SessionName.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is PeerToPeerConnection peerToPeerConnection)
                return string.Compare(SessionName, peerToPeerConnection.SessionName) == 0;
            return false;
        }

        public int CompareTo(object obj)
        {
            if (obj is PeerToPeerConnection peerToPeerConnection)
                return string.Compare(SessionName, peerToPeerConnection.SessionName);
            return -1;
        }
    }


    public class BluetoothWindowsPeerToPeer : BluetoothWindows
    {
        private BluetoothWindowsClient bluetoothWindowsClient;
        private BluetoothWindowsServer bluetoothWindowsServer;

        public override bool IsHost => bluetoothWindowsServer?.IsHost == true;

        public readonly ConcurrentDictionary<PeerToPeerConnection, ISyncDevice> PeerToPeerConnections = new ConcurrentDictionary<PeerToPeerConnection, ISyncDevice>();

        public override IList<ISyncDevice> Connections => PeerToPeerConnections.Values.ToList();

        #region Bluetooth Client
        private Task ConnectToHost()
        {
            if (bluetoothWindowsClient == null)
            {
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsClient.OnConnectionStarted += BluetoothPeerToPeer_OnConnectionStarted;
                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothWindowsClient_OnDeviceDisconnected;
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Searching for hosts");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsClient_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (syncDevice == bluetoothWindowsClient)
            {
                bluetoothWindowsClient?.RestartAsync("Restarting. Searching for hosts");
            }
        }

        private void BluetoothPeerToPeer_OnConnectionStarted(object sender, ISyncDevice syncDevice)
        {
            if (syncDevice != bluetoothWindowsClient && syncDevice != bluetoothWindowsServer)
            {
                if (PeerToPeerConnections.TryAdd(PeerToPeerConnection.Create(syncDevice), syncDevice))
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
            if (PeerToPeerConnections.TryRemove(PeerToPeerConnection.Create(syncDevice), out var sd))
            {
                sd.OnMessageReceived -= BluetoothPeerToPeer_OnMessageReceived;
                sd.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                RaiseOnDeviceDisconnected(sd);
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
                _bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothWindowsClient_OnDeviceDisconnected;
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
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"Hosting");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (syncDevice == bluetoothWindowsServer)
                bluetoothWindowsServer?.StartAsync(SessionName, Pin, $"Restarting. Connecting to host");
        }

        private Task StopHosting(string reason)
        {
            var _bluetoothWindowsServer = bluetoothWindowsServer;
            bluetoothWindowsServer = null;
            if (_bluetoothWindowsServer != null)
            {
                _bluetoothWindowsServer.OnConnectionStarted -= BluetoothPeerToPeer_OnConnectionStarted;
                bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
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
                await ConnectToHost();
                await StartHosting();
                Status = SyncDeviceStatus.Started;
            }
        }

        public override async Task StopAsync(string reason)
        {
            Status = SyncDeviceStatus.Stopped;
            await StopHosting(reason);
            await DisconnectFromHost(reason);
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
