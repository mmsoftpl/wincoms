﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private async Task SetSignature(string value)
        {
            if (Signature != value)
            {
                Signature = value;

                await StopPublishingSignatureAsync("signature changed");
                await StartHosting();
                await StartPublishingSignatureAsync();
            }

        }

        public override bool IsHost => bluetoothWindowsServer?.IsHost == true;

        public override IList<ISyncDevice> Connections => bluetoothWindowsServer?.Connections ?? bluetoothWindowsClient?.Connections ?? base.Connections;

        #region Bluetooth LE Watcher
        private Task ScanForSignatures()
        {
            if (bluetoothLeWatcher == null)
            {
                bluetoothLeWatcher = new BluetoothLeWatcher() { Logger = Logger };
                bluetoothLeWatcher.OnError += BluetoothLeWatcher_OnError;
                bluetoothLeWatcher.OnMessage += BluetoothLeWatcher_OnMessage;
                return bluetoothLeWatcher.StartAsync(SessionName, null, $"Start scanning for '{ServiceName}' signatures");
            }
            return Task.CompletedTask;
        }
        private void BluetoothLeWatcher_OnMessage(object sender, MessageEventArgs e)
        {
            ConnectToHost();
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
                bluetoothWindowsClient.OnMessage += BluetoothWindowsClient_OnMessage;
                bluetoothWindowsClient.OnDeviceDisconnected += BluetoothWindowsClient_OnDeviceDisconnected;
                return bluetoothWindowsClient.StartAsync(SessionName, Pin, $"Connecting to host");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsClient_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (sender==bluetoothWindowsClient)
            {
                bluetoothWindowsClient.OnMessage -= BluetoothWindowsClient_OnMessage;
                bluetoothWindowsClient.OnDeviceDisconnected -= BluetoothWindowsClient_OnDeviceDisconnected;
                bluetoothWindowsClient = null;
            }
        }

        private void BluetoothWindowsClient_OnMessage(object sender, MessageEventArgs e)
        {
            RaiseOnMessage(e.Message);
        }

        private Task DisconnectFromHost(string reason)
        {
            try
            {
                if (bluetoothWindowsClient != null)
                {
                    bluetoothWindowsClient.OnMessage -= BluetoothWindowsClient_OnMessage;
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
                bluetoothWindowsServer.OnDeviceDisconnected += BluetoothWindowsServer_OnDeviceDisconnected;
                return bluetoothWindowsServer.StartAsync(SessionName, Pin, $"Connecting to host");
            }
            return Task.CompletedTask;
        }

        private void BluetoothWindowsServer_OnDeviceDisconnected(object sender, ISyncDevice syncDevice)
        {
            if (sender == bluetoothWindowsServer)
            {
                bluetoothWindowsServer.OnMessage -= BluetoothWindowsClient_OnMessage;
                bluetoothWindowsServer.OnDeviceDisconnected -= BluetoothWindowsServer_OnDeviceDisconnected;
                bluetoothWindowsServer = null;
            }
        }

        private void BluetoothWindowsServer_OnConnectionStarted(object sender, ISyncDevice syncDevice)
        {
            if (!string.IsNullOrEmpty(LastMessage))
                syncDevice.SendMessageAsync(LastMessage);
        }

        private Task StopHosting(string reason)
        {
            try
            {
                if (bluetoothWindowsServer != null)
                {
                    bluetoothWindowsServer.OnMessage -= BluetoothWindowsClient_OnMessage;
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
                Pin = pin;
                await ScanForSignatures();
                Status = SyncDeviceStatus.Started;
            }
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
                    _ = SetSignature(SignatureId.ToString());
                }
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

        public override Task SendMessageAsync(string message)
        {
            if (Status == SyncDeviceStatus.Started)
            {
                LastMessage = message;

                return bluetoothWindowsServer?.SendMessageAsync(message);
            }
            else
                RaiseOnError("Not started");
            return Task.CompletedTask;
        }

    }
}
