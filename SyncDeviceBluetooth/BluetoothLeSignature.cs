using SyncDevice.Windows.Bluetooth;
using System.Threading.Tasks;

namespace SyncDevice.Windows.Bluetooth
{ 
    public class BluetoothLeSignature : BluetoothWindows
    {
        public BluetoothLeSignature()
        {

        }

        public override bool IsHost => false;

        public override Task StartAsync(string sessionName, string pin, string reason)
        {
            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            return Task.CompletedTask;
        }
    }
}
