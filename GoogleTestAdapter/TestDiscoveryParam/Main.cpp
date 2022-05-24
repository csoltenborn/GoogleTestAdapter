#include "string.h"
#include "gtest/gtest.h"

std::string TEST_DIRECTORY;

int main(int argc, char ** argv)
{
	std::string prefix("-testDiscoveryFlag");

	for (int i = 0; i < argc; i++)
	{
		if (strncmp(argv[i], prefix.c_str(), strlen(prefix.c_str())) == 0)
		{
			::testing::InitGoogleTest(&argc, argv);
			return RUN_ALL_TESTS();
		}
	}
	return -1;
}

TEST(TestDiscovery, TestFails)
{
	EXPECT_EQ(1000, (10 + 10));
}

TEST(TestDiscovery, TestPasses)
{
	EXPECT_EQ(20, (10 + 10));
}
