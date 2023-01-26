using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothLeWatcher : BluetoothWindows
    {
        public override bool IsHost { get => false; }

        public readonly ConcurrentDictionary<ulong, string> Signatures = new ConcurrentDictionary<ulong, string>();

        // The Bluetooth LE advertisement publisher class is used to control and customize Bluetooth LE advertising.
        private Lazy<BluetoothLEAdvertisementWatcher> WatcherSingleton = null;

        private BluetoothLEAdvertisementWatcher GetWatcher()
        {

            // Create and initialize a new watcher instance.
            var watcher = new BluetoothLEAdvertisementWatcher();

            // Begin of watcher configuration. Configure the advertisement filter to look for the data advertised by the publisher 
            // in Scenario 2 or 4. You need to run Scenario 2 on another Windows platform within proximity of this one for Scenario 1 to 
            // take effect. The APIs shown in this Scenario are designed to operate only if the App is in the foreground. For background
            // watcher operation, please refer to Scenario 3.

            // Please comment out this following section (watcher configuration) if you want to remove all filters. By not specifying
            // any filters, all advertisements received will be notified to the App through the event handler. You should comment out the following
            // section if you do not have another Windows platform to run Scenario 2 alongside Scenario 1 or if you want to scan for 
            // all LE advertisements around you.

            // For determining the filter restrictions programatically across APIs, use the following properties:
            //      MinSamplingInterval, MaxSamplingInterval, MinOutOfRangeTimeout, MaxOutOfRangeTimeout

            // Part 1A: Configuring the advertisement filter to watch for a particular advertisement payload

            // First, let create a manufacturer data section we wanted to match for. These are the same as the one 
            // created in Scenario 2 and 4.
            var manufacturerData = new BluetoothLEManufacturerData
            {
                // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
                CompanyId = 0xFFFE
            };

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new DataWriter();
            writer.WriteUInt16(0x1234);

            // Make sure that the buffer length can fit within an advertisement payload. Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);


            // Part 1B: Configuring the signal strength filter for proximity scenarios

            // Configure the signal strength filter to only propagate events when in-range
            // Please adjust these values if you cannot receive any advertisement 
            // Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
            // will start to be considered "in-range".
            watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;

            // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
            // to determine when an advertisement is no longer considered "in-range"
            watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;

            // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
            // to determine when an advertisement is no longer considered "in-range"
            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);

            // By default, the sampling interval is set to zero, which means there is no sampling and all
            // the advertisement received is returned in the Received event

            // End of watcher configuration. There is no need to comment out any code beyond this point.
            watcher.Received += OnAdvertisementReceived;
            watcher.Stopped += OnAdvertisementWatcherStopped;

            return watcher;
        }

        public override async Task StartAsync(string sessionName, string pin, string reason)
        {
            if (WatcherSingleton == null)
            {
                WatcherSingleton = new Lazy<BluetoothLEAdvertisementWatcher>(GetWatcher);

                await BluetoothStartAction(() =>
                {
                    WatcherSingleton.Value.Start();
                    Logger?.LogInformation($"BluetoothLeWatcher started, {reason}");
                    Status = SyncDeviceStatus.Started;
                    return Task.Run(() => true);
                });
            }
        }

        public override Task StopAsync(string reason)
        {
            if (WatcherSingleton.IsValueCreated)
            {
                var watcher = WatcherSingleton.Value;
                WatcherSingleton = null;                

                watcher.Stop();
                watcher.Received -= OnAdvertisementReceived;
                watcher.Stopped -= OnAdvertisementWatcherStopped;

                Logger?.LogInformation("BluetoothLeWatcher stopped.");
                Status = SyncDeviceStatus.Stopped;
            }
            return Task.CompletedTask;
        }

        public string GetSignature(string macAddress)
        {
            foreach(var signature in Signatures.Values)
                if (signature.Contains(macAddress))
                    return signature;
            return null;
        }

        /// <summary>
        /// Invoked as an event handler when an advertisement is received.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about the advertisement event.</param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {

            if (eventArgs.BluetoothAddress == ThisBluetoothAddress) //is this needed?
                return;

            // We can obtain various information about the advertisement we just received by accessing 
            // the properties of the EventArgs class

            // The timestamp of the event
            DateTimeOffset timestamp = eventArgs.Timestamp;

            // The type of advertisement
            BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;

            // The received signal strength indicator (RSSI)
            Int16 rssi = eventArgs.RawSignalStrengthInDBm;

            // The local name of the advertising device contained within the payload, if any
            string localName = eventArgs.Advertisement.LocalName;

            // Check if there are any manufacturer-specific sections.
            // If there is, print the raw data of the first manufacturer section (if there are multiple).
            string manufacturerDataString = "";
            var manufacturerSections = eventArgs.Advertisement.ManufacturerData;
            if (manufacturerSections.Count > 0)
            {
                // Only print the first one of the list
                var manufacturerData = manufacturerSections[0];
                var data = new byte[manufacturerData.Data.Length];
                using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                {
                    reader.ReadBytes(data);
                }

                string s = Encoding.ASCII.GetString(data, 2, data.Length - 2);

                if (HasServiceName(s))
                {
                    bool updated;
                    if (Signatures.TryGetValue(eventArgs.BluetoothAddress, out var existingSignature) && existingSignature != s)
                        updated = Signatures.TryUpdate(eventArgs.BluetoothAddress, s, existingSignature);
                    else
                        updated = Signatures.TryAdd(eventArgs.BluetoothAddress, s);

                    if (updated)
                        RaiseOnMessage(s);
                }
            }

        }

        /// <summary>
        /// Invoked as an event handler when the watcher is stopped or aborted.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about why the watcher stopped or aborted.</param>
        private void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            // Notify the user that the watcher was stopped
            Logger?.LogInformation(string.Format("Watcher stopped or aborted: {0}", eventArgs.Error.ToString()));
        }

    }
}
