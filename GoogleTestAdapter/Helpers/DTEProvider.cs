using EnvDTE;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace GoogleTestAdapter
{

    // after http://www.viva64.com/en/b/0169/#ID0EGFAG
    public static class DTEProvider
    {
        private static DTE INSTANCE = null;

        public static DTE DTE {
            get
            {
                if (INSTANCE == null)
                {
                    INSTANCE = GetByID(ProcessUtils.VisualStudioMainProcess.Id);
                }
                return INSTANCE;
            }
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved,
                                                 out IBindCtx ppbc);
        [DllImport("ole32.dll")]
        private static extern void GetRunningObjectTable(int reserved,
          out IRunningObjectTable prot);

        private static DTE GetByID(int ID)
        {
            //rot entry for visual studio running under current process.
            IRunningObjectTable rot;
            GetRunningObjectTable(0, out rot);
            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            enumMoniker.Reset();
            IntPtr fetched = IntPtr.Zero;
            IMoniker[] moniker = new IMoniker[1];
            while (enumMoniker.Next(1, moniker, fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);
                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);
                if (displayName.StartsWith("!VisualStudio.DTE.") && displayName.EndsWith(ID.ToString()))
                {
                    object comObject;
                    rot.GetObject(moniker[0], out comObject);
                    return (DTE)comObject;
                }
            }
            return null;
        }

    }

}
