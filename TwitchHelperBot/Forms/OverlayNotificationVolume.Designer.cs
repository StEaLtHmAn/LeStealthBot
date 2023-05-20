namespace TwitchHelperBot
{
    partial class OverlayNotificationVolume
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
            this.notificationIcon = new System.Windows.Forms.PictureBox();
            this.notificationText = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.notificationIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // notificationIcon
            // 
            this.notificationIcon.Location = new System.Drawing.Point(0, 0);
            this.notificationIcon.Name = "notificationIcon";
            this.notificationIcon.Size = new System.Drawing.Size(36, 50);
            this.notificationIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.notificationIcon.TabIndex = 1;
            this.notificationIcon.TabStop = false;
            // 
            // notificationText
            // 
            this.notificationText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.notificationText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.notificationText.Location = new System.Drawing.Point(36, 0);
            this.notificationText.Margin = new System.Windows.Forms.Padding(3);
            this.notificationText.Name = "notificationText";
            this.notificationText.Padding = new System.Windows.Forms.Padding(3);
            this.notificationText.Size = new System.Drawing.Size(160, 21);
            this.notificationText.TabIndex = 0;
            this.notificationText.Text = "Loading...";
            // 
            // trackBar1
            // 
            this.trackBar1.AutoSize = false;
            this.trackBar1.Enabled = false;
            this.trackBar1.Location = new System.Drawing.Point(38, 15);
            this.trackBar1.Maximum = 100;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(158, 36);
            this.trackBar1.TabIndex = 2;
            this.trackBar1.TickFrequency = 10;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            // 
            // OverlayNotificationVolume
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(196, 50);
            this.Controls.Add(this.notificationIcon);
            this.Controls.Add(this.notificationText);
            this.Controls.Add(this.trackBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OverlayNotificationVolume";
            this.Opacity = 0D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            ((System.ComponentModel.ISupportInitialize)(this.notificationIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox notificationIcon;
        private System.Windows.Forms.Label notificationText;
        private System.Windows.Forms.TrackBar trackBar1;
    }
}