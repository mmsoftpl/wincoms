﻿using SyncDevice.Windows.Bluetooth;
using SyncDevice;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class BluetoothLePublisherPanel : SyncPanel
    {
        readonly BluetoothLePublisher publisher = null;

        public override ISyncDevice SyncDevice => publisher;

        protected override string StartText => "Start publishing";

        protected override string StopText => "Stop publishing";

        public BluetoothLePublisherPanel()
        {
            InitializeComponent();

            publisher = new BluetoothLePublisher() { Logger = SDKTemplate.MainPage.mainPage };
            publisher.OnStatus += Server_OnStatus;
            publisher.OnMessage += Server_OnMessage;
            publisher.OnConnectionStarted += Server_OnConnectionStarted;
            publisher.OnDeviceConnected += Server_OnDeviceConnected;
            publisher.OnDeviceDisconnected += Server_OnDeviceDisconnected;
        }

        private void Server_OnConnectionStarted(object sender, string deviceId)
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