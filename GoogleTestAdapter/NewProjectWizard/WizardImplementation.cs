// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using EnvDTE;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;
using VSLangProj;

namespace Microsoft.NewProjectWizard
{
    public class WizardImplementation : IWizard
    {
        private const string TargetPlatformVersion = "$targetplatformversion$";
        private const string WizardData = "$wizarddata$";
        private const string RuntimeDebug = "$rtdebug$";
        private const string RuntimeRelease = "$rtrelease$";
        private const string RunSilent = "$runsilent$";
        private const string ARM64DebugPlatform = "$arm64debugplatform$";
        private string arm64DebugXMLChunk = "  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|ARM64'\"> <ClCompile>  <PrecompiledHeader>Use</PrecompiledHeader>  <PrecompiledHeaderFile>pch.h</PrecompiledHeaderFile>  <Optimization>Disabled</Optimization>  <PreprocessorDefinitions>ARM64;_DEBUG;_CONSOLE;%(PreprocessorDefinitions)</PreprocessorDefinitions>   <BasicRuntimeChecks>EnableFastChecks</BasicRuntimeChecks>   <RuntimeLibrary>$rtdebug$</RuntimeLibrary>   <WarningLevel>Level3</WarningLevel> </ClCompile> <Link>   <GenerateDebugInformation>true</GenerateDebugInformation>   <SubSystem>Console</SubSystem> </Link>  </ItemDefinitionGroup>";
        private const string ARM64ReleasePlatform = "$arm64releaseplatform$";
        private string arm64ReleaseXMLChunk = "  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|ARM64'\"> <ClCompile>   <PrecompiledHeader>Use</PrecompiledHeader>   <PrecompiledHeaderFile>pch.h</PrecompiledHeaderFile>   <PreprocessorDefinitions>ARM64;NDEBUG;_CONSOLE;%(PreprocessorDefinitions)</PreprocessorDefinitions>   <RuntimeLibrary>$rtrelease$</RuntimeLibrary>   <WarningLevel>Level3</WarningLevel>   <DebugInformationFormat>ProgramDatabase</DebugInformationFormat> </ClCompile> <Link>   <GenerateDebugInformation>true</GenerateDebugInformation>   <SubSystem>Console</SubSystem>   <OptimizeReferences>true</OptimizeReferences>   <EnableCOMDATFolding>true</EnableCOMDATFolding> </Link>  </ItemDefinitionGroup>";
        private const string ARM64Config = "$arm64config$";
        private string arm64ConfigXML = "<ProjectConfiguration Include=\"Debug|ARM64\">   <Configuration>Debug</Configuration>   <Platform>ARM64</Platform> </ProjectConfiguration> <ProjectConfiguration Include=\"Release|ARM64\"> <Configuration>Release</Configuration>  <Platform>ARM64</Platform> </ProjectConfiguration>";
        private List<Project> projects = new List<Project>();
        private int selectedProjectIndex;
        private IWizard nugetWizard;

        #region IWizard Interface Methods

        // This method is called before opening any item that
        // has the OpenInEditor attribute.
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
            if (selectedProjectIndex >= 0)
            {
                try
                {
                    VSProject vsProj = project.Object as VSProject;
                    vsProj.References.AddProject(projects[selectedProjectIndex]);
                }
                catch (Exception ex)
                {
                    // Known issue, remove when fixed
                    if (!ex.Message.Equals("Error HRESULT E_FAIL has been returned from a call to a COM component."))
                        throw;
                }
            }

            // Call the NuGet Wizard ProjectFinishedGenerating to add the reference
            if (nugetWizard != null)
                nugetWizard.ProjectFinishedGenerating(project);
        }

        // This method is only called for item templates,
        // not for project templates.
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        // This method is called after the project is created.
        public void RunFinished()
        {
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            Projects dteProjects = dte.Solution.Projects;
            List<string> projectNames = new List<string>();
            bool isPlatformSet = false;

            foreach (Project project in dteProjects)
            {
                // TODO: Filter out projects not applicable
                projects.Add(project);
                projectNames.Add(project.Name);
            }

            ConfigurationData configurationData = new ConfigurationData(dte, projectNames.ToArray());

            SinglePageWizardDialog wiz = new SinglePageWizardDialog(Resources.WizardTitle, configurationData);

            bool? success = false;

            // If RunSilent is true, we're in automated testing
            if (replacementsDictionary[RunSilent] == "True")
            {
                success = true;
                SetDefaultData(ref configurationData);
            }
            else
            {
                success = wiz.ShowModal();
            }

            if (success == false)
            {
                throw new WizardCancelledException();
            }

            selectedProjectIndex = configurationData.ProjectIndex;

            if (selectedProjectIndex >= 0)
            {
                foreach (Property prop in projects[selectedProjectIndex].Properties)
                {
                    if (prop.Name.Equals("WindowsTargetPlatformVersion"))
                    {
                        replacementsDictionary[TargetPlatformVersion] = (string)prop.Value;
                        isPlatformSet = true;
                    }
                }
            }

            string consumeGTestAs = configurationData.IsGTestStatic ? "static" : "dyn";
            string runtimeLibs = configurationData.IsRuntimeStatic ? "static" : "dyn";
            string nugetPackage = "Microsoft.googletest.v140.windesktop.msvcstl." + consumeGTestAs + ".rt-" + runtimeLibs;

            // Work around so we can choose the package for the nuget wizard
            string tmpWizardData = Path.GetTempFileName();
            File.AppendAllText(tmpWizardData, "<VSTemplate Version=\"3.0.0\" xmlns=\"http://schemas.microsoft.com/developer/vstemplate/2005\" Type=\"Project\"><WizardData>");
            File.AppendAllText(tmpWizardData, replacementsDictionary[WizardData].Replace("$nugetpackage$", nugetPackage));
            File.AppendAllText(tmpWizardData, "</WizardData></VSTemplate>");
            customParams[0] = tmpWizardData;

            try
            {
                Assembly nugetAssembly = Assembly.Load("NuGet.VisualStudio.Interop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                nugetWizard = (IWizard)nugetAssembly.CreateInstance("NuGet.VisualStudio.TemplateWizard");
                nugetWizard.RunStarted(automationObject, replacementsDictionary, runKind, customParams);
            }
            catch (Exception)
            {
                ShowRtlAwareMessageBox(Resources.NuGetInteropNotFound);
                throw;
            }

            if (configurationData.IsRuntimeStatic)
            {
                replacementsDictionary[RuntimeRelease] = "MultiThreaded";
                replacementsDictionary[RuntimeDebug] = "MultiThreadedDebug";
            }
            else
            {
                replacementsDictionary[RuntimeRelease] = "MultiThreadedDLL";
                replacementsDictionary[RuntimeDebug] = "MultiThreadedDebugDLL";
            }

            if (!isPlatformSet)
            {
                IEnumerable<TargetPlatformSDK> platformSdks = ToolLocationHelper.GetTargetPlatformSdks();
                IEnumerable<TargetPlatformSDK> allSdks = WizardImplementation.GetAllPlatformSdks();
                TargetPlatformSDK latestSdk = allSdks.FirstOrDefault();

                if (latestSdk == null)
                {
                    ShowRtlAwareMessageBox(Resources.WinSDKNotFound);
                    throw new WizardCancelledException(Resources.WinSDKNotFound);
                }

                string versionString;

                if (latestSdk.TargetPlatformVersion.Major >= 10)
                {
                    List<Platform> allPlatformsForLatestSdk = ToolLocationHelper.GetPlatformsForSDK("Windows", latestSdk.TargetPlatformVersion)
                        .Select(moniker => TryParsePlatformVersion(moniker))
                        .Where(name => name != null)
                        .OrderByDescending(p => p.Version).ToList();

                    if (!this.IsARM64()) {
                        arm64DebugXMLChunk = "";
                        arm64ReleaseXMLChunk = "";
                        arm64ConfigXML = "";
                    }
                    replacementsDictionary[ARM64Config] = arm64ConfigXML;
                    replacementsDictionary[ARM64DebugPlatform] = arm64DebugXMLChunk;
                    replacementsDictionary[ARM64ReleasePlatform] = arm64ReleaseXMLChunk;

                    Platform latestPlatform = allPlatformsForLatestSdk.FirstOrDefault();

                    if (latestPlatform == null)
                    {
                        ShowRtlAwareMessageBox(Resources.WinSDKNotFound);
                        throw new WizardCancelledException(Resources.WinSDKNotFound);
                    }

                    versionString = latestPlatform.Version.ToString();
                }
                else
                {
                    versionString = latestSdk.TargetPlatformVersion.ToString();
                }

                replacementsDictionary[TargetPlatformVersion] = versionString;
            }
        }

        /// <summary>
        /// Checks architecture of Visual Studio Process.
        /// </summary>
        /// <returns>boolean indicating if running arm64 VS</returns>
        protected bool IsARM64() {
            string cpu = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE", EnvironmentVariableTarget.Process);

            return cpu.Equals("ARM64", StringComparison.OrdinalIgnoreCase);
        }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
#endregion

        private void ShowRtlAwareMessageBox(string text)
        {
            MessageBoxOptions options = 0;
            if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
            {
                options |= MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign;
            }
            MessageBox.Show(
                text,
                Resources.WizardTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1,
                options);
        }

        private static IEnumerable<TargetPlatformSDK> GetAllPlatformSdks()
        {
            IEnumerable<TargetPlatformSDK> platformSdks = ToolLocationHelper.GetTargetPlatformSdks();
            return platformSdks.Where(p => p.TargetPlatformIdentifier.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.TargetPlatformVersion).ToList();
        }

        private class Platform
        {
            public Platform(string name, Version version)
            {
                this.Name = name;
                this.Version = version;
            }

            public string Name { get; private set; }

            public Version Version { get; private set; }
        }

        private static Platform TryParsePlatformVersion(string platformString)
        {
            FrameworkName frameworkName;
            // Only return a platform when the Profile is an empty string
            if (TryParseFrameworkName(platformString, out frameworkName) && string.IsNullOrEmpty(frameworkName.Profile))
            {
                return new Platform(frameworkName.Identifier, frameworkName.Version);
            }

            return null;
        }

        private static bool TryParseFrameworkName(string moniker, out FrameworkName result)
        {
            try
            {
                result = new FrameworkName(moniker);
                return true;
            }
            catch (Exception)
            {
            }

            result = null;
            return false;
        }

        private void SetDefaultData(ref ConfigurationData configurationData)
        {
            configurationData.ProjectIndex = -1;
            configurationData.IsGTestStatic = true;
            configurationData.IsRuntimeStatic = false;
        }
    }

    public class ConfigurationData : IWizardData
    {
        public List<string> Projects { get; set; }
        public int ProjectIndex { get; set; }
        public bool IsGTestStatic { get; set; }
        public bool IsRuntimeStatic { get; set; }
        public DTE DTE { get; set; }

        public bool OnTryFinish() { return true; }

        public ConfigurationData(DTE DTE, string[] projects)
        {
            this.DTE = DTE;
            this.Projects = projects.ToList();
        }
    }
}