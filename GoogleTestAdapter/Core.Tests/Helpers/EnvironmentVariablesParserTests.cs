using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class EnvironmentVariablesParserTests
    {
        private FakeLogger _fakeLogger;
        private EnvironmentVariablesParser _parser;

        [TestInitialize]
        public void SetUp()
        {
            _fakeLogger = new FakeLogger();
            _parser = new EnvironmentVariablesParser(_fakeLogger);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_EmptyString_ReturnsCorrectDictionary()
        {
            CheckParserInvocation("", result => result.Should().BeEmpty());
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_SimpleCase_ReturnsCorrectDictionary()
        {
            CheckParserInvocation("Foo=Bar", result =>
            {
                result.Should().HaveCount(1);
                result["Foo"].Should().Be("Bar");
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_EnvVarWithEmptyValue_ReturnsCorrectDictionary()
        {
            CheckParserInvocation("Foo=", result =>
            {
                result.Should().HaveCount(1);
                result["Foo"].Should().Be("");
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_EnvVarWithoutValue_IsIgnoredAndLogged()
        {
            CheckParserInvocation("Foo", result => result.Should().BeEmpty(), "must be of the form Name=Value");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_EnvVarWithStrangeName_ReturnsCorrectDictionary()
        {
            CheckParserInvocation(@"_(){}[]$*+-\/""#',;.@!?=Bar", result =>
            {
                result.Should().HaveCount(1);
                result[@"_(){}[]$*+-\/""#',;.@!?"].Should().Be("Bar");
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_EnvVarWithInvalidName_IsIgnoredAndLogged()
        {
            CheckParserInvocation(@"%Foo=Bar", result => result.Should().BeEmpty(), "regex");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_NameIsTooLong_IsIgnoredAndLogged()
        {
            var name = "F" + new string('o', 255);
            name.Length.Should().Be(256);

            CheckParserInvocation($"{name}=Bar", result => 
            {
                result.Should().HaveCount(1);
                result[name].Should().Be("Bar");
            });

            string invalidName = name + "o";
            CheckParserInvocation($"{invalidName}=Bar", result => result.Should().BeEmpty(), "variable names must not be longer than");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_ValueIsTooLong_IsIgnoredAndLogged()
        {
            var value = "B" + new string('a', 32767);
            value.Length.Should().Be(32768);

            CheckParserInvocation($"Foo={value}", result => 
            {
                result.Should().HaveCount(1);
                result["Foo"].Should().Be(value);
            });

            string invalidValue = value + "a";
            CheckParserInvocation($"Foo={invalidValue}", result => result.Should().BeEmpty(), "variable values must not be longer than");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_ValueHasEqualSign_ReturnsCorrectDictionary()
        {
            CheckParserInvocation("MyVar=A=B", result =>
            {
                result.Should().HaveCount(1);
                result["MyVar"].Should().Be("A=B");
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseEnvironmentVariablesString_SeveralVariables_ReturnsCorrectDictionary()
        {
            CheckParserInvocation("MyVar=A=B//||//MyOtherVar==//||//MyLastVar=Foo///||//", result =>
            {
                result.Should().HaveCount(3);
                result["MyVar"].Should().Be("A=B");
                result["MyOtherVar"].Should().Be("=");
                result["MyLastVar"].Should().Be("Foo/");
            });
        }

        private void CheckParserInvocation(string option, Action<IDictionary<string, string>> assertions, string errorMessage = null)
        {
            IDictionary<string, string> result;
            if (errorMessage == null)
            {
                result = _parser.ParseEnvironmentVariablesString(option);
                assertions(result);
                _fakeLogger.All.Should().BeEmpty();
                return;
            }

            // logging
            result = _parser.ParseEnvironmentVariablesString(option);
            assertions(result);
            _fakeLogger.Warnings.Should().ContainSingle();
            _fakeLogger.Warnings.Single().Should().Contain(errorMessage);

            // Exception
            _parser.Invoking(p => p.ParseEnvironmentVariablesString(option, false))
                .Should().Throw<Exception>().WithMessage($"*{errorMessage}*");
        }

    }

}