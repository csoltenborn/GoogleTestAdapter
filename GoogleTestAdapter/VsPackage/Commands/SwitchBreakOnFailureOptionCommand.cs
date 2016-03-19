using System;

namespace GoogleTestAdapter.VsPackage.Commands
{

    internal sealed class SwitchBreakOnFailureOptionCommand : AbstractSwitchBooleanOptionCommand
    {

        private SwitchBreakOnFailureOptionCommand(GoogleTestExtensionOptionsPage package) : base(package)
        {
            InitCommand(0x0101, new Guid("e0d9835f-9c16-4d27-a9ad-4df7568650f7"));
        }

        private static SwitchBreakOnFailureOptionCommand Instance
        {
            get; set;
        }

        public static void Initialize(GoogleTestExtensionOptionsPage package)
        {
            Instance = new SwitchBreakOnFailureOptionCommand(package);
        }

        protected override bool Value
        {
            get { return Package.BreakOnFailure; }
            set { Package.BreakOnFailure = value; }
        }

    }

}