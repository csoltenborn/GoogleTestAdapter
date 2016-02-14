#include "gtest/gtest.h"
#include <concrt.h>
#include "../LibProject/Lib.h"
#include "../../GoogleTestAdapter/Core/Resources/GTA_Traits.h"

#include "../Tests/Main.cpp"

extern "C" void CrashReallyHard(void);

TEST(Crashing, AddFailsBeforeCrash)
{
	EXPECT_EQ(1000, Add(10, 10));
}

TEST(Crashing, AddPassesBeforeCrash)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST(Crashing, TheCrash)
{
	CrashReallyHard();
}

TEST(Crashing, AddFailsAfterCrash)
{
	EXPECT_EQ(1000, Add(10, 10));
}

TEST(Crashing, AddPassesAfterCrash)
{
	EXPECT_EQ(20, Add(10, 10));
}

TEST(Crashing, LongRunning)
{
	Concurrency::wait(2000);
	EXPECT_EQ(1, 1);
}
