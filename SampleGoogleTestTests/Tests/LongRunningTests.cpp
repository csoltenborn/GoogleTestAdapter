#include <concrt.h>
#include "gtest/gtest.h"

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
