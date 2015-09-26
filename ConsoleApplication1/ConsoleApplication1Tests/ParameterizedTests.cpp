#include "gtest/gtest.h"
#include "../ConsoleApplication1/ConsoleApplication1.h"
#include "../../GoogleTestExtension/GoogleTestAdapter\Resources/GTA_Traits.h"

using std::string;

class MyParam
{
public:
	MyParam(std::string s, int i) : s(s), i(i) {}
	int i;
	std::string s;
};

void PrintTo(const MyParam& param, ::std::ostream* os) {
	*os << "(" << param.i << "," << param.s << ")";
}

class ParameterizedTests : public testing::TestWithParam<MyParam>
{
};

TEST_P(ParameterizedTests, Simple) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("", GetParam().s);
}

TEST_P_TRAITS1(ParameterizedTests, SimpleTraits, Type, Small) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("", GetParam().s);
}

TEST_P_TRAITS2(ParameterizedTests, SimpleTraits2, Type, Small, Author, CSO) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("", GetParam().s);
}

TEST_P_TRAITS3(ParameterizedTests, SimpleTraits3, Type, Medium, Author, MSI, Category, Integration) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("", GetParam().s);
}

INSTANTIATE_TEST_CASE_P(InstantiationName,
	ParameterizedTests,
	testing::Values(MyParam("", 1), MyParam("!", 1), MyParam("", -1))
);