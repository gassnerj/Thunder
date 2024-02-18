namespace WeatherAlertsWF
{
    partial class AlertForm
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
            this.headerPanel = new System.Windows.Forms.Panel();
            this.effectiveLabel = new System.Windows.Forms.Label();
            this.eventLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.headerPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerPanel
            // 
            this.headerPanel.Controls.Add(this.effectiveLabel);
            this.headerPanel.Controls.Add(this.eventLabel);
            this.headerPanel.Location = new System.Drawing.Point(12, 12);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(1299, 222);
            this.headerPanel.TabIndex = 0;
            // 
            // effectiveLabel
            // 
            this.effectiveLabel.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.effectiveLabel.Location = new System.Drawing.Point(12, 121);
            this.effectiveLabel.Name = "effectiveLabel";
            this.effectiveLabel.Size = new System.Drawing.Size(873, 92);
            this.effectiveLabel.TabIndex = 1;
            this.effectiveLabel.Text = "label1";
            // 
            // eventLabel
            // 
            this.eventLabel.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.eventLabel.Location = new System.Drawing.Point(12, 19);
            this.eventLabel.Name = "eventLabel";
            this.eventLabel.Size = new System.Drawing.Size(873, 92);
            this.eventLabel.TabIndex = 0;
            this.eventLabel.Text = "label1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(12, 270);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1299, 386);
            this.panel1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(1269, 203);
            this.label2.TabIndex = 0;
            this.label2.Text = "label1";
            // 
            // AlertForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1323, 668);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.headerPanel);
            this.Name = "AlertForm";
            this.Text = "AlertForm";
            this.headerPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label eventLabel;
        private System.Windows.Forms.Label effectiveLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
    }
}