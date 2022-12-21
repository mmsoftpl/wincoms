using SDKTemplate;
using System;
using System.Windows.Forms;
using Windows.Devices.WiFiDirect;

namespace WindowsFormsApp1
{
    public class ComsPanel : UserControl, IComsPanel
    {
        private ProgressBar progressBar;
        private Button button;
        protected Label headerLabel;
        private Label messageLabel;

        public MainPage MainPage { get; set; }

        private WiFiDirectAdvertisementPublisherStatus status = WiFiDirectAdvertisementPublisherStatus.Stopped;
        public WiFiDirectAdvertisementPublisherStatus Status
        {
            get => status;
            set
            {
                status = value;
                UpdateControls();
            }
        }

        public virtual void OnUpdateControls()
        {
            switch (Status)
            {
                case WiFiDirectAdvertisementPublisherStatus.Stopped:
                    button.Enabled = true;
                    button.Text = "Start";
                    break;
                case WiFiDirectAdvertisementPublisherStatus.Created:
                    button.Text = "...starting...";
                    button.Enabled = false;
                    break;
                case WiFiDirectAdvertisementPublisherStatus.Aborted:
                    button.Text = "...stopping...";
                    button.Enabled = false;
                    break;
                case WiFiDirectAdvertisementPublisherStatus.Started:
                    button.Enabled = true;
                    button.Text = "Stop";
                    break;
            }

            progressBar.Visible = Status == WiFiDirectAdvertisementPublisherStatus.Created;
        }    

        public void UpdateControls() {

            Invoke((MethodInvoker)(() =>
            {
                OnUpdateControls();
            }));
        }

        public virtual string Value
        {
            get => messageLabel.Text;
            set
            {
                Invoke((MethodInvoker)(() =>
                {
                    messageLabel.Text = value;
                }));
            }
        }

        public ComsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.button = new System.Windows.Forms.Button();
            this.headerLabel = new System.Windows.Forms.Label();
            this.messageLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar.Location = new System.Drawing.Point(0, 49);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(471, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 10;
            this.progressBar.Value = 1;
            this.progressBar.Visible = false;
            // 
            // button
            // 
            this.button.Dock = System.Windows.Forms.DockStyle.Top;
            this.button.Location = new System.Drawing.Point(0, 26);
            this.button.Name = "button";
            this.button.Size = new System.Drawing.Size(471, 23);
            this.button.TabIndex = 9;
            this.button.Text = "Connect";
            this.button.UseVisualStyleBackColor = true;
            this.button.Click += new System.EventHandler(this.button_Click);
            // 
            // headerLabel
            // 
            this.headerLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(0, 0);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(471, 26);
            this.headerLabel.TabIndex = 8;
            this.headerLabel.Text = "Header";
            this.headerLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // messageLabel
            // 
            this.messageLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messageLabel.Location = new System.Drawing.Point(0, 72);
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Size = new System.Drawing.Size(471, 141);
            this.messageLabel.TabIndex = 11;
            this.messageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ComsPanel
            // 
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.button);
            this.Controls.Add(this.headerLabel);
            this.Name = "ComsPanel";
            this.Size = new System.Drawing.Size(471, 213);
            this.ResumeLayout(false);

        }

        private void button_Click(object sender, EventArgs e)
        {
            if (Status == WiFiDirectAdvertisementPublisherStatus.Stopped)
            {
                Status = WiFiDirectAdvertisementPublisherStatus.Created;
                FindDevices();
            }
            else
            if (Status == WiFiDirectAdvertisementPublisherStatus.Started)
            {
                Status = WiFiDirectAdvertisementPublisherStatus.Aborted;
                Disconnect("Disconnect requested by user");
            }
            UpdateControls();
        }

        public virtual void FindDevices()
        {

        }

        public virtual void Disconnect(string reason)
        {

        }
    }
}
