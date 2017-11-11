namespace NewProjectWizard.GTA
{
    partial class SinglePageWizard
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SinglePageWizard));
            this.rootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.selectedProjectsCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.titleLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.rootTableLayoutPanel.SuspendLayout();
            this.buttonsFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rootTableLayoutPanel.ColumnCount = 2;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.selectedProjectsCheckedListBox, 0, 2);
            this.rootTableLayoutPanel.Controls.Add(this.buttonsFlowLayoutPanel, 0, 3);
            this.rootTableLayoutPanel.Controls.Add(this.titleLabel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.descriptionLabel, 0, 1);
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 4;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(334, 361);
            this.rootTableLayoutPanel.TabIndex = 0;
            // 
            // selectedProjectsCheckedListBox
            // 
            this.selectedProjectsCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rootTableLayoutPanel.SetColumnSpan(this.selectedProjectsCheckedListBox, 2);
            this.selectedProjectsCheckedListBox.FormattingEnabled = true;
            this.selectedProjectsCheckedListBox.Location = new System.Drawing.Point(3, 54);
            this.selectedProjectsCheckedListBox.MinimumSize = new System.Drawing.Size(4, 100);
            this.selectedProjectsCheckedListBox.Name = "selectedProjectsCheckedListBox";
            this.selectedProjectsCheckedListBox.Size = new System.Drawing.Size(328, 259);
            this.selectedProjectsCheckedListBox.Sorted = true;
            this.selectedProjectsCheckedListBox.TabIndex = 1;
            this.selectedProjectsCheckedListBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.selectedProjectsCheckedListBox_MouseMove);
            // 
            // buttonsFlowLayoutPanel
            // 
            this.buttonsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonsFlowLayoutPanel.AutoSize = true;
            this.rootTableLayoutPanel.SetColumnSpan(this.buttonsFlowLayoutPanel, 2);
            this.buttonsFlowLayoutPanel.Controls.Add(this.cancelButton);
            this.buttonsFlowLayoutPanel.Controls.Add(this.okButton);
            this.buttonsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonsFlowLayoutPanel.Location = new System.Drawing.Point(0, 326);
            this.buttonsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.buttonsFlowLayoutPanel.MinimumSize = new System.Drawing.Size(200, 32);
            this.buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            this.buttonsFlowLayoutPanel.Size = new System.Drawing.Size(334, 32);
            this.buttonsFlowLayoutPanel.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(246, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(85, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.okButton.Location = new System.Drawing.Point(155, 3);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(85, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "Create project";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titleLabel.AutoSize = true;
            this.rootTableLayoutPanel.SetColumnSpan(this.titleLabel, 2);
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(3, 3);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(3);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(328, 13);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Select project(s) under test";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionLabel.AutoSize = true;
            this.rootTableLayoutPanel.SetColumnSpan(this.descriptionLabel, 2);
            this.descriptionLabel.Location = new System.Drawing.Point(3, 22);
            this.descriptionLabel.Margin = new System.Windows.Forms.Padding(3);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(328, 26);
            this.descriptionLabel.TabIndex = 3;
            this.descriptionLabel.Text = "The selected projects will be added to the created Google Test project as referen" +
    "ces.";
            // 
            // SinglePageWizard
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(334, 361);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(231, 231);
            this.Name = "SinglePageWizard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Google Test project";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SinglePageWizard_FormClosed);
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.rootTableLayoutPanel.PerformLayout();
            this.buttonsFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.FlowLayoutPanel buttonsFlowLayoutPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.CheckedListBox selectedProjectsCheckedListBox;
        private System.Windows.Forms.Label descriptionLabel;
    }
}