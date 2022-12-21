using System;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect;

namespace WindowsFormsApp1
{
    public partial class WiFiDirectAdvertiserPanel : ComsPanel
    {
        public WiFiDirectAdvertiserSettings Settings { get; set; }

        public WiFiDirectAdvertiserPanel()
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
