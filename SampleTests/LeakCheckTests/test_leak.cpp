#include <iostream>
#include "gtest\gtest.h"

TEST(memory_leaks, passing)
{
   ASSERT_TRUE(true);
}

// causes memory leak!?
//TEST(memory_leaks, failing)
//{
//   ASSERT_TRUE(false);
//}

TEST(memory_leaks, passing_and_leaking)
{
   char * ch = new char[100];
   std::cout << "Leaking 100 chars...\n";
   ASSERT_TRUE(true);
}

TEST(memory_leaks, failing_and_leaking)
{
   char * ch = new char[100];
   std::cout << "Leaking 100 chars...\n";
   ASSERT_TRUE(false);
}
