// Tests with failing, passing, and skipping SetUpTestSuite() and TearDownTestSuite() methods of test fixtures (see
// option "TearDownTestSuite test cases" of GTA)

#include "gtest/gtest.h"

// SetUpTestSuite() fails, so Google Test skips the tests, which GTA reports as failed
class FailingSetUp : public ::testing::Test {
protected:
  static void SetUpTestSuite() { FAIL() << "SetUpTestSuite failure"; }
};
TEST_F(FailingSetUp, First) { EXPECT_TRUE(true); }
TEST_F(FailingSetUp, Second) { EXPECT_TRUE(true); }

// SetUpTestSuite() skips the tests
class SkippingSetUp : public ::testing::Test {
protected:
  static void SetUpTestSuite() { GTEST_SKIP() << "Skipped by SetUpTestSuite"; }
};
TEST_F(SkippingSetUp, Skipped) { EXPECT_TRUE(true); }

// TearDownTestSuite() fails after the test has passed, which is reported by test FailingTearDown.TearDownTestSuite
class FailingTearDown : public ::testing::Test {
protected:
  static void TearDownTestSuite() { ADD_FAILURE() << "TearDownTestSuite failure"; }
};
TEST_F(FailingTearDown, Passes) { EXPECT_TRUE(true); }

class PassingFixture : public ::testing::Test {
protected:
  static void SetUpTestSuite() {}
  static void TearDownTestSuite() {}
};
TEST_F(PassingFixture, Passes) { EXPECT_TRUE(true); }
TEST_F(PassingFixture, Fails) { EXPECT_TRUE(false) << "Test failure"; }

// the deprecated name of TearDownTestSuite()
class LegacyTearDown : public ::testing::Test {
protected:
  static void TearDownTestCase() { ADD_FAILURE() << "TearDownTestCase failure"; }
};
TEST_F(LegacyTearDown, Passes) { EXPECT_TRUE(true); }

namespace fixtures {
  class NamespacedFixture : public ::testing::Test {
  protected:
    static void TearDownTestSuite() {}
  };
  TEST_F(NamespacedFixture, Passes) { EXPECT_TRUE(true); }
}

// one suite (and thus one TearDownTestSuite test) per type
template <typename T>
class TypedFixture : public ::testing::Test {
protected:
  static void TearDownTestSuite() {}
};
using FixtureTypes = ::testing::Types<int, char>;
TYPED_TEST_SUITE(TypedFixture, FixtureTypes);
TYPED_TEST(TypedFixture, Passes) { EXPECT_TRUE(true); }

class ParameterizedFixture : public ::testing::TestWithParam<int> {
protected:
  static void TearDownTestSuite() { ADD_FAILURE() << "Parameterized TearDownTestSuite failure"; }
};
TEST_P(ParameterizedFixture, Passes) { EXPECT_TRUE(GetParam() > 0); }
INSTANTIATE_TEST_SUITE_P(Instantiation, ParameterizedFixture, ::testing::Values(1, 2));

// no fixture, no TearDownTestSuite test
TEST(PlainTests, Passes) { EXPECT_TRUE(true); }
