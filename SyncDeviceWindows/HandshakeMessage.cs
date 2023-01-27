namespace SyncDevice.Windows
{
    public class HandshakeMessage
    {
        const int PinLenght = 4;

        public string Pin { get; set; }
        public string SessionName { get; set; }

        public static string EncodeMessage(string pin, string sessionName)
        {
            return (pin ?? string.Empty).PadLeft(PinLenght) + sessionName;
        }

        public static HandshakeMessage DecodeMessage(string msg)
        {
            if (!string.IsNullOrEmpty(msg) && msg.Length> PinLenght)
            {
                return new HandshakeMessage()
                {
                    Pin = msg.Substring(0, PinLenght),
                    SessionName = msg.Substring(PinLenght, msg.Length - PinLenght)
                };
            }
            return null;
        }
    }
}
