#include "gtest/gtest.h"

extern std::string THE_TARGET;

TEST(HelperFileTests, TheTargetIsSet)
{
	ASSERT_STREQ("HelperFileTests_gta.exe", THE_TARGET.c_str());
}