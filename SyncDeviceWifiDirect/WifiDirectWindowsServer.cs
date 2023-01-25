using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect.Services;
using Windows.Storage.Streams;

namespace SyncDevice.Windows.WifiDirect
{
    public class WifiDirectWindowsServer : WifiDirectWindows
    {
        private WiFiDirectServiceAdvertiser advertiser { get; set; }

        public override Task StartAsync(string sessionName, string pin, string reason)
        {
            SessionName = sessionName;
            StartAdvertisement("efm", true, true, "", null, WiFiDirectServiceStatus.Available, 2,
                "AAAAAAAAAA", null, null);

            return Task.CompletedTask;
        }

        private void Stop()
        {
            advertiser?.Stop();
            advertiser = null;

            foreach (var session in connectedSessions)
            {
                session.Dispose();
            }
            connectedSessions.Clear();
        }

        public override Task StopAsync(string reason)
        {
            Stop();

            Status = SyncDeviceStatus.Stopped;

            return Task.CompletedTask;
        }

        public void StartAdvertisement(
                string serviceName,
                bool autoAccept,
                bool preferGO,
                string pin,
                IList<WiFiDirectServiceConfigurationMethod> configMethods,
                WiFiDirectServiceStatus status,
                uint customStatus,
                string serviceInfo,
                string deferredServiceInfo,
                IList<String> prefixList
                )
        {
         //   ThrowIfDisposed();

            // Create Advertiser object for the service
            // NOTE: service name is internally limited to up to 255 bytes in UTF-8 encoding
            // Valid characters include alpha-numeric, '.', '-', and any multi-byte character
            // characters a-z, A-Z are case-insensitive when discovering services
            advertiser = new WiFiDirectServiceAdvertiser(serviceName);

            // Auto-accept services will connect without interaction from advertiser
            // NOTE: if the config method used for a connection requires a PIN, then the advertiser will have to accept the connection
            advertiser.AutoAcceptSession = autoAccept;

            // Set the Group Owner intent to a large value so that the advertiser will try to become the group owner (GO)
            // NOTE: The GO of a P2P connection can connect to multiple clients while the client can connect to a single GO only
            advertiser.PreferGroupOwnerMode = preferGO;

            // Default status is "Available", but services may use a custom status code (value > 1) if applicable
            advertiser.ServiceStatus = status;
            advertiser.CustomServiceStatusCode = customStatus;

            // Service information can be up to 65000 bytes.
            // Service Seeker may explicitly discover this by specifying a short buffer that is a subset of this buffer.
            // If seeker portion matches, then entire buffer is returned, otherwise, the service information is not returned to the seeker
            // This sample uses a string for the buffer but it can be any data
            if (serviceInfo != null && serviceInfo.Length > 0)
            {
                using (var tempStream = new InMemoryRandomAccessStream())
                {
                    using (var serviceInfoDataWriter = new DataWriter(tempStream))
                    {
                        serviceInfoDataWriter.WriteString(serviceInfo);
                        advertiser.ServiceInfo = serviceInfoDataWriter.DetachBuffer();
                    }
                }
            }
            else
            {
                advertiser.ServiceInfo = null;
            }

            // This is a buffer of up to 144 bytes that is sent to the seeker in case the connection is "deferred" (i.e. not auto-accepted)
            // This buffer will be sent when auto-accept is false, or if a PIN is required to complete the connection
            // For the sample, we use a string, but it can contain any data
            if (deferredServiceInfo != null && deferredServiceInfo.Length > 0)
            {
                using (var tempStream = new InMemoryRandomAccessStream())
                {
                    using (var deferredSessionInfoDataWriter = new DataWriter(tempStream))
                    {
                        deferredSessionInfoDataWriter.WriteString(deferredServiceInfo);
                        advertiser.DeferredSessionInfo = deferredSessionInfoDataWriter.DetachBuffer();
                    }
                }
            }
            else
            {
                advertiser.DeferredSessionInfo = null;
            }

            // The advertiser supported configuration methods
            // Valid values are PIN-only (either keypad entry, display, or both), or PIN (keypad entry, display, or both) and WFD Services default
            // WFD Services Default config method does not require explicit PIN entry and offers a more seamless connection experience
            // Typically, an advertiser will support PIN display (and WFD Services Default), and a seeker will connect with either PIN entry or WFD Services Default
            if (configMethods != null)
            {
                advertiser.PreferredConfigurationMethods.Clear();
                foreach (var configMethod in configMethods)
                {
                    advertiser.PreferredConfigurationMethods.Add(configMethod);
                }
            }
            else
                advertiser.PreferredConfigurationMethods.Add(WiFiDirectServiceConfigurationMethod.Default);

            // Advertiser may also be discoverable by a prefix of the service name. Must explicitly specify prefixes allowed here.
            if (prefixList != null && prefixList.Count > 0)
            {
                advertiser.ServiceNamePrefixes.Clear();
                foreach (var prefix in prefixList)
                {
                    advertiser.ServiceNamePrefixes.Add(prefix);
                }
            }

            // This should fire when the service is created and advertisement has started
            // It will also fire when the advertisement has stopped for any reason
            advertiser.AdvertisementStatusChanged += OnAdvertisementStatusChanged;
            // This will fire when an auto-accept session is connected. Advertiser should keep track of the new service session
            advertiser.AutoAcceptSessionConnected += OnAutoAcceptSessionConnected;
            // This will fire when a session is requested and it must be explicitly accepted or rejected.
            // The advertiser may need to display a PIN or take user input for a PIN
            advertiser.SessionRequested += OnSessionRequested;

            Logger?.LogInformation("Starting service...");

            try
            {
                // This may fail if the driver is unable to handle the request or if services is not supported
                // NOTE: this must be called from the UI thread of the app
                advertiser.Start();

                Status = SyncDeviceStatus.Started;
            }
            catch (Exception ex)
            {
                Logger?.LogError(String.Format(CultureInfo.InvariantCulture, "Failed to start service: {0}", ex.Message));
                throw;
            }
        }

        private void OnAdvertisementStatusChanged(WiFiDirectServiceAdvertiser sender, object args)
        {
            try
            {
               // ThrowIfDisposed();

                WiFiDirectServiceAdvertisementStatus status = advertiser.AdvertisementStatus;

                Logger?.LogInformation("Advertisement Status Changed to " + status.ToString());

                //manager.AdvertiserStatusChanged(this);
            }
            catch (Exception ex)
            {
                Logger?.LogError("OnAdvertisementStatusChanged Failed: " + ex.Message);
            }
        }

        // NOTE: this is mutually exclusive with OnSessionRequested
        private void OnAutoAcceptSessionConnected(WiFiDirectServiceAdvertiser sender, WiFiDirectServiceAutoAcceptSessionConnectedEventArgs args)
        {
            try
            {
               // ThrowIfDisposed();

                string sessionInfo = "";
                if (args.SessionInfo != null && args.SessionInfo.Length > 0)
                {
                    using (DataReader sessionInfoDataReader = DataReader.FromBuffer(args.SessionInfo))
                    {
                        sessionInfo = sessionInfoDataReader.ReadString(args.SessionInfo.Length);
                    }
                }

                Logger?.LogInformation("Auto-Accept Session Connected: sessionInfo=" + sessionInfo);

                SessionWrapper sessionWrapper = new SessionWrapper(this, args.Session) { Logger= Logger };
                connectedSessions.Add(sessionWrapper);

                sessionWrapper.AddStreamSocketListenerAsync(9801);
                //sessionWrapper.AddDatagramSocketAsync(55555);
                
                //manager.AddSession(sessionWrapper);                
            }
            catch (Exception ex)
            {
                Logger?.LogError("OnAutoAcceptSessionConnected Failed: " + ex.Message);
            }
        }

        // NOTE: this is mutually exclusive with OnAutoAcceptSessionConnected
        private void OnSessionRequested(WiFiDirectServiceAdvertiser sender, WiFiDirectServiceSessionRequestedEventArgs args)
        {
            try
            {
                // ThrowIfDisposed();

                Logger?.LogInformation("Received session request");

                string sessionInfo = "";
                if (args.GetSessionRequest().SessionInfo != null && args.GetSessionRequest().SessionInfo.Length > 0)
                {
                    using (DataReader sessionInfoDataReader = DataReader.FromBuffer(args.GetSessionRequest().SessionInfo))
                    {
                        sessionInfo = sessionInfoDataReader.ReadString(args.GetSessionRequest().SessionInfo.Length);
                        Logger?.LogInformation("Received Session Info: " + sessionInfo);
                    }
                }
                var r = args.GetSessionRequest();

                Logger?.LogInformation("Accepting session request...");

                // NOTE: This MUST be called from the UI thread
                var sT = advertiser.ConnectAsync(r.DeviceInformation).AsTask();

                sT.Wait();

                var session = sT.Result;
                
                Logger?.LogInformation("Session request accepted");

                SessionWrapper sessionWrapper = new SessionWrapper(this, session) { Logger = Logger };
                connectedSessions.Add(sessionWrapper);
            }
            catch (Exception ex)
            {
                Logger?.LogError("OnSessionRequest Failed: " + ex.Message);
            }
        }
        #region Dispose
        bool disposed = false;

        ~WifiDirectWindowsServer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                Stop();

            }

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("WiFiDirectServiceManager");
            }
        }
        #endregion

    }
}
