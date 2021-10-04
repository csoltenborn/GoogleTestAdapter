#pragma warning( disable : 4251 4275 )
#include "GTA_Traits_1.8.0.h"
#pragma warning( default : 4251 4275 )


// code under test
class IFibonacci
{
public:
   virtual unsigned int Fib(unsigned int n) = 0;
};

class RecursiveFibonacci : public IFibonacci
{
public:
   unsigned int Fib(unsigned int n) override
   {
      if (n < 1) throw std::invalid_argument("n must be >=1");

      return n == 1 || n == 2 ? 1 : Fib(n - 1) + Fib(n - 2);
   }
};

class IterativeFibonacci : public IFibonacci
{
public:
   unsigned int Fib(unsigned int n) override
   {
      if (n < 1) throw std::invalid_argument("n must be >=1");

      unsigned int last = 1, result = 1;
      for (unsigned int i = 3; i <= n; i++)
      {
         unsigned int temp = last + result;
         last = result;
         result = temp;
      }
      return result;
   }
};


// the assertions (to be reused by the different test types of this demo)
void AssertThrowsForZero(IFibonacci* fibonacci)
{
   ASSERT_THROW(fibonacci->Fib(0), std::invalid_argument);
}

void AssertComputesCorrectValues(IFibonacci* fibonacci)
{
   EXPECT_EQ(1, fibonacci->Fib(1));
   EXPECT_EQ(1, fibonacci->Fib(2));
   EXPECT_EQ(2, fibonacci->Fib(3));
   EXPECT_EQ(3, fibonacci->Fib(4));
   EXPECT_EQ(5, fibonacci->Fib(5));
   EXPECT_EQ(832040, fibonacci->Fib(30));
}


// simple tests
TEST(SimpleTests, ThrowsForZero)
{
   IFibonacci* fibonacci = new IterativeFibonacci();
   AssertThrowsForZero(fibonacci);
   delete fibonacci;
}

TEST_TRAITS(SimpleTests, ComputesCorrectValue, Type, Complex)
{
   IFibonacci* fibonacci = new IterativeFibonacci();
   AssertComputesCorrectValues(fibonacci);
   delete fibonacci;
}


// text fixtures
class FixtureTests : public testing::Test
{
protected:
   static int* some_shared_expensive_resource;
   IFibonacci* _fibonacci;

   static void SetUpTestCase()
   {
      some_shared_expensive_resource = new int(0);
   }

   void SetUp() override
   {
      _fibonacci = new IterativeFibonacci();
   }

   void TearDown() override
   {
      delete _fibonacci;
   }

   static void TearDownTestCase()
   {
      delete some_shared_expensive_resource;
   }
};

int* FixtureTests::some_shared_expensive_resource = NULL;

TEST_F(FixtureTests, ThrowsForZero)
{
   AssertThrowsForZero(_fibonacci);
}

TEST_F_TRAITS(FixtureTests, ComputesCorrectValue, Type, Complex)
{
   AssertComputesCorrectValues(_fibonacci);
}


// parameterized tests
class ParameterizedTests : public testing::TestWithParam<IFibonacci*>
{
};

TEST_P(ParameterizedTests, ThrowsForZero)
{
   AssertThrowsForZero(GetParam());
}

TEST_P_TRAITS(ParameterizedTests, ComputesCorrectValue, Type, Complex)
{
   AssertComputesCorrectValues(GetParam());
}

IterativeFibonacci iterativeFibonacci;
RecursiveFibonacci recursiveFibonacci;

INSTANTIATE_TEST_CASE_P(
   GTA,
   ParameterizedTests,
   testing::Values(&iterativeFibonacci, &recursiveFibonacci)
);


// typed tests
template < typename TTypeUnderTest >
class TypedTests : public ::testing::Test {
protected:
   typename TTypeUnderTest* _fibonacci;

   void SetUp() override
   {
      _fibonacci = new TTypeUnderTest;
   }

   void TearDown() override
   {
      delete _fibonacci;
   }
};

typedef ::testing::Types<RecursiveFibonacci, IterativeFibonacci> FibonacciTypes;
TYPED_TEST_CASE(TypedTests, FibonacciTypes);

TYPED_TEST(TypedTests, ThrowsForZero) {
   AssertThrowsForZero(_fibonacci);
}

TYPED_TEST_TRAITS(TypedTests, ComputesCorrectValue, Type, Complex) {
   AssertComputesCorrectValues(_fibonacci);
}


// type-parameterized tests
template < typename TTypeUnderTest >
class TypeParameterizedTests : public ::testing::Test {
protected:
   typename TTypeUnderTest* _fibonacci;

   void SetUp() override
   {
      _fibonacci = new TTypeUnderTest;
   }

   void TearDown() override
   {
      delete _fibonacci;
   }
};

TYPED_TEST_CASE_P(TypeParameterizedTests);

TYPED_TEST_P(TypeParameterizedTests, ThrowsForZero) {
   AssertThrowsForZero(_fibonacci);
}

TYPED_TEST_P_TRAITS(TypeParameterizedTests, ComputesCorrectValue, Type, Complex) {
   AssertComputesCorrectValues(_fibonacci);
}

REGISTER_TYPED_TEST_CASE_P(TypeParameterizedTests, ThrowsForZero, ComputesCorrectValue);

INSTANTIATE_TYPED_TEST_CASE_P(GTA, TypeParameterizedTests, FibonacciTypes);