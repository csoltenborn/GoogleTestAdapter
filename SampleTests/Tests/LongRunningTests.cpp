#include <concrt.h>
#include "gtest/gtest.h"

#include "../Tests/Main.cpp"

TEST(LongRunningTests, Test1)
{
	Concurrency::wait(2000);
	EXPECT_EQ(1, 1);
}

TEST(LongRunningTests, Test2)
{
	Concurrency::wait(2000);
	EXPECT_EQ(1, 2);
}
