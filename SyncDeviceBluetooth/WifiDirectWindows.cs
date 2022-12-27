using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SyncDevice.Windows.WifiDirect
{
    public abstract class WifiDirectWindows : ISyncDevice
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

        public abstract Task StartAsync(string reason);

        public abstract Task StopAsync(string reason);

        public abstract Task SendMessageAsync(string message);
    }
}
