namespace WindowsFormsApp1
{
    partial class ConnectionSettingsPanel
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
            this.cmbGOIntent = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbPreferredPairingProcedure = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmbGOIntent
            // 
            this.cmbGOIntent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbGOIntent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGOIntent.FormattingEnabled = true;
            this.cmbGOIntent.Location = new System.Drawing.Point(188, 21);
            this.cmbGOIntent.Name = "cmbGOIntent";
            this.cmbGOIntent.Size = new System.Drawing.Size(257, 21);
            this.cmbGOIntent.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "cmbGOIntent";
            // 
            // cmbPreferredPairingProcedure
            // 
            this.cmbPreferredPairingProcedure.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPreferredPairingProcedure.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPreferredPairingProcedure.FormattingEnabled = true;
            this.cmbPreferredPairingProcedure.Location = new System.Drawing.Point(188, 75);
            this.cmbPreferredPairingProcedure.Name = "cmbPreferredPairingProcedure";
            this.cmbPreferredPairingProcedure.Size = new System.Drawing.Size(257, 21);
            this.cmbPreferredPairingProcedure.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(151, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "cmbPreferredPairingProcedure";
            // 
            // ConnectionSettingsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbPreferredPairingProcedure);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbGOIntent);
            this.Name = "ConnectionSettingsPanel";
            this.Size = new System.Drawing.Size(463, 133);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbGOIntent;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbPreferredPairingProcedure;
        private System.Windows.Forms.Label label2;
    }
}
