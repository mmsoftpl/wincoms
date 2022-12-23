//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect;

namespace SyncDevice.Windows
{
    public class ConnectedDevice : IDisposable
    {
        public SocketReaderWriter SocketRW { get; }

        public SocketWrapper SocketWrapper { get; set; }

        public WiFiDirectDevice WfdDevice { get; }
        public string DisplayName { get; }

        public ConnectedDevice(string displayName, WiFiDirectDevice wfdDevice, SocketReaderWriter socketRW, SocketWrapper socketWrapper)
        {
            DisplayName = displayName;
            WfdDevice = wfdDevice;
            SocketRW = socketRW;
            SocketWrapper = socketWrapper;
        }

        public override string ToString() => DisplayName;

        public void Dispose()
        {
            // Close socket
            SocketRW.Dispose();

            // Close WiFiDirectDevice object
            WfdDevice.Dispose();
        }

        public async Task WriteMessageAsync(string message)
        {
            if (SocketRW != null)
                await SocketRW.WriteMessageAsync(message);
            if (SocketWrapper != null)
                await SocketWrapper.SendMessageAsync(message);
        }

        public async Task<string> ReadMessageAsync()
        {
            if (SocketRW != null)
                return await SocketRW.ReadMessageAsync();
          //  if (SocketWrapper != null)
            //    return await SocketWrapper.ReceiveMessageAsync();
            return null;
        }
    }
}
