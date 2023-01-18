﻿using Microsoft.Extensions.Logging;
using System;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.Bluetooth
{
    public class BluetoothLePublisher : BluetoothWindows
    {
        public override bool IsHost { get => true; set { } }

        public override string Id => PublisherSingleton?.Value?.Advertisement?.ToString();

        // The Bluetooth LE advertisement publisher class is used to control and customize Bluetooth LE advertising.
        private Lazy<BluetoothLEAdvertisementPublisher> PublisherSingleton = null;

        private BluetoothLEAdvertisementPublisher GetPublisher()
        {
            // Create and initialize a new publisher instance.
            var publisher = new BluetoothLEAdvertisementPublisher();


            // We need to add some payload to the advertisement. A publisher without any payload
            // or with invalid ones cannot be started. We only need to configure the payload once
            // for any publisher.

            // Add a manufacturer-specific section:
            // First, let create a manufacturer data section
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = 0xFFFE;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new DataWriter();
            UInt16 uuidData = 0x1234;
            writer.WriteUInt16(uuidData);

            byte[] bytes = Encoding.ASCII.GetBytes(SdpServiceName.Remove(24));
            writer.WriteBytes(bytes);

            // Make sure that the buffer length can fit within an advertisement payload. Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement publisher:
            publisher.Advertisement.ManufacturerData.Add(manufacturerData);


            return publisher;

            //// Display the information about the published payload
            //PublisherPayloadBlock.Text = string.Format("Published payload information: CompanyId=0x{0}, ManufacturerData=0x{1}",
            //    manufacturerData.CompanyId.ToString("X"),
            //    uuidData.ToString("X"));

            //// Display the current status of the publisher
            //PublisherStatusBlock.Text = string.Format("Published Status: {0}, Error: {1}",
            //    publisher.Status,
            //    BluetoothError.Success);
        }

        public override Task StartAsync(string sessionName, string reason)
        {
            if (PublisherSingleton == null)
            {
                PublisherSingleton = new Lazy<BluetoothLEAdvertisementPublisher>(GetPublisher);
                PublisherSingleton.Value.Start();
                PublisherSingleton.Value.StatusChanged += OnPublisherStatusChanged;
                Logger?.LogInformation("Publisher started.");
                Status = SyncDeviceStatus.Started;
            }
            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            if (PublisherSingleton.IsValueCreated)
            {
                var Publisher = PublisherSingleton.Value;
                PublisherSingleton = null;                

                Publisher.Stop();
                Publisher.StatusChanged -= OnPublisherStatusChanged;

                Logger?.LogInformation("Publisher stopped.");
                Status = SyncDeviceStatus.Stopped;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invoked as an event handler when the status of the publisher changes.
        /// </summary>
        /// <param name="publisher">Instance of publisher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about the publisher status change event.</param>
        private void OnPublisherStatusChanged(
            BluetoothLEAdvertisementPublisher publisher,
            BluetoothLEAdvertisementPublisherStatusChangedEventArgs eventArgs)
        {
            // This event handler can be used to monitor the status of the publisher.
            // We can catch errors if the publisher is aborted by the system
            BluetoothLEAdvertisementPublisherStatus status = eventArgs.Status;
            BluetoothError error = eventArgs.Error;

            Logger?.LogInformation(string.Format("Published Status: {0}, Error: {1}", status.ToString(), error.ToString()));
        }
    }
}