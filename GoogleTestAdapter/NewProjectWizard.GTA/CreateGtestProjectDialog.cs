using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    public partial class CreateGtestProjectDialog : Form
    {
        private class Item
        {
            public ProjectExtensions.ConfigurationType ConfigurationType { get; }

            public Item(ProjectExtensions.ConfigurationType configurationType)
            {
                ConfigurationType = configurationType;
            }

            public override string ToString()
            {
                switch (ConfigurationType)
                {
                    case ProjectExtensions.ConfigurationType.Static:
                        return "Static (.lib)";
                    case ProjectExtensions.ConfigurationType.Dynamic:
                        return "Dynamic (.dll)";
                    default:
                        throw new InvalidOperationException($"Unknown literal {ConfigurationType}");
                }
            }
        }

        public ProjectExtensions.ConfigurationType ConfigurationType
        {
            get => ((Item)gtestProjectComboBox.SelectedItem).ConfigurationType; 
            set => gtestProjectComboBox.SelectedItem = _configurationTypes2Items[value];
        }

        public bool IncludeGoogleMock
        {
            get => includeGoogleMockCheckBox.Checked;
            set => includeGoogleMockCheckBox.Checked = value;
        }

        private readonly IDictionary<ProjectExtensions.ConfigurationType, Item> _configurationTypes2Items = new Dictionary<ProjectExtensions.ConfigurationType, Item>();

        public CreateGtestProjectDialog()
        {
            InitializeComponent();

            foreach (ProjectExtensions.ConfigurationType configurationType in Enum.GetValues(typeof(ProjectExtensions.ConfigurationType)))
            {
                var item = new Item(configurationType);
                _configurationTypes2Items.Add(configurationType, item);
                gtestProjectComboBox.Items.Add(item); 
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK; 
            Close();
        }
    }
}