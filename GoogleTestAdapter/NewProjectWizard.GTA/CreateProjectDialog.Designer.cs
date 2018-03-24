namespace NewProjectWizard.GTA
{
    partial class CreateProjectDialog
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
            this.rootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.selectGtestProjectTitleLabel = new System.Windows.Forms.Label();
            this.gtestProjectComboBox = new System.Windows.Forms.ComboBox();
            this.selectProjectsUnderTestTitleLabel = new System.Windows.Forms.Label();
            this.projectsUnderTestCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.rootTableLayoutPanel.SuspendLayout();
            this.buttonsFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.descriptionLabel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.selectGtestProjectTitleLabel, 0, 1);
            this.rootTableLayoutPanel.Controls.Add(this.gtestProjectComboBox, 0, 2);
            this.rootTableLayoutPanel.Controls.Add(this.selectProjectsUnderTestTitleLabel, 0, 3);
            this.rootTableLayoutPanel.Controls.Add(this.projectsUnderTestCheckedListBox, 0, 4);
            this.rootTableLayoutPanel.Controls.Add(this.buttonsFlowLayoutPanel, 0, 5);
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 6;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(334, 361);
            this.rootTableLayoutPanel.TabIndex = 0;
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.Location = new System.Drawing.Point(3, 3);
            this.descriptionLabel.Margin = new System.Windows.Forms.Padding(3);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(328, 26);
            this.descriptionLabel.TabIndex = 3;
            this.descriptionLabel.Text = "The selected projects will be added to the created Google Test project as referen" +
    "ces.";
            // 
            // selectGtestProjectTitleLabel
            // 
            this.selectGtestProjectTitleLabel.AutoSize = true;
            this.selectGtestProjectTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectGtestProjectTitleLabel.Location = new System.Drawing.Point(3, 48);
            this.selectGtestProjectTitleLabel.Margin = new System.Windows.Forms.Padding(3, 16, 3, 0);
            this.selectGtestProjectTitleLabel.Name = "selectGtestProjectTitleLabel";
            this.selectGtestProjectTitleLabel.Size = new System.Drawing.Size(215, 13);
            this.selectGtestProjectTitleLabel.TabIndex = 6;
            this.selectGtestProjectTitleLabel.Text = "Select project providing Google Test";
            // 
            // gtestProjectComboBox
            // 
            this.gtestProjectComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gtestProjectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gtestProjectComboBox.FormattingEnabled = true;
            this.gtestProjectComboBox.Location = new System.Drawing.Point(3, 64);
            this.gtestProjectComboBox.Name = "gtestProjectComboBox";
            this.gtestProjectComboBox.Size = new System.Drawing.Size(328, 21);
            this.gtestProjectComboBox.TabIndex = 0;
            // 
            // selectProjectsUnderTestTitleLabel
            // 
            this.selectProjectsUnderTestTitleLabel.AutoSize = true;
            this.selectProjectsUnderTestTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectProjectsUnderTestTitleLabel.Location = new System.Drawing.Point(3, 104);
            this.selectProjectsUnderTestTitleLabel.Margin = new System.Windows.Forms.Padding(3, 16, 3, 0);
            this.selectProjectsUnderTestTitleLabel.Name = "selectProjectsUnderTestTitleLabel";
            this.selectProjectsUnderTestTitleLabel.Size = new System.Drawing.Size(172, 13);
            this.selectProjectsUnderTestTitleLabel.TabIndex = 7;
            this.selectProjectsUnderTestTitleLabel.Text = "Select project(s) to be tested";
            // 
            // projectsUnderTestCheckedListBox
            // 
            this.projectsUnderTestCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.projectsUnderTestCheckedListBox.FormattingEnabled = true;
            this.projectsUnderTestCheckedListBox.Location = new System.Drawing.Point(3, 120);
            this.projectsUnderTestCheckedListBox.MinimumSize = new System.Drawing.Size(4, 100);
            this.projectsUnderTestCheckedListBox.Name = "projectsUnderTestCheckedListBox";
            this.projectsUnderTestCheckedListBox.Size = new System.Drawing.Size(328, 199);
            this.projectsUnderTestCheckedListBox.Sorted = true;
            this.projectsUnderTestCheckedListBox.TabIndex = 1;
            // 
            // buttonsFlowLayoutPanel
            // 
            this.buttonsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonsFlowLayoutPanel.AutoSize = true;
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
            // CreateProjectDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(334, 361);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.MinimumSize = new System.Drawing.Size(231, 231);
            this.Name = "CreateProjectDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Google Test project";
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.rootTableLayoutPanel.PerformLayout();
            this.buttonsFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel buttonsFlowLayoutPanel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label selectGtestProjectTitleLabel;
        private System.Windows.Forms.ComboBox gtestProjectComboBox;
        private System.Windows.Forms.CheckedListBox projectsUnderTestCheckedListBox;
        private System.Windows.Forms.Label selectProjectsUnderTestTitleLabel;
        private System.Windows.Forms.Label descriptionLabel;
    }
}