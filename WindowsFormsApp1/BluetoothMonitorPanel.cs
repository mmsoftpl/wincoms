﻿using SyncDevice;
using SyncDevice.Windows.Bluetooth;
using System.Threading;

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
            monitor.OnMessageSent += Watcher_OnMessageSent;
            monitor.OnConnectionStarted += Server_OnConnectionStarted;
            monitor.OnDeviceConnected += Server_OnDeviceConnected;
            monitor.OnDeviceDisconnected += Server_OnDeviceDisconnected;

            SyncDevice = monitor;
        }

        private int SignatureId;
        private string lastMessage;
        public string LastMessage
        {
            get => lastMessage;
            set
            {
                if (lastMessage != value)
                {
                    lastMessage = value;
                    Interlocked.Increment(ref SignatureId);
                    _ = (SyncDevice as BluetoothWindowsMonitor).SetSignatureAsync(SignatureId.ToString());
                }
            }
        }

        private void Server_OnConnectionStarted(object sender, ISyncDevice device)
        {
            if (!string.IsNullOrEmpty(LastMessage))
                SyncDevice.SendMessageAsync(LastMessage);

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

        private void Watcher_OnMessageSent(object sender, MessageEventArgs e)
        {
            _= e.SyncDevice?.StopAsync("Power save ;)");
        }

        private void Server_OnStatus(object sender, SyncDeviceStatus status)
        {
            Status = status;
            UpdateControls();
        }

        protected override void OnPingBackButtonClick()
        {
            LastMessage = LastReceivedMessage ?? System.DateTime.UtcNow.ToString();

            SyncDevice.SendMessageAsync(System.DateTime.UtcNow.ToString());
        }
    }
}
