using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.WiFiDirect;
using Windows.UI.Core;
using WindowsFormsApp1;

namespace SDKTemplate
{
    public class MainPage : Form
    {
        private Panel leftPanel;
        private Button btnBluetoothConnector;
        private Button btnBluetoothAdvertiser;
        private Label label1;
        private Splitter splitter;
        private Button btnWifiDirectConnector;
        private Button bntWifiDirectAdevrtiser;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private Label label2;

        public static MainPage mainPage { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            mainPage = this;
        }

        public static void Log(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            mainPage.Log(ex.ToString(), null, NotifyType.ErrorMessage);
        }

        public static void Log(string str, NotifyType notifyType)
        {
            System.Diagnostics.Debug.WriteLine(str);
            mainPage.Log(str, null, notifyType);
        }

        private void InitializeComponent()
        {
            this.leftPanel = new System.Windows.Forms.Panel();
            this.btnWifiDirectConnector = new System.Windows.Forms.Button();
            this.bntWifiDirectAdevrtiser = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btnBluetoothConnector = new System.Windows.Forms.Button();
            this.btnBluetoothAdvertiser = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.splitter = new System.Windows.Forms.Splitter();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.leftPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // leftPanel
            // 
            this.leftPanel.AutoSize = true;
            this.leftPanel.Controls.Add(this.btnWifiDirectConnector);
            this.leftPanel.Controls.Add(this.bntWifiDirectAdevrtiser);
            this.leftPanel.Controls.Add(this.label2);
            this.leftPanel.Controls.Add(this.btnBluetoothConnector);
            this.leftPanel.Controls.Add(this.btnBluetoothAdvertiser);
            this.leftPanel.Controls.Add(this.label1);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.leftPanel.Location = new System.Drawing.Point(0, 0);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Size = new System.Drawing.Size(685, 160);
            this.leftPanel.TabIndex = 0;
            // 
            // btnWifiDirectConnector
            // 
            this.btnWifiDirectConnector.AutoSize = true;
            this.btnWifiDirectConnector.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnWifiDirectConnector.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWifiDirectConnector.Location = new System.Drawing.Point(0, 133);
            this.btnWifiDirectConnector.Name = "btnWifiDirectConnector";
            this.btnWifiDirectConnector.Size = new System.Drawing.Size(685, 27);
            this.btnWifiDirectConnector.TabIndex = 11;
            this.btnWifiDirectConnector.Text = "Connector";
            this.btnWifiDirectConnector.UseVisualStyleBackColor = true;
            this.btnWifiDirectConnector.Click += new System.EventHandler(this.btnWifiDirectConnector_Click);
            // 
            // bntWifiDirectAdevrtiser
            // 
            this.bntWifiDirectAdevrtiser.AutoSize = true;
            this.bntWifiDirectAdevrtiser.Dock = System.Windows.Forms.DockStyle.Top;
            this.bntWifiDirectAdevrtiser.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bntWifiDirectAdevrtiser.Location = new System.Drawing.Point(0, 106);
            this.bntWifiDirectAdevrtiser.Name = "bntWifiDirectAdevrtiser";
            this.bntWifiDirectAdevrtiser.Size = new System.Drawing.Size(685, 27);
            this.bntWifiDirectAdevrtiser.TabIndex = 9;
            this.bntWifiDirectAdevrtiser.Text = "Advertiser";
            this.bntWifiDirectAdevrtiser.UseVisualStyleBackColor = true;
            this.bntWifiDirectAdevrtiser.Click += new System.EventHandler(this.bntWifiDirectAdevrtiser_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(0, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 26);
            this.label2.TabIndex = 10;
            this.label2.Text = "Wi-Fi Direct";
            // 
            // btnBluetoothConnector
            // 
            this.btnBluetoothConnector.AutoSize = true;
            this.btnBluetoothConnector.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnBluetoothConnector.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBluetoothConnector.Location = new System.Drawing.Point(0, 53);
            this.btnBluetoothConnector.Name = "btnBluetoothConnector";
            this.btnBluetoothConnector.Size = new System.Drawing.Size(685, 27);
            this.btnBluetoothConnector.TabIndex = 2;
            this.btnBluetoothConnector.Text = "Bluetooth client";
            this.btnBluetoothConnector.UseVisualStyleBackColor = true;
            this.btnBluetoothConnector.Click += new System.EventHandler(this.btnBluetoothConnector_Click);
            // 
            // btnBluetoothAdvertiser
            // 
            this.btnBluetoothAdvertiser.AutoSize = true;
            this.btnBluetoothAdvertiser.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnBluetoothAdvertiser.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBluetoothAdvertiser.Location = new System.Drawing.Point(0, 26);
            this.btnBluetoothAdvertiser.Name = "btnBluetoothAdvertiser";
            this.btnBluetoothAdvertiser.Size = new System.Drawing.Size(685, 27);
            this.btnBluetoothAdvertiser.TabIndex = 0;
            this.btnBluetoothAdvertiser.Text = "Bluetooth Server";
            this.btnBluetoothAdvertiser.UseVisualStyleBackColor = true;
            this.btnBluetoothAdvertiser.Click += new System.EventHandler(this.btnBluetoothAdvertiser_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 26);
            this.label1.TabIndex = 1;
            this.label1.Text = "Bluetooth";
            // 
            // splitter
            // 
            this.splitter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter.Location = new System.Drawing.Point(0, 418);
            this.splitter.Name = "splitter";
            this.splitter.Size = new System.Drawing.Size(685, 3);
            this.splitter.TabIndex = 5;
            this.splitter.TabStop = false;
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView1.HideSelection = false;
            this.listView1.LabelWrap = false;
            this.listView1.Location = new System.Drawing.Point(0, 421);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(685, 97);
            this.listView1.TabIndex = 6;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 2577;
            // 
            // MainPage
            // 
            this.ClientSize = new System.Drawing.Size(685, 518);
            this.Controls.Add(this.splitter);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.listView1);
            this.Name = "MainPage";
            this.leftPanel.ResumeLayout(false);
            this.leftPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void ShowPanel(ComsPanel wiFiPanel)
        {
            Controls.Remove(leftPanel);

            wiFiPanel.MainPage = this;
            wiFiPanel.Dock = DockStyle.Fill;
            wiFiPanel.BringToFront();

            Controls.Add(wiFiPanel);
        }

        public void Log(string message, Exception exception, NotifyType notifyType)
        {
            if (!string.IsNullOrEmpty(message))
                listView1.Invoke((MethodInvoker)(() =>
            {
                ListViewItem listViewItem = new ListViewItem(message);

                if (notifyType == NotifyType.ErrorMessage)
                    listViewItem.ForeColor = System.Drawing.Color.Red;

                if (exception!= null)
                    listViewItem.ToolTipText = exception.ToString();
                
                    listView1.Items.Add(listViewItem);
            }));
        }

        private void btnBluetoothAdvertiser_Click(object sender, EventArgs e)
        {
            ShowPanel(new BluetoothAdvertiserPanel());
        }

        private void btnBluetoothConnector_Click(object sender, EventArgs e)
        {
            ShowPanel(new BluetoothConnectorPanel());
        }

        private void bntWifiDirectAdevrtiser_Click(object sender, EventArgs e)
        {
            ShowPanel(new WiFiDirectAdvertiserPanel());
        }

        private void btnWifiDirectConnector_Click(object sender, EventArgs e)
        {
            ShowPanel(new WiFiDirectConnectorPanel());
        }
    }


}
