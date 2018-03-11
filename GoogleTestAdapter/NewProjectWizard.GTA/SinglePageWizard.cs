using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;

namespace NewProjectWizard.GTA
{
    public partial class SinglePageWizard : Form
    {
        private class Item
        {
            public Project Project { get; }

            public string ToolTipTitle => Project.Name;
            public string ToolTipContent => Project.FullName;

            public Item(Project project)
            {
                Project = project;
            }

            public override string ToString()
            {
                return Project.Name;
            }
        }


        private readonly ToolTip _toolTip = new ToolTip();
        private int _currentToolTipIndex = -1;

        public IList<Project> SelectedProjects
        {
            get
            {
                return selectedProjectsCheckedListBox.CheckedItems
                    .Cast<Item>()
                    .Select(i => i.Project)
                    .ToList();
            }
        }

        public SinglePageWizard() : this(new List<Project>())
        {
        }

        public SinglePageWizard(IEnumerable<Project> projects)
        {
            InitializeComponent();

            foreach (var item in projects.Select(p => new Item(p)))
            {
                selectedProjectsCheckedListBox.Items.Add(item);
            }
        }

        private void SinglePageWizard_FormClosed(object sender, FormClosedEventArgs e)
        {
            _toolTip.Dispose();
        }

        private void okButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void selectedProjectsCheckedListBox_MouseMove(object sender, MouseEventArgs e)
        {
            int newToolTipIndex = selectedProjectsCheckedListBox.IndexFromPoint(e.Location);
            if (_currentToolTipIndex == newToolTipIndex)
            {
                return;
            }

            _currentToolTipIndex = newToolTipIndex;
            if (_currentToolTipIndex < 0)
            {
                _toolTip.SetToolTip(selectedProjectsCheckedListBox, null);
            }
            else
            {
                Item item = (Item)selectedProjectsCheckedListBox.Items[_currentToolTipIndex];
                _toolTip.ToolTipTitle = item.ToolTipTitle;
                _toolTip.SetToolTip(selectedProjectsCheckedListBox, item.ToolTipContent);
            }
        }
        
    }
}