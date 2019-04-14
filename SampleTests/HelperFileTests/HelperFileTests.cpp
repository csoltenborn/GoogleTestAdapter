#include "gtest/gtest.h"

extern std::string ARCH_DIR;

TEST(HelperFileTests, ArchDirIsSet)
{
	ASSERT_STRNE("", ARCH_DIR.c_str());
}