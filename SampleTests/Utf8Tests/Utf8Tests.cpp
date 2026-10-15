// This file is UTF-8 encoded and compiled with /utf-8, i.e., the names of the tests are UTF-8 encoded
// within the executable (in contrast to the tests in Tests\UmlautTests.cpp, which are encoded in the ANSI code page)
#include "../Tests/gtest_wrapper.h"

TEST(Ümlautß, Täst)
{
  EXPECT_EQ(1, 1);
}

TEST(Ümlautß, Fäilüng)
{
  EXPECT_EQ(1, 2) << "Fäilüre";
}

TEST_TRAITS(Ümlautß, Träits, Type, Ümlaut)
{
  EXPECT_EQ(1, 1);
}

TEST(Ελληνικά, Δοκιμή)
{
  EXPECT_EQ(1, 1);
}

TEST(中文, 测试)
{
  EXPECT_EQ(1, 1);
}

// names with characters which are not word characters for .NET's regular expressions: a spacing combining mark
// (Devanagari vowel sign aa, category Mc) in the namespace and the test's name, and letter numbers (Roman numerals,
// category Nl)
namespace परीक्षण
{
  TEST(सूट, नाम)
  {
    EXPECT_EQ(1, 1);
  }
}

TEST(SuiteⅠ, TestⅡ)
{
  EXPECT_EQ(1, 1);
}
