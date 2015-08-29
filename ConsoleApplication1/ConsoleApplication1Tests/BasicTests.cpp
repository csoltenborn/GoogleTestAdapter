#include "gtest/gtest.h"
#include "../ConsoleApplication1/ConsoleApplication1.h"

TEST(TestMath, AddFails)
{
	EXPECT_EQ(1000, Add(10, 10));
}

TEST(TestMath, AddPasses)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST(TestMath, Crash)
{
	int* pInt = NULL;
	EXPECT_EQ(20, Add(*pInt, 10));
}


