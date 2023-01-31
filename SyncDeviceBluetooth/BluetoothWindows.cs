using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
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

        protected string Pin { get; set; }
        public string DeviceId { get; internal set; }

        const string DefaultServiceName = "EFM";
        const string DefaultSessionName = "XYZ";

        public string ServiceName { get; set; } = DefaultServiceName;
        public string SessionName { get; internal set; } = DefaultSessionName;

        public ILogger Logger { get; set; }

        public event OnMessageReceivedEventHandler OnMessageReceived;
        public event OnMessageSentEventHandler OnMessageSent;
        public event OnStatusEventHandler OnStatus;
        public event OnDeviceConnected OnDeviceConnected;
        public event OnConnectionStarted OnConnectionStarted;
        public event OnDeviceDisconnected OnDeviceDisconnected;
        public event OnDeviceError OnError;

        internal virtual void RaiseOnMessageReceived(string message) => OnMessageReceived?.Invoke(this, new MessageEventArgs()
        {
            SyncDevice = this,
            Message = message
        });

        internal virtual void RaiseOnMessageSent(string message) => OnMessageSent?.Invoke(this, new MessageEventArgs()
        {
            SyncDevice = this,
            Message = message
        });

        internal virtual void RaiseOnStatus(SyncDeviceStatus status)
        {
            OnStatus?.Invoke(this, status);
        }

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

        internal async Task WriteMessageAsync(DataWriter chatWriter, string message, bool notifyOnSent)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    chatWriter?.WriteUInt32((uint)message.Length);
                    chatWriter?.WriteString(message);

                    await chatWriter?.StoreAsync();

                    if (notifyOnSent)
                        RaiseOnMessageSent(message);
                }
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
            {
                // The remote device has disconnected the connection
                Logger?.LogInformation("Remote side disconnect: " + ex.HResult.ToString() + " - " + ex.Message);
            }
        }

        public virtual async Task SendMessageAsync(string message, string[] recipients= null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                foreach (var writer in Channels.Values)
                    await writer.SendMessageAsync(message);
            }
        }

        public abstract Task StartAsync(string sessionName, string pin, string reason);

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
                return s[s.Length - 1];
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

        public virtual IList<ISyncDevice> Connections { get => Channels?.Values?.Cast<ISyncDevice>().ToList(); }

        protected void RegisterChannel(BluetoothWindowsChannel channel, string pin)
        {
            if (!Channels.TryAdd(channel.DeviceId, channel))
            {
                Logger?.LogInformation($"Channel {channel?.DeviceId} already registered?");
            }
            else
            {
                Logger?.LogInformation($"Channel {channel?.DeviceId} registered");
                RaiseOnDeviceConnected(channel);

                if (!string.IsNullOrEmpty(pin))
                {
                    _ = channel.StartAsync(SessionName, pin, "PIN provided, connecting");
                }
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

        protected async Task<T> BluetoothStartAction<T>(Func<Task<T>> action)
        {
            try
            {
                var r = await action.Invoke();
                return r;
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
            return default;
        }

        public static IEnumerable<NetworkInterface> BluetoothAdapters()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            if (adapters?.Length > 0)
            {
                foreach (NetworkInterface adapter in adapters)
                {
                    if (adapter.Name.Contains("Bluetooth") && adapter.Name.Contains("Bluetooth"))
                    {
                        yield return adapter;
                    }
                }
            }
        }

        private static string thisBluetoothMac = null;
        public static string ThisBluetoothMac
        {
            get
            {
                if (thisBluetoothMac == null)
                    thisBluetoothMac = BluetoothAdapters().Select(a => a.GetPhysicalAddress().ToString().Replace(":", "")).FirstOrDefault() ?? string.Empty;
                return thisBluetoothMac;
            }
        }

        public static ulong ToUlong(string s)
        {
            return Convert.ToUInt64(s.ToString().Replace(":", ""), 16);
        }

        public static ulong ThisBluetoothAddress
        {
            get
            {
                return Convert.ToUInt64(ThisBluetoothMac, 16);
            }
        }

    }
}
