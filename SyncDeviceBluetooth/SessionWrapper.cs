using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect.Services;
using Windows.Networking.Sockets;

namespace SyncDevice.Windows.WifiDirect
{
    public class SessionWrapper : IDisposable
    {
        private WiFiDirectServiceSession session;
        
        //// Used to update main state
        private WifiDirectWindows WifiDirectWindows = null;

        // Stream Socket Listeners are created when locally opening TCP ports
        private IList<StreamSocketListener> streamSocketListeners = new List<StreamSocketListener>();

        // Keep a list of connected sockets
        private IList<SocketWrapper> socketList = new List<SocketWrapper>();

        // Used to wait for the session to close before cleaning up the wrapper
        private AutoResetEvent sessionClosedEvent = new AutoResetEvent(false);

        public ILogger Logger { get; set; }

        public SessionWrapper(WifiDirectWindows wifiDirectWindows,
            WiFiDirectServiceSession session)
        {
            this.session = session;
            this.WifiDirectWindows= wifiDirectWindows;

            this.session.SessionStatusChanged += OnSessionStatusChanged;
            this.session.RemotePortAdded += OnRemotePortAdded;
        }

        public WiFiDirectServiceSession Session
        {
            get { return session; }
        }

        public uint AdvertisementId
        {
            get { return session.AdvertisementId; }
        }
        public uint SessionId
        {
            get { return session.SessionId; }
        }

        public string ServiceAddress
        {
            get { return session.ServiceAddress; }
        }

        public string SessionAddress
        {
            get { return session.SessionAddress; }
        }

        public IList<SocketWrapper> SocketList
        {
            get { return socketList; }
        }

        public void Close()
        {
            //  ThrowIfDisposed();

            foreach (var socket in socketList)
            {
                socket.Dispose();
            }
            socketList.Clear();

            session.Dispose();

            // Wait for status change
            // NOTE: this should complete in 5 seconds under normal circumstances
            if (!sessionClosedEvent.WaitOne(60000))
            {
                throw new Exception("Timed out waiting for session to close");
            }
        }

        public void AddStreamSocketInternal(StreamSocket socket)
        {
            SocketWrapper socketWrapper = new SocketWrapper(Logger, socket, null, true);

            // Start receiving messages recursively
            var rcv = socketWrapper.ReceiveMessageAsync();

            socketList.Add(socketWrapper);
            // Update manager so UI can add to list
           // manager.AddSocket(socketWrapper);
        }

        public async void AddStreamSocketListenerAsync(UInt16 port)
        {
            //   ThrowIfDisposed();

            Logger?.LogInformation("Adding stream socket listener for TCP port " + port);

            var endpointPairCollection = session.GetConnectionEndpointPairs();

            // Create listener for socket connection (and add to list to cleanup later)
            StreamSocketListener listenerSocket = new StreamSocketListener();

            listenerSocket.ConnectionReceived += (sender, args) =>
            {
                Logger?.LogInformation("Connection received for TCP Port " + sender.Information.LocalPort);
                AddStreamSocketInternal(args.Socket);
            };

            try
            {
                Logger?.LogInformation("BindEndpointAsync...");
                await listenerSocket.BindEndpointAsync(endpointPairCollection[0].LocalHostName, Convert.ToString(port, CultureInfo.InvariantCulture));
                Logger?.LogInformation("BindEndpointAsync Done");

                Logger?.LogInformation("AddStreamSocketListenerAsync...");
                await session.AddStreamSocketListenerAsync(listenerSocket);
                Logger?.LogInformation("AddStreamSocketListenerAsync Done");

               // WifiDirectWindows.RaiseOnDeviceConnected(session.SessionAddress);
            }
            catch (Exception ex)
            {
                Logger?.LogError("AddStreamSocketListenerAsync Failed: " + ex.Message);
            }

            streamSocketListeners.Add(listenerSocket);
        }

        private void ListenerSocket_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            throw new NotImplementedException();
        }

        public async void AddDatagramSocketAsync(UInt16 port)
        {
            ThrowIfDisposed();

            Logger?.LogInformation("Adding stream socket listener for UDP port " + port);

            var endpointPairCollection = session.GetConnectionEndpointPairs();

            DatagramSocket socket = new DatagramSocket();
            // Socket is "read-only", cannot send data but can receive data
            // Expectation is that application starts a listening socket, then remote device sends data
            SocketWrapper socketWrapper = new SocketWrapper(Logger, null, socket, false);

            StreamSocketListener listenerSocket = new StreamSocketListener();

            try
            {
                // Bind UDP socket for receiving messages (peer should call connect and send messages to this socket)
                Logger?.LogInformation("BindEndpointAsync...");
                await socket.BindEndpointAsync(endpointPairCollection[0].LocalHostName, Convert.ToString(port, CultureInfo.InvariantCulture));
                Logger?.LogInformation("BindEndpointAsync Done");

                Logger?.LogInformation("AddDatagramSocketAsync...");
                await session.AddDatagramSocketAsync(socket);
                Logger?.LogInformation("AddDatagramSocketAsync Done");

                //// Update manager so UI can add to list
                //manager.AddSocket(socketWrapper);

               // WifiDirectWindows.RaiseOnDeviceConnected(session.SessionAddress);
            }
            catch (Exception ex)
            {
                Logger?.LogError("AddDatagramSocketAsync Failed: " + ex.Message);
            }

            socketList.Add(socketWrapper);
        }

        // This will fire when the connected peer attempts to open a port for this connection
        // The peer should start a stream socket listener (for TCP)
        private async void OnRemotePortAdded(WiFiDirectServiceSession sender, WiFiDirectServiceRemotePortAddedEventArgs args)
        {
            try
            {
                ThrowIfDisposed();

                var endpointPairCollection = args.EndpointPairs;
                var protocol = args.Protocol;
                if (endpointPairCollection.Count == 0)
                {
                    Logger?.LogInformation("No endpoint pairs for remote port added event");
                    return;
                }

                Logger?.LogInformation(String.Format("{0} Port {1} Added by remote peer",
                    (protocol == WiFiDirectServiceIPProtocol.Tcp) ? "TCP" : ((protocol == WiFiDirectServiceIPProtocol.Udp) ? "UDP" : "???"),
                    endpointPairCollection[0].RemoteServiceName
                    )
                    );

                SocketWrapper socketWrapper = null;

                if (args.Protocol == WiFiDirectServiceIPProtocol.Tcp)
                {
                    // Connect to the stream socket listener
                    StreamSocket streamSocket = new StreamSocket();
                    socketWrapper = new SocketWrapper(Logger, streamSocket, null, true);

                    Logger?.LogInformation("Connecting to stream socket...");
                    await streamSocket.ConnectAsync(endpointPairCollection[0]);
                    // Start receiving messages recursively
                    var rcv = socketWrapper.ReceiveMessageAsync();
                    Logger?.LogInformation("Stream socket connected");
                }
                else if (args.Protocol == WiFiDirectServiceIPProtocol.Udp)
                {
                    // Connect a socket over UDP
                    DatagramSocket datagramSocket = new DatagramSocket();
                    socketWrapper = new SocketWrapper(Logger, null, datagramSocket, true);

                    Logger?.LogInformation("Connecting to datagram socket...");
                    await datagramSocket.ConnectAsync(endpointPairCollection[0]);
                    Logger?.LogInformation("Datagram socket connected");
                }
                else
                {
                    Logger?.LogError("Bad protocol for remote port added event");
                    return;
                }

                socketList.Add(socketWrapper);
                //// Update manager so UI can add to list
                //manager.AddSocket(socketWrapper);
            }
            catch (Exception ex)
            {
                Logger?.LogError("OnRemotePortAdded Failed: " + ex.Message);
            }
        }
        private void OnSessionStatusChanged(WiFiDirectServiceSession sender, object args)
        {
            try
            {
                ThrowIfDisposed();

                Logger?.LogInformation("Session Status Changed to " + session.Status.ToString());

                if (session.Status == WiFiDirectServiceSessionStatus.Closed)
                {
                    sessionClosedEvent.Set();

                   // WifiDirectWindows.RaiseOnDeviceDisconnected(session.SessionAddress);
                    //// Cleanup
                    //manager.RemoveSession(this);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError("OnSessionStatusChanged Failed: " + ex.Message);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                foreach (var socket in socketList)
                {
                    await socket.SendMessageAsync(message);
                }
            }
        }

        #region Dispose
        bool disposed = false;

        ~SessionWrapper()
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
                // This will close the session
                session.Dispose();

                // Remove event handlers
                session.SessionStatusChanged -= OnSessionStatusChanged;
                session.RemotePortAdded -= OnRemotePortAdded;

                foreach (var listener in streamSocketListeners)
                {
                    listener.Dispose();
                }
                streamSocketListeners.Clear();

                foreach (var socket in socketList)
                {
                    socket.Dispose();
                }
                socketList.Clear();

                sessionClosedEvent.Dispose();
            }

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("SessionWrapper");
            }
        }
        #endregion
    }
}
