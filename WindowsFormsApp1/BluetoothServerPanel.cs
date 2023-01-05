﻿using SyncDevice;
using SyncDevice.Windows.Bluetooth;
using System.Linq.Expressions;

namespace WindowsFormsApp1
{
    public partial class BluetoothServerPanel : SyncPanel
    {
        readonly BluetoothWindowsServer server = null;

        public override ISyncDevice SyncDevice => server;

        public BluetoothServerPanel()
        {
            InitializeComponent();

            server = new BluetoothWindowsServer() { Logger = SDKTemplate.MainPage.mainPage };
            server.OnStatus += Server_OnStatus;
            server.OnMessage += Server_OnMessage;
            server.OnConnectionStarted += Server_OnConnectionStarted;
            server.OnDeviceConnected += Server_OnDeviceConnected;
            server.OnDeviceDisconnected += Server_OnDeviceDisconnected;
        }

        private void Server_OnConnectionStarted(object sender, string deviceId)
        {
            _ = KeepWriting();

            string s = "{\"TargetNodeId\":1,\"SourceNodeId\":2,\"PayloadLength\":105,\"Payload\":\"eyJNZXNzYWdlSWQiOiJJc0FsaXZlIiwiUGF5bG9hZCI6IntcIk1lc3NhZ2VJZFwiOlwiSXNBbGl2ZVwiLFwiTnVtQ2xpZW50c1wiOjEsXCJDb25uZWN0ZWROb2RlSWRzXCI6WzFdfSJ9\"}";

            SyncDevice?.SendMessageAsync(s);
        }

        private void Server_OnDeviceDisconnected(object sender, ISyncDevice device)
        {
        }

        private void Server_OnDeviceConnected(object sender, ISyncDevice device)
        {
            _ = device.StartAsync("server auto starting device");
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
