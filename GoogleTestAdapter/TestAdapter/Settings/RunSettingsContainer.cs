// This file has been modified by Microsoft on 6/2017.

#pragma warning disable IDE0017 // Simplify object initialization

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
        // virtual for mocking
        public virtual RunSettings SolutionSettings { get; }

        public List<RunSettings> ProjectSettings { get; } = new List<RunSettings>();

        public RunSettingsContainer(RunSettings solutionSettings)
            : base(GoogleTestConstants.SettingsName)
        {
            SolutionSettings = solutionSettings ?? throw new ArgumentNullException(nameof(solutionSettings));
        }

        public RunSettingsContainer() : this(new RunSettings())
        {
        }

        public RunSettingsContainer(SettingsSerializationContainer serializationContainer) :  
            this(serializationContainer.SolutionSettings.Settings)
        {
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
                settings.ValidationEventHandler += (o, e) => throw e.Exception;
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
                    throw new InvalidRunSettingsException($"Invalid {GoogleTestConstants.SettingsName}", e.InnerException);
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

        public bool GetUnsetValuesFrom(string settingsFile)
        {
            var settings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
            settings.XmlResolver = null;
            using (var reader = XmlReader.Create(settingsFile, settings))
            {
                var solutionRunSettingsDocument = new XPathDocument(reader);
                XPathNavigator solutionRunSettingsNavigator = solutionRunSettingsDocument.CreateNavigator();
                if (solutionRunSettingsNavigator.MoveToChild(Constants.RunSettingsName, ""))
                {
                    return GetUnsetValuesFrom(solutionRunSettingsNavigator);
                }

                return false;
            }
        }

        public bool GetUnsetValuesFrom(XPathNavigator sourceNavigator)
        {
            if (sourceNavigator.MoveToChild(GoogleTestConstants.SettingsName, ""))
            {
                var sourceRunSettings = LoadFromXml(sourceNavigator);
                GetUnsetValuesFrom(sourceRunSettings);
                return true;
            }

            return false;
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
            ValidateOne(nameof(settings.ProjectRegex), settings.ProjectRegex, Utils.ValidateRegex);
            ValidateOne(nameof(settings.TestDiscoveryRegex), settings.TestDiscoveryRegex, Utils.ValidateRegex);
            ValidateOne(nameof(settings.TraitsRegexesBefore), settings.TraitsRegexesBefore, Utils.ValidateTraitRegexes);
            ValidateOne(nameof(settings.TraitsRegexesAfter), settings.TraitsRegexesAfter, Utils.ValidateTraitRegexes);
            ValidateOne(nameof(settings.EnvironmentVariables), settings.EnvironmentVariables, Utils.ValidateEnvironmentVariables);
            ValidateOne(nameof(settings.ShuffleTestsSeed), settings.ShuffleTestsSeed, GoogleTestConstants.ValidateShuffleTestsSeedValue);
        }

    }

}