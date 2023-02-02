
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncDevice
{
    public class MessageEventArgs
    {
        public string Message { get; set; }
        public ISyncDevice SyncDevice { get; set; }
    }

    public enum SyncDeviceStatus
    {
        Stopped,
        Created,
        Started,
        Aborted
    }

    public delegate void OnMessageReceivedEventHandler(object sender, MessageEventArgs e);
    
    public delegate void OnMessageSentEventHandler(object sender, MessageEventArgs e);

    public delegate void OnStatusEventHandler(object sender, SyncDeviceStatus status);

    public delegate void OnDeviceConnected(object sender, ISyncDevice syncDevice);

    public delegate void OnConnectionStarted(object sender, ISyncDevice syncDevice);

    public delegate void OnDeviceDisconnected(object sender, ISyncDevice syncDevice);

    public delegate void OnDeviceError(object sender, string error);

    public interface ISyncDevice
    {
        Task SendMessageAsync(string message, string[] recipients=null);

        SyncDeviceStatus Status { get; }

        bool IsHost { get; }

        string SessionName { get; }

        string ServiceName { get;set; }

        string DeviceId { get; }

        IList<ISyncDevice> Connections { get; }

        ILogger Logger { get; }

        event OnMessageReceivedEventHandler OnMessageReceived;
        event OnMessageSentEventHandler OnMessageSent;
        event OnStatusEventHandler OnStatus;
        event OnConnectionStarted OnConnectionStarted;
        event OnDeviceConnected OnDeviceConnected;
        event OnDeviceDisconnected OnDeviceDisconnected;
        event OnDeviceError OnError;

        Task StartAsync(string sessionName, string pin, string reason);

        Task StopAsync(string reason);

        Task RestartAsync(string reason);
    }

}