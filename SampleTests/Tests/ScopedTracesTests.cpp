#include <windows.h>
#include <string>
#include "gtest/gtest.h"
#include "../LibProject/Lib.h"
#include "../../GoogleTestAdapter/Core/Resources/GTA_Traits.h"
#include "Main.h"

extern std::string TEST_DIRECTORY;

void CheckIfZero(int i)
{
	EXPECT_EQ(0, i);
}

void HelperMethodWithScopedTrace()
{
	SCOPED_TRACE("HelperMethod");
	CheckIfZero(1);
}

TEST(MessageParserTests, SimpleAssert)
{
	ASSERT_EQ(1, 2);
}

TEST(MessageParserTests, SimpleExpect)
{
	EXPECT_EQ(2, 3);
}

TEST(MessageParserTests, ExpectAndAssert)
{
	EXPECT_EQ(3, 4);
	ASSERT_EQ(4, 5);
}

TEST(MessageParserTests, ExpectInOtherMethod)
{
	CheckIfZero(1);
}

TEST(MessageParserTests, ExpectInOtherFile)
{
	CheckIfZeroInMain(1);
}

TEST(MessageParserTests, ExpectInTestAndMethodAndOtherFile)
{
	EXPECT_EQ(5, 6);
	CheckIfZero(1);
	CheckIfZeroInMain(1);
}

TEST(MessageParserTests, ScopedTraceInTestMethod)
{
	SCOPED_TRACE("TestMethod");
	CheckIfZero(1);
}

TEST(MessageParserTests, TwoScopedTracesInTestMethod)
{
	SCOPED_TRACE("TestMethod Outer");
	{
		SCOPED_TRACE("TestMethod Inner");
		CheckIfZero(1);
	}
}

TEST(MessageParserTests, ScopedTraceInHelperMethod)
{
	HelperMethodWithScopedTrace();
}

TEST(MessageParserTests, ScopedTraceInTestMethodANdHelperMethod)
{
	SCOPED_TRACE("TestMethod");
	HelperMethodWithScopedTrace();
}

TEST(MessageParserTests, ScopedTraceInTestMethodANdHelperMethodAndExpectInTestMethod)
{
	SCOPED_TRACE("TestMethod");
	HelperMethodWithScopedTrace();
	EXPECT_EQ(0, 1);
}
