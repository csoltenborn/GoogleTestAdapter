using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    partial class ReleaseNotesDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(ReleaseNotesDialog));
            this.RootPanel = new TableLayoutPanel();
            this.NavigationPanel = new FlowLayoutPanel();
            this.BackButton = new Button();
            this.ForwardButton = new Button();
            this.WebBrowser = new WebBrowser();
            this.OkButtonPanel = new TableLayoutPanel();
            this.DonateButton = new Button();
            this.OkButton = new Button();
            this.toolTip = new ToolTip(this.components);
            this.RootPanel.SuspendLayout();
            this.NavigationPanel.SuspendLayout();
            this.OkButtonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // RootPanel
            // 
            this.RootPanel.ColumnCount = 2;
            this.RootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.RootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.RootPanel.Controls.Add(this.NavigationPanel, 0, 0);
            this.RootPanel.Controls.Add(this.WebBrowser, 0, 1);
            this.RootPanel.Controls.Add(this.OkButtonPanel, 0, 2);
            this.RootPanel.Dock = DockStyle.Fill;
            this.RootPanel.Location = new Point(0, 0);
            this.RootPanel.Margin = new Padding(7);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.RowCount = 3;
            this.RootPanel.RowStyles.Add(new RowStyle());
            this.RootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.RootPanel.RowStyles.Add(new RowStyle());
            this.RootPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            this.RootPanel.Size = new Size(1829, 1251);
            this.RootPanel.TabIndex = 0;
            // 
            // NavigationPanel
            // 
            this.NavigationPanel.AutoSize = true;
            this.RootPanel.SetColumnSpan(this.NavigationPanel, 2);
            this.NavigationPanel.Controls.Add(this.BackButton);
            this.NavigationPanel.Controls.Add(this.ForwardButton);
            this.NavigationPanel.Dock = DockStyle.Fill;
            this.NavigationPanel.Location = new Point(7, 7);
            this.NavigationPanel.Margin = new Padding(7);
            this.NavigationPanel.Name = "NavigationPanel";
            this.NavigationPanel.Size = new Size(1815, 65);
            this.NavigationPanel.TabIndex = 3;
            // 
            // BackButton
            // 
            this.BackButton.AccessibleName = "Go back";
            this.BackButton.Enabled = false;
            this.BackButton.Location = new Point(7, 7);
            this.BackButton.Margin = new Padding(7);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new Size(54, 51);
            this.BackButton.TabIndex = 3;
            this.BackButton.Text = "<";
            this.BackButton.UseVisualStyleBackColor = true;
            // 
            // ForwardButton
            // 
            this.ForwardButton.AccessibleName = "Go forward";
            this.ForwardButton.Enabled = false;
            this.ForwardButton.Location = new Point(75, 7);
            this.ForwardButton.Margin = new Padding(7);
            this.ForwardButton.Name = "ForwardButton";
            this.ForwardButton.Size = new Size(54, 51);
            this.ForwardButton.TabIndex = 4;
            this.ForwardButton.Text = ">";
            this.ForwardButton.UseVisualStyleBackColor = true;
            // 
            // WebBrowser
            // 
            this.WebBrowser.AccessibleName = "Browser";
            this.WebBrowser.AllowWebBrowserDrop = false;
            this.RootPanel.SetColumnSpan(this.WebBrowser, 2);
            this.WebBrowser.Dock = DockStyle.Fill;
            this.WebBrowser.IsWebBrowserContextMenuEnabled = false;
            this.WebBrowser.Location = new Point(7, 86);
            this.WebBrowser.Margin = new Padding(7);
            this.WebBrowser.MinimumSize = new Size(47, 45);
            this.WebBrowser.Name = "WebBrowser";
            this.WebBrowser.ScriptErrorsSuppressed = true;
            this.WebBrowser.Size = new Size(1815, 1079);
            this.WebBrowser.TabIndex = 2;
            this.WebBrowser.WebBrowserShortcutsEnabled = false;
            this.WebBrowser.Navigating += new WebBrowserNavigatingEventHandler(this.WebBrowser_Navigating);
            // 
            // OkButtonPanel
            // 
            this.OkButtonPanel.Anchor = ((AnchorStyles)((AnchorStyles.Left | AnchorStyles.Right)));
            this.OkButtonPanel.AutoSize = true;
            this.OkButtonPanel.ColumnCount = 2;
            this.RootPanel.SetColumnSpan(this.OkButtonPanel, 2);
            this.OkButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            this.OkButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            this.OkButtonPanel.Controls.Add(this.DonateButton, 0, 0);
            this.OkButtonPanel.Controls.Add(this.OkButton, 1, 0);
            this.OkButtonPanel.Location = new Point(7, 1179);
            this.OkButtonPanel.Margin = new Padding(7);
            this.OkButtonPanel.Name = "OkButtonPanel";
            this.OkButtonPanel.RowCount = 1;
            this.OkButtonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.OkButtonPanel.Size = new Size(1815, 65);
            this.OkButtonPanel.TabIndex = 4;
            // 
            // DonateButton
            // 
            this.DonateButton.AccessibleName = "Close release notes dialog";
            this.DonateButton.Anchor = AnchorStyles.None;
            this.DonateButton.Location = new Point(303, 7);
            this.DonateButton.Margin = new Padding(7);
            this.DonateButton.Name = "DonateButton";
            this.DonateButton.Size = new Size(301, 51);
            this.DonateButton.TabIndex = 0;
            this.DonateButton.Text = "Cool - I want to donate!";
            this.DonateButton.UseVisualStyleBackColor = true;
            // 
            // OkButton
            // 
            this.OkButton.AccessibleName = "Close release notes dialog";
            this.OkButton.Anchor = AnchorStyles.None;
            this.OkButton.DialogResult = DialogResult.Cancel;
            this.OkButton.Location = new Point(1273, 7);
            this.OkButton.Margin = new Padding(7);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new Size(175, 51);
            this.OkButton.TabIndex = 1;
            this.OkButton.Text = "Thanks anyways...";
            this.OkButton.UseVisualStyleBackColor = true;
            // 
            // ReleaseNotesDialog
            // 
            this.AcceptButton = this.OkButton;
            this.AccessibleName = "Release notes of Google Test Adapter";
            this.AutoScaleDimensions = new SizeF(14F, 29F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.OkButton;
            this.CausesValidation = false;
            this.ClientSize = new Size(1829, 1251);
            this.Controls.Add(this.RootPanel);
            this.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new Padding(7);
            this.Name = "ReleaseNotesDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Google Test Adapter: Release notes";
            this.RootPanel.ResumeLayout(false);
            this.RootPanel.PerformLayout();
            this.NavigationPanel.ResumeLayout(false);
            this.OkButtonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel RootPanel;
        private WebBrowser WebBrowser;
        private Button OkButton;
        private FlowLayoutPanel NavigationPanel;
        private Button BackButton;
        private Button ForwardButton;
        private TableLayoutPanel OkButtonPanel;
        private ToolTip toolTip;
        private Button DonateButton;
    }
}