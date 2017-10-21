// This file has been modified by Microsoft on 8/2017.

using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [Serializable]
    public class InvalidRunSettingsException : Exception
    {
        public InvalidRunSettingsException() { }
        public InvalidRunSettingsException(string message) : base(message) { }
        public InvalidRunSettingsException(string message, Exception inner) : base(message, inner) { }
        protected InvalidRunSettingsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

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

        public static RunSettingsContainer LoadFromXml(XPathNavigator rootElement)
        {
            ValidateArg.NotNull(rootElement, nameof(rootElement));

            RunSettingsContainer runSettingsContainer = null;
            if (rootElement.Name.Equals(GoogleTestConstants.SettingsName))
            {
                var schemaSet = new XmlSchemaSet();
                var schemaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GoogleTestAdapterSettings.xsd");
                var schemaSettings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
                schemaSettings.XmlResolver = null;
                schemaSet.Add(null, XmlReader.Create(schemaStream, schemaSettings));

                var settings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
                settings.Schemas = schemaSet;
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.XmlResolver = null;
                settings.ValidationEventHandler += (object o, ValidationEventArgs e) => throw e.Exception;
                var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(rootElement.OuterXml)), settings);

                try
                {
                    var serializer = new XmlSerializer(typeof(SettingsSerializationContainer));
                    var serializationContainer = serializer.Deserialize(reader) as SettingsSerializationContainer;
                    runSettingsContainer = new RunSettingsContainer(serializationContainer);

                    ValidateAdditionalRunSettingsConstraints(runSettingsContainer.SolutionSettings);
                    foreach (var runSettings in runSettingsContainer.ProjectSettings)
                        ValidateAdditionalRunSettingsConstraints(runSettings);
                }
                catch (InvalidRunSettingsException)
                {
                    throw;
                }
                catch (InvalidOperationException e) when (e.InnerException is XmlSchemaValidationException)
                {
                    throw new InvalidRunSettingsException(String.Format(Resources.Invalid, GoogleTestConstants.SettingsName), e.InnerException);
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

        private static void ValidateOne<T>(string name, T value, Action<T> validator)
        {
            try
            {
                if (value != null)
                    validator(value);
            }
            catch (Exception e)
            {
                throw new InvalidRunSettingsException($"Invalid {name}: {value}.", e.InnerException);
            }
        }

        private static void ValidateOne<T>(string name, T? value, Action<T> validator) where T : struct
        {
            if (value.HasValue)
                ValidateOne(name, value.Value, validator);
        }

        private static void ValidateAdditionalRunSettingsConstraints(RunSettings settings)
        {
            ValidateOne("ProjectRegex", settings.ProjectRegex, Utils.ValidateRegex);
            ValidateOne("TestDiscoveryRegex", settings.TestDiscoveryRegex, Utils.ValidateRegex);
            ValidateOne("TraitsRegexesBefore", settings.TraitsRegexesBefore, Utils.ValidateTraitRegexes);
            ValidateOne("TraitsRegexesAfter", settings.TraitsRegexesAfter, Utils.ValidateTraitRegexes);
            ValidateOne("ShuffleTestsSeed", settings.ShuffleTestsSeed, GoogleTestConstants.ValidateShuffleTestsSeedValue);
        }

    }

}