#include "string.h"
#include "gtest/gtest.h"

std::string THE_TARGET;

int main(int argc, char ** argv)
{
	std::string prefix_arch_dir("-TheTarget=");

	for (int i = 0; i < argc; i++)
	{
		std::string s = argv[i];
		if (strncmp(argv[i], prefix_arch_dir.c_str(), strlen(prefix_arch_dir.c_str())) == 0)
		{
			std::string arch_dir(argv[i]);
			arch_dir.erase(0, strlen(prefix_arch_dir.c_str()));
			THE_TARGET = arch_dir;
		}
	}

	::testing::InitGoogleTest(&argc, argv);
	return RUN_ALL_TESTS();
}