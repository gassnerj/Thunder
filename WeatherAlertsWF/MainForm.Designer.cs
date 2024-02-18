namespace WeatherAlertsWF
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.alertsDataGrid = new System.Windows.Forms.DataGridView();
            this.alertBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.featureCollectionBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.featureListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tornadoCheckBox = new System.Windows.Forms.CheckBox();
            this.severeCheckBox = new System.Windows.Forms.CheckBox();
            this.flashFloodCheckBox = new System.Windows.Forms.CheckBox();
            this.floodCheckBox = new System.Windows.Forms.CheckBox();
            this.blizzardCheckBox = new System.Windows.Forms.CheckBox();
            this.allCheckBox = new System.Windows.Forms.CheckBox();
            this.gpsCoords = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.myLocationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lastRefreshStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.alertsDataGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.alertBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.featureCollectionBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.featureListBindingSource)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // alertsDataGrid
            // 
            this.alertsDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.alertsDataGrid.AutoGenerateColumns = false;
            this.alertsDataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.alertsDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.alertsDataGrid.DataSource = this.alertBindingSource;
            this.alertsDataGrid.Location = new System.Drawing.Point(10, 80);
            this.alertsDataGrid.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.alertsDataGrid.MultiSelect = false;
            this.alertsDataGrid.Name = "alertsDataGrid";
            this.alertsDataGrid.ReadOnly = true;
            this.alertsDataGrid.RowHeadersWidth = 51;
            this.alertsDataGrid.RowTemplate.Height = 29;
            this.alertsDataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.alertsDataGrid.ShowEditingIcon = false;
            this.alertsDataGrid.Size = new System.Drawing.Size(1133, 388);
            this.alertsDataGrid.TabIndex = 0;
            // 
            // featureListBindingSource
            // 
            this.featureListBindingSource.DataMember = "FeatureList";
            this.featureListBindingSource.DataSource = this.featureCollectionBindingSource;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.lastRefreshStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 484);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 12, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1154, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // tornadoCheckBox
            // 
            this.tornadoCheckBox.AutoSize = true;
            this.tornadoCheckBox.Location = new System.Drawing.Point(10, 40);
            this.tornadoCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tornadoCheckBox.Name = "tornadoCheckBox";
            this.tornadoCheckBox.Size = new System.Drawing.Size(117, 19);
            this.tornadoCheckBox.TabIndex = 2;
            this.tornadoCheckBox.Text = "Tornado Warning";
            this.tornadoCheckBox.UseVisualStyleBackColor = true;
            // 
            // severeCheckBox
            // 
            this.severeCheckBox.AutoSize = true;
            this.severeCheckBox.Location = new System.Drawing.Point(143, 40);
            this.severeCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.severeCheckBox.Name = "severeCheckBox";
            this.severeCheckBox.Size = new System.Drawing.Size(186, 19);
            this.severeCheckBox.TabIndex = 3;
            this.severeCheckBox.Text = "Severe Thunderstorm Warning";
            this.severeCheckBox.UseVisualStyleBackColor = true;
            // 
            // flashFloodCheckBox
            // 
            this.flashFloodCheckBox.AutoSize = true;
            this.flashFloodCheckBox.Location = new System.Drawing.Point(349, 40);
            this.flashFloodCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.flashFloodCheckBox.Name = "flashFloodCheckBox";
            this.flashFloodCheckBox.Size = new System.Drawing.Size(134, 19);
            this.flashFloodCheckBox.TabIndex = 4;
            this.flashFloodCheckBox.Text = "Flash Flood Warning";
            this.flashFloodCheckBox.UseVisualStyleBackColor = true;
            // 
            // floodCheckBox
            // 
            this.floodCheckBox.AutoSize = true;
            this.floodCheckBox.Location = new System.Drawing.Point(499, 40);
            this.floodCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.floodCheckBox.Name = "floodCheckBox";
            this.floodCheckBox.Size = new System.Drawing.Size(104, 19);
            this.floodCheckBox.TabIndex = 5;
            this.floodCheckBox.Text = "Flood Warning";
            this.floodCheckBox.UseVisualStyleBackColor = true;
            // 
            // blizzardCheckBox
            // 
            this.blizzardCheckBox.AutoSize = true;
            this.blizzardCheckBox.Location = new System.Drawing.Point(616, 40);
            this.blizzardCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.blizzardCheckBox.Name = "blizzardCheckBox";
            this.blizzardCheckBox.Size = new System.Drawing.Size(114, 19);
            this.blizzardCheckBox.TabIndex = 6;
            this.blizzardCheckBox.Text = "Blizzard Warning";
            this.blizzardCheckBox.UseVisualStyleBackColor = true;
            // 
            // allCheckBox
            // 
            this.allCheckBox.AutoSize = true;
            this.allCheckBox.Location = new System.Drawing.Point(746, 40);
            this.allCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.allCheckBox.Name = "allCheckBox";
            this.allCheckBox.Size = new System.Drawing.Size(40, 19);
            this.allCheckBox.TabIndex = 7;
            this.allCheckBox.Text = "All";
            this.allCheckBox.UseVisualStyleBackColor = true;
            // 
            // gpsCoords
            // 
            this.gpsCoords.Location = new System.Drawing.Point(924, 39);
            this.gpsCoords.Name = "gpsCoords";
            this.gpsCoords.Size = new System.Drawing.Size(220, 19);
            this.gpsCoords.TabIndex = 8;
            this.gpsCoords.Text = "label1";
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(1154, 24);
            this.menuStrip1.TabIndex = 9;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.myLocationsToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // myLocationsToolStripMenuItem
            // 
            this.myLocationsToolStripMenuItem.Name = "myLocationsToolStripMenuItem";
            this.myLocationsToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.myLocationsToolStripMenuItem.Text = "My Locations";
            // 
            // lastRefreshStatusLabel
            // 
            this.lastRefreshStatusLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.lastRefreshStatusLabel.Name = "lastRefreshStatusLabel";
            this.lastRefreshStatusLabel.Size = new System.Drawing.Size(118, 17);
            this.lastRefreshStatusLabel.Text = "toolStripStatusLabel2";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1154, 506);
            this.Controls.Add(this.gpsCoords);
            this.Controls.Add(this.allCheckBox);
            this.Controls.Add(this.blizzardCheckBox);
            this.Controls.Add(this.floodCheckBox);
            this.Controls.Add(this.flashFloodCheckBox);
            this.Controls.Add(this.severeCheckBox);
            this.Controls.Add(this.tornadoCheckBox);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.alertsDataGrid);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.alertsDataGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.alertBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.featureCollectionBindingSource)).EndInit();
            //((System.ComponentModel.ISupportInitialize)(this.featureListBindingSource)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView alertsDataGrid;
        private System.Windows.Forms.BindingSource featureCollectionBindingSource;
        private System.Windows.Forms.BindingSource featureListBindingSource;
        private System.Windows.Forms.BindingSource alertBindingSource;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn expiresDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn eventDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn senderNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn headlineDataGridViewTextBoxColumn;
        private System.Windows.Forms.CheckBox tornadoCheckBox;
        private System.Windows.Forms.CheckBox severeCheckBox;
        private System.Windows.Forms.CheckBox flashFloodCheckBox;
        private System.Windows.Forms.CheckBox floodCheckBox;
        private System.Windows.Forms.CheckBox blizzardCheckBox;
        private System.Windows.Forms.CheckBox allCheckBox;
        private System.Windows.Forms.Label gpsCoords;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem myLocationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel lastRefreshStatusLabel;
    }
}