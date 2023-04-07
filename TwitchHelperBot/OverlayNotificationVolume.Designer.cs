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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.notificationIcon)).BeginInit();
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
            this.notificationText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.notificationText.Location = new System.Drawing.Point(36, 0);
            this.notificationText.Margin = new System.Windows.Forms.Padding(3);
            this.notificationText.Name = "notificationText";
            this.notificationText.Padding = new System.Windows.Forms.Padding(3);
            this.notificationText.Size = new System.Drawing.Size(160, 21);
            this.notificationText.TabIndex = 0;
            this.notificationText.Text = "Loading...";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(37, 27);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(157, 14);
            this.progressBar1.TabIndex = 2;
            // 
            // OverlayNotificationVolume
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(196, 50);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.notificationIcon);
            this.Controls.Add(this.notificationText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OverlayNotificationVolume";
            this.Opacity = 0.9D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            ((System.ComponentModel.ISupportInitialize)(this.notificationIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox notificationIcon;
        private System.Windows.Forms.Label notificationText;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}