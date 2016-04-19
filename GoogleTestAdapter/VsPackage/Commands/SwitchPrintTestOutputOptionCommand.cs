namespace GoogleTestAdapter.VsPackage.Commands
{

    internal sealed class SwitchPrintTestOutputOptionCommand : AbstractSwitchBooleanOptionCommand
    {

        private SwitchPrintTestOutputOptionCommand(IGoogleTestExtensionOptionsPage package) : base(package, 0x0103) {}

        private static SwitchPrintTestOutputOptionCommand Instance
        {
            get; set;
        }

        internal static void Initialize(IGoogleTestExtensionOptionsPage package)
        {
            Instance = new SwitchPrintTestOutputOptionCommand(package);
        }

        protected override bool Value 
        {
            get { return Package.PrintTestOutput; }
            set { Package.PrintTestOutput = value; }
        }

    }

}