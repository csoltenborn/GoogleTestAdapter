using System;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NewProjectWizard.GTA
{
    public static class ProjectExtensions
    {
        private const string CppKindGuid = "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942";

        private static readonly string[] GoogleTestProjectNames = { "gtest" , "GoogleTest" };

        public static bool IsGtestProject(this Project project)
        {
            string normalizedName = project.Name.ToLower();
            return GoogleTestProjectNames.Any(n => n == normalizedName);
        }

        public static bool IsCppProject(this Project project)
        {
            return GetProjectTypeGuids(project).Contains(CppKindGuid);
        }

        // after https://www.mztools.com/Articles/2007/MZ2007016.aspx
        private static string GetProjectTypeGuids(Project project)
        {
            string projectTypeGuids = "";

            var service = GetService(project.DTE, typeof(IVsSolution));
            var solution = (IVsSolution)service;

            var result = solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy);
            if (result == 0)
            {
                var aggregatableProject = (IVsAggregatableProject)hierarchy;
                aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);
            }

            return projectTypeGuids;
        }

        private static object GetService(object serviceProvider, Type type)
        {
            return GetService(serviceProvider, type.GUID);
        }

        private static object GetService(object serviceProviderObject, Guid guid)
        {
            object service = null;

            Guid sidGuid = guid;
            Guid iidGuid = sidGuid;
            var serviceProvider = (IServiceProvider)serviceProviderObject;

            var hr = serviceProvider.QueryService(ref sidGuid, iidGuid, out IntPtr serviceIntPtr);
            if (hr != 0)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                service = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);
                System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
            }

            return service;
        }

    }
    
}