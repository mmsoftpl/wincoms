using SDKTemplate;
using System;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage.Streams;
using System.Collections.Concurrent;

namespace WindowsFormsApp1
{
    public partial class BluetoothPanel : ComsPanel
    {
        // The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
        public static readonly Guid RfcommChatServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

        // The Id of the Service Name SDP attribute
        public const UInt16 SdpServiceNameAttributeId = 0x100;

        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.
        public const byte SdpServiceNameAttributeType = (4 << 3) | 5;

        // The value of the Service Name SDP attribute
        public const string SdpServiceName = "Bluetooth eFM Service";

        protected ConcurrentDictionary<string, DataWriter> Writers = new ConcurrentDictionary<string, DataWriter>();

        protected void ClearWriters()
        {
            foreach (var w in Writers.Keys)
            {
                if (Writers.TryRemove(w, out var writer))
                {
                    writer.DetachStream();
                }
            }

            Writers.Clear();
        }

        public BluetoothPanel()
        {
            InitializeComponent();
        }

        protected async Task KeepWriting()
        {
            int i = 0;
            while (Writers.Count>0)
            {
                if (ShouldSendMessages)
                {
                    string msg = (++i).ToString();

                    foreach (var writer in Writers.Values)
                    {
                        await Utils.SendMessageAsync(writer, msg);
                        RecordSentMessage(msg);
                    }
                }

                Thread.Sleep(MessagesInterval);
            }
        }

    }
}
