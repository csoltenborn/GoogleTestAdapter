#include "gtest/gtest.h"
#include "../ClrLibProject/ClrLibProject.h"

TEST(ClrTests, Pass)
{
	ClrLibProject::ClrClass instance;
	ASSERT_EQ(2, instance.Add(1, 1));
}

TEST(ClrTests, Fail)
{
	ClrLibProject::ClrClass instance;
	ASSERT_EQ(3, instance.Add(1, 1));
}