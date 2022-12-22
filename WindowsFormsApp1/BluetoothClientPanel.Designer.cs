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
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // headerLabel
            // 
            this.headerLabel.Size = new System.Drawing.Size(629, 26);
            this.headerLabel.Text = "Bluetooth client";
            // 
            // panel5
            // 
            this.panel5.Size = new System.Drawing.Size(629, 80);
            // 
            // cbSendMessages
            // 
            this.cbSendMessages.Checked = false;
            this.cbSendMessages.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.cbSendMessages.Size = new System.Drawing.Size(180, 40);
            // 
            // BluetoothClientPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "BluetoothClientPanel";
            this.Size = new System.Drawing.Size(629, 338);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}
