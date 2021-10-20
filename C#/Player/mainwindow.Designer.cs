namespace Player
{
    partial class mainwindow
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.button_PlayOrStop = new System.Windows.Forms.Button();
            this.button_Setting = new System.Windows.Forms.Button();
            this.button_Refresh = new System.Windows.Forms.Button();
            this.comboBox_DeviceList = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.timer_DevInfo = new System.Windows.Forms.Timer(this.components);
            this.label_DevInfo = new System.Windows.Forms.Label();
            this.SaveImageBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // button_PlayOrStop
            // 
            this.button_PlayOrStop.Location = new System.Drawing.Point(33, 46);
            this.button_PlayOrStop.Name = "button_PlayOrStop";
            this.button_PlayOrStop.Size = new System.Drawing.Size(75, 23);
            this.button_PlayOrStop.TabIndex = 0;
            this.button_PlayOrStop.Text = "Play";
            this.button_PlayOrStop.UseVisualStyleBackColor = true;
            this.button_PlayOrStop.Click += new System.EventHandler(this.button_PlayOrStop_Click);
            // 
            // button_Setting
            // 
            this.button_Setting.Enabled = true;
            this.button_Setting.Location = new System.Drawing.Point(159, 46);
            this.button_Setting.Name = "button_Setting";
            this.button_Setting.Size = new System.Drawing.Size(75, 23);
            this.button_Setting.TabIndex = 1;
            this.button_Setting.Text = "Setting";
            this.button_Setting.UseVisualStyleBackColor = true;
            this.button_Setting.Click += new System.EventHandler(this.button_Setting_Click);
            // 
            // button_Refresh
            // 
            this.button_Refresh.Location = new System.Drawing.Point(288, 7);
            this.button_Refresh.Name = "button_Refresh";
            this.button_Refresh.Size = new System.Drawing.Size(75, 23);
            this.button_Refresh.TabIndex = 3;
            this.button_Refresh.Text = "Refresh";
            this.button_Refresh.UseVisualStyleBackColor = true;
            this.button_Refresh.Click += new System.EventHandler(this.button_Refresh_Click);
            // 
            // comboBox_DeviceList
            // 
            this.comboBox_DeviceList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_DeviceList.FormattingEnabled = true;
            this.comboBox_DeviceList.Location = new System.Drawing.Point(130, 9);
            this.comboBox_DeviceList.Name = "comboBox_DeviceList";
            this.comboBox_DeviceList.Size = new System.Drawing.Size(121, 20);
            this.comboBox_DeviceList.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(33, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "Device List:";
            // 
            // pictureBox
            // 
            this.pictureBox.Location = new System.Drawing.Point(12, 87);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(822, 493);
            this.pictureBox.TabIndex = 7;
            this.pictureBox.TabStop = false;
            // 
            // timer_DevInfo
            // 
            this.timer_DevInfo.Interval = 1000;
            this.timer_DevInfo.Tick += new System.EventHandler(this.timer_DevInfo_Tick);
            // 
            // label_DevInfo
            // 
            this.label_DevInfo.AutoSize = true;
            this.label_DevInfo.Location = new System.Drawing.Point(12, 593);
            this.label_DevInfo.Name = "label_DevInfo";
            this.label_DevInfo.Size = new System.Drawing.Size(41, 12);
            this.label_DevInfo.TabIndex = 8;
            this.label_DevInfo.Text = "label1";
            // 
            // SaveImageBtn
            // 
            this.SaveImageBtn.Location = new System.Drawing.Point(437, 46);
            this.SaveImageBtn.Name = "SaveImageBtn";
            this.SaveImageBtn.Size = new System.Drawing.Size(75, 23);
            this.SaveImageBtn.TabIndex = 9;
            this.SaveImageBtn.Text = "Save Image";
            this.SaveImageBtn.UseVisualStyleBackColor = true;
            this.SaveImageBtn.Click += new System.EventHandler(this.SaveImageBtn_Click);
            // 
            // mainwindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(846, 614);
            this.Controls.Add(this.SaveImageBtn);
            this.Controls.Add(this.label_DevInfo);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_DeviceList);
            this.Controls.Add(this.button_Refresh);
            this.Controls.Add(this.button_Setting);
            this.Controls.Add(this.button_PlayOrStop);
            this.Name = "mainwindow";
            this.Text = "Player";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_PlayOrStop;
        private System.Windows.Forms.Button button_Setting;
        private System.Windows.Forms.Button button_Refresh;
        private System.Windows.Forms.ComboBox comboBox_DeviceList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Timer timer_DevInfo;
        private System.Windows.Forms.Label label_DevInfo;
        private System.Windows.Forms.Button SaveImageBtn;
    }
}

