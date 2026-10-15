using System.ComponentModel;
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
            this.WebBrowser = new System.Windows.Forms.WebBrowser();
            this.OkButtonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.OkButton = new System.Windows.Forms.Button();
            this.RootPanel.SuspendLayout();
            this.OkButtonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // RootPanel
            //
            this.RootPanel.ColumnCount = 1;
            this.RootPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.RootPanel.Controls.Add(this.WebBrowser, 0, 0);
            this.RootPanel.Controls.Add(this.OkButtonPanel, 0, 1);
            this.RootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RootPanel.Location = new System.Drawing.Point(0, 0);
            this.RootPanel.Margin = new System.Windows.Forms.Padding(7);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.RowCount = 2;
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.RootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.RootPanel.Size = new System.Drawing.Size(1829, 1251);
            this.RootPanel.TabIndex = 0;
            //
            // WebBrowser
            //
            this.WebBrowser.AccessibleName = "Release notes";
            this.WebBrowser.AllowWebBrowserDrop = false;
            this.WebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WebBrowser.IsWebBrowserContextMenuEnabled = false;
            this.WebBrowser.Location = new System.Drawing.Point(7, 7);
            this.WebBrowser.Margin = new System.Windows.Forms.Padding(7);
            this.WebBrowser.MinimumSize = new System.Drawing.Size(47, 45);
            this.WebBrowser.Name = "WebBrowser";
            this.WebBrowser.ScriptErrorsSuppressed = true;
            this.WebBrowser.Size = new System.Drawing.Size(1815, 1158);
            this.WebBrowser.TabIndex = 2;
            this.WebBrowser.WebBrowserShortcutsEnabled = false;
            this.WebBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.WebBrowser_Navigating);
            //
            // OkButtonPanel
            //
            this.OkButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButtonPanel.AutoSize = true;
            this.OkButtonPanel.ColumnCount = 1;
            this.OkButtonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OkButtonPanel.Controls.Add(this.OkButton, 0, 0);
            this.OkButtonPanel.Location = new System.Drawing.Point(7, 1179);
            this.OkButtonPanel.Margin = new System.Windows.Forms.Padding(7);
            this.OkButtonPanel.Name = "OkButtonPanel";
            this.OkButtonPanel.RowCount = 1;
            this.OkButtonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OkButtonPanel.Size = new System.Drawing.Size(1815, 65);
            this.OkButtonPanel.TabIndex = 4;
            //
            // OkButton
            //
            this.OkButton.AccessibleName = "Close release notes dialog";
            this.OkButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.OkButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.OkButton.Location = new System.Drawing.Point(1548, 7);
            this.OkButton.Margin = new System.Windows.Forms.Padding(7);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(260, 51);
            this.OkButton.TabIndex = 1;
            this.OkButton.Text = "Close";
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
            this.OkButtonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel RootPanel;
        private WebBrowser WebBrowser;
        private Button OkButton;
        private TableLayoutPanel OkButtonPanel;
    }
}
