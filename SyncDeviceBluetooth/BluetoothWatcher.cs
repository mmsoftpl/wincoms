using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace SyncDevice.Windows.Bluetooth
{
    public class DeviceInformationDetails
    {
        public DeviceInformation DeviceInformation { get; set; }
        public DateTime LastStamp { get; set; }
    }

    public class BluetoothWatcher : BluetoothWindows
    {
        public override bool IsHost { get => false; }

        public ConnectStrategy ConnectStrategy = ConnectStrategy.ScanServices;

        private bool enumerationCompleted = false;
        private DeviceWatcher deviceWatcher = null;

        public event EventHandler<IEnumerable<DeviceInformationDetails>> OnChanged;

        public ConcurrentDictionary<string, DeviceInformationDetails> DevicesCollection
        {
            get;
            private set;
        } = new ConcurrentDictionary<string, DeviceInformationDetails>();


        public override Task StartAsync(string sessionName, string pin, string reason)
        {
            if (Status != SyncDeviceStatus.Started)
            {
                enumerationCompleted = false;
                Status = SyncDeviceStatus.Started;
                SessionName = sessionName;
                StartWatcher();
            }
            return Task.CompletedTask;
        }

        public override Task StopAsync(string reason)
        {
            StopWatcher();
            return Task.CompletedTask;
        }

        SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(1);

        CancellationTokenSource StartConnectToHostCancelationTokenSource;
        private async Task ResultCollectionHasChanged()
        {
            StartConnectToHostCancelationTokenSource?.Cancel();

            StartConnectToHostCancelationTokenSource = new CancellationTokenSource();
            var token = StartConnectToHostCancelationTokenSource.Token;

            await concurrencySemaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return;

                if (enumerationCompleted)
                    await Task.Delay(TimeSpan.FromSeconds(5), token);

                if (!token.IsCancellationRequested)
                {
                    Logger?.LogInformation("RaiseOnConnectionStarted");
                    OnChanged.Invoke(this, DevicesCollection.Values);
                }
            }
            finally
            {
                concurrencySemaphore.Release();
            }
        }

        private bool AddDeviceInformation(DeviceInformation deviceInfo)
        {
            if (ConnectStrategy == ConnectStrategy.ScanDevices)
            {
                var deviceInformationDetails = new DeviceInformationDetails() { DeviceInformation = deviceInfo, LastStamp = DateTime.UtcNow };
                if (DevicesCollection.TryAdd(deviceInfo.Id, deviceInformationDetails))
                {
                    Logger?.LogInformation($"[Device added] {deviceInfo.Id}, {deviceInfo.Name}");
                    return true;
                }
              //  else
               //     Logger?.LogWarning($"[Device NOT added] {deviceInfo.Id}, {deviceInfo.Name}");
            }
            else
            if (HasServiceName(deviceInfo.Name) && deviceInfo.Id.Contains("RFCOMM"))
            {
                var deviceInformationDetails = new DeviceInformationDetails() { DeviceInformation = deviceInfo, LastStamp = DateTime.UtcNow };
                if (DevicesCollection.TryAdd(deviceInfo.Id, deviceInformationDetails))
                {
                    Logger?.LogInformation($"[Service added] {deviceInfo.Id}, {deviceInfo.Name}");
                    return true;
                }
               // else
                //    Logger?.LogWarning($"[Service NOT added] {deviceInfo.Id}, {deviceInfo.Name}");
            }
            
            return false;
        }

        private bool UpdateDeviceInformation(DeviceInformationUpdate deviceInfoUpdate)
        {
            if (DevicesCollection.TryGetValue(deviceInfoUpdate.Id, out var exitingInfo))
            {
                exitingInfo.DeviceInformation.Update(deviceInfoUpdate);
                exitingInfo.LastStamp = DateTime.UtcNow;

                Logger?.LogInformation($"[Device updated] {exitingInfo.DeviceInformation.Id}, {exitingInfo.DeviceInformation.Name}");

                return true;
            }
            else
            {
               // Logger?.LogWarning($"[Device NOT updated] {deviceInfoUpdate.Id}");
                return false;
            }
        }

        private bool RemoveDeviceInformation(DeviceInformationUpdate deviceInfoUpdate)
        {
            if (DevicesCollection.TryRemove(deviceInfoUpdate.Id, out var exitingInfo))
            {
                Logger?.LogInformation($"[Device removed] {exitingInfo.DeviceInformation.Id}, {exitingInfo.DeviceInformation.Name}");

                return true;
            }
            else
            {
              //  Logger?.LogWarning($"[Device NOT removed] {deviceInfoUpdate.Id}");
                return false;
            }
        }

        public void StartWatcher()
        {
            if (deviceWatcher != null)
                return;

            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            //string asqFilter = $"(System.Devices.AepService.ProtocolId:=\"{BluetoothProtocolId}\" AND\r\nSystem.Devices.AepService.ServiceClassId:=\"{RfcommChatServiceUuid}\")";
            string asqFilter = $"(System.Devices.AepService.ProtocolId:=\"{{{BluetoothProtocolId}}}\")";

            if (ConnectStrategy == ConnectStrategy.ScanDevices)
            {
                Logger?.LogInformation("Scaning for devices (slow)");
                deviceWatcher = DeviceInformation.CreateWatcher(asqFilter,
                                                                requestedProperties,
                                                                DeviceInformationKind.AssociationEndpoint);
            }
            else
            if (ConnectStrategy == ConnectStrategy.ScanServices)
            {
                Logger?.LogInformation("Scaning for services (quick)");
                deviceWatcher = DeviceInformation.CreateWatcher(asqFilter,
                                                                requestedProperties,
                                                                DeviceInformationKind.AssociationEndpointService);
            }

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>((watcher, deviceInfo) =>
            {
                if (AddDeviceInformation(deviceInfo)) 
                    _ = ResultCollectionHasChanged();
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (UpdateDeviceInformation(deviceInfoUpdate))
                    _ = ResultCollectionHasChanged();                
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                if (RemoveDeviceInformation(deviceInfoUpdate))
                    _ = ResultCollectionHasChanged();
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, object>((watcher, obj) =>
            {
                if (ConnectStrategy == ConnectStrategy.ScanServices)
                    Logger?.LogInformation($"[Enumeration completed] {DevicesCollection.Count} services found");
                else
                    Logger?.LogInformation($"[Enumeration completed] {DevicesCollection.Count} devices found");
                
                enumerationCompleted = true;
                _ = ResultCollectionHasChanged();
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, object>((watcher, obj) =>
            {
                DevicesCollection.Clear();
            });

            deviceWatcher.Start();
        }


        private void StopWatcher()
        {
            var w = deviceWatcher;

            if (null != w)
            {
                if ((DeviceWatcherStatus.Started == w.Status ||
                     DeviceWatcherStatus.EnumerationCompleted == w.Status))
                {
                    Logger?.LogTrace("Stopping DeviceWatcher");
                    w.Stop();
                }
                deviceWatcher = null;
            }
        }
    }
}
