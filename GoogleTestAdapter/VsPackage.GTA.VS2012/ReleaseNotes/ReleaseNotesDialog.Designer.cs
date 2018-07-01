namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    partial class ReleaseNotesDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReleaseNotesDialog));
            this.RootPanel = new System.Windows.Forms.TableLayoutPanel();
            this.NavigationPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.BackButton = new System.Windows.Forms.Button();
            this.ForwardButton = new System.Windows.Forms.Button();
            this.WebBrowser = new System.Windows.Forms.WebBrowser();
            this.OkButtonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ShowReleaseNotesCheckBox = new System.Windows.Forms.CheckBox();
            this.OkButton = new System.Windows.Forms.Button();
            this.RootPanel.SuspendLayout();
            this.NavigationPanel.SuspendLayout();
            this.OkButtonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // RootPanel
            // 
            this.RootPanel.ColumnCount = 2;
            this.RootPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.RootPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.RootPanel.Controls.Add(this.NavigationPanel, 0, 0);
            this.RootPanel.Controls.Add(this.WebBrowser, 0, 1);
            this.RootPanel.Controls.Add(this.OkButtonPanel, 0, 2);
            this.RootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RootPanel.Location = new System.Drawing.Point(0, 0);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.RowCount = 3;
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.RootPanel.Size = new System.Drawing.Size(784, 561);
            this.RootPanel.TabIndex = 0;
            // 
            // NavigationPanel
            // 
            this.NavigationPanel.AutoSize = true;
            this.RootPanel.SetColumnSpan(this.NavigationPanel, 2);
            this.NavigationPanel.Controls.Add(this.BackButton);
            this.NavigationPanel.Controls.Add(this.ForwardButton);
            this.NavigationPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NavigationPanel.Location = new System.Drawing.Point(3, 3);
            this.NavigationPanel.Name = "NavigationPanel";
            this.NavigationPanel.Size = new System.Drawing.Size(778, 29);
            this.NavigationPanel.TabIndex = 3;
            // 
            // BackButton
            // 
            this.BackButton.AccessibleName = "Go back";
            this.BackButton.Enabled = false;
            this.BackButton.Location = new System.Drawing.Point(3, 3);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(23, 23);
            this.BackButton.TabIndex = 0;
            this.BackButton.Text = "<";
            this.BackButton.UseVisualStyleBackColor = true;
            // 
            // ForwardButton
            // 
            this.ForwardButton.AccessibleName = "Go forward";
            this.ForwardButton.Enabled = false;
            this.ForwardButton.Location = new System.Drawing.Point(32, 3);
            this.ForwardButton.Name = "ForwardButton";
            this.ForwardButton.Size = new System.Drawing.Size(23, 23);
            this.ForwardButton.TabIndex = 1;
            this.ForwardButton.Text = ">";
            this.ForwardButton.UseVisualStyleBackColor = true;
            // 
            // WebBrowser
            // 
            this.WebBrowser.AccessibleName = "Browser";
            this.WebBrowser.AllowWebBrowserDrop = false;
            this.RootPanel.SetColumnSpan(this.WebBrowser, 2);
            this.WebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WebBrowser.IsWebBrowserContextMenuEnabled = false;
            this.WebBrowser.Location = new System.Drawing.Point(3, 38);
            this.WebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.WebBrowser.Name = "WebBrowser";
            this.WebBrowser.ScriptErrorsSuppressed = true;
            this.WebBrowser.Size = new System.Drawing.Size(778, 485);
            this.WebBrowser.TabIndex = 0;
            this.WebBrowser.WebBrowserShortcutsEnabled = false;
            this.WebBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.WebBrowser_Navigating);
            // 
            // OkButtonPanel
            // 
            this.OkButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButtonPanel.AutoSize = true;
            this.OkButtonPanel.ColumnCount = 3;
            this.RootPanel.SetColumnSpan(this.OkButtonPanel, 2);
            this.OkButtonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.OkButtonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.OkButtonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.OkButtonPanel.Controls.Add(this.ShowReleaseNotesCheckBox, 0, 0);
            this.OkButtonPanel.Controls.Add(this.OkButton, 1, 0);
            this.OkButtonPanel.Location = new System.Drawing.Point(3, 529);
            this.OkButtonPanel.Name = "OkButtonPanel";
            this.OkButtonPanel.RowCount = 1;
            this.OkButtonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OkButtonPanel.Size = new System.Drawing.Size(778, 29);
            this.OkButtonPanel.TabIndex = 4;
            // 
            // ShowReleaseNotesCheckBox
            // 
            this.ShowReleaseNotesCheckBox.AccessibleName = "Show release notes after extension has been updated";
            this.ShowReleaseNotesCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ShowReleaseNotesCheckBox.AutoSize = true;
            this.ShowReleaseNotesCheckBox.Location = new System.Drawing.Point(3, 6);
            this.ShowReleaseNotesCheckBox.Name = "ShowReleaseNotesCheckBox";
            this.ShowReleaseNotesCheckBox.Size = new System.Drawing.Size(179, 17);
            this.ShowReleaseNotesCheckBox.TabIndex = 1;
            this.ShowReleaseNotesCheckBox.Text = "Show release notes after update";
            this.ShowReleaseNotesCheckBox.UseVisualStyleBackColor = true;
            // 
            // OkButton
            // 
            this.OkButton.AccessibleName = "Close release notes dialog";
            this.OkButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OkButton.Location = new System.Drawing.Point(351, 3);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 2;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            // 
            // ReleaseNotesDialog
            // 
            this.AcceptButton = this.OkButton;
            this.AccessibleName = "Release notes of Google Test Adapter";
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.OkButton;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.RootPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ReleaseNotesDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Google Test Adapter: Release notes";
            this.RootPanel.ResumeLayout(false);
            this.RootPanel.PerformLayout();
            this.NavigationPanel.ResumeLayout(false);
            this.OkButtonPanel.ResumeLayout(false);
            this.OkButtonPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel RootPanel;
        private System.Windows.Forms.WebBrowser WebBrowser;
        private System.Windows.Forms.CheckBox ShowReleaseNotesCheckBox;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.FlowLayoutPanel NavigationPanel;
        private System.Windows.Forms.Button BackButton;
        private System.Windows.Forms.Button ForwardButton;
        private System.Windows.Forms.TableLayoutPanel OkButtonPanel;
    }
}