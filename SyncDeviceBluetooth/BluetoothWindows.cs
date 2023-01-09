using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System;
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

        public string SessionName { get; protected set; }

        public ILogger Logger { get; set; }

        public event OnMessageEventHandler OnMessage;
        public event OnStatusEventHandler OnStatus;
        public event OnDeviceConnected OnDeviceConnected;
        public event OnConnectionStarted OnConnectionStarted;
        public event OnDeviceDisconnected OnDeviceDisconnected;

        internal virtual void RaiseOnMessage(string message)
        {
            OnMessage?.Invoke(this, new MessageEventArgs() { Message = message });
        }

        internal virtual void RaiseOnConnectionStarted(string deviceId)
        {
            Status = SyncDeviceStatus.Started;
            OnConnectionStarted?.Invoke(this, deviceId);
        }

        internal virtual void RaiseOnDeviceConnected(ISyncDevice device)
        {            
            OnDeviceConnected?.Invoke(this, device);
        }

        internal virtual void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            Status = SyncDeviceStatus.Stopped;

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

        public bool IsEFMserviceName(string serviceName) => serviceName?.StartsWith("Bluetooth eFM Service ") == true;

        // The value of the Service Name SDP attribute
        public static string SdpServiceName(ISyncDevice syncDevice) => $"Bluetooth eFM Service [{syncDevice?.SessionName}]";

        protected ConcurrentDictionary<string, BluetoothWindowsChannel> Channels = new ConcurrentDictionary<string, BluetoothWindowsChannel>();

        public int Connections { get => Channels.Count; }

        protected void ClearChannels()
        {
            foreach (var w in Channels.Values)
            {
                RaiseOnDeviceDisconnected(w);
            }

            Channels.Clear();
        }
    }
}
