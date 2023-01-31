using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothWindowsPeerToPeer : BluetoothWindows
    {
        private BluetoothWindowsClient bluetoothWindowsClient;
        private BluetoothWindowsServer bluetoothWindowsServer;

        public override bool IsHost => bluetoothWindowsServer?.IsHost == true;

        public readonly ConcurrentDictionary<string, ISyncDevice> PeerToPeerConnections = new ConcurrentDictionary<string, ISyncDevice>();

        public override IList<ISyncDevice> Connections => PeerToPeerConnections.Values.ToList();

        #region Bluetooth Client
        private Task ConnectToHost()
        {
            if (bluetoothWindowsClient == null)
            {
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsClient.OnDeviceConnected += BluetoothPeerToPeer_OnDeviceConnected;
                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothPeerToPeer_OnDeviceDisconnected;
             //   bluetoothWindowsClient.OnStatus += BluetoothPeerToPeer_OnStatus;
                /*                bluetoothWindowsClient.OnMessageReceived += BluetoothWindowsClient_OnMessage;
                                bluetoothWindowsClient.OnError += BluetoothWindowsClient_OnError;
                                bluetoothWindowsClient.OnStatus += BluetoothWindowsClient_OnStatus;
                                bluetoothWindowsClient.OnDeviceConnected += BluetoothWindowsClient_OnDeviceConnected;
                                bluetoothWindowsClient.OnMessageSent += BluetoothWindowsClient_OnMessageSent;
                                // bluetoothWindowsClient.
                                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothWindowsClient_OnDeviceDisconnected;*/
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Connecting to host");
            }
            return Task.CompletedTask;
        }

        private void BluetoothPeerToPeer_OnDeviceConnected(object sender, ISyncDevice syncDevice)
        {
            if (PeerToPeerConnections.TryAdd(syncDevice.SessionName, syncDevice))
            {
                syncDevice.OnMessageReceived += BluetoothPeerToPeer_OnMessageReceived;
                RaiseOnConnected(syncDevice); RaiseOnStatus(Status);
            }
            else
                syncDevice.StopAsync("Already connected");
        }

        private void BluetoothPeerToPeer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (PeerToPeerConnections.TryRemove(syncDevice.SessionName, out var sd))
            {
                sd.OnMessageReceived -= BluetoothPeerToPeer_OnMessageReceived;
                RaiseOnDeviceConnected(syncDevice); RaiseOnStatus(Status);
            }
        }

        private void BluetoothPeerToPeer_OnStatus(object sender, SyncDeviceStatus status)
        {
            RaiseOnStatus(status);
        }



        private void BluetoothPeerToPeer_OnMessageSent(object sender, MessageEventArgs e)
        {
            RaiseOnMessageSent(e.Message);
        }

        private void BluetoothPeerToPeer_OnMessageReceived(object sender, MessageEventArgs e)
        {
            RaiseOnMessageReceived(e.Message);
        }

        private void BluetoothWindowsClient_OnError(object sender, string error)
        {
            RaiseOnError(error);
        }

        private Task DisconnectFromHost(string reason)
        {
            try
            {
                if (bluetoothWindowsClient != null)
                {
                    bluetoothWindowsClient.OnDeviceConnected -= BluetoothPeerToPeer_OnDeviceConnected;
                    bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                   // bluetoothWindowsClient.OnStatus -= BluetoothPeerToPeer_OnStatus;
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
                bluetoothWindowsServer = new BluetoothWindowsServer() { Logger = Logger, ServiceName = ServiceName };
                bluetoothWindowsServer.OnDeviceConnected += BluetoothPeerToPeer_OnDeviceConnected;
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothPeerToPeer_OnDeviceDisconnected;
              //  bluetoothWindowsServer.OnStatus += BluetoothPeerToPeer_OnStatus;
                //bluetoothWindowsServer.OnConnectionStarted += BluetoothWindowsServer_OnConnectionStarted;
                //bluetoothWindowsServer.OnMessageSent += BluetoothWindowsServer_OnMessageSent;
                //bluetoothWindowsClient.OnStatus += BluetoothWindowsClient_OnStatus;
                //bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"Connecting to host");
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
            RaiseOnConnectionStarted(syncDevice);
        }

        private Task StopHosting(string reason)
        {
            try
            {
                if (bluetoothWindowsServer != null)
                {
                    bluetoothWindowsServer.OnDeviceConnected -= BluetoothPeerToPeer_OnDeviceConnected;
                    bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothPeerToPeer_OnDeviceDisconnected;
                  //  bluetoothWindowsServer.OnStatus -= BluetoothPeerToPeer_OnStatus;

                    //bluetoothWindowsServer.OnConnectionStarted -= BluetoothWindowsServer_OnConnectionStarted;
                    //bluetoothWindowsServer.OnMessageSent -= BluetoothWindowsServer_OnMessageSent;

                    //bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
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

        public override async Task SendMessageAsync(string message, string[] recipients = null)
        {
            foreach(var connection in Connections)
                await connection.SendMessageAsync(message, recipients);
        }

    }
}
