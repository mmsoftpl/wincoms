namespace WindowsFormsApp1
{
    partial class Scenario2_Connector
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
            this.btnWatcher = new System.Windows.Forms.Button();
            this.lvDiscoveredDevices = new System.Windows.Forms.ListBox();
            this.connectionSettingsPanel = new WindowsFormsApp1.ConnectionSettingsPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSendMessage = new System.Windows.Forms.TextBox();
            this.lvConnectedDevices = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbDeviceSelector = new System.Windows.Forms.ComboBox();
            this.btnUnpair = new System.Windows.Forms.Button();
            this.btnIe = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnWatcher
            // 
            this.btnWatcher.Location = new System.Drawing.Point(42, 446);
            this.btnWatcher.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnWatcher.Name = "btnWatcher";
            this.btnWatcher.Size = new System.Drawing.Size(188, 35);
            this.btnWatcher.TabIndex = 23;
            this.btnWatcher.Text = "btnWatcher";
            this.btnWatcher.UseVisualStyleBackColor = true;
            this.btnWatcher.Click += new System.EventHandler(this.btnWatcher_Click);
            // 
            // lvDiscoveredDevices
            // 
            this.lvDiscoveredDevices.FormattingEnabled = true;
            this.lvDiscoveredDevices.ItemHeight = 20;
            this.lvDiscoveredDevices.Location = new System.Drawing.Point(42, 86);
            this.lvDiscoveredDevices.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.lvDiscoveredDevices.Name = "lvDiscoveredDevices";
            this.lvDiscoveredDevices.Size = new System.Drawing.Size(457, 124);
            this.lvDiscoveredDevices.TabIndex = 24;
            this.lvDiscoveredDevices.Click += new System.EventHandler(this.btnFromId_Click);
            // 
            // connectionSettingsPanel
            // 
            this.connectionSettingsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionSettingsPanel.Location = new System.Drawing.Point(510, 18);
            this.connectionSettingsPanel.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.connectionSettingsPanel.Name = "connectionSettingsPanel";
            this.connectionSettingsPanel.Size = new System.Drawing.Size(488, 194);
            this.connectionSettingsPanel.TabIndex = 25;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(38, 394);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(129, 20);
            this.label3.TabIndex = 27;
            this.label3.Text = "txtSendMessage";
            // 
            // txtSendMessage
            // 
            this.txtSendMessage.Location = new System.Drawing.Point(176, 389);
            this.txtSendMessage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtSendMessage.Name = "txtSendMessage";
            this.txtSendMessage.Size = new System.Drawing.Size(148, 26);
            this.txtSendMessage.TabIndex = 26;
            // 
            // lvConnectedDevices
            // 
            this.lvConnectedDevices.FormattingEnabled = true;
            this.lvConnectedDevices.ItemHeight = 20;
            this.lvConnectedDevices.Location = new System.Drawing.Point(42, 234);
            this.lvConnectedDevices.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.lvConnectedDevices.Name = "lvConnectedDevices";
            this.lvConnectedDevices.Size = new System.Drawing.Size(457, 124);
            this.lvConnectedDevices.TabIndex = 28;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(38, 34);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(146, 20);
            this.label1.TabIndex = 29;
            this.label1.Text = "cmbDeviceSelector";
            // 
            // cmbDeviceSelector
            // 
            this.cmbDeviceSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDeviceSelector.FormattingEnabled = true;
            this.cmbDeviceSelector.Location = new System.Drawing.Point(204, 29);
            this.cmbDeviceSelector.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cmbDeviceSelector.Name = "cmbDeviceSelector";
            this.cmbDeviceSelector.Size = new System.Drawing.Size(148, 28);
            this.cmbDeviceSelector.TabIndex = 30;
            // 
            // btnUnpair
            // 
            this.btnUnpair.Location = new System.Drawing.Point(331, 446);
            this.btnUnpair.Name = "btnUnpair";
            this.btnUnpair.Size = new System.Drawing.Size(154, 48);
            this.btnUnpair.TabIndex = 31;
            this.btnUnpair.Text = "btnUnpair";
            this.btnUnpair.UseVisualStyleBackColor = true;
            this.btnUnpair.Click += new System.EventHandler(this.btnUnpair_Click);
            // 
            // btnIe
            // 
            this.btnIe.Location = new System.Drawing.Point(492, 374);
            this.btnIe.Name = "btnIe";
            this.btnIe.Size = new System.Drawing.Size(135, 60);
            this.btnIe.TabIndex = 32;
            this.btnIe.Text = "btnIe";
            this.btnIe.UseVisualStyleBackColor = true;
            this.btnIe.Click += new System.EventHandler(this.btnIe_Click);
            // 
            // Scenario2_Connector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1011, 512);
            this.Controls.Add(this.btnIe);
            this.Controls.Add(this.btnUnpair);
            this.Controls.Add(this.cmbDeviceSelector);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lvConnectedDevices);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtSendMessage);
            this.Controls.Add(this.connectionSettingsPanel);
            this.Controls.Add(this.lvDiscoveredDevices);
            this.Controls.Add(this.btnWatcher);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Scenario2_Connector";
            this.Text = "Scenario2_Connector";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnWatcher;
        private System.Windows.Forms.ListBox lvDiscoveredDevices;
        private ConnectionSettingsPanel connectionSettingsPanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSendMessage;
        private System.Windows.Forms.ListBox lvConnectedDevices;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbDeviceSelector;
        private System.Windows.Forms.Button btnUnpair;
        private System.Windows.Forms.Button btnIe;
    }
}