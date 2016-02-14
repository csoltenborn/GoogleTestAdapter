using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace GoogleTestAdapterUiTests.Model
{
    [XmlRoot("TestRun")]
    public class TestRun
    {
        [XmlElement("TestGroup")]
        public List<TestGroup> testGroups = new List<TestGroup>();

        [XmlElement("TestOutput")]
        public string testOutput;

        public void Add(TestGroup tg)
        {
            testGroups.Add(tg);
        }

        public string ToXML()
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer xmlSerializer = new XmlSerializer(GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, this, ns);
                return textWriter.ToString();
            }
        }
    }

    public class TestGroup
    {
        public string description;

        [XmlElement("TestCase")]
        public List<TestCase> testCases = new List<TestCase>();

        public TestGroup(string description)
        {
            this.description = description ?? string.Empty;
        }
        private TestGroup() { } // only to make XmlSerializer happy

        public void Add(TestCase tr) { testCases.Add(tr); }
    }

    public class TestCase
    {
        public string Name = string.Empty;
        public string FullyQualifiedName = string.Empty;
        public string Result = string.Empty;
        public string Source = string.Empty;
        public string Error = string.Empty;
        public string Stacktrace = string.Empty;
        public string Unexpected = string.Empty;
        public bool ShouldSerializeFullyQualifiedName() { return !string.IsNullOrWhiteSpace(FullyQualifiedName); }
        public bool ShouldSerializeError() { return !string.IsNullOrWhiteSpace(Error); }
        public bool ShouldSerializeUnexpected() { return !string.IsNullOrWhiteSpace(Unexpected); }
        public bool ShouldSerializeStacktrace() { return !string.IsNullOrWhiteSpace(Stacktrace); }
    }

}
