// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.NewProjectWizard
{
    /// <summary>
    /// Interaction logic for WizardDialog.xaml
    /// </summary>
    public partial class SinglePageWizardDialog : DialogWindow
    {
        private ConfigurationData data;

        public SinglePageWizardDialog(string title, ConfigurationData data)
        {
            this.data = data;

            this.InitializeComponent();

            // Set handlers here after all objects are initialized
            staticLibRadioButton.Checked += StaticLibRadioButton_Checked;
            staticLibRadioButton.Unchecked += StaticLibRadioButton_Unchecked;

            if (data.Projects.Count == 0)
            {
                projectGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                projectComboBox.Items.Add("<No project>");
                foreach (string project in data.Projects)
                {
                    projectComboBox.Items.Add(project);
                }
            }
            projectComboBox.SelectedIndex = 0;
            this.Label_DialogTitle.Content = title;
        }

        #region Title Bar

        /// <summary>
        /// Moves the window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ClickClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Action Buttons

        /// <summary>
        /// Closes the dialog and cancels the operation.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.CloseWizard(finished: false);
        }

        /// <summary>
        /// Closes the dialog and completes the operation.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            this.CloseWizard(finished: true);
        }

        #endregion

        private void StaticLibRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            linkStaticRadioButton.IsEnabled = true;
        }

        private void StaticLibRadioButton_Unchecked (object sender, RoutedEventArgs e)
        {
            linkStaticRadioButton.IsEnabled = false;
            linkDynamicRadioButton.IsChecked = true;
        }

        private void CloseWizard(bool finished)
        {
            if (finished)
            {
                data.OnTryFinish();
                data.IsGTestStatic = (staticLibRadioButton.IsChecked == true);
                data.IsRuntimeStatic = (linkStaticRadioButton.IsChecked == true);
                data.ProjectIndex = projectComboBox.SelectedIndex - 1;
            }

            this.DialogResult = finished;
            this.Close();
        }
    }
}
