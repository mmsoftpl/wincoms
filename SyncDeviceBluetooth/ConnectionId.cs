using SyncDevice.Windows.Bluetooth;
using System;

namespace SyncDevice.Windows
{
    public class ConnectionId : IComparable
    {
        public readonly string SessionNameA;
        public readonly string SessionNameB;

        protected ConnectionId(string sessionNameA, string sessionNameB)
        {
            if (string.Compare( sessionNameA, sessionNameB) <=0)
                SessionName = sessionNameA + " - " + sessionNameB;
            else
                SessionName = sessionNameB + " - " + sessionNameA;
            SessionNameA = sessionNameA;
            SessionNameB = sessionNameB;
        }

        public static ConnectionId Create(string sesionNameA,string SesionNameB)
        {
            if (string.IsNullOrEmpty(sesionNameA)) throw new ArgumentNullException(nameof(sesionNameA));
            if (string.IsNullOrEmpty(SesionNameB)) throw new ArgumentNullException(nameof(SesionNameB));
            return new ConnectionId(sesionNameA, SesionNameB);
        }

        public static ConnectionId Create(ISyncDevice syncDevice)
        {
            return ConnectionId.Create(
                    syncDevice.SessionName,
                    (syncDevice as BluetoothWindowsChannel).Creator.SessionName);
        }

        public override int GetHashCode() => SessionName.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is ConnectionId peerToPeerConnection)
                return string.Compare(SessionName, peerToPeerConnection.SessionName) == 0;
            return false;
        }

        public int CompareTo(object obj)
        {
            if (obj is ConnectionId peerToPeerConnection)
                return string.Compare(SessionName, peerToPeerConnection.SessionName);
            return -1;
        }

        public string SessionName { get; private set; }
    }
}
