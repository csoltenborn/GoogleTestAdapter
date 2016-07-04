#include "gtest/gtest.h"

#include "../Tests/Main.cpp"


class LoadTests : public testing::TestWithParam<int>{};

TEST_P(LoadTests, Test) {
	EXPECT_EQ(1, GetParam() % 2);
}

// create any number of tests here
INSTANTIATE_TEST_CASE_P(, LoadTests, testing::Range(0, 5000));