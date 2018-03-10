#pragma warning( disable : 4251 4275 )
#include "gmock/gmock.h"
#pragma warning( default : 4251 4275 )

int main(int argc, char **argv) {
   testing::InitGoogleMock(&argc, argv);
   return RUN_ALL_TESTS();
}