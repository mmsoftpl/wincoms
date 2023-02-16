﻿using SyncDevice;
using SyncDevice.Windows.Bluetooth;

namespace WindowsFormsApp1
{
    public partial class BluetoothWatcherPanel : SyncPanel
    {
        protected override string StartText => "Start watching";

        protected override string StopText => "Stop watching";

        public BluetoothWatcherPanel()
        {
            InitializeComponent();

            var watcher = new BluetoothWatcher() { Logger = SDKTemplate.MainPage.mainPage };
            watcher.OnStatus += Server_OnStatus;
            watcher.OnMessageReceived += Server_OnMessage;
            watcher.OnConnectionStarted += Server_OnConnectionStarted;
            watcher.OnDeviceConnected += Server_OnDeviceConnected;
            watcher.OnDeviceDisconnected += Server_OnDeviceDisconnected;

            SyncDevice = watcher;
        }

        private void Server_OnConnectionStarted(object sender, ISyncDevice device)
        {
            _ = KeepWriting();
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