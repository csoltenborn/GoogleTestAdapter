#include <windows.h>
#include <string>
#include "gtest/gtest.h"
#include "../LibProject/Lib.h"
#include "../../GoogleTestAdapter/Core/Resources/GTA_Traits.h"

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

TEST_TRAITS1(TestMath, AddPassesWithTraits, Type, Medium)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_TRAITS2(TestMath, AddPassesWithTraits2, Type, Small, Author, CSO)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST_TRAITS3(TestMath, AddPassesWithTraits3, Type, Small, Author, CSO, TestCategory, Integration)
{
	EXPECT_EQ(20, Add(10, 10));
}


