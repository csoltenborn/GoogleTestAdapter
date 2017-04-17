#include <windows.h>
#include <string>
#include "gtest/gtest.h"
#include "gtest_wrapper.h"


TEST(�mlaut�, T�st)
{
	ASSERT_EQ(1, 2);
}

TEST_TRAITS(�mlaut�, Tr�its, Tr�it1, V�lue1a, Tr�it1, V�lue1b, Tr�it2, V�lue2)
{
	EXPECT_EQ(1, 1);
}



class TheFixt�re : public testing::Test
{
};

TEST_F(TheFixt�re, T�st)
{
	EXPECT_EQ(1, 2);
}

TEST_F_TRAITS(TheFixt�re, Tr�its, Tr�it1, V�lue1a, Tr�it1, V�lue1b, Tr�it2, V�lue2)
{
	EXPECT_EQ(1, 1);
}



class MyP�r�m
{
public:
	MyP�r�m(std::string s, int i) : s(s), i(i) {}
	int i;
	std::string s;
};

void PrintTo(const MyP�r�m& param, ::std::ostream* os) {
	*os << "(" << param.i << "," << param.s << ")";
}

class ParameterizedT�sts : public testing::TestWithParam<MyP�r�m>
{
};

TEST_P(ParameterizedT�sts, T�st) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("�������", GetParam().s);
}

TEST_P_TRAITS(ParameterizedT�sts, Tr�its, Tr�it1, V�lue1a, Tr�it1, V�lue1b, Tr�it2, V�lue2) {
	EXPECT_EQ(1, GetParam().i);
	EXPECT_EQ("�������", GetParam().s);
}

INSTANTIATE_TEST_CASE_P(�nstanceName,
	ParameterizedT�sts,
	testing::Values(MyP�r�m("�������", 1))
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
class �mlautTypedTests : public ::testing::Test {
};

typedef ::testing::Types<ImplementationA, ImplementationB> ImplementationTypes;
TYPED_TEST_CASE(�mlautTypedTests, ImplementationTypes);

TYPED_TEST(�mlautTypedTests, T�st) {
	TypeParam theInstance;
	EXPECT_EQ(2, theInstance.GetValue(1));
}