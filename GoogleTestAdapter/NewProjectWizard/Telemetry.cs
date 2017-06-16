// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using EnvDTE;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.NewProjectWizard
{
    public static class Telemetry
    {
        private static TelemetryClient wizardClient = null;

        static Telemetry()
        {
            TelemetryConfiguration config = CreateTelemetryConfig();
            wizardClient = new TelemetryClient(config);
            PopulateContext(wizardClient, "GoogleTestAdapter");
        }

        public static void LogProjectCreated(string nugetPackage)
        {
            Dictionary<string, string> projectCreatedProperties = new Dictionary<string, string>();
            projectCreatedProperties.Add("NuGetUsed", nugetPackage);
            wizardClient.TrackEvent("GTestProjectCreated", projectCreatedProperties, null);
        }

        public static void LogProjectCancelled(string exception)
        {
            Dictionary<string, string> projectCreatedProperties = new Dictionary<string, string>();
            projectCreatedProperties.Add("Exception", exception);
            wizardClient.TrackEvent("GTestProjectCancelled", projectCreatedProperties, null);
        }

        private static TelemetryConfiguration CreateTelemetryConfig()
        {
            TelemetryConfiguration config = TelemetryConfiguration.Active;
#if DEBUG
            config.TelemetryChannel.DeveloperMode = true;
#endif
            return config;
        }

        private static void PopulateContext(TelemetryClient client, string scenario)
        {
            try
            {
                client.Context.Session.Id = DateTime.Now.ToFileTime().ToString();

                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                client.Context.Properties["App.Version"] = version;

                var dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
                if (dte != null)
                    client.Context.Properties["VisualStudio.Version"] = dte.Version;

                client.Context.Properties["App.Scenario"] = scenario;
            }
            catch (MissingMemberException mme)
            {
                Trace.WriteLine(String.Format("Error populating telemetry context: {0}", mme.ToString()));
            }
        }
    }
}
