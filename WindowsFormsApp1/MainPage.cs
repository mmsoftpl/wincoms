using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.WiFiDirect;
using Windows.UI.Core;
using WindowsFormsApp1;
using static System.Windows.Forms.AxHost;

namespace SDKTemplate
{
    public class MainPage : Form, ILogger<MainPage>
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
        private Button copyToClipboardButton;
        private ToolTip toolTip;
        private System.ComponentModel.IContainer components;
        private Button clearLogButton;
        private Label label2;
        private Label label3;
        private Button buttonBluetoothLePublisher;
        private Button buttonBluetoothLEWatcher;

        public static MainPage mainPage { get; private set; }

        internal readonly ILogger logger;
        public MainPage(ILogger<MainPage> logger)
        {
            this.logger = logger;
            this.InitializeComponent();
            mainPage = this;

        }

        public static void Log(Exception ex)
        {
            mainPage?.logger?.LogError(ex, null);
            mainPage.logToListView(ex.ToString(), null, LogLevel.Error);
        }

        public static void Log(string str, LogLevel loglevel)
        {
            mainPage?.logger?.Log(loglevel, str);
            mainPage.logToListView(str, null, loglevel);
        }

        public static void Log(string str, Exception ex, LogLevel loglevel)
        {
            mainPage?.logger?.Log(loglevel, ex, str);
            mainPage.logToListView(str, null, loglevel);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
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
            this.copyToClipboardButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.clearLogButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonBluetoothLePublisher = new System.Windows.Forms.Button();
            this.buttonBluetoothLEWatcher = new System.Windows.Forms.Button();
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
            this.leftPanel.Size = new System.Drawing.Size(871, 160);
            this.leftPanel.TabIndex = 0;
            // 
            // btnWifiDirectConnector
            // 
            this.btnWifiDirectConnector.AutoSize = true;
            this.btnWifiDirectConnector.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnWifiDirectConnector.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWifiDirectConnector.Location = new System.Drawing.Point(0, 133);
            this.btnWifiDirectConnector.Name = "btnWifiDirectConnector";
            this.btnWifiDirectConnector.Size = new System.Drawing.Size(871, 27);
            this.btnWifiDirectConnector.TabIndex = 11;
            this.btnWifiDirectConnector.Text = "Wi-Fi Direct Client";
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
            this.bntWifiDirectAdevrtiser.Size = new System.Drawing.Size(871, 27);
            this.bntWifiDirectAdevrtiser.TabIndex = 9;
            this.bntWifiDirectAdevrtiser.Text = "Wi-Fi Direct Server";
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
            this.btnBluetoothConnector.Size = new System.Drawing.Size(871, 27);
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
            this.btnBluetoothAdvertiser.Size = new System.Drawing.Size(871, 27);
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
            this.splitter.Location = new System.Drawing.Point(0, 488);
            this.splitter.Name = "splitter";
            this.splitter.Size = new System.Drawing.Size(871, 3);
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
            this.listView1.Location = new System.Drawing.Point(0, 491);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(871, 100);
            this.listView1.TabIndex = 6;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 2000;
            // 
            // copyToClipboardButton
            // 
            this.copyToClipboardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.copyToClipboardButton.AutoSize = true;
            this.copyToClipboardButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.copyToClipboardButton.Location = new System.Drawing.Point(830, 568);
            this.copyToClipboardButton.Name = "copyToClipboardButton";
            this.copyToClipboardButton.Size = new System.Drawing.Size(41, 23);
            this.copyToClipboardButton.TabIndex = 8;
            this.copyToClipboardButton.Text = "Copy";
            this.toolTip.SetToolTip(this.copyToClipboardButton, "Copy log to clipboard");
            this.copyToClipboardButton.UseVisualStyleBackColor = true;
            this.copyToClipboardButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // clearLogButton
            // 
            this.clearLogButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.clearLogButton.AutoSize = true;
            this.clearLogButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.clearLogButton.Location = new System.Drawing.Point(830, 497);
            this.clearLogButton.Name = "clearLogButton";
            this.clearLogButton.Size = new System.Drawing.Size(41, 23);
            this.clearLogButton.TabIndex = 9;
            this.clearLogButton.Text = "Clear";
            this.toolTip.SetToolTip(this.clearLogButton, "Clear log");
            this.clearLogButton.UseVisualStyleBackColor = true;
            this.clearLogButton.Click += new System.EventHandler(this.clearLogButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(0, 160);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(137, 26);
            this.label3.TabIndex = 10;
            this.label3.Text = "Bluetooth LE";
            // 
            // buttonBluetoothLePublisher
            // 
            this.buttonBluetoothLePublisher.AutoSize = true;
            this.buttonBluetoothLePublisher.Dock = System.Windows.Forms.DockStyle.Top;
            this.buttonBluetoothLePublisher.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonBluetoothLePublisher.Location = new System.Drawing.Point(0, 186);
            this.buttonBluetoothLePublisher.Name = "buttonBluetoothLePublisher";
            this.buttonBluetoothLePublisher.Size = new System.Drawing.Size(871, 27);
            this.buttonBluetoothLePublisher.TabIndex = 11;
            this.buttonBluetoothLePublisher.Text = "Bluetooth LE Publisher";
            this.buttonBluetoothLePublisher.UseVisualStyleBackColor = true;
            this.buttonBluetoothLePublisher.Click += new System.EventHandler(this.btnBluetoothLePublisher_Click);
            // 
            // buttonBluetoothLEWatcher
            // 
            this.buttonBluetoothLEWatcher.AutoSize = true;
            this.buttonBluetoothLEWatcher.Dock = System.Windows.Forms.DockStyle.Top;
            this.buttonBluetoothLEWatcher.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonBluetoothLEWatcher.Location = new System.Drawing.Point(0, 213);
            this.buttonBluetoothLEWatcher.Name = "buttonBluetoothLEWatcher";
            this.buttonBluetoothLEWatcher.Size = new System.Drawing.Size(871, 27);
            this.buttonBluetoothLEWatcher.TabIndex = 12;
            this.buttonBluetoothLEWatcher.Text = "Bluetooth LE Watcher";
            this.buttonBluetoothLEWatcher.UseVisualStyleBackColor = true;
            this.buttonBluetoothLEWatcher.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // MainPage
            // 
            this.ClientSize = new System.Drawing.Size(871, 591);
            this.Controls.Add(this.buttonBluetoothLEWatcher);
            this.Controls.Add(this.buttonBluetoothLePublisher);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.clearLogButton);
            this.Controls.Add(this.copyToClipboardButton);
            this.Controls.Add(this.splitter);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.listView1);
            this.Name = "MainPage";
            this.Text = "eFM comms spike";
            this.leftPanel.ResumeLayout(false);
            this.leftPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void ShowPanel(SyncPanel comsPanel)
        {
            Controls.Remove(leftPanel);

            comsPanel.MainPage = this;
            comsPanel.Dock = DockStyle.Fill;
            comsPanel.BringToFront();

            Controls.Add(comsPanel);
        }

        public void logToListView(string message, Exception exception, LogLevel logLevel)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                    listView1.Invoke((MethodInvoker)(() =>
                {
                    try
                    {
                        ListViewItem listViewItem = new ListViewItem(DateTime.UtcNow.ToString() + " | " + message);

                        if (logLevel == LogLevel.Error)
                            listViewItem.ForeColor = System.Drawing.Color.Red;

                        if (exception != null)
                            listViewItem.ToolTipText = exception.ToString();

                        listView1.Items.Add(listViewItem);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

        }

        private void btnBluetoothAdvertiser_Click(object sender, EventArgs e)
        {
            ShowPanel(new BluetoothServerPanel());
        }

        private void btnBluetoothConnector_Click(object sender, EventArgs e)
        {
            ShowPanel(new BluetoothClientPanel());
        }

        private void bntWifiDirectAdevrtiser_Click(object sender, EventArgs e)
        {
            ShowPanel(new WiFiDirectServerPanel());
        }

        private void btnWifiDirectConnector_Click(object sender, EventArgs e)
        {
            ShowPanel(new WiFiDirectClientPanel());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ListViewItem item in listView1.Items)
            {
                if (!string.IsNullOrEmpty(item.Text))
                    sb.AppendLine(item.Text.ToString());

                if (!string.IsNullOrEmpty(item.ToolTipText))
                    sb.AppendLine(item.ToolTipText.ToString());

                sb.AppendLine();
            }
            if (sb.Length > 0)
                Clipboard.SetText(sb.ToString());
        }

        private void clearLogButton_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger?.Log<TState>(logLevel,eventId, state, exception, formatter);
            logToListView(state?.ToString(), exception, logLevel);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger?.IsEnabled(logLevel) == true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope<TState>(state);
        }

        private void btnBluetoothLePublisher_Click(object sender, EventArgs e)
        {
            ShowPanel(new BluetoothLePublisherPanel());
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            ShowPanel(new BluetoothLeWatcherPanel());
        }
    }


}
