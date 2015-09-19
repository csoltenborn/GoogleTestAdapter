#include "gtest/gtest.h"
#include <concrt.h>
#include "../ConsoleApplication1/ConsoleApplication1.h"
#include "../../GoogleTestExtension/GTA_Traits.h"

TEST(Crashing, TheCrash)
{
	__asm {
		mov esp, 0
	}
}

TEST(Crashing, AddFails)
{
	EXPECT_EQ(1000, Add(10, 10));
}

TEST(Crashing, AddPasses)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST(Crashing, LongRunning)
{
	Concurrency::wait(2000);
	EXPECT_EQ(1, 1);
}
