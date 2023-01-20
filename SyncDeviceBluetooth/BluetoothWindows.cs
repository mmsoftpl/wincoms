﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public abstract class BluetoothWindows : ISyncDevice
    {
        private SyncDeviceStatus status = SyncDeviceStatus.Stopped;
        public SyncDeviceStatus Status
        {
            get => status;
            set
            {
                status = value;
                OnStatus?.Invoke(this, status);                
            }
        }

        public abstract bool IsHost { get; }
        public string DeviceId { get; internal set; }

        const string DefaultServiceName = "EFM";
        const string DefaultSessionName = "XYZ";

        public string ServiceName { get; internal set; } = DefaultServiceName;
        public string SessionName { get; internal set; } = DefaultSessionName;

        public ILogger Logger { get; set; }

        public event OnMessageEventHandler OnMessage;
        public event OnStatusEventHandler OnStatus;
        public event OnDeviceConnected OnDeviceConnected;
        public event OnConnectionStarted OnConnectionStarted;
        public event OnDeviceDisconnected OnDeviceDisconnected;
        public event OnDeviceError OnError;

        internal virtual void RaiseOnMessage(string message) => OnMessage?.Invoke(this, new MessageEventArgs() { Message = message });
        internal virtual void RaiseOnError(string error)
        {
            Logger?.LogError(error);
            OnError?.Invoke(this, error);
        }

        internal virtual void RaiseOnConnectionStarted(ISyncDevice device)
        {
            Status = SyncDeviceStatus.Started;
            OnConnectionStarted?.Invoke(this, device);
        }

        internal virtual void RaiseOnDeviceConnected(ISyncDevice device)
        {            
            OnDeviceConnected?.Invoke(this, device);
        }

        internal virtual void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            OnDeviceDisconnected?.Invoke(this, device);

            if (device is BluetoothWindowsChannel channel)
            {
                if (Channels.TryRemove(channel.DeviceId, out var writer))
                {
                    writer.StopAsync($"Stoping & removing {writer.DeviceId} from directory");
                }
            }
        }

        internal async Task WriteMessageAsync(DataWriter chatWriter, string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    chatWriter?.WriteUInt32((uint)message.Length);
                    chatWriter?.WriteString(message);

                    await chatWriter?.StoreAsync();

                }
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
            {
                // The remote device has disconnected the connection
                Logger?.LogInformation("Remote side disconnect: " + ex.HResult.ToString() + " - " + ex.Message);
            }
        }

        public virtual async Task SendMessageAsync(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                foreach (var writer in Channels.Values)
                    await writer.SendMessageAsync(message);
            }
        }

        public abstract Task StartAsync(string sessionName, string reason);

        public virtual Task RestartAsync(string reason) { throw new NotImplementedException(); }

        public abstract Task StopAsync(string reason);

        // The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
        public static readonly Guid RfcommChatServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");
        public static readonly Guid BluetoothProtocolId = Guid.Parse("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");

        // The Id of the Service Name SDP attribute
        protected const ushort SdpServiceNameAttributeId = 0x100;

        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.
        protected const byte SdpServiceNameAttributeType = (4 << 3) | 5;

        public bool HasServiceName(string serviceName) => serviceName?.ToUpper().Contains(ServiceName.ToUpper()) == true;

        public string GetSessionName(string serviceName)
        {
            var s = serviceName.Split('|');
            if (s.Length > 0)
            {
                return s[s.Length-1];
            }
            return serviceName;
        }

        // The value of the Service Name SDP attribute
        public string SdpServiceName
        {
            get
            {
                var sn = $"{ServiceName ?? DefaultServiceName}|{SessionName ?? DefaultSessionName}";
                if (sn.Length > 23)
                    sn = sn.Substring(0, 23);
                return sn;
            }
        }

        protected ConcurrentDictionary<string, BluetoothWindowsChannel> Channels = new ConcurrentDictionary<string, BluetoothWindowsChannel>();

        public IList<ISyncDevice> Connections { get => Channels?.Values?.Cast<ISyncDevice>().ToList(); }

        protected void RegisterChannel(BluetoothWindowsChannel channel)
        {
            if (!Channels.TryAdd(channel.DeviceId, channel))
            {
                Logger?.LogInformation($"Channel {channel?.DeviceId} already registered?");
            }
            else
            {
                Logger?.LogInformation($"Channel {channel?.DeviceId} registered");
                RaiseOnDeviceConnected(channel);
            }
        }

        protected void ClearChannels()
        {
            foreach (var w in Channels.Values)
            {
                RaiseOnDeviceDisconnected(w);
            }

            Channels.Clear();
        }

        protected bool BluetoothAction(Action action)
        {
            try
            {
                action.Invoke();
                return true;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x800710DF)
            {
                // The Bluetooth radio may be off.
                RaiseOnError($"Make sure your Bluetooth Radio is on; {ex.Message}");
                Status = SyncDeviceStatus.Stopped;
            }
            catch (Exception ex)
            {
                RaiseOnError($"Error occured, {ex?.InnerException?.Message ?? ex?.Message}");
                 
            }
            return false;
        }
    }
}
