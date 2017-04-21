#include "../DllProject/DllProject.h"
#include "gtest\gtest.h"

TEST(Passing, InvokeFunction)
{
	ASSERT_EQ(0, ReturnZero());
}

TEST(Failing, InvokeFunction)
{
	ASSERT_EQ(1, ReturnZero());
}