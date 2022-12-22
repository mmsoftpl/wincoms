using System;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect;

namespace WindowsFormsApp1
{
    public partial class WiFiDirectServerPanel : ComsPanel
    {
        public WiFiDirectAdvertiserSettings Settings { get; set; }

        public WiFiDirectServerPanel()
        {
            InitializeComponent();
        }

        public override void FindDevices()
        {
            Status = WiFiDirectAdvertisementPublisherStatus.Created;
            _ = Connect();
             
            UpdateControls();
        }

        public async Task Connect()
        {
            await Task.Delay(3000);

            Status = WiFiDirectAdvertisementPublisherStatus.Started;
            UpdateControls();
        }
    }
}
