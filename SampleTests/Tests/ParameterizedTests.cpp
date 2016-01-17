#include "gtest/gtest.h"
#include "../LibProject/Lib.h"
#include "../../GoogleTestExtension/GoogleTestAdapter/Resources/GTA_Traits.h"

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

INSTANTIATE_TEST_CASE_P(/* no instantiation name*/,
	ParameterizedTests,
	testing::Values(MyParam("_", 0))
	);


// Some parameters can have changing output between test runs.
// Best example: strings whose addresses can change between runs.
// The test runner should be able to handle this (hasn't in the past).
typedef std::pair<char*, int> MyPointerParam;

class PointerParameterizedTests : public testing::TestWithParam<MyPointerParam>
{
};

TEST_P(PointerParameterizedTests, CheckStringLength) {
	EXPECT_EQ(GetParam().second, strlen(GetParam().first));
}

INSTANTIATE_TEST_CASE_P(/* no instantiation name*/,
	PointerParameterizedTests,
	// use _strdup to have strings on the heap and enforce a new address each test run (yes... we leak memory)
	testing::Values(MyPointerParam(_strdup(""), 0), MyPointerParam(_strdup("Test"), 4), MyPointerParam(_strdup("ooops"), 23))
	);
