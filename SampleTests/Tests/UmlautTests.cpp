#include <windows.h>
#include <string>
#include "gtest/gtest.h"
#include "gtest_wrapper.h"


TEST(Ümlautß, Täst)
{
	ASSERT_EQ(1, 2);
}

TEST_TRAITS(Ümlautß, Träits, Träit1, Völue1a, Träit1, Völue1b, Träit2, Völue2)
{
	EXPECT_EQ(1, 1);
}



class TheFixtüre : public testing::Test
{
};

TEST_F(TheFixtüre, Täst)
{
	EXPECT_EQ(1, 2);
}

TEST_F_TRAITS(TheFixtüre, Träits, Träit1, Völue1a, Träit1, Völue1b, Träit2, Völue2)
{
	EXPECT_EQ(1, 1);
}



class MyPäräm
{
public:
	MyPäräm(std::string s, int i) : s(s), i(i) {}
	int i;
	std::string s;
};

void PrintTo(const MyPäräm& param, ::std::ostream* os) {
	*os << "(" << param.i << "," << param.s << ")";
}

class ParameterizedTästs : public testing::TestWithParam<MyPäräm>
{
};

TEST_P(ParameterizedTästs, Täst) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("ÄÖÜäöüß", GetParam().s);
}

TEST_P_TRAITS(ParameterizedTästs, Träits, Träit1, Völue1a, Träit1, Völue1b, Träit2, Völue2) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("äöüßÄÖÜ", GetParam().s);
}

INSTANTIATE_TEST_CASE_P(ÜnstanceName,
	ParameterizedTästs,
	testing::Values(MyPäräm("ÄÖÜäöüß", 1))
);



class TheInterface {
public:
	virtual int GetValue(int i) = 0;
};

class ImplementationA : public TheInterface
{
public:
	int GetValue(int i) override { return i + 1; }
};

class ImplementationB : public TheInterface
{
public:
	int GetValue(int i) override { return i + 2; }
};

template< typename type >
class ÜmlautTypedTests : public ::testing::Test {
};

typedef ::testing::Types<ImplementationA, ImplementationB> ImplementationTypes;
TYPED_TEST_CASE(ÜmlautTypedTests, ImplementationTypes);

TYPED_TEST(ÜmlautTypedTests, Täst) {
	TypeParam theInstance;
	EXPECT_EQ(2, theInstance.GetValue(1));
}