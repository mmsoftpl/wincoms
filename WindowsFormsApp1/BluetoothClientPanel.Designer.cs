namespace WindowsFormsApp1
{
    partial class BluetoothClientPanel
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
            this.rescanButton = new System.Windows.Forms.Button();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // headerLabel
            // 
            this.headerLabel.Text = "Bluetooth client";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.rescanButton);
            this.panel4.Controls.SetChildIndex(this.rescanButton, 0);
            // 
            // panel5
            // 
            this.panel5.Size = new System.Drawing.Size(732, 80);
            // 
            // cbSendMessages
            // 
            this.cbSendMessages.Checked = false;
            this.cbSendMessages.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.cbSendMessages.Size = new System.Drawing.Size(180, 40);
            // 
            // userTextBox
            // 
            this.userTextBox.Text = "PilotB";
            // 
            // rescanButton
            // 
            this.rescanButton.Enabled = false;
            this.rescanButton.Location = new System.Drawing.Point(5, 65);
            this.rescanButton.Name = "rescanButton";
            this.rescanButton.Size = new System.Drawing.Size(75, 23);
            this.rescanButton.TabIndex = 4;
            this.rescanButton.Text = "Refresh";
            this.rescanButton.UseVisualStyleBackColor = true;
            this.rescanButton.Click += new System.EventHandler(this.rescanButton_Click);
            // 
            // BluetoothClientPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "BluetoothClientPanel";
            this.panel4.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button rescanButton;
    }
}
