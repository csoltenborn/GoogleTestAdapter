#include "gtest/gtest.h"
#include "../LibProject/Lib.h"
#include "../../GoogleTestExtension/GoogleTestAdapter/Resources/GTA_Traits.h"


class TheFixture : public testing::Test
{
};

TEST_F(TheFixture, AddFails)
{
	EXPECT_EQ(1000, Add(10, 10));
}

TEST_F(TheFixture, AddPasses)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_F(TheFixture, Crash)
{
	int* pInt = NULL;
	EXPECT_EQ(20, Add(*pInt, 10));
}

TEST_F_TRAITS1(TheFixture, AddPassesWithTraits, Type, Small)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_F_TRAITS2(TheFixture, AddPassesWithTraits2, Type, Small, Author, CSO)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_F_TRAITS3(TheFixture, AddPassesWithTraits3, Type, Small, Author, CSO, Category, Integration)
{
	EXPECT_EQ(20, Add(10, 10));
}
