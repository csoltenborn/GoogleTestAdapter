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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReleaseNotesDialog));
            this.RootPanel = new System.Windows.Forms.TableLayoutPanel();
            this.NavigationPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.BackButton = new System.Windows.Forms.Button();
            this.ForwardButton = new System.Windows.Forms.Button();
            this.WebBrowser = new System.Windows.Forms.WebBrowser();
            this.OkButtonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.DonateButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
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
            this.RootPanel.Margin = new System.Windows.Forms.Padding(7);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.RowCount = 3;
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.RootPanel.Size = new System.Drawing.Size(1829, 1251);
            this.RootPanel.TabIndex = 0;
            // 
            // NavigationPanel
            // 
            this.NavigationPanel.AutoSize = true;
            this.RootPanel.SetColumnSpan(this.NavigationPanel, 2);
            this.NavigationPanel.Controls.Add(this.BackButton);
            this.NavigationPanel.Controls.Add(this.ForwardButton);
            this.NavigationPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NavigationPanel.Location = new System.Drawing.Point(7, 7);
            this.NavigationPanel.Margin = new System.Windows.Forms.Padding(7);
            this.NavigationPanel.Name = "NavigationPanel";
            this.NavigationPanel.Size = new System.Drawing.Size(1815, 65);
            this.NavigationPanel.TabIndex = 3;
            // 
            // BackButton
            // 
            this.BackButton.AccessibleName = "Go back";
            this.BackButton.Enabled = false;
            this.BackButton.Location = new System.Drawing.Point(7, 7);
            this.BackButton.Margin = new System.Windows.Forms.Padding(7);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(54, 51);
            this.BackButton.TabIndex = 3;
            this.BackButton.Text = "<";
            this.BackButton.UseVisualStyleBackColor = true;
            // 
            // ForwardButton
            // 
            this.ForwardButton.AccessibleName = "Go forward";
            this.ForwardButton.Enabled = false;
            this.ForwardButton.Location = new System.Drawing.Point(75, 7);
            this.ForwardButton.Margin = new System.Windows.Forms.Padding(7);
            this.ForwardButton.Name = "ForwardButton";
            this.ForwardButton.Size = new System.Drawing.Size(54, 51);
            this.ForwardButton.TabIndex = 4;
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
            this.WebBrowser.Location = new System.Drawing.Point(7, 86);
            this.WebBrowser.Margin = new System.Windows.Forms.Padding(7);
            this.WebBrowser.MinimumSize = new System.Drawing.Size(47, 45);
            this.WebBrowser.Name = "WebBrowser";
            this.WebBrowser.ScriptErrorsSuppressed = true;
            this.WebBrowser.Size = new System.Drawing.Size(1815, 1079);
            this.WebBrowser.TabIndex = 2;
            this.WebBrowser.WebBrowserShortcutsEnabled = false;
            this.WebBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.WebBrowser_Navigating);
            // 
            // OkButtonPanel
            // 
            this.OkButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButtonPanel.AutoSize = true;
            this.OkButtonPanel.ColumnCount = 2;
            this.RootPanel.SetColumnSpan(this.OkButtonPanel, 2);
            this.OkButtonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.OkButtonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.OkButtonPanel.Controls.Add(this.DonateButton, 0, 0);
            this.OkButtonPanel.Controls.Add(this.OkButton, 1, 0);
            this.OkButtonPanel.Location = new System.Drawing.Point(7, 1179);
            this.OkButtonPanel.Margin = new System.Windows.Forms.Padding(7);
            this.OkButtonPanel.Name = "OkButtonPanel";
            this.OkButtonPanel.RowCount = 1;
            this.OkButtonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OkButtonPanel.Size = new System.Drawing.Size(1815, 65);
            this.OkButtonPanel.TabIndex = 4;
            // 
            // DonateButton
            // 
            this.DonateButton.AccessibleName = "Close release notes dialog";
            this.DonateButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.DonateButton.Location = new System.Drawing.Point(303, 7);
            this.DonateButton.Margin = new System.Windows.Forms.Padding(7);
            this.DonateButton.Name = "DonateButton";
            this.DonateButton.Size = new System.Drawing.Size(301, 51);
            this.DonateButton.TabIndex = 0;
            this.DonateButton.Text = "Cool - I want to donate!";
            this.DonateButton.UseVisualStyleBackColor = true;
            // 
            // OkButton
            // 
            this.OkButton.AccessibleName = "Close release notes dialog";
            this.OkButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OkButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.OkButton.Location = new System.Drawing.Point(1231, 7);
            this.OkButton.Margin = new System.Windows.Forms.Padding(7);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(260, 51);
            this.OkButton.TabIndex = 1;
            this.OkButton.Text = "Thanks anyways...";
            this.OkButton.UseVisualStyleBackColor = true;
            // 
            // ReleaseNotesDialog
            // 
            this.AcceptButton = this.OkButton;
            this.AccessibleName = "Release notes of Google Test Adapter";
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.OkButton;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(1829, 1251);
            this.Controls.Add(this.RootPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(7);
            this.Name = "ReleaseNotesDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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