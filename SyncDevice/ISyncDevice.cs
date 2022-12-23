using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SyncDevice
{

    public class MessageEventArgs
    {
        public string Message { get; set; }
    }

    public enum SyncDeviceStatus
    {
        Stopped,
        Created,
        Started,        
        Aborted,        
    }

    public delegate void OnMessageEventHandler(object sender, MessageEventArgs e);

    public delegate void OnStatusEventHandler(object sender, SyncDeviceStatus status);

    public delegate void OnDeviceConnected(object sender, string deviceId);

    public delegate void OnDeviceDisconnected(object sender, string deviceId);

    public interface ISyncDevice
    {        
        Task SendMessageAsync(string message);

        SyncDeviceStatus Status { get; }

        ILogger Logger { get; }

        event OnMessageEventHandler OnMessage;
        event OnStatusEventHandler OnStatus;
        event OnDeviceConnected OnDeviceConnected;
        event OnDeviceDisconnected OnDeviceDisconnected;

        Task StartAsync(string reason);

        Task StopAsync(string reason);
    }

}