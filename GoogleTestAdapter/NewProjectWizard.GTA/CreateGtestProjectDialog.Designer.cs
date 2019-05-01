namespace NewProjectWizard.GTA
{
    partial class CreateGtestProjectDialog
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
            this.rootTableLayoutPanel.Controls.Add(this.buttonsFlowLayoutPanel, 0, 5);
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(7);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 6;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(779, 805);
            this.rootTableLayoutPanel.TabIndex = 0;
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.Location = new System.Drawing.Point(7, 7);
            this.descriptionLabel.Margin = new System.Windows.Forms.Padding(7);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(765, 29);
            this.descriptionLabel.TabIndex = 3;
            this.descriptionLabel.Text = "Configure how Google Test is built.";
            // 
            // selectGtestProjectTitleLabel
            // 
            this.selectGtestProjectTitleLabel.AutoSize = true;
            this.selectGtestProjectTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectGtestProjectTitleLabel.Location = new System.Drawing.Point(7, 79);
            this.selectGtestProjectTitleLabel.Margin = new System.Windows.Forms.Padding(7, 36, 7, 0);
            this.selectGtestProjectTitleLabel.Name = "selectGtestProjectTitleLabel";
            this.selectGtestProjectTitleLabel.Size = new System.Drawing.Size(625, 29);
            this.selectGtestProjectTitleLabel.TabIndex = 6;
            this.selectGtestProjectTitleLabel.Text = "Select how the Google Test project shall be linked";
            // 
            // gtestProjectComboBox
            // 
            this.gtestProjectComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gtestProjectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gtestProjectComboBox.FormattingEnabled = true;
            this.gtestProjectComboBox.Location = new System.Drawing.Point(7, 115);
            this.gtestProjectComboBox.Margin = new System.Windows.Forms.Padding(7);
            this.gtestProjectComboBox.Name = "gtestProjectComboBox";
            this.gtestProjectComboBox.Size = new System.Drawing.Size(765, 37);
            this.gtestProjectComboBox.TabIndex = 0;
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
            this.buttonsFlowLayoutPanel.Location = new System.Drawing.Point(0, 727);
            this.buttonsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 7);
            this.buttonsFlowLayoutPanel.MinimumSize = new System.Drawing.Size(467, 71);
            this.buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            this.buttonsFlowLayoutPanel.Size = new System.Drawing.Size(779, 71);
            this.buttonsFlowLayoutPanel.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(574, 7);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(7);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(198, 51);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.okButton.Location = new System.Drawing.Point(362, 7);
            this.okButton.Margin = new System.Windows.Forms.Padding(7);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(198, 51);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "Create project";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // CreateGtestProjectDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(779, 805);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Margin = new System.Windows.Forms.Padding(7);
            this.MinimumSize = new System.Drawing.Size(502, 418);
            this.Name = "CreateGtestProjectDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create project containig Google Test";
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.rootTableLayoutPanel.PerformLayout();
            this.buttonsFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.Label selectGtestProjectTitleLabel;
        private System.Windows.Forms.ComboBox gtestProjectComboBox;
        private System.Windows.Forms.FlowLayoutPanel buttonsFlowLayoutPanel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
    }
}