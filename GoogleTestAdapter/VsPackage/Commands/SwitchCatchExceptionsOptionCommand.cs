using System;

namespace GoogleTestAdapter.VsPackage.Commands
{

    internal sealed class SwitchCatchExceptionsOptionCommand : AbstractSwitchBooleanOptionCommand
    {

        private SwitchCatchExceptionsOptionCommand(GoogleTestExtensionOptionsPage package) : base(package)
        {
            InitCommand(0x0100, new Guid("e0d9835f-9c16-4d27-a9ad-4df7568650f7"));
        }

        private static SwitchCatchExceptionsOptionCommand Instance
        {
            get; set;
        }

        public static void Initialize(GoogleTestExtensionOptionsPage package)
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