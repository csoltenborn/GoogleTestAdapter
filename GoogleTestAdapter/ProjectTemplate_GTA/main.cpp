#pragma warning( disable : 4251 4275 )
#include "gtest/gtest.h"
#pragma warning( default : 4251 4275 )

int main(int argc, char **argv) {
   testing::InitGoogleTest(&argc, argv);
   return RUN_ALL_TESTS();
}