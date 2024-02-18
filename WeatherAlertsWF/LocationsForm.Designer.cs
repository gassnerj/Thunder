namespace WeatherAlertsWF
{
    partial class LocationsForm
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
            this.statesListbox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.myStatesListbox = new System.Windows.Forms.ListBox();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // statesListbox
            // 
            this.statesListbox.FormattingEnabled = true;
            this.statesListbox.ItemHeight = 20;
            this.statesListbox.Location = new System.Drawing.Point(17, 53);
            this.statesListbox.Name = "statesListbox";
            this.statesListbox.Size = new System.Drawing.Size(196, 384);
            this.statesListbox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "States";
            // 
            // myStatesListbox
            // 
            this.myStatesListbox.FormattingEnabled = true;
            this.myStatesListbox.ItemHeight = 20;
            this.myStatesListbox.Location = new System.Drawing.Point(319, 54);
            this.myStatesListbox.Name = "myStatesListbox";
            this.myStatesListbox.Size = new System.Drawing.Size(196, 384);
            this.myStatesListbox.TabIndex = 2;
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(219, 210);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(94, 29);
            this.addButton.TabIndex = 3;
            this.addButton.Text = ">>";
            this.addButton.UseVisualStyleBackColor = true;
            // 
            // removeButton
            // 
            this.removeButton.Location = new System.Drawing.Point(219, 245);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(94, 29);
            this.removeButton.TabIndex = 4;
            this.removeButton.Text = "<<";
            this.removeButton.UseVisualStyleBackColor = true;
            // 
            // LocationsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(543, 450);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.myStatesListbox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.statesListbox);
            this.Name = "LocationsForm";
            this.Text = "LocationsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox statesListbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox myStatesListbox;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
    }
}