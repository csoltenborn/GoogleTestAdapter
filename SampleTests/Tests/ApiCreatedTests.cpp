#include <string>
#include "gtest/gtest.h"


class CustomTestBase
{
public:
	virtual std::string DoTest() = 0;
};

class FakeTest : public CustomTestBase
{
private:
	std::string message;

public:
	FakeTest(std::string message) : message(message) {}

	std::string DoTest() override
	{
		SCOPED_TRACE("FakeTest, Message: " + message);
		return message;
	}
};

class CustomTestFactory : public testing::internal::TestFactoryBase {
private:

	class CustomTestBaseWrapper : public ::testing::Test
	{
	private:
		CustomTestBase* actualTest;

	public:
		CustomTestBaseWrapper(CustomTestBase* actualTest) : actualTest(actualTest) {}

		void TestBody() override
		{
			std::string message = actualTest->DoTest();
			ASSERT_TRUE(message == "") << "Test failed: " << message;
		}
	};

	CustomTestBase* actualTestCase;

public:
	CustomTestFactory(CustomTestBase* actualTestCase) : actualTestCase(actualTestCase) {}

	virtual ::testing::Test* CreateTest() { return new CustomTestBaseWrapper(actualTestCase);	}
};

testing::TestInfo* CreateTest(std::string suitename, std::string testname, CustomTestBase* actualTest)
{
	return ::testing::internal::MakeAndRegisterTestInfo(
		suitename.c_str(), testname.c_str(), NULL, NULL,
#ifndef GTEST_1_7_0
		::testing::internal::CodeLocation(__FILE__, __LINE__),
#endif // GTEST_1_7_0
		::testing::internal::GetTestTypeId(),
		::testing::Test::SetUpTestCase,
		::testing::Test::TearDownTestCase,
		new CustomTestFactory(actualTest)
	);
}


testing::TestInfo* passingTest = CreateTest(
	"Api.Created.Tests", "PassingTest", new FakeTest(""));

testing::TestInfo* failingTest = CreateTest(
	"Api_Created_Tests", "FailingTest", new FakeTest("Something is wrong"));