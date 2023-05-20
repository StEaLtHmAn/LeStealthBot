namespace TwitchHelperBot
{
    partial class GameMatcherForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.txtPresetTitle = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbxPresetExePath = new System.Windows.Forms.ComboBox();
            this.presetCategoryPictureBox = new System.Windows.Forms.PictureBox();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.txtPresetCategory = new AutoCompleteTextBox();
            this.presetsListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            ((System.ComponentModel.ISupportInitialize)(this.presetCategoryPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.Green;
            this.button1.Location = new System.Drawing.Point(417, 358);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 36);
            this.button1.TabIndex = 4;
            this.button1.Text = "Add/Save";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.txtPresetTitle.Location = new System.Drawing.Point(93, 287);
            this.txtPresetTitle.Name = "textBox1";
            this.txtPresetTitle.Size = new System.Drawing.Size(480, 20);
            this.txtPresetTitle.TabIndex = 2;
            // 
            // button2
            // 
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.button2.Location = new System.Drawing.Point(498, 358);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 36);
            this.button2.TabIndex = 6;
            this.button2.Text = "Delete";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 264);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Target exe path:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 290);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Preset Title:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(2, 316);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Preset Category:";
            // 
            // comboBox2
            // 
            this.cbxPresetExePath.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxPresetExePath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbxPresetExePath.FormattingEnabled = true;
            this.cbxPresetExePath.Location = new System.Drawing.Point(93, 261);
            this.cbxPresetExePath.Name = "comboBox2";
            this.cbxPresetExePath.Size = new System.Drawing.Size(411, 21);
            this.cbxPresetExePath.TabIndex = 1;
            this.cbxPresetExePath.DropDown += new System.EventHandler(this.cbxPresetExePath_DropDown);
            this.cbxPresetExePath.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.cbxPresetExePath_Format);
            // 
            // pictureBox1
            // 
            this.presetCategoryPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.presetCategoryPictureBox.Location = new System.Drawing.Point(93, 313);
            this.presetCategoryPictureBox.Name = "pictureBox1";
            this.presetCategoryPictureBox.Size = new System.Drawing.Size(52, 72);
            this.presetCategoryPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.presetCategoryPictureBox.TabIndex = 14;
            this.presetCategoryPictureBox.TabStop = false;
            // 
            // button3
            // 
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(255)))));
            this.button3.Location = new System.Drawing.Point(336, 358);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 36);
            this.button3.TabIndex = 5;
            this.button3.Text = "Activate";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button4.Location = new System.Drawing.Point(510, 259);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(63, 23);
            this.button4.TabIndex = 101;
            this.button4.Text = "Browse";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // textBox2
            // 
            this.txtPresetCategory.Location = new System.Drawing.Point(151, 313);
            this.txtPresetCategory.Name = "textBox2";
            this.txtPresetCategory.Size = new System.Drawing.Size(422, 20);
            this.txtPresetCategory.TabIndex = 3;
            this.txtPresetCategory.TextChanged += new System.EventHandler(this.txtPresetCategory_TextChanged);
            // 
            // listView1
            // 
            this.presetsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.presetsListView.FullRowSelect = true;
            this.presetsListView.GridLines = true;
            this.presetsListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.presetsListView.HideSelection = false;
            this.presetsListView.Location = new System.Drawing.Point(0, 0);
            this.presetsListView.MultiSelect = false;
            this.presetsListView.Name = "listView1";
            this.presetsListView.ShowGroups = false;
            this.presetsListView.Size = new System.Drawing.Size(585, 240);
            this.presetsListView.TabIndex = 0;
            this.presetsListView.UseCompatibleStateImageBehavior = false;
            this.presetsListView.View = System.Windows.Forms.View.Details;
            this.presetsListView.SelectedIndexChanged += new System.EventHandler(this.presetsListView_SelectedIndexChanged);
            this.presetsListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listView1_KeyDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Target exe path";
            this.columnHeader1.Width = 292;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Title";
            this.columnHeader2.Width = 150;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Category";
            this.columnHeader3.Width = 150;
            // 
            // GameMatcherForm
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(585, 406);
            this.Controls.Add(this.presetsListView);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.presetCategoryPictureBox);
            this.Controls.Add(this.txtPresetCategory);
            this.Controls.Add(this.cbxPresetExePath);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtPresetTitle);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GameMatcherForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Preset Manager";
            ((System.ComponentModel.ISupportInitialize)(this.presetCategoryPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtPresetTitle;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbxPresetExePath;
        private AutoCompleteTextBox txtPresetCategory;
        private System.Windows.Forms.PictureBox presetCategoryPictureBox;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ListView presetsListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
    }
}