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

        public ILogger Logger { get; set; }

        public event OnMessageEventHandler OnMessage;
        public event OnStatusEventHandler OnStatus;
        public event OnDeviceConnected OnDeviceConnected;
        public event OnDeviceDisconnected OnDeviceDisconnected;

        protected void RaiseOnMessage(string message)
        {
            OnMessage?.Invoke(this, new MessageEventArgs() { Message = message });
        }

        protected void RaiseOnDeviceConnected(string deviceId)
        {
            OnDeviceConnected?.Invoke(this, deviceId);
        }

        protected void RaiseOnDeviceDisconnected(string deviceId)
        {
            OnDeviceDisconnected?.Invoke(this, deviceId);
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
                foreach (var writer in Writers.Values)
                {
                    await WriteMessageAsync(writer, message);
                }
            }
        }

        public abstract Task StartAsync(string reason);

        public abstract Task StopAsync(string reason);

        // The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
        public static readonly Guid RfcommChatServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

        // The Id of the Service Name SDP attribute
        protected const UInt16 SdpServiceNameAttributeId = 0x100;

        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.
        protected const byte SdpServiceNameAttributeType = (4 << 3) | 5;

        // The value of the Service Name SDP attribute
        public const string SdpServiceName = "Bluetooth eFM Service";

        protected ConcurrentDictionary<string, DataWriter> Writers = new ConcurrentDictionary<string, DataWriter>();

        protected void ClearWriters()
        {
            foreach (var w in Writers.Keys)
            {
                if (Writers.TryRemove(w, out var writer))
                {
                    writer.DetachStream();
                    RaiseOnDeviceConnected(w);
                }
            }

            Writers.Clear();
        }
    }
}
