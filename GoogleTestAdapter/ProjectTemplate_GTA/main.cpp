#include "$main_gtest_include$"

int main(int argc, char **argv) {
   testing::$main_init_framework$(&argc, argv);
   return RUN_ALL_TESTS();
}