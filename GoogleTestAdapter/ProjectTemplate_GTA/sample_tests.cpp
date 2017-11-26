#pragma warning( disable : 4251 4275 )
#include "GTA_Traits_1.8.0.h"
#pragma warning( default : 4251 4275 )

/*
 * The following are two very simple tests written with the Google Test framework. If you are interested
 * in the more sophisticated test types of Google Test, check out the item template TODO
 */
TEST(PassingTests, OneIsIndeedOne)
{
	ASSERT_EQ(1, 1);
}

TEST(FailingTests, OneIsNeitherTwoNorThree)
{
	EXPECT_EQ(1, 2);
	EXPECT_EQ(1, 3);
}


/*
 * The following tests make use of Google Test Adapter's macros with traits support. The tests can be filtered by traits
 * via the Test Explorer's Traits filter, and by the Settings option of VsTest.Console.Exe
 */
TEST_TRAITS(PassingTests, TwoIsIndeedTwo, Type, Small)
{
   ASSERT_EQ(2, 2);
}

TEST_TRAITS(FailingTests, TwoIsNeitherOneNorThree, Type, Medium)
{
   EXPECT_EQ(2, 1);
   EXPECT_EQ(2, 3);
}