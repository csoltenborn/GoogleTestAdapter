using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;

namespace NewProjectWizard.GTA
{
    public partial class CreateProjectDialog : Form
    {
        private class Item
        {
            public static readonly Item NoProjectItem = new Item();

            public Project Project { get; }

            private Item() { }

            public Item(Project project)
            {
                Project = project ?? throw new ArgumentNullException(nameof(project));
            }

            public override string ToString()
            {
                return Project == null 
                    ? "<none>" 
                    : $"{Project.Name} ({Project.FullName})";
            }
        }

        private readonly IDictionary<Project, Item> _projectsToItems = new Dictionary<Project, Item>();

        public ISet<Project> ProjectsUnderTest
        {
            get 
            { 
                return new HashSet<Project>(
                    projectsUnderTestCheckedListBox.CheckedItems
                        .Cast<Item>()
                        .Select(i => i.Project));
            }
        }

        public Project GtestProject
        {
            get => ((Item)gtestProjectComboBox.SelectedItem).Project; 

            set => gtestProjectComboBox.SelectedItem = value != null 
                ? _projectsToItems[value] 
                : Item.NoProjectItem;
        }

        public CreateProjectDialog() : this(new List<Project>())
        {
        }

        public CreateProjectDialog(IEnumerable<Project> projects)
        {
            InitializeComponent(); 

            gtestProjectComboBox.Items.Add(Item.NoProjectItem);
            foreach (var project in projects)
            {
                var item = new Item(project); 
                _projectsToItems.Add(project, item); 
                gtestProjectComboBox.Items.Add(item); 
                projectsUnderTestCheckedListBox.Items.Add(item);
            } 
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK; 
            Close();
        }
    }
}