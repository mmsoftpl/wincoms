namespace WindowsFormsApp1
{
    partial class BluetoothServerPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // headerLabel
            // 
            this.headerLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.headerLabel.Size = new System.Drawing.Size(976, 74);
            this.headerLabel.Text = "Bluetooth server";
            // 
            // panel4
            // 
            this.panel4.Location = new System.Drawing.Point(0, 150);
            this.panel4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel4.Size = new System.Drawing.Size(976, 133);
            // 
            // panel5
            // 
            this.panel5.Location = new System.Drawing.Point(0, 383);
            this.panel5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel5.Padding = new System.Windows.Forms.Padding(13, 49, 0, 0);
            this.panel5.Size = new System.Drawing.Size(976, 81);
            // 
            // cbSendMessages
            // 
            this.cbSendMessages.Location = new System.Drawing.Point(13, 49);
            this.cbSendMessages.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbSendMessages.Size = new System.Drawing.Size(180, 32);
            // 
            // numericUpDown
            // 
            this.numericUpDown.Location = new System.Drawing.Point(193, 49);
            this.numericUpDown.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            // 
            // sessionIdTextBox
            // 
            this.pinTextBox.Location = new System.Drawing.Point(791, 39);
            this.pinTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pinTextBox.Size = new System.Drawing.Size(177, 20);
            // 
            // sessionIdLabel
            // 
            this.sessionIdLabel.Location = new System.Drawing.Point(867, 9);
            this.sessionIdLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            // 
            // userLabel
            // 
            this.userLabel.Location = new System.Drawing.Point(11, 9);
            // 
            // userTextBox
            // 
            this.userTextBox.Location = new System.Drawing.Point(13, 39);
            this.userTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userTextBox.Size = new System.Drawing.Size(177, 20);
            this.userTextBox.Text = "PilotA";
            // 
            // BluetoothServerPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "BluetoothServerPanel";
            this.Size = new System.Drawing.Size(976, 585);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
