namespace GPSMonitor
{
    partial class GPSMonitorForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serialPortsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.textBoxRefreshInterval = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.maskedTextBoxApiKey = new System.Windows.Forms.MaskedTextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.labelState = new System.Windows.Forms.Label();
            this.labelCounty = new System.Windows.Forms.Label();
            this.labelRoad = new System.Windows.Forms.Label();
            this.labelCity = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.textBoxGpsLog = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.labelHeading = new System.Windows.Forms.Label();
            this.labelAltimeter = new System.Windows.Forms.Label();
            this.labelCoords = new System.Windows.Forms.Label();
            this.labelSpeed = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.gpsStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItem1.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serialPortsToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // serialPortsToolStripMenuItem
            // 
            this.serialPortsToolStripMenuItem.Name = "serialPortsToolStripMenuItem";
            this.serialPortsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.serialPortsToolStripMenuItem.Text = "SerialPorts";
            this.serialPortsToolStripMenuItem.Click += new System.EventHandler(this.serialPortsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Location = new System.Drawing.Point(0, 27);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 479);
            this.panel1.TabIndex = 1;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.textBoxRefreshInterval);
            this.groupBox5.Controls.Add(this.label14);
            this.groupBox5.Controls.Add(this.label13);
            this.groupBox5.Controls.Add(this.maskedTextBoxApiKey);
            this.groupBox5.Location = new System.Drawing.Point(12, 3);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(400, 132);
            this.groupBox5.TabIndex = 7;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Google API";
            // 
            // textBoxRefreshInterval
            // 
            this.textBoxRefreshInterval.Location = new System.Drawing.Point(103, 43);
            this.textBoxRefreshInterval.Name = "textBoxRefreshInterval";
            this.textBoxRefreshInterval.Size = new System.Drawing.Size(100, 23);
            this.textBoxRefreshInterval.TabIndex = 3;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(9, 46);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(88, 15);
            this.label14.TabIndex = 2;
            this.label14.Text = "Refresh Interval";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(50, 17);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(47, 15);
            this.label13.TabIndex = 1;
            this.label13.Text = "API Key";
            // 
            // maskedTextBoxApiKey
            // 
            this.maskedTextBoxApiKey.Location = new System.Drawing.Point(103, 14);
            this.maskedTextBoxApiKey.Name = "maskedTextBoxApiKey";
            this.maskedTextBoxApiKey.Size = new System.Drawing.Size(252, 23);
            this.maskedTextBoxApiKey.TabIndex = 0;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.labelState);
            this.groupBox4.Controls.Add(this.labelCounty);
            this.groupBox4.Controls.Add(this.labelRoad);
            this.groupBox4.Controls.Add(this.labelCity);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Location = new System.Drawing.Point(12, 287);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(400, 129);
            this.groupBox4.TabIndex = 6;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Location Information";
            // 
            // labelState
            // 
            this.labelState.AutoSize = true;
            this.labelState.Location = new System.Drawing.Point(86, 106);
            this.labelState.Name = "labelState";
            this.labelState.Size = new System.Drawing.Size(16, 15);
            this.labelState.TabIndex = 15;
            this.labelState.Text = "...";
            // 
            // labelCounty
            // 
            this.labelCounty.AutoSize = true;
            this.labelCounty.Location = new System.Drawing.Point(86, 77);
            this.labelCounty.Name = "labelCounty";
            this.labelCounty.Size = new System.Drawing.Size(16, 15);
            this.labelCounty.TabIndex = 14;
            this.labelCounty.Text = "...";
            // 
            // labelRoad
            // 
            this.labelRoad.AutoSize = true;
            this.labelRoad.Location = new System.Drawing.Point(86, 19);
            this.labelRoad.Name = "labelRoad";
            this.labelRoad.Size = new System.Drawing.Size(16, 15);
            this.labelRoad.TabIndex = 13;
            this.labelRoad.Text = "...";
            // 
            // labelCity
            // 
            this.labelCity.AutoSize = true;
            this.labelCity.Location = new System.Drawing.Point(86, 48);
            this.labelCity.Name = "labelCity";
            this.labelCity.Size = new System.Drawing.Size(16, 15);
            this.labelCity.TabIndex = 12;
            this.labelCity.Text = "...";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(47, 106);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(33, 15);
            this.label9.TabIndex = 11;
            this.label9.Text = "State";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(34, 77);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(46, 15);
            this.label10.TabIndex = 10;
            this.label10.Text = "County";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(52, 48);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(28, 15);
            this.label11.TabIndex = 9;
            this.label11.Text = "City";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(46, 19);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(34, 15);
            this.label12.TabIndex = 8;
            this.label12.Text = "Road";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBoxGpsLog);
            this.groupBox3.Location = new System.Drawing.Point(427, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(369, 473);
            this.groupBox3.TabIndex = 5;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "GPS Log";
            // 
            // textBoxGpsLog
            // 
            this.textBoxGpsLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxGpsLog.BackColor = System.Drawing.Color.Black;
            this.textBoxGpsLog.ForeColor = System.Drawing.Color.White;
            this.textBoxGpsLog.Location = new System.Drawing.Point(6, 22);
            this.textBoxGpsLog.Multiline = true;
            this.textBoxGpsLog.Name = "textBoxGpsLog";
            this.textBoxGpsLog.Size = new System.Drawing.Size(357, 442);
            this.textBoxGpsLog.TabIndex = 2;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.labelHeading);
            this.groupBox2.Controls.Add(this.labelAltimeter);
            this.groupBox2.Controls.Add(this.labelCoords);
            this.groupBox2.Controls.Add(this.labelSpeed);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(12, 141);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(402, 140);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "GPS Data";
            // 
            // labelHeading
            // 
            this.labelHeading.AutoSize = true;
            this.labelHeading.Location = new System.Drawing.Point(85, 112);
            this.labelHeading.Name = "labelHeading";
            this.labelHeading.Size = new System.Drawing.Size(16, 15);
            this.labelHeading.TabIndex = 7;
            this.labelHeading.Text = "...";
            // 
            // labelAltimeter
            // 
            this.labelAltimeter.AutoSize = true;
            this.labelAltimeter.Location = new System.Drawing.Point(85, 83);
            this.labelAltimeter.Name = "labelAltimeter";
            this.labelAltimeter.Size = new System.Drawing.Size(16, 15);
            this.labelAltimeter.TabIndex = 6;
            this.labelAltimeter.Text = "...";
            // 
            // labelCoords
            // 
            this.labelCoords.AutoSize = true;
            this.labelCoords.Location = new System.Drawing.Point(85, 25);
            this.labelCoords.Name = "labelCoords";
            this.labelCoords.Size = new System.Drawing.Size(16, 15);
            this.labelCoords.TabIndex = 5;
            this.labelCoords.Text = "...";
            // 
            // labelSpeed
            // 
            this.labelSpeed.AutoSize = true;
            this.labelSpeed.Location = new System.Drawing.Point(85, 54);
            this.labelSpeed.Name = "labelSpeed";
            this.labelSpeed.Size = new System.Drawing.Size(16, 15);
            this.labelSpeed.TabIndex = 4;
            this.labelSpeed.Text = "...";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(27, 112);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 15);
            this.label4.TabIndex = 3;
            this.label4.Text = "Heading";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(30, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "Altitude";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(40, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "Speed";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Coordinates";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnStart);
            this.groupBox1.Controls.Add(this.btnStop);
            this.groupBox1.Location = new System.Drawing.Point(12, 422);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(400, 54);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GPS Controls";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(6, 22);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(87, 22);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gpsStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 509);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // gpsStatusLabel
            // 
            this.gpsStatusLabel.Name = "gpsStatusLabel";
            this.gpsStatusLabel.Size = new System.Drawing.Size(118, 17);
            this.gpsStatusLabel.Text = "toolStripStatusLabel1";
            // 
            // GPSMonitorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 531);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GPSMonitorForm";
            this.Text = "Form1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MenuStrip menuStrip1;
        private Panel panel1;
        private Button btnStop;
        private Button btnStart;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel gpsStatusLabel;
        private TextBox textBoxGpsLog;
        private GroupBox groupBox1;
        private GroupBox groupBox3;
        private GroupBox groupBox2;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label labelHeading;
        private Label labelAltimeter;
        private Label labelCoords;
        private Label labelSpeed;
        private GroupBox groupBox4;
        private Label labelState;
        private Label labelCounty;
        private Label labelRoad;
        private Label labelCity;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label label12;
        private GroupBox groupBox5;
        private MaskedTextBox maskedTextBoxApiKey;
        private Label label14;
        private Label label13;
        private TextBox textBoxRefreshInterval;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem serialPortsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
    }
}