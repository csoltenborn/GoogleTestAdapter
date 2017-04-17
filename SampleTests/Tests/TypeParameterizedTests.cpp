#include "gtest/gtest.h"
#include <array>
#include <list>
#include "gtest_wrapper.h"

struct MyStrangeArray : public std::array<int, 3>
{
	MyStrangeArray(std::initializer_list<int> init) : array({ 3,2,1 }) {}
};


template< typename int_container_type >
class TypedTests : public ::testing::Test {
public:
	typename int_container_type container = int_container_type { 1,2,3 };
};

typedef ::testing::Types<std::vector<int>, std::array<int, 3>, MyStrangeArray> IntContainerTypes;
TYPED_TEST_CASE(TypedTests, IntContainerTypes);

TYPED_TEST_TRAITS(TypedTests, CanIterate, Author, JOG) {
	int sum = 0;
	for (int value : this->container)
		sum += value;
	EXPECT_EQ(1 + 2 + 3, sum);
}

TYPED_TEST(TypedTests, CanDefeatMath) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}

TYPED_TEST_TRAITS(TypedTests, TwoTraits, Author, IBM, TestCategory, Integration) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}

TYPED_TEST_TRAITS(TypedTests, ThreeTraits, Author, IBM, TestCategory, Integration, Class, Simple) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}


template< typename int_container_type >
class TypeParameterizedTests : public TypedTests<int_container_type> {};

TYPED_TEST_CASE_P(TypeParameterizedTests);

TYPED_TEST_P_TRAITS(TypeParameterizedTests, CanIterate, Author, CSO) {
	int sum = 0;
	for (int value : this->container)
		sum += value;
	EXPECT_EQ(1 + 2 + 3, sum);
}

TYPED_TEST_P(TypeParameterizedTests, CanDefeatMath) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}

TYPED_TEST_P_TRAITS(TypeParameterizedTests, TwoTraits, Author, HAL, TestCategory, Unit) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}

TYPED_TEST_P_TRAITS(TypeParameterizedTests, ThreeTraits, Author, HAL, TestCategory, Unit, Class, Cake) {
	EXPECT_NE(this->container[0] + this->container[1], this->container[2]);
}

REGISTER_TYPED_TEST_CASE_P(TypeParameterizedTests, CanIterate, CanDefeatMath, TwoTraits, ThreeTraits);

typedef ::testing::Types<std::array<int, 3>, MyStrangeArray> IntArrayTypes;
INSTANTIATE_TYPED_TEST_CASE_P(Vec, TypeParameterizedTests, std::vector<int>);
INSTANTIATE_TYPED_TEST_CASE_P(Arr, TypeParameterizedTests, IntArrayTypes);




template< typename number_type >
class PrimitivelyTypedTests : public ::testing::Test {
public:
	std::list<number_type> container = std::list<number_type> { 1,2,127 };
};

typedef ::testing::Types<signed char, int, long> IntNumberTypes;
TYPED_TEST_CASE(PrimitivelyTypedTests, IntNumberTypes);

TYPED_TEST(PrimitivelyTypedTests, CanHasBigNumbers) {
	TypeParam sum = 0;
	for (auto value : this->container)
		sum += value;
	EXPECT_EQ(130, sum);
}

