using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncDevice.Windows.WifiDirect
{
    public abstract class WifiDirectWindows : ISyncDevice
    {
        //// Keep track of all sessions that are connected to this device
        //// NOTE: it may make sense to track sessions per-advertiser and a second list for the seeker, but this sample keeps a global list
        protected IList<SessionWrapper> connectedSessions = new List<SessionWrapper>();

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

        public void RaiseOnMessage(string message)
        {
            OnMessage?.Invoke(this, new MessageEventArgs() { Message = message });
        }

        public void RaiseOnDeviceConnected(string deviceId)
        {
            OnDeviceConnected?.Invoke(this, deviceId);
        }

        public void RaiseOnDeviceDisconnected(string deviceId)
        {
            OnDeviceDisconnected?.Invoke(this, deviceId);
        }

        public abstract Task StartAsync(string reason);

        public abstract Task StopAsync(string reason);

        public virtual async Task SendMessageAsync(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                foreach (var session in connectedSessions)
                {
                    await session.SendMessageAsync(message);
                }
            }
        }
    }
}
