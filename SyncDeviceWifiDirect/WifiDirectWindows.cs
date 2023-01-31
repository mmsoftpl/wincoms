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
        public string DeviceId { get; internal set; }

        public string SessionName { get; set; }
        public string ServiceName { get; set; }

        public ILogger Logger { get; set; }

        public IList<ISyncDevice> Connections { get => null; }

        public string Id { get => "???"; }

        public event OnMessageReceivedEventHandler OnMessageReceived;
        public event OnMessageSentEventHandler OnMessageSent;
        public event OnStatusEventHandler OnStatus;
        public event OnConnectionStarted OnConnectionStarted;
        public event OnDeviceConnected OnDeviceConnected;
        public event OnDeviceDisconnected OnDeviceDisconnected;
        public event OnDeviceError OnError;

        public void RaiseOnMessageReceived(string message) => OnMessageReceived?.Invoke(this, new MessageEventArgs() { Message = message });        
        public void RaiseOnMessageSent(string message) => OnMessageSent?.Invoke(this, new MessageEventArgs() { Message = message });
        public void RaiseOnError(string error) => OnError?.Invoke(this, error);
        public void RaiseOnConnectionStarted(ISyncDevice device) => OnConnectionStarted?.Invoke(this, device);
        public void RaiseOnDeviceConnected(ISyncDevice device) => OnDeviceConnected?.Invoke(this, device);
        public void RaiseOnDeviceDisconnected(ISyncDevice device) => OnDeviceDisconnected?.Invoke(this, device);

        public abstract Task StartAsync(string sessionName, string pin, string reason);

        public virtual Task RestartAsync(string reason) { throw new NotImplementedException(); }
        public abstract Task StopAsync(string reason);

        public virtual async Task SendMessageAsync(string message, string[] recipients= null)
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
