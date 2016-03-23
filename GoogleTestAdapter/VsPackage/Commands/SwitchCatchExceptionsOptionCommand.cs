namespace GoogleTestAdapter.VsPackage.Commands
{

    internal sealed class SwitchCatchExceptionsOptionCommand : AbstractSwitchBooleanOptionCommand
    {

        private SwitchCatchExceptionsOptionCommand(GoogleTestExtensionOptionsPage package) : base(package, 0x0100) {}

        private static SwitchCatchExceptionsOptionCommand Instance
        {
            get; set;
        }

        internal static void Initialize(GoogleTestExtensionOptionsPage package)
        {
            Instance = new SwitchCatchExceptionsOptionCommand(package);
        }

        protected override bool Value
        {
            get { return Package.CatchExtensions; }
            set { Package.CatchExtensions = value; }
        }

    }

}