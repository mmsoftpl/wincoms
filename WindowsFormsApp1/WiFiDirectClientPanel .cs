﻿using SyncDevice;
using SyncDevice.Windows.Bluetooth;

namespace WindowsFormsApp1
{
    public partial class WiFiDirectClientPanel : SyncPanel
    {
        readonly WifiDirectWindowsClient client = null;

        public override ISyncDevice SyncDevice => client;

        public WiFiDirectClientPanel()
        {
            InitializeComponent();

            client = new WifiDirectWindowsClient() { Logger = SDKTemplate.MainPage.mainPage };
            client.OnStatus += Server_OnStatus;
            client.OnMessage += Server_OnMessage;
            client.OnDeviceConnected += Server_OnDeviceConnected;
            client.OnDeviceDisconnected += Server_OnDeviceDisconnected;
        }

        private void Server_OnDeviceDisconnected(object sender, ISyncDevice device)
        {
        }

        private void Server_OnDeviceConnected(object sender, ISyncDevice device)
        {
            KeepWriting();
        }

        private void Server_OnMessage(object sender, MessageEventArgs e)
        {
            RecordReciveMessage(e.Message);
        }

        private void Server_OnStatus(object sender, SyncDeviceStatus status)
        {
            Status = status;
        }
    }
}