#include "gtest/gtest.h"
#include <array>

class MyStrangeArray : public std::array<int, 3>
{
public:  MyStrangeArray(std::initializer_list<int> init) : array({ 3,2,1 }) {}
};


template< typename int_container_type >
class TypedTests : public ::testing::Test {
public:
	typename int_container_type container{ 1,2,3 };
};

typedef ::testing::Types<std::vector<int>, std::array<int, 3>, MyStrangeArray> IntContainerTypes;
TYPED_TEST_CASE(TypedTests, IntContainerTypes);

TYPED_TEST(TypedTests, CanIterate) {
	int sum = 0;
	for (int value : this->container)
		sum += value;
	EXPECT_EQ(1 + 2 + 3, sum);
}

TYPED_TEST(TypedTests, CanDefeatMath) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}


template< typename int_container_type >
class TypeParameterizedTests : public TypedTests<int_container_type> {};

TYPED_TEST_CASE_P(TypeParameterizedTests);

TYPED_TEST_P(TypeParameterizedTests, CanIterate) {
	int sum = 0;
	for (int value : this->container)
		sum += value;
	EXPECT_EQ(1 + 2 + 3, sum);
}

TYPED_TEST_P(TypeParameterizedTests, CanDefeatMath) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}

REGISTER_TYPED_TEST_CASE_P(TypeParameterizedTests, CanIterate, CanDefeatMath);

typedef ::testing::Types<std::array<int, 3>, MyStrangeArray> IntArrayTypes;
INSTANTIATE_TYPED_TEST_CASE_P(Vec, TypeParameterizedTests, std::vector<int>);
INSTANTIATE_TYPED_TEST_CASE_P(Arr, TypeParameterizedTests, IntArrayTypes);