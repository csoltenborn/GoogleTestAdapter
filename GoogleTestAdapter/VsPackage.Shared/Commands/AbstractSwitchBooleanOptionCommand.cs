using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.VsPackage.Commands
{
    internal abstract class AbstractSwitchBooleanOptionCommand
    {
        private static Guid CommandSet { get; } = new Guid("e0d9835f-9c16-4d27-a9ad-4df7568650f7");

        protected readonly IGoogleTestExtensionOptionsPage Package;

        protected AbstractSwitchBooleanOptionCommand(IGoogleTestExtensionOptionsPage package, int commandId)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
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
            if (sender is OleMenuCommand command)
                command.Checked = Value;
        }

        private void OnCommandInvoked(object sender, EventArgs e)
        {
            Value = !Value;
        }

    }

}