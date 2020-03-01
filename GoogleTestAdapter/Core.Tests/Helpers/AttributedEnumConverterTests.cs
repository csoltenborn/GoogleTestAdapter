using System;
using System.ComponentModel;
using FluentAssertions;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = System.ComponentModel.DescriptionAttribute;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class AttributedEnumConverterTests
    {
        [TypeConverter(typeof(AttributedEnumConverter))]
        private enum TestEnum
        {
            [Description("X_Desc")]
            X, 
            [Description("Y_Desc")]
            // ReSharper disable once InconsistentNaming
            Y_,
            Z
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Converter_HasCorrectType()
        {
            var converter = TypeDescriptor.GetConverter(typeof(TestEnum));

            converter.GetType().Should().Be(typeof(AttributedEnumConverter));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CanConvertFrom_ReturnsCorrectResult()
        {
            var converter = TypeDescriptor.GetConverter(typeof(TestEnum));

            converter.CanConvertFrom(typeof(string)).Should().BeTrue();
            converter.CanConvertFrom(typeof(TestEnum)).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CanConvertTo_ReturnsCorrectResult()
        {
            var converter = TypeDescriptor.GetConverter(typeof(TestEnum));

            converter.CanConvertTo(typeof(string)).Should().BeTrue();
            converter.CanConvertTo(typeof(TestEnum)).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ConvertFrom_ReturnsCorrectResult()
        {
            var converter = TypeDescriptor.GetConverter(typeof(TestEnum));

            converter.ConvertFrom("X").Should().Be(TestEnum.X);
            converter.ConvertFrom("X_Desc").Should().Be(TestEnum.X);

            converter.ConvertFrom("Y_").Should().Be(TestEnum.Y_);
            converter.ConvertFrom("Y_Desc").Should().Be(TestEnum.Y_);

            converter.ConvertFrom("Z").Should().Be(TestEnum.Z);

            Action convertFromInvalidEnumValue = () => converter.ConvertFrom("X_");
            convertFromInvalidEnumValue.Should().Throw<FormatException>();

            Action convertFromInt = () => converter.ConvertFrom(1);
            convertFromInt.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ConvertTo_ReturnsCorrectResult()
        {
            var converter = TypeDescriptor.GetConverter(typeof(TestEnum));

            converter.ConvertTo(TestEnum.X, typeof(string)).Should().Be("X_Desc");
            converter.ConvertTo(TestEnum.Y_, typeof(string)).Should().Be("Y_Desc");
            converter.ConvertTo(TestEnum.Z, typeof(string)).Should().Be("Z");

            Action convertToEnumValue = () => converter.ConvertTo("X", typeof(TestEnum));
            convertToEnumValue.Should().Throw<NotSupportedException>();
            
            Action convertFromInvalidEnumValue = () => converter.ConvertTo((TestEnum)4711, typeof(string));
            convertFromInvalidEnumValue.Should().Throw<ArgumentException>();
        }

    }
}