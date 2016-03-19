using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.VsPackage.Commands
{
    internal abstract class AbstractSwitchBooleanOptionCommand
    {
        private static Guid CommandSet { get; } = new Guid("e0d9835f-9c16-4d27-a9ad-4df7568650f7");

        protected GoogleTestExtensionOptionsPage Package { get; }

        protected AbstractSwitchBooleanOptionCommand(GoogleTestExtensionOptionsPage package, int commandId)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            Package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var finalCommandId = new CommandID(CommandSet, commandId);
                var command = new OleMenuCommand(OnCommandInvoked, finalCommandId);
                command.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        protected abstract bool Value { get; set; }

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