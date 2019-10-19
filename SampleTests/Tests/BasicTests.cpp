#include <windows.h>
#include <string>
#include "gtest/gtest.h"
#include "../LibProject/Lib.h"
#include "gtest_wrapper.h"

extern std::string TEST_DIRECTORY;

// http://stackoverflow.com/questions/8233842/how-to-check-if-directory-exist-using-c-and-winapi
bool DirExists(const std::string& dirName_in)
{
	DWORD ftyp = GetFileAttributesA(dirName_in.c_str());
	if (ftyp == INVALID_FILE_ATTRIBUTES)
		return false;  //something is wrong with your path!

	if (ftyp & FILE_ATTRIBUTE_DIRECTORY)
		return true;   // this is a directory!

	return false;    // this is not a directory!
}

TEST(CommandArgs, TestDirectoryIsSet)
{
	ASSERT_STRNE("", TEST_DIRECTORY.c_str());
	ASSERT_TRUE(DirExists(TEST_DIRECTORY));
}

inline bool ends_with(std::string const & value, std::string const & ending)
{
	if (ending.size() > value.size()) return false;
	return std::equal(ending.rbegin(), ending.rend(), value.rbegin());
}

TEST(WorkingDir, IsSolutionDirectory)
{
	char _working_directory[MAX_PATH + 1];
	GetCurrentDirectoryA(sizeof(_working_directory), _working_directory);
	std::string working_directory(_working_directory);

	ASSERT_TRUE(ends_with(working_directory, "SampleTests")) << "working_directory is " << working_directory;
}

TEST(EnvironmentVariable, IsSet)
{
	char* buf = nullptr;
	size_t sz = 0;
	ASSERT_EQ(0, _dupenv_s(&buf, &sz, "MYENVVAR"));
	ASSERT_TRUE(buf != nullptr);
    ASSERT_EQ(std::string(buf), "MyValue");
	free(buf);
}


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

TEST_TRAITS(TestMath, AddPassesWithTraits, Type, Medium)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_TRAITS(Traits, With8Traits, Trait1, Equals1, Trait2, Equals2, Trait3, Equals3, Trait4, Equals4, Trait5, Equals5, Trait6, Equals6, Trait7, Equals7, Trait8, Equals8)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With7Traits, Trait1, Equals1, Trait2, Equals2, Trait3, Equals3, Trait4, Equals4, Trait5, Equals5, Trait6, Equals6, Trait7, Equals7)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With6Traits, Trait1, Equals1, Trait2, Equals2, Trait3, Equals3, Trait4, Equals4, Trait5, Equals5, Trait6, Equals6)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With5Traits, Trait1, Equals1, Trait2, Equals2, Trait3, Equals3, Trait4, Equals4, Trait5, Equals5)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With4Traits, Trait1, Equals1, Trait2, Equals2, Trait3, Equals3, Trait4, Equals4)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With3Traits, Trait1, Equals1, Trait2, Equals2, Trait3, Equals3)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With2Traits, Trait1, Equals1, Trait2, Equals2)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, With1Traits, Trait1, Equals1)
{
	EXPECT_EQ(1, 1);
}

TEST_TRAITS(Traits, WithEqualTraits, Author, CSO, Author, JOG)
{
	EXPECT_EQ(1, 1);
}

TEST(OutputHandling, Output_ManyLinesWithNewlines)
{
	std::cout << "before test 1\n";
	std::cout << "before test 2\n";
	EXPECT_EQ(1, 2) << "test output";
	std::cout << "after test 1\n";
	std::cout << "after test 2\n";
}

TEST(OutputHandling, Output_OneLineWithNewlines)
{
	std::cout << "before test\n";
	EXPECT_EQ(1, 2) << "test output";
	std::cout << "after test\n";
}

TEST(OutputHandling, Output_OneLine)
{
	std::cout << "before test";
	EXPECT_EQ(1, 2) << "test output";
	std::cout << "after test";
}

TEST(OutputHandling, ManyLinesWithNewlines)
{
	std::cout << "before test 1\n";
	std::cout << "before test 2\n";
	EXPECT_EQ(1, 2);
	std::cout << "after test 1\n";
	std::cout << "after test 2\n";
}

TEST(OutputHandling, OneLineWithNewlines)
{
	std::cout << "before test\n";
	EXPECT_EQ(1, 2);
	std::cout << "after test\n";
}

TEST(OutputHandling, OneLine)
{
	std::cout << "before test";
	EXPECT_EQ(1, 2);
	std::cout << "after test";
}

TEST(abcd, t)
{
	EXPECT_EQ(1, 1);
}

TEST(bbcd, t)
{
   	EXPECT_EQ(1, 1);
}

TEST(bcd, t)
{
   	EXPECT_EQ(1, 1);
}