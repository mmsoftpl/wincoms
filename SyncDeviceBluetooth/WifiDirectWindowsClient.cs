using Microsoft.Extensions.Logging;
using SyncDevice.Windows.WifiDirect;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect.Services;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using static System.Collections.Specialized.BitVector32;

namespace SyncDevice.Windows.Bluetooth
{
    public class WifiDirectWindowsClient : WifiDirectWindows
    {
        // Keep track of all devices found during previous discovery
        private IList<DiscoveredDevice> discoveredDevices = new List<DiscoveredDevice>();
        private SessionWrapper SessionWrapper = null;

        private void Stop()
        {
            //advertiser?.Stop();
            //advertiser = null;

            //foreach (var session in connectedSessions)
            //{
            //    session.Dispose();
            //}
            //connectedSessions.Clear();
        }

        public override Task StopAsync(string reason)
        {
            Stop();

            Status = SyncDeviceStatus.Stopped;

            return Task.CompletedTask;
        }

        public override Task StartAsync(string reason)
        {
            DiscoverServicesAsync("efm", null);
            return Task.CompletedTask;
        }

        public async void DiscoverServicesAsync(string serviceName, string requestedServiceInfo)
        {
            try
            {
              //  ThrowIfDisposed();

                Logger?.LogInformation("Discover services... (name='" + serviceName + "', requestedInfo='" + requestedServiceInfo + "')");

                // Clear old results
                discoveredDevices.Clear();

                // Discovery depends on whether service information is requested
                string serviceSelector = "";
                if (string.IsNullOrEmpty(requestedServiceInfo))
                {
                    // Just search by name
                    serviceSelector = WiFiDirectService.GetSelector(serviceName);
                }
                else
                {
                    using (var serviceInfoDataWriter = new DataWriter(new InMemoryRandomAccessStream()))
                    {
                        serviceInfoDataWriter.WriteString(requestedServiceInfo);
                        // Discover by name and try to discover service information
                        serviceSelector = WiFiDirectService.GetSelector(serviceName, serviceInfoDataWriter.DetachBuffer());
                    }
                }

                List<string> additionalProperties = new List<string>();
                additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceAddress");
                additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceName");
                additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceInformation");
                additionalProperties.Add("System.Devices.WiFiDirectServices.AdvertisementId");
                additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceConfigMethods");

                // Note: This sample demonstrates finding services with FindAllAsync, which does a discovery and returns a list
                // It is also possible to use DeviceWatcher to receive updates as soon as services are found and to continue the discovery until it is stopped
                // See the DeviceWatcher sample for an example on how to use that class instead of DeviceInformation.FindAllAsync
                DeviceInformationCollection deviceInfoCollection = await DeviceInformation.FindAllAsync(serviceSelector, additionalProperties);

                if (deviceInfoCollection != null && deviceInfoCollection.Count > 0)
                {
                    Logger?.LogInformation("Done discovering services, found " + deviceInfoCollection.Count + " services");

                    foreach (DeviceInformation deviceInfo in deviceInfoCollection)
                    {
                        discoveredDevices.Add(new DiscoveredDevice(deviceInfo));
                    }

                    if (discoveredDevices.Count == 1)
                    {
                        ConnectToSession(discoveredDevices[0]);
                    }
                    else
                    {
                        Logger?.LogInformation("Done discovering services, More than one service found");
                        Status = SyncDeviceStatus.Stopped;
                    }
                }
                else
                {
                    Logger?.LogInformation("Done discovering services, No services found");
                    Status = SyncDeviceStatus.Stopped;
                }

                //// Update UI list
                //if (scenario3 != null)
                //{
                //    scenario3.UpdateDiscoveryList(discoveredDevices);
                //}
            }
            catch (Exception ex)
            {
                Logger?.LogError(String.Format("Failed to discover services: {0}", ex.Message));
                throw ex;
            }
        }

        public async void ConnectToSession(DiscoveredDevice device)
        {
            Logger?.LogInformation("Open session...");

            try
            {
                // NOTE: This MUST be called from the UI thread
                var Service = await WiFiDirectService.FromIdAsync(device.DeviceInfo.Id);
                Service.SessionDeferred += OnSessionDeferred;
                Service.PreferGroupOwnerMode = false;

                Logger?.LogInformation("Connecting...");
                var Session = await Service.ConnectAsync();

                Logger?.LogInformation("Done Connecting");

                // Now we are done with this WiFiDirectService instance
                // Clear state so a new connection can be started
                Service.SessionDeferred -= OnSessionDeferred;
                Service = null;

                SessionWrapper = new SessionWrapper(this, Session);

                Status = SyncDeviceStatus.Started;
            }
            catch (Exception ex)
            {
                Logger?.LogError("ConnectToSession Failed: " + ex.Message);
                Status = SyncDeviceStatus.Stopped;
            }
        }

        private void OnSessionDeferred(WiFiDirectService sender, WiFiDirectServiceSessionDeferredEventArgs args)
        {
            string deferredSessionInfo = "";
            if (args.DeferredSessionInfo != null && args.DeferredSessionInfo.Length > 0)
            {
                using (DataReader sessionInfoDataReader = DataReader.FromBuffer(args.DeferredSessionInfo))
                {
                    deferredSessionInfo = sessionInfoDataReader.ReadString(args.DeferredSessionInfo.Length);
                }
            }

            Logger?.LogInformation("Session Connection was deferred... (" + deferredSessionInfo + ")");
        }


    }
}
