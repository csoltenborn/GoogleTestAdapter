#include "Main.h"
#include "string.h"
#include "gtest/gtest.h"

std::string TEST_DIRECTORY;

int main(int argc, char ** argv)
{
	std::string prefix("-testdirectory=");
	std::string justFail("-justfail");

   bool weShouldFail = false;
   for (int i = 0; i < argc; i++)
	{
		if (strncmp(argv[i], prefix.c_str(), strlen(prefix.c_str())) == 0)
		{
			std::string testDirectory(argv[i]);
			testDirectory.erase(0, strlen(prefix.c_str()));
			TEST_DIRECTORY = testDirectory;
		}
      if (strncmp(argv[i], justFail.c_str(), strlen(justFail.c_str())) == 0)
      {
         weShouldFail = true;
      }
	}

   if (weShouldFail)
   {
      std::cout << "This test failed intentionally\n";
      return 1;
   }

	::testing::InitGoogleTest(&argc, argv);
   int result = RUN_ALL_TESTS();

   std::cout << "GTA_EXIT_CODE_OUTPUT_BEGIN\n";
   std::cout << "The result code output\n";
   std::cout << "GTA_EXIT_CODE_OUTPUT_END\n";
   std::cout << "Some more output\n";

   return result;
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
