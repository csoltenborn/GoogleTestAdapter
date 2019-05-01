using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NewProjectWizard.GTA.Helpers
{
    public static class ProjectExtensions
    {
        public enum ConfigurationType { Static, Dynamic}

        private const string CppKindGuid = "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942";

        public const string ConfigurationTypeDynamic = "DynamicLibrary";
        public const string ConfigurationTypeStatic = "StaticLibrary";


        public static bool IsCppProject(this Project project)
        {
            return GetProjectTypeGuids(project).ToUpper().Contains(CppKindGuid);
        }

        // after https://www.mztools.com/Articles/2007/MZ2007016.aspx
        private static string GetProjectTypeGuids(Project project)
        {
            string projectTypeGuids = "";

            // ReSharper disable once SuspiciousTypeConversion.Global
            IVsSolution solution = (IVsSolution)GetService((IServiceProvider)project.DTE, typeof(IVsSolution));
            var result = solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy);
            if (result == 0)
            {
                var aggregatableProject = hierarchy as IVsAggregatableProject;
                aggregatableProject?.GetAggregateProjectTypeGuids(out projectTypeGuids);
            }

            return projectTypeGuids;
        }

        private static object GetService(IServiceProvider serviceProvider, Type type)
        {
            object service = null;

            Guid sidGuid = type.GUID;
            Guid iidGuid = sidGuid;

            var hr = serviceProvider.QueryService(ref sidGuid, iidGuid, out IntPtr serviceIntPtr);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                service = Marshal.GetObjectForIUnknown(serviceIntPtr);
                Marshal.Release(serviceIntPtr);
            }

            return service;
        }
    }
    
}