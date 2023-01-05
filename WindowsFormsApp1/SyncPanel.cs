using SDKTemplate;
using SyncDevice;
using System;
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
        private Panel panel4;
        protected Panel panel5;
        protected CheckBox cbSendMessages;
        private Label label1;
        protected NumericUpDown numericUpDown;

        public MainPage MainPage { get; set; }

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

        public virtual ISyncDevice SyncDevice { get; private set; }

        public virtual void OnUpdateControls()
        {
            switch (Status)
            {
                case SyncDeviceStatus.Stopped:
                    button.Enabled = true;
                    button.Text = "Start";
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
                    button.Text = "Stop";
                    break;
            }

            progressBar.Visible = Status == SyncDeviceStatus.Created;
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
                lastMessageReceivedTextBox.Text = message;
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
            while (Status == SyncDeviceStatus.Started)
            {
                if (ShouldSendMessages && SyncDevice.Connections > 0)
                {
                    string msg = $"Time at {headerLabel.Text} is {DateTime.UtcNow}";

                    msg = "{\"TargetNodeId\":7777,\"SourceNodeId\":8888,\"PayloadLength\":105,\"Payload\":\"eyJNZXNzYWdlSWQiOiJJc0FsaXZlIiwiUGF5bG9hZCI6IntcIk1lc3NhZ2VJZFwiOlwiSXNBbGl2ZVwiLFwiTnVtQ2xpZW50c1wiOjEsXCJDb25uZWN0ZWROb2RlSWRzXCI6WzFdfSJ9\"}";

                    await SyncDevice?.SendMessageAsync(msg);
                    RecordSentMessage(msg);                    
                }
                Thread.Sleep(MessagesInterval);
            }
        }

        public SyncPanel()
        {
            InitializeComponent();
        }

        public SyncPanel(ISyncDevice syncDevice) : this()
        {
            SyncDevice = syncDevice;
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
            this.panel5 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown = new System.Windows.Forms.NumericUpDown();
            this.cbSendMessages = new System.Windows.Forms.CheckBox();
            this.lastMessagePanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar.Location = new System.Drawing.Point(0, 72);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(589, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 10;
            this.progressBar.Value = 1;
            this.progressBar.Visible = false;
            // 
            // button
            // 
            this.button.Dock = System.Windows.Forms.DockStyle.Top;
            this.button.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button.Location = new System.Drawing.Point(0, 26);
            this.button.Name = "button";
            this.button.Size = new System.Drawing.Size(589, 46);
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
            this.headerLabel.Size = new System.Drawing.Size(589, 26);
            this.headerLabel.TabIndex = 8;
            this.headerLabel.Text = "Header";
            this.headerLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lastMessagePanel
            // 
            this.lastMessagePanel.Controls.Add(this.lastMessageSentTextBox);
            this.lastMessagePanel.Controls.Add(this.lastMessageLabel);
            this.lastMessagePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.lastMessagePanel.Location = new System.Drawing.Point(0, 321);
            this.lastMessagePanel.Name = "lastMessagePanel";
            this.lastMessagePanel.Padding = new System.Windows.Forms.Padding(5);
            this.lastMessagePanel.Size = new System.Drawing.Size(589, 41);
            this.lastMessagePanel.TabIndex = 13;
            // 
            // lastMessageSentTextBox
            // 
            this.lastMessageSentTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lastMessageSentTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastMessageSentTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lastMessageSentTextBox.Location = new System.Drawing.Point(274, 5);
            this.lastMessageSentTextBox.Name = "lastMessageSentTextBox";
            this.lastMessageSentTextBox.Size = new System.Drawing.Size(310, 31);
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
            this.panel1.Location = new System.Drawing.Point(0, 162);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(5);
            this.panel1.Size = new System.Drawing.Size(589, 41);
            this.panel1.TabIndex = 14;
            // 
            // lastMessageReceivedTextBox
            // 
            this.lastMessageReceivedTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lastMessageReceivedTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastMessageReceivedTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lastMessageReceivedTextBox.Location = new System.Drawing.Point(275, 5);
            this.lastMessageReceivedTextBox.Name = "lastMessageReceivedTextBox";
            this.lastMessageReceivedTextBox.Size = new System.Drawing.Size(309, 31);
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
            this.panel2.Location = new System.Drawing.Point(0, 280);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(5);
            this.panel2.Size = new System.Drawing.Size(589, 41);
            this.panel2.TabIndex = 15;
            // 
            // messagesSentTextBox
            // 
            this.messagesSentTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.messagesSentTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messagesSentTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messagesSentTextBox.Location = new System.Drawing.Point(275, 5);
            this.messagesSentTextBox.Name = "messagesSentTextBox";
            this.messagesSentTextBox.Size = new System.Drawing.Size(309, 31);
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
            this.panel3.Location = new System.Drawing.Point(0, 121);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(5);
            this.panel3.Size = new System.Drawing.Size(589, 41);
            this.panel3.TabIndex = 16;
            // 
            // messagesReceivedTextBox
            // 
            this.messagesReceivedTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.messagesReceivedTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messagesReceivedTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messagesReceivedTextBox.Location = new System.Drawing.Point(283, 5);
            this.messagesReceivedTextBox.Name = "messagesReceivedTextBox";
            this.messagesReceivedTextBox.Size = new System.Drawing.Size(301, 31);
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
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(0, 95);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(589, 26);
            this.panel4.TabIndex = 17;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.label1);
            this.panel5.Controls.Add(this.numericUpDown);
            this.panel5.Controls.Add(this.cbSendMessages);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel5.Location = new System.Drawing.Point(0, 203);
            this.panel5.Name = "panel5";
            this.panel5.Padding = new System.Windows.Forms.Padding(10, 40, 0, 0);
            this.panel5.Size = new System.Drawing.Size(589, 77);
            this.panel5.TabIndex = 18;
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
            5000,
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
            // SyncPanel
            // 
            this.Controls.Add(this.lastMessagePanel);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.button);
            this.Controls.Add(this.headerLabel);
            this.Name = "SyncPanel";
            this.Size = new System.Drawing.Size(589, 363);
            this.lastMessagePanel.ResumeLayout(false);
            this.lastMessagePanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        private void button_Click(object sender, EventArgs e)
        {
            if (Status == SyncDeviceStatus.Stopped)
            {
                Reset();
                Status = SyncDeviceStatus.Created;
                SyncDevice.StartAsync("Connect requested by app user");
            }
            else
            if (Status == SyncDeviceStatus.Started)
            {
                Status = SyncDeviceStatus.Aborted;
                SyncDevice.StopAsync("Disconnect requested by app user");
            }
        }
    }
}
