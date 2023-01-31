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

        public override IList<ISyncDevice> Connections => bluetoothWindowsServer?.Connections ?? Enumerable.Empty<ISyncDevice>().ToList();

        #region Bluetooth Client
        private Task ConnectToHost()
        {
            if (bluetoothWindowsClient == null)
            {
                bluetoothWindowsClient = new BluetoothWindowsClient() { Logger = Logger, ServiceName = ServiceName };
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
                bluetoothWindowsServer = new BluetoothWindowsServer() { Logger = Logger, ServiceName = ServiceName };
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
            RaiseOnConnectionStarted(syncDevice);
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

        public override Task SendMessageAsync(string message, string[] recipients = null)
        {
            return bluetoothWindowsServer?.SendMessageAsync(message);
        }

    }
}
