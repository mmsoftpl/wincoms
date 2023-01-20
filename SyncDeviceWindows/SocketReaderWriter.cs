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

using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyncDevice.Windows
{

    /// Based on https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/WiFiDirect/cs/SocketReaderWriter.cs
    ///    
    public class SocketReaderWriter : IDisposable
    {
        readonly DataReader _dataReader;
        readonly DataWriter _dataWriter;
        readonly StreamSocket _streamSocket;
        readonly ILogger _logger;

        public SocketReaderWriter(StreamSocket socket, ILogger logger)
        {
            _dataReader = new DataReader(socket.InputStream)
            {
                UnicodeEncoding = UnicodeEncoding.Utf8,
                ByteOrder = ByteOrder.LittleEndian
            };

            _dataWriter = new DataWriter(socket.OutputStream)
            {
                UnicodeEncoding = UnicodeEncoding.Utf8,
                ByteOrder = ByteOrder.LittleEndian
            };

            _streamSocket = socket;
            _logger = logger;
        }

        public void Dispose()
        {
            _dataReader.Dispose();
            _dataWriter.Dispose();
            _streamSocket.Dispose();
        }

        public async Task WriteMessageAsync(string message)
        {
            try
            {
                _dataWriter.WriteUInt32(_dataWriter.MeasureString(message));
                _dataWriter.WriteString(message);
                await _dataWriter.StoreAsync();
              //  _logger?.LogInformation("Sent message: " + message);
            }
            catch (Exception ex)
            {
                _logger?.LogError("WriteMessage threw exception: " + ex.Message);
            }
        }

        public async Task<string> ReadMessageAsync()
        {
            try
            {
                UInt32 bytesRead = await _dataReader.LoadAsync(sizeof(UInt32));
                if (bytesRead > 0)
                {
                    // Determine how long the string is.
                    UInt32 messageLength = _dataReader.ReadUInt32();
                    bytesRead = await _dataReader.LoadAsync(messageLength);
                    if (bytesRead > 0)
                    {
                        // Decode the string.
                        string message = _dataReader.ReadString(messageLength);
                        _logger?.LogInformation("Got message: " + message);
                        return message;
                    }
                }
            }
            catch (Exception)
            {
                _logger?.LogError("Socket was closed!");
            }
            return null;
        }
    }
}
