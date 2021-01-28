namespace GoogleTestAdapter.Common
{
    using System.IO;
    using System.Reflection;

    /// <summary>
    ///   Based on the auto-generated resources file from Common.Dynamic
    /// </summary>
    public class CommonResources
    {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CommonResources()
        {
        }

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    // Get the path to GoogleTestAdapter.Common.Dynamic.dll where the resources are defined
                    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GoogleTestAdapter.Common.Dynamic.dll");
                    var asm = Assembly.LoadFrom(path);
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GoogleTestAdapter.Common.Resources", asm);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Test Adapter for Google Test.
        /// </summary>
        public static string ExtensionName
        {
            get
            {
                return ResourceManager.GetString("ExtensionName", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Test Adapter for Google Test: Test discovery starting....
        /// </summary>
        public static string TestDiscoveryStarting
        {
            get
            {
                return ResourceManager.GetString("TestDiscoveryStarting", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Test Adapter for Google Test: Test execution starting....
        /// </summary>
        public static string TestExecutionStarting
        {
            get
            {
                return ResourceManager.GetString("TestExecutionStarting", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0}Check out Test Adapter for Google Test&apos;s trouble shooting section at https://go.microsoft.com/fwlink/?linkid=848168.
        /// </summary>
        public static string TroubleShootingLink
        {
            get
            {
                return ResourceManager.GetString("TroubleShootingLink", resourceCulture);
            }
        }
    }
}
