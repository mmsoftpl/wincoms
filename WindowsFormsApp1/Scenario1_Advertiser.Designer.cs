namespace WindowsFormsApp1
{
    partial class Scenario1_Advertiser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnStopAdvertisement = new System.Windows.Forms.Button();
            this.chkLegacySetting = new System.Windows.Forms.CheckBox();
            this.chkPreferGroupOwnerMode = new System.Windows.Forms.CheckBox();
            this.btnStartAdvertisement = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSendMessage = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtPassphrase = new System.Windows.Forms.TextBox();
            this.txtSsid = new System.Windows.Forms.TextBox();
            this.chkListener = new System.Windows.Forms.CheckBox();
            this.connectionSettingsPanel = new WindowsFormsApp1.ConnectionSettingsPanel();
            this.txtInformationElement = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lvConnectedDevices = new System.Windows.Forms.ListBox();
            this.cmbListenState = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnCloseDevice = new System.Windows.Forms.Button();
            this.btnSendMessage = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnStopAdvertisement
            // 
            this.btnStopAdvertisement.Location = new System.Drawing.Point(153, 265);
            this.btnStopAdvertisement.Name = "btnStopAdvertisement";
            this.btnStopAdvertisement.Size = new System.Drawing.Size(125, 23);
            this.btnStopAdvertisement.TabIndex = 22;
            this.btnStopAdvertisement.Text = "btnStopAdvertisement";
            this.btnStopAdvertisement.UseVisualStyleBackColor = true;
            this.btnStopAdvertisement.Click += new System.EventHandler(this.btnStopAdvertisement_Click);
            // 
            // chkLegacySetting
            // 
            this.chkLegacySetting.AutoSize = true;
            this.chkLegacySetting.Location = new System.Drawing.Point(49, 58);
            this.chkLegacySetting.Name = "chkLegacySetting";
            this.chkLegacySetting.Size = new System.Drawing.Size(112, 17);
            this.chkLegacySetting.TabIndex = 21;
            this.chkLegacySetting.Text = "chkLegacySetting";
            this.chkLegacySetting.UseVisualStyleBackColor = true;
            // 
            // chkPreferGroupOwnerMode
            // 
            this.chkPreferGroupOwnerMode.AutoSize = true;
            this.chkPreferGroupOwnerMode.Location = new System.Drawing.Point(49, 35);
            this.chkPreferGroupOwnerMode.Name = "chkPreferGroupOwnerMode";
            this.chkPreferGroupOwnerMode.Size = new System.Drawing.Size(159, 17);
            this.chkPreferGroupOwnerMode.TabIndex = 20;
            this.chkPreferGroupOwnerMode.Text = "chkPreferGroupOwnerMode";
            this.chkPreferGroupOwnerMode.UseVisualStyleBackColor = true;
            // 
            // btnStartAdvertisement
            // 
            this.btnStartAdvertisement.Location = new System.Drawing.Point(12, 265);
            this.btnStartAdvertisement.Name = "btnStartAdvertisement";
            this.btnStartAdvertisement.Size = new System.Drawing.Size(125, 23);
            this.btnStartAdvertisement.TabIndex = 19;
            this.btnStartAdvertisement.Text = "btnStartAdvertisement";
            this.btnStartAdvertisement.UseVisualStyleBackColor = true;
            this.btnStartAdvertisement.Click += new System.EventHandler(this.btnStartAdvertisement_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 181);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "txtSendMessage";
            // 
            // txtSendMessage
            // 
            this.txtSendMessage.Location = new System.Drawing.Point(108, 178);
            this.txtSendMessage.Name = "txtSendMessage";
            this.txtSendMessage.Size = new System.Drawing.Size(100, 20);
            this.txtSendMessage.TabIndex = 17;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "txtSsid";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 139);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "txtPassphrase";
            // 
            // txtPassphrase
            // 
            this.txtPassphrase.Location = new System.Drawing.Point(108, 136);
            this.txtPassphrase.Name = "txtPassphrase";
            this.txtPassphrase.Size = new System.Drawing.Size(100, 20);
            this.txtPassphrase.TabIndex = 14;
            this.txtPassphrase.Text = "aa123";
            // 
            // txtSsid
            // 
            this.txtSsid.Location = new System.Drawing.Point(108, 90);
            this.txtSsid.Name = "txtSsid";
            this.txtSsid.Size = new System.Drawing.Size(100, 20);
            this.txtSsid.TabIndex = 13;
            this.txtSsid.Text = "ABCDE12345";
            // 
            // chkListener
            // 
            this.chkListener.AutoSize = true;
            this.chkListener.Location = new System.Drawing.Point(12, 12);
            this.chkListener.Name = "chkListener";
            this.chkListener.Size = new System.Drawing.Size(81, 17);
            this.chkListener.TabIndex = 12;
            this.chkListener.Text = "chkListener";
            this.chkListener.UseVisualStyleBackColor = true;
            // 
            // connectionSettingsPanel
            // 
            this.connectionSettingsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionSettingsPanel.Location = new System.Drawing.Point(225, 12);
            this.connectionSettingsPanel.Name = "connectionSettingsPanel";
            this.connectionSettingsPanel.Size = new System.Drawing.Size(707, 126);
            this.connectionSettingsPanel.TabIndex = 23;
            // 
            // txtInformationElement
            // 
            this.txtInformationElement.Location = new System.Drawing.Point(354, 131);
            this.txtInformationElement.Name = "txtInformationElement";
            this.txtInformationElement.Size = new System.Drawing.Size(100, 20);
            this.txtInformationElement.TabIndex = 24;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(240, 138);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "txtInformationElement";
            // 
            // lvConnectedDevices
            // 
            this.lvConnectedDevices.FormattingEnabled = true;
            this.lvConnectedDevices.Location = new System.Drawing.Point(602, 131);
            this.lvConnectedDevices.Name = "lvConnectedDevices";
            this.lvConnectedDevices.Size = new System.Drawing.Size(306, 82);
            this.lvConnectedDevices.TabIndex = 29;
            // 
            // cmbListenState
            // 
            this.cmbListenState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbListenState.FormattingEnabled = true;
            this.cmbListenState.Location = new System.Drawing.Point(108, 225);
            this.cmbListenState.Name = "cmbListenState";
            this.cmbListenState.Size = new System.Drawing.Size(216, 21);
            this.cmbListenState.TabIndex = 30;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 225);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 13);
            this.label5.TabIndex = 31;
            this.label5.Text = "cmbListenState";
            // 
            // btnCloseDevice
            // 
            this.btnCloseDevice.Location = new System.Drawing.Point(19, 306);
            this.btnCloseDevice.Name = "btnCloseDevice";
            this.btnCloseDevice.Size = new System.Drawing.Size(75, 23);
            this.btnCloseDevice.TabIndex = 32;
            this.btnCloseDevice.Text = "btnCloseDevice";
            this.btnCloseDevice.UseVisualStyleBackColor = true;
            this.btnCloseDevice.Click += new System.EventHandler(this.btnCloseDevice_Click);
            // 
            // btnSendMessage
            // 
            this.btnSendMessage.Location = new System.Drawing.Point(243, 175);
            this.btnSendMessage.Name = "btnSendMessage";
            this.btnSendMessage.Size = new System.Drawing.Size(119, 23);
            this.btnSendMessage.TabIndex = 33;
            this.btnSendMessage.Text = "btnSendMessage";
            this.btnSendMessage.UseVisualStyleBackColor = true;
            this.btnSendMessage.Click += new System.EventHandler(this.btnSendMessage_Click);
            // 
            // Scenario1_Advertiser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(932, 398);
            this.Controls.Add(this.btnSendMessage);
            this.Controls.Add(this.btnCloseDevice);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cmbListenState);
            this.Controls.Add(this.lvConnectedDevices);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtInformationElement);
            this.Controls.Add(this.connectionSettingsPanel);
            this.Controls.Add(this.btnStopAdvertisement);
            this.Controls.Add(this.chkLegacySetting);
            this.Controls.Add(this.chkPreferGroupOwnerMode);
            this.Controls.Add(this.btnStartAdvertisement);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtSendMessage);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtPassphrase);
            this.Controls.Add(this.txtSsid);
            this.Controls.Add(this.chkListener);
            this.Name = "Scenario1_Advertiser";
            this.Text = "Scenario1_Advertiser";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ConnectionSettingsPanel connectionSettingsPanel;
        private System.Windows.Forms.Button btnStopAdvertisement;
        private System.Windows.Forms.CheckBox chkLegacySetting;
        private System.Windows.Forms.CheckBox chkPreferGroupOwnerMode;
        private System.Windows.Forms.Button btnStartAdvertisement;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSendMessage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPassphrase;
        private System.Windows.Forms.TextBox txtSsid;
        private System.Windows.Forms.CheckBox chkListener;
        private System.Windows.Forms.TextBox txtInformationElement;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox lvConnectedDevices;
        private System.Windows.Forms.ComboBox cmbListenState;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnCloseDevice;
        private System.Windows.Forms.Button btnSendMessage;
    }
}