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

TEST(MessageParserTests, SimpleAssert)
{
	ASSERT_EQ(1, 2);
}

TEST(MessageParserTests, SimpleExpect)
{
	EXPECT_EQ(1, 2);
}

TEST(MessageParserTests, ExpectAndAssert)
{
	EXPECT_EQ(1, 2);
	ASSERT_EQ(1, 2);
}

TEST(MessageParserTests, ExpectInOtherMethod)
{
	CheckIfZero(1);
}

TEST(MessageParserTests, ExpectInOtherFile)
{
	CheckIfZeroInMain(1);
}

TEST(MessageParserTests, ExpectInOtherMethodAndFile)
{
	CheckIfZero(1);
	CheckIfZeroInMain(1);
}

TEST(MessageParserTests, ScopedTraceInSameFile)
{
	{
		SCOPED_TRACE("Scoped");
		CheckIfZero(1);
	}
	CheckIfZero(1);
}

//TEST(ScopedTraceTests, InAnotherFile)
//{
//	MethodInMainWithScopedTrace();
//}
//
//TEST(ScopedTraceTests, WithAndWithoutFile)
//{
//	MethodWithScopedTrace();
//	MethodInMainWithScopedTrace();
//}
//
//TEST(ScopedTraceTests, LocalAssertAndWithAndWithoutFile)
//{
//	EXPECT_EQ(1, 2);
//	MethodWithScopedTrace();
//	MethodInMainWithScopedTrace();
//}
