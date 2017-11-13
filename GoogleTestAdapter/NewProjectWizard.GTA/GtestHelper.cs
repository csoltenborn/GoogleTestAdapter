using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace NewProjectWizard.GTA
{
    public static class GtestHelper
    {
        private const string GtestInclude = "$(SolutionDir)gtest\\include;";
        private const string LinkGtestAsDll = "GTEST_LINKED_AS_SHARED_LIBRARY;";

        public static Project FindGtestProject(IEnumerable<Project> cppProjects)
        {
            return cppProjects.FirstOrDefault(p => p.Name.ToLower() == "gtest");
        }

        public static string GetGtestInclude(Project gtestProject)
        {
            return gtestProject != null ? GtestInclude : "";
        }

        public static string GetLinkGtestAsDll(Project gtestProject)
        {
            return gtestProject != null ? LinkGtestAsDll : "";
        }

    }
}
