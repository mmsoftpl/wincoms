using SDKTemplate;
using SyncDevice;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class SyncPanel : UserControl
    {
        private ProgressBar progressBar;
        private Button button;
        protected Label headerLabel;
        private Panel lastMessagePanel;
        private Label lastMessageSentTextBox;
        private Label lastMessageLabel;
        private Panel panel1;
        private Label lastMessageReceivedTextBox;
        private Label label2;
        private Panel panel2;
        private Label messagesSentTextBox;
        private Label label4;
        private Panel panel3;
        private Label messagesReceivedTextBox;
        private Label label6;
        protected Panel panel4;
        protected Panel panel5;
        protected CheckBox cbSendMessages;
        private Label label1;
        protected NumericUpDown numericUpDown;
        protected TextBox pinTextBox;
        protected Label sessionIdLabel;
        private ListBox connectionsListBox;
        private Button buttonConnect;
        private Button buttonDisconnect;
        protected Label userLabel;
        protected TextBox userTextBox;
        protected Button pingBackButton;
        private Label lastErrorLabel;

        public MainPage MainPage { get; set; }

        protected string SessionId => $"{userTextBox?.Text}";
        protected string Pin => pinTextBox.Text;

        private SyncDeviceStatus status = SyncDeviceStatus.Stopped;
        public SyncDeviceStatus Status
        {
            get => status;
            set
            {
                status = value;
                UpdateControls();
            }
        }

        private ISyncDevice syncDevice;
        public ISyncDevice SyncDevice
        {
            get => syncDevice;
            set
            {
                if (syncDevice!=null)
                    syncDevice.OnError -= SyncDevice_OnError;

                syncDevice = value;
                if (syncDevice!=null)
                    syncDevice.OnError += SyncDevice_OnError;
            }
        }

        private void SyncDevice_OnError(object sender, string error)
        {
            Invoke((MethodInvoker)(() =>
            {
                lastErrorLabel.Text = error;
            }));
        }

        protected virtual string StartText { get; } = "Start";
        protected virtual string StopText { get; } = "Stop";

        private string ConnectionId(ISyncDevice syncDevice)
        {
            if (syncDevice != null)
                return $"{syncDevice.SessionName} - {syncDevice.NetworkId} - [{syncDevice.Status}]";
            return null;
        }

        private string SessionName(object connectionId)
        {
            string connectionIdText = connectionId?.ToString();
            if (!string.IsNullOrEmpty(connectionIdText))
                return connectionIdText.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return null;
        }

        public virtual void OnUpdateControls()
        {
            switch (Status)
            {
                case SyncDeviceStatus.Stopped:
                    button.Enabled = true;
                    button.Text = StartText;
                    break;
                case SyncDeviceStatus.Created:
                    button.Text = "...starting...";
                    button.Enabled = false;
                    break;
                case SyncDeviceStatus.Aborted:
                    button.Text = "...stopping...";
                    button.Enabled = false;
                    break;
                case SyncDeviceStatus.Started:
                    button.Enabled = true;
                    button.Text = StopText;
                    break;
            }

            progressBar.Visible = Status == SyncDeviceStatus.Created;
            pinTextBox.Enabled = Status == SyncDeviceStatus.Stopped;
            userTextBox.Enabled = Status == SyncDeviceStatus.Stopped;

            var selectedDevice = GetSelectedDevice();

            if (SyncDevice != null)
            {
                connectionsListBox.BeginUpdate();
                try
                {
                    connectionsListBox.Items.Clear();

                    foreach (var connection in SyncDevice.Connections)
                    {
                        connectionsListBox.Items.Add(ConnectionId(connection));
                    }
                    connectionsListBox.SelectedItem = ConnectionId(selectedDevice);

                    buttonConnect.Enabled = selectedDevice?.Status == SyncDeviceStatus.Created;
                    buttonDisconnect.Enabled = selectedDevice?.Status == SyncDeviceStatus.Started;
                }
                finally
                {
                    connectionsListBox.EndUpdate();
                }
            }
        }

        private ISyncDevice GetSelectedDevice()
        {
            var v = connectionsListBox.SelectedItem;
            if (v != null && SyncDevice != null)
            {
                foreach (var c in SyncDevice.Connections)
                {
                    if (ConnectionId(c) == v.ToString())
                        return c;
                }

            }
            return null;
        }

        public void UpdateControls() {

            Invoke((MethodInvoker)(() =>
            {
                OnUpdateControls();
            }));
        }

        int messagesRecived = 0;
        int messagesSent = 0;

        public void Reset()
        {
            if (string.IsNullOrEmpty(userTextBox.Text))
                userTextBox.Text = $"Pilot{Char.ConvertFromUtf32(62 + r.Next(30))}";

            messagesRecived = 0;
            messagesSent = 0;
            Invoke((MethodInvoker)(() =>
            {
                messagesReceivedTextBox.Text = null;
                lastMessageSentTextBox.Text = null;
                messagesSentTextBox.Text = null;
                lastMessageReceivedTextBox.Text = null;
            }));
        }

        public void RecordReciveMessage(string message)
        {
            Interlocked.Increment(ref messagesRecived);
            Invoke((MethodInvoker)(() =>
            {
                messagesReceivedTextBox.Text = messagesRecived.ToString();

                if (message.Length > 100)
                    lastMessageReceivedTextBox.Text = message.Length + " bytes";
                else
                    lastMessageReceivedTextBox.Text = message;

                LastReceivedMessage = message;

            }));
        }

        public void RecordSentMessage(string message)
        {
            Interlocked.Increment(ref messagesSent);

            Invoke((MethodInvoker)(() =>
            {
                messagesSentTextBox.Text = messagesSent.ToString();
                lastMessageSentTextBox.Text = message;
            }));
        }

        public bool ShouldSendMessages
        {
            get => cbSendMessages.Checked;
        //    set
        //    {
        //        Invoke((MethodInvoker)(() =>
        //        {
        //            lastMessageReceivedTextBox.Text = value;
        //        }));
        //    }
        }
        public int MessagesInterval
        {
            get
            {
                try
                {
                    var d = numericUpDown.Value;
                    return (int)d;
                }
                catch
                {

                }
                return 1000;
            }
            //    set
            //    {
            //        Invoke((MethodInvoker)(() =>
            //        {
            //            lastMessageReceivedTextBox.Text = value;
            //        }));
            //    }
        }

        protected async Task KeepWriting()
        {
            if (Status == SyncDeviceStatus.Started)
            {
                if (ShouldSendMessages && SyncDevice.Connections?.Count > 0)
                {
                    string msg = $"Time at {headerLabel.Text} is {DateTime.UtcNow}";

                   // msg = "{\"TargetNodeId\":7777,\"SourceNodeId\":8888,\"PayloadLength\":105,\"Payload\":\"eyJNZXNzYWdlSWQiOiJJc0FsaXZlIiwiUGF5bG9hZCI6IntcIk1lc3NhZ2VJZFwiOlwiSXNBbGl2ZVwiLFwiTnVtQ2xpZW50c1wiOjEsXCJDb25uZWN0ZWROb2RlSWRzXCI6WzFdfSJ9\"}";

                    await SyncDevice?.SendMessageAsync(msg);
                    RecordSentMessage(msg);                    
                }
                await Task.Delay(MessagesInterval);
                _ = KeepWriting();
            }
        }

        public SyncPanel()
        {
            InitializeComponent();
            OnUpdateControls();
        }

        Random r = new Random();

        public SyncPanel(ISyncDevice syncDevice) : this()
        {
            SyncDevice = syncDevice;

            OnUpdateControls();
        }

        private void InitializeComponent()
        {
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.button = new System.Windows.Forms.Button();
            this.headerLabel = new System.Windows.Forms.Label();
            this.lastMessagePanel = new System.Windows.Forms.Panel();
            this.lastMessageSentTextBox = new System.Windows.Forms.Label();
            this.lastMessageLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lastMessageReceivedTextBox = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.messagesSentTextBox = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.messagesReceivedTextBox = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.buttonDisconnect = new System.Windows.Forms.Button();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.connectionsListBox = new System.Windows.Forms.ListBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.pingBackButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown = new System.Windows.Forms.NumericUpDown();
            this.cbSendMessages = new System.Windows.Forms.CheckBox();
            this.pinTextBox = new System.Windows.Forms.TextBox();
            this.sessionIdLabel = new System.Windows.Forms.Label();
            this.userLabel = new System.Windows.Forms.Label();
            this.userTextBox = new System.Windows.Forms.TextBox();
            this.lastErrorLabel = new System.Windows.Forms.Label();
            this.lastMessagePanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar.Location = new System.Drawing.Point(0, 122);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(732, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 10;
            this.progressBar.Value = 1;
            this.progressBar.Visible = false;
            // 
            // button
            // 
            this.button.Dock = System.Windows.Forms.DockStyle.Top;
            this.button.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button.Location = new System.Drawing.Point(0, 83);
            this.button.Name = "button";
            this.button.Size = new System.Drawing.Size(732, 39);
            this.button.TabIndex = 9;
            this.button.Text = "Connect";
            this.button.UseVisualStyleBackColor = true;
            this.button.Click += new System.EventHandler(this.button_Click);
            // 
            // headerLabel
            // 
            this.headerLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(0, 23);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(732, 60);
            this.headerLabel.TabIndex = 8;
            this.headerLabel.Text = "Header";
            this.headerLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lastMessagePanel
            // 
            this.lastMessagePanel.Controls.Add(this.lastMessageSentTextBox);
            this.lastMessagePanel.Controls.Add(this.lastMessageLabel);
            this.lastMessagePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.lastMessagePanel.Location = new System.Drawing.Point(0, 453);
            this.lastMessagePanel.Name = "lastMessagePanel";
            this.lastMessagePanel.Padding = new System.Windows.Forms.Padding(5);
            this.lastMessagePanel.Size = new System.Drawing.Size(732, 41);
            this.lastMessagePanel.TabIndex = 13;
            // 
            // lastMessageSentTextBox
            // 
            this.lastMessageSentTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lastMessageSentTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastMessageSentTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lastMessageSentTextBox.Location = new System.Drawing.Point(274, 5);
            this.lastMessageSentTextBox.Name = "lastMessageSentTextBox";
            this.lastMessageSentTextBox.Size = new System.Drawing.Size(453, 31);
            this.lastMessageSentTextBox.TabIndex = 16;
            this.lastMessageSentTextBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lastMessageLabel
            // 
            this.lastMessageLabel.AutoSize = true;
            this.lastMessageLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.lastMessageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lastMessageLabel.Location = new System.Drawing.Point(5, 5);
            this.lastMessageLabel.Name = "lastMessageLabel";
            this.lastMessageLabel.Size = new System.Drawing.Size(269, 36);
            this.lastMessageLabel.TabIndex = 15;
            this.lastMessageLabel.Text = "Last message sent:";
            this.lastMessageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lastMessageReceivedTextBox);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 294);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(5);
            this.panel1.Size = new System.Drawing.Size(732, 41);
            this.panel1.TabIndex = 14;
            // 
            // lastMessageReceivedTextBox
            // 
            this.lastMessageReceivedTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lastMessageReceivedTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastMessageReceivedTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lastMessageReceivedTextBox.Location = new System.Drawing.Point(275, 5);
            this.lastMessageReceivedTextBox.Name = "lastMessageReceivedTextBox";
            this.lastMessageReceivedTextBox.Size = new System.Drawing.Size(452, 31);
            this.lastMessageReceivedTextBox.TabIndex = 16;
            this.lastMessageReceivedTextBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Left;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(5, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(270, 36);
            this.label2.TabIndex = 15;
            this.label2.Text = "Last msg. received:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.messagesSentTextBox);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 412);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(5);
            this.panel2.Size = new System.Drawing.Size(732, 41);
            this.panel2.TabIndex = 15;
            // 
            // messagesSentTextBox
            // 
            this.messagesSentTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.messagesSentTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messagesSentTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messagesSentTextBox.Location = new System.Drawing.Point(275, 5);
            this.messagesSentTextBox.Name = "messagesSentTextBox";
            this.messagesSentTextBox.Size = new System.Drawing.Size(452, 31);
            this.messagesSentTextBox.TabIndex = 16;
            this.messagesSentTextBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Left;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(5, 5);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(270, 36);
            this.label4.TabIndex = 15;
            this.label4.Text = "Messages sent:      ";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.messagesReceivedTextBox);
            this.panel3.Controls.Add(this.label6);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 253);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(5);
            this.panel3.Size = new System.Drawing.Size(732, 41);
            this.panel3.TabIndex = 16;
            // 
            // messagesReceivedTextBox
            // 
            this.messagesReceivedTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.messagesReceivedTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messagesReceivedTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messagesReceivedTextBox.Location = new System.Drawing.Point(283, 5);
            this.messagesReceivedTextBox.Name = "messagesReceivedTextBox";
            this.messagesReceivedTextBox.Size = new System.Drawing.Size(444, 31);
            this.messagesReceivedTextBox.TabIndex = 16;
            this.messagesReceivedTextBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Left;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(5, 5);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(278, 36);
            this.label6.TabIndex = 15;
            this.label6.Text = "Messages received:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.buttonDisconnect);
            this.panel4.Controls.Add(this.buttonConnect);
            this.panel4.Controls.Add(this.connectionsListBox);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(0, 145);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(732, 108);
            this.panel4.TabIndex = 17;
            // 
            // buttonDisconnect
            // 
            this.buttonDisconnect.Location = new System.Drawing.Point(5, 35);
            this.buttonDisconnect.Name = "buttonDisconnect";
            this.buttonDisconnect.Size = new System.Drawing.Size(75, 23);
            this.buttonDisconnect.TabIndex = 2;
            this.buttonDisconnect.Text = "Disconnect";
            this.buttonDisconnect.UseVisualStyleBackColor = true;
            this.buttonDisconnect.Click += new System.EventHandler(this.buttonDisconnect_Click);
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(5, 6);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(75, 23);
            this.buttonConnect.TabIndex = 1;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // connectionsListBox
            // 
            this.connectionsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionsListBox.FormattingEnabled = true;
            this.connectionsListBox.Location = new System.Drawing.Point(86, 6);
            this.connectionsListBox.Name = "connectionsListBox";
            this.connectionsListBox.Size = new System.Drawing.Size(631, 43);
            this.connectionsListBox.TabIndex = 0;
            this.connectionsListBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.connectionsListBox_MouseClick);
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.pingBackButton);
            this.panel5.Controls.Add(this.label1);
            this.panel5.Controls.Add(this.numericUpDown);
            this.panel5.Controls.Add(this.cbSendMessages);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel5.Location = new System.Drawing.Point(0, 335);
            this.panel5.Name = "panel5";
            this.panel5.Padding = new System.Windows.Forms.Padding(10, 40, 0, 0);
            this.panel5.Size = new System.Drawing.Size(732, 77);
            this.panel5.TabIndex = 18;
            // 
            // pingBackButton
            // 
            this.pingBackButton.AutoSize = true;
            this.pingBackButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.pingBackButton.Location = new System.Drawing.Point(550, 40);
            this.pingBackButton.Name = "pingBackButton";
            this.pingBackButton.Size = new System.Drawing.Size(182, 37);
            this.pingBackButton.TabIndex = 3;
            this.pingBackButton.Text = "Ping back the last message";
            this.pingBackButton.UseVisualStyleBackColor = true;
            this.pingBackButton.Click += new System.EventHandler(this.pingBackButton_Click);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Left;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(269, 40);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.label1.Size = new System.Drawing.Size(117, 37);
            this.label1.TabIndex = 2;
            this.label1.Text = "milliseconds";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numericUpDown
            // 
            this.numericUpDown.AutoSize = true;
            this.numericUpDown.Dock = System.Windows.Forms.DockStyle.Left;
            this.numericUpDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numericUpDown.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown.Location = new System.Drawing.Point(190, 40);
            this.numericUpDown.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDown.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown.Name = "numericUpDown";
            this.numericUpDown.Size = new System.Drawing.Size(79, 26);
            this.numericUpDown.TabIndex = 1;
            this.numericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDown.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // cbSendMessages
            // 
            this.cbSendMessages.AutoSize = true;
            this.cbSendMessages.Checked = true;
            this.cbSendMessages.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSendMessages.Dock = System.Windows.Forms.DockStyle.Left;
            this.cbSendMessages.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbSendMessages.Location = new System.Drawing.Point(10, 40);
            this.cbSendMessages.Name = "cbSendMessages";
            this.cbSendMessages.Size = new System.Drawing.Size(180, 37);
            this.cbSendMessages.TabIndex = 0;
            this.cbSendMessages.Text = "Send message every ";
            this.cbSendMessages.UseVisualStyleBackColor = true;
            // 
            // pinTextBox
            // 
            this.pinTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pinTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pinTextBox.Location = new System.Drawing.Point(593, 32);
            this.pinTextBox.MaxLength = 4;
            this.pinTextBox.Name = "pinTextBox";
            this.pinTextBox.Size = new System.Drawing.Size(134, 20);
            this.pinTextBox.TabIndex = 19;
            this.pinTextBox.Text = "ABCD";
            this.pinTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.pinTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // sessionIdLabel
            // 
            this.sessionIdLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sessionIdLabel.AutoSize = true;
            this.sessionIdLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sessionIdLabel.Location = new System.Drawing.Point(633, 7);
            this.sessionIdLabel.Name = "sessionIdLabel";
            this.sessionIdLabel.Size = new System.Drawing.Size(94, 17);
            this.sessionIdLabel.TabIndex = 20;
            this.sessionIdLabel.Text = "PIN (optional)";
            // 
            // userLabel
            // 
            this.userLabel.AutoSize = true;
            this.userLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.userLabel.Location = new System.Drawing.Point(8, 7);
            this.userLabel.Margin = new System.Windows.Forms.Padding(0);
            this.userLabel.Name = "userLabel";
            this.userLabel.Size = new System.Drawing.Size(38, 17);
            this.userLabel.TabIndex = 24;
            this.userLabel.Text = "User";
            // 
            // userTextBox
            // 
            this.userTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.userTextBox.Location = new System.Drawing.Point(10, 32);
            this.userTextBox.Name = "userTextBox";
            this.userTextBox.Size = new System.Drawing.Size(134, 20);
            this.userTextBox.TabIndex = 23;
            this.userTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lastErrorLabel
            // 
            this.lastErrorLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.lastErrorLabel.ForeColor = System.Drawing.Color.Red;
            this.lastErrorLabel.Location = new System.Drawing.Point(0, 0);
            this.lastErrorLabel.Name = "lastErrorLabel";
            this.lastErrorLabel.Size = new System.Drawing.Size(732, 23);
            this.lastErrorLabel.TabIndex = 26;
            this.lastErrorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SyncPanel
            // 
            this.Controls.Add(this.userLabel);
            this.Controls.Add(this.userTextBox);
            this.Controls.Add(this.sessionIdLabel);
            this.Controls.Add(this.pinTextBox);
            this.Controls.Add(this.lastMessagePanel);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.button);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.lastErrorLabel);
            this.Name = "SyncPanel";
            this.Size = new System.Drawing.Size(732, 475);
            this.lastMessagePanel.ResumeLayout(false);
            this.lastMessagePanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        protected void button_Click(object sender, EventArgs e)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                Reset();
                Status = SyncDeviceStatus.Created;
                SyncDevice.StartAsync(SessionId, Pin, "Connect requested by app user");
            }
            else
            if (Status == SyncDeviceStatus.Started)
            {
                Status = SyncDeviceStatus.Aborted;
                SyncDevice.StopAsync("Disconnect requested by app user");
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            var selectedDevice = GetSelectedDevice();

            if (selectedDevice != null)
            {
                _ = selectedDevice.StartAsync(selectedDevice?.SessionName, Pin, "Manual connect");
            }
        }

        private void connectionsListBox_MouseClick(object sender, MouseEventArgs e)
        {
            UpdateControls();
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            var selectedDevice = GetSelectedDevice();

            if (selectedDevice != null)
            {
                _ = selectedDevice.StopAsync("Manual disconnect");
            }
        }

        public string LastReceivedMessage { get; private set; }

        public string[] GetRecipients()
        {
            string sessionName = SessionName(connectionsListBox?.SelectedItem);
            if (!string.IsNullOrEmpty(sessionName))
            {
                return new string[] { sessionName };
            }

            return Array.Empty<string>();
        }

        protected virtual void OnPingBackButtonClick()
        {
            if (LastReceivedMessage != null)
                _ = SyncDevice?.SendMessageAsync(LastReceivedMessage, GetRecipients());
        }
        private void pingBackButton_Click(object sender, EventArgs e)
        {
            OnPingBackButtonClick();
        }
    }
}
