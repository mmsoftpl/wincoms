using Microsoft.Extensions.Logging;
using System;
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

        public bool IsHost => false;

        public string SessionName { get; set; }
        public ILogger Logger { get; set; }

        public IList<ISyncDevice> Connections { get => null; }

        public string Id { get => "???"; }

        public virtual Task RestartAsync(string reason) { throw new NotImplementedException(); }

        public event OnMessageEventHandler OnMessage;
        public event OnStatusEventHandler OnStatus;
        public event OnConnectionStarted OnConnectionStarted;
        public event OnDeviceConnected OnDeviceConnected;
        public event OnDeviceDisconnected OnDeviceDisconnected;

        public void RaiseOnMessage(string message)
        {
            OnMessage?.Invoke(this, new MessageEventArgs() { Message = message });
        }

        public void RaiseOnDeviceConnected(ISyncDevice device)
        {
            OnDeviceConnected?.Invoke(this, device);
        }

        public void RaiseOnDeviceDisconnected(ISyncDevice device)
        {
            OnDeviceDisconnected?.Invoke(this, device);
        }

        public abstract Task StartAsync(string sessionName, string reason);

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
