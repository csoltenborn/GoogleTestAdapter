using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.VsPackage.Commands
{
    internal abstract class AbstractSwitchBooleanOptionCommand
    {
        protected GoogleTestExtensionOptionsPage Package { get; }

        protected AbstractSwitchBooleanOptionCommand(GoogleTestExtensionOptionsPage package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            Package = package;
        }

        protected abstract bool Value { get; set; }

        protected void InitCommand(int commandId, Guid commandSet)
        {
            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var finalCommandId = new CommandID(commandSet, commandId);
                var command = new OleMenuCommand(OnCommandInvoked, finalCommandId);
                command.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private IServiceProvider ServiceProvider => Package;

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command != null)
                command.Checked = Value;
        }

        private void OnCommandInvoked(object sender, EventArgs e)
        {
            Value = !Value;
        }

    }

}