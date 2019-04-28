#include "gtest/gtest.h"

namespace main_namespace
{
   int main(int argn, char** argv)
   {
      EXPECT_STREQ("This is really a stupid choice for a method name", "");
      return 0;
   }
}

namespace
{
   int main()
   {
      EXPECT_STREQ("This is another a stupid choice for a method name", "");
      return 0;
   }
}

TEST(MainMethodTests, StupidMethod)
{
   EXPECT_EQ(1, main_namespace::main(0, 0));
   EXPECT_EQ(1, ::main());
}