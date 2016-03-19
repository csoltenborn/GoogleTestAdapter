namespace GoogleTestAdapter.VsPackage.Commands
{

    internal sealed class SwitchBreakOnFailureOptionCommand : AbstractSwitchBooleanOptionCommand
    {

        private SwitchBreakOnFailureOptionCommand(GoogleTestExtensionOptionsPage package) : base(package, 0x0101) {}

        private static SwitchBreakOnFailureOptionCommand Instance
        {
            get; set;
        }

        internal static void Initialize(GoogleTestExtensionOptionsPage package)
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