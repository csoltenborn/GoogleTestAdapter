#include "gtest/gtest.h"

// NOTE THAT getest discourages the use of namespaces (see e.g. this thread: https://groups.google.com/a/chromium.org/forum/#!topic/chromium-dev/MwPIo2BnVhM)
// GTA still makes a best effort to handle such tests...

namespace Namespace_1 {

   TEST(Namespace_Named, Test)
   {
      EXPECT_EQ(1, 1);
   }

   namespace Namespace_2_Nested {

      TEST(Namespace_Named_Named, Test)
      {
         EXPECT_EQ(1, 1);
      }

   } // Namespace_1

   namespace {

      TEST(Namespace_Named_Anon, Test)
      {
         EXPECT_EQ(1, 1);
      }
   }

} // Namespace_2_Nested

namespace {
	
   TEST(Namespace_Anon, Test)
   {
      EXPECT_EQ(1, 1);
   }

   namespace {

      TEST(Namespace_Anon_Anon, Test)
      {
         EXPECT_EQ(1, 1);
      }

   }

   namespace Anon_Nested {

      TEST(Namespace_Anon_Named, Test)
      {
         EXPECT_EQ(1, 1);
      }

   }
}

