#include "gtest/gtest.h"
#include "../ConsoleApplication1/ConsoleApplication1.h"
#include "../../GoogleTestExtension/GTA_Traits.h"

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

TEST_TRAITS1(TestMath, AddPassesWithTraits, Type, Small)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_TRAITS2(TestMath, AddPassesWithTraits2, Type, Small, Author, CSO)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_TRAITS3(TestMath, AddPassesWithTraits3, Type, Small, Author, CSO, Category, Integration)
{
	EXPECT_EQ(20, Add(10, 10));
}


