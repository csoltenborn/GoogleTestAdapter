using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using EnvDTE;

namespace NewProjectWizard.GTA.Helpers
{
    public static class GtestHelper
    {
        private const string LinkGtestAsDll = "GTEST_LINKED_AS_SHARED_LIBRARY;";

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static IEnumerable<Project> FindGtestProjects(IEnumerable<Project> cppProjects)
        {
            foreach (Project project in cppProjects)
            {
                string projectDir = Path.GetDirectoryName(project.FullName);
                // ReSharper disable once AssignNullToNotNullAttribute
                string gtest_all_cc = Path.Combine(projectDir, "gmock-gtest-all.cc");
                string gtest_h = Path.Combine(projectDir, "include", "gtest", "gtest.h");

                if (File.Exists(gtest_h) && File.Exists(gtest_all_cc))
                {
                    yield return project;
                }
            }
        }

        public static string GetGtestInclude(Project gtestProject)
        {
            if (gtestProject == null)
            {
                return "";
            }

            string solutionDir = Path.GetDirectoryName(gtestProject.DTE.Solution.FullName);
            string projectDir = Path.GetDirectoryName(gtestProject.FullName);
            string relativeProjectDir = GetRelativePath($@"{solutionDir}\", $@"{projectDir}\").Replace('/', Path.DirectorySeparatorChar);
            return $@"$(SolutionDir){relativeProjectDir}include;";
        }

        public static string GetLinkGtestAsDll(Project gtestProject)
        {
            return gtestProject != null ? LinkGtestAsDll : "";
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            return new Uri(fromPath).MakeRelativeUri(new Uri(toPath)).OriginalString;
        }

    }
}
