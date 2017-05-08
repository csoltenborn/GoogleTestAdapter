using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    public class RunSettingsContainer : TestRunSettings, IGoogleTestAdapterSettingsContainer
    {
        private RunSettings _solutionSettings;

        public RunSettings SolutionSettings
        {
            get { return _solutionSettings ?? new RunSettings(); }
            set { _solutionSettings = value; }
        }

        public List<RunSettings> ProjectSettings { get; set; } = new List<RunSettings>();

        public RunSettingsContainer()
            : base(GoogleTestConstants.SettingsName)
        { }

        public RunSettingsContainer(SettingsSerializationContainer serializationContainer) :  this()
        {
            _solutionSettings = serializationContainer.SolutionSettings.Settings;
            ProjectSettings.AddRange(serializationContainer.SettingsList);
        }

        public RunSettings GetSettingsForExecutable(string executable)
        {
            return
                ProjectSettings.FirstOrDefault(s => Regex.IsMatch(executable, s.ProjectRegex));
        }

        public override XmlElement ToXml()
        {
            var document = new XmlDocument();
            using (XmlWriter writer = document.CreateNavigator().AppendChild())
            {
                new XmlSerializer(typeof(SettingsSerializationContainer))
                    .Serialize(writer, new SettingsSerializationContainer(this));
            }
            return document.DocumentElement;
        }

        public static RunSettingsContainer LoadFromXml(XmlReader reader)
        {
            ValidateArg.NotNull(reader, nameof(reader));

            RunSettingsContainer runSettingsContainer = null;
            if (reader.Read() && reader.Name.Equals(GoogleTestConstants.SettingsName))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(SettingsSerializationContainer));
                    var serializationContainer = serializer.Deserialize(reader) as SettingsSerializationContainer;
                    runSettingsContainer = new RunSettingsContainer(serializationContainer);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            return runSettingsContainer ?? new RunSettingsContainer();
        }

        public void GetUnsetValuesFrom(RunSettingsContainer other)
        {
            SolutionSettings.GetUnsetValuesFrom(other.SolutionSettings);

            var unmatchedProjectSettings = new List<RunSettings>(other.ProjectSettings);
            foreach (RunSettings myProjectSettings in ProjectSettings)
            {
                var otherProjectSettings =
                    unmatchedProjectSettings.FirstOrDefault(s => myProjectSettings.ProjectRegex == s.ProjectRegex);
                if (otherProjectSettings != null)
                {
                    unmatchedProjectSettings.Remove(otherProjectSettings);
                    myProjectSettings.GetUnsetValuesFrom(otherProjectSettings);
                }
            }
            foreach (RunSettings remainingProjectSettings in unmatchedProjectSettings)
            {
                ProjectSettings.Add(remainingProjectSettings);
            }
        }

    }

}