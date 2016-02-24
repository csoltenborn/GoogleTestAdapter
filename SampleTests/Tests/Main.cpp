#include "Main.h"
#include "string.h"
#include "gtest/gtest.h"

std::string TEST_DIRECTORY;

int main(int argc, char ** argv)
{
	std::string prefix("-testdirectory=");

	for (int i = 0; i < argc; i++)
	{
		if (strncmp(argv[i], prefix.c_str(), strlen(prefix.c_str())) == 0)
		{
			std::string testDirectory(argv[i]);
			testDirectory.erase(0, strlen(prefix.c_str()));
			TEST_DIRECTORY = testDirectory;
		}
	}

	::testing::InitGoogleTest(&argc, argv);
	return RUN_ALL_TESTS();
}

void CheckIfZeroInMain(int i)
{
	EXPECT_EQ(0, i);
}

void HelpMethodWithScopedTrace()
{
	SCOPED_TRACE("Main HelperMethod");
	CheckIfZeroInMain(1);
}
