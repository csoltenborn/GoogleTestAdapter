#pragma once

#include "gtest\gtest.h"

#define GTA_TRAIT_(name, value) \
  name##__GTA__##value##_GTA_TRAIT


/* internal helpers */
#define _VA_NARGS_GLUE(x, y) x y
#define _VA_NARGS_RETURN_COUNT(_1_, _2_, _3_, _4_, _5_, _6_, _7_, _8_, _9_, _10_, _11_, _12_, _13_, _14_, _15_, _16_, count, ...) count
#define _VA_NARGS_EXPAND(args) _VA_NARGS_RETURN_COUNT args
#define _VA_NARGS_COUNT_MAX16(...) _VA_NARGS_EXPAND((__VA_ARGS__, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0))

#define _VA_NARGS_OVERLOAD_MACRO2(name, count) name##count
#define _VA_NARGS_OVERLOAD_MACRO1(name, count) _VA_NARGS_OVERLOAD_MACRO2(name, count)
#define _VA_NARGS_OVERLOAD_MACRO(name,  count) _VA_NARGS_OVERLOAD_MACRO1(name, count)

/* expose for re-use */
#define VA_NARGS_CALL_OVERLOAD(name, ...) _VA_NARGS_GLUE(_VA_NARGS_OVERLOAD_MACRO(name, _VA_NARGS_COUNT_MAX16(__VA_ARGS__)), (__VA_ARGS__))


#define GTA_TRAITS_MARKER2(name1, value1) \
    __declspec(dllexport) static void name1##__GTA__##value1##_GTA_TRAIT() {}
#define GTA_TRAITS_MARKER4(name1, value1, name2, value2) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER2(name2, value2)
#define GTA_TRAITS_MARKER6(name1, value1, name2, value2, name3, value3) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER4(name2, value2, name3, value3)
#define GTA_TRAITS_MARKER8(name1, value1, name2, value2, name3, value3, name4, value4) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER6(name2, value2, name3, value3, name4, value4)
#define GTA_TRAITS_MARKER10(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER8(name2, value2, name3, value3, name4, value4, name5, value5)
#define GTA_TRAITS_MARKER12(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5, name6, value6) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER10(name2, value2, name3, value3, name4, value4, name5, value5, name6, value6)
#define GTA_TRAITS_MARKER14(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER12(name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7)
#define GTA_TRAITS_MARKER16(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7, name8, value8) \
    GTA_TRAITS_MARKER2(name1, value1) \
    GTA_TRAITS_MARKER14(name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7, name8, value8)
#define GTA_TRAITS_MARKER(...) VA_NARGS_CALL_OVERLOAD(GTA_TRAITS_MARKER, __VA_ARGS__)

#define GTA_TRAITS_CALL2(name1, value1) \
    name1##__GTA__##value1##_GTA_TRAIT();
#define GTA_TRAITS_CALL4(name1, value1, name2, value2) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL2(name2, value2)
#define GTA_TRAITS_CALL6(name1, value1, name2, value2, name3, value3) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL4(name2, value2, name3, value3)
#define GTA_TRAITS_CALL8(name1, value1, name2, value2, name3, value3, name4, value4) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL6(name2, value2, name3, value3, name4, value4)
#define GTA_TRAITS_CALL10(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL8(name2, value2, name3, value3, name4, value4, name5, value5)
#define GTA_TRAITS_CALL12(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5, name6, value6) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL10(name2, value2, name3, value3, name4, value4, name5, value5, name6, value6)
#define GTA_TRAITS_CALL14(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL12(name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7)
#define GTA_TRAITS_CALL16(name1, value1, name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7, name8, value8) \
    GTA_TRAITS_CALL2(name1, value1) \
    GTA_TRAITS_CALL14(name2, value2, name3, value3, name4, value4, name5, value5, name6, value6, name7, value7, name8, value8)
#define GTA_TRAITS_CALL(...) VA_NARGS_CALL_OVERLOAD(GTA_TRAITS_CALL, __VA_ARGS__)


#define TEST_P_TRAITS(test_case_name, test_name, ...) \
  class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) \
      : public test_case_name { \
   public: \
    GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() {} \
    virtual void TestBody(); \
   private: \
    GTA_TRAITS_MARKER(__VA_ARGS__) \
    static int AddToRegistry() { \
      ::testing::UnitTest::GetInstance()->parameterized_test_registry(). \
          GetTestCasePatternHolder<test_case_name>(\
              #test_case_name, \
              ::testing::internal::CodeLocation(\
                  __FILE__, __LINE__))->AddTestPattern(\
                      #test_case_name, \
                      #test_name, \
                      new ::testing::internal::TestMetaFactory< \
                          GTEST_TEST_CLASS_NAME_(\
                              test_case_name, test_name)>()); \
      return 0; \
    } \
    static int gtest_registering_dummy_ GTEST_ATTRIBUTE_UNUSED_; \
    GTEST_DISALLOW_COPY_AND_ASSIGN_(\
        GTEST_TEST_CLASS_NAME_(test_case_name, test_name)); \
  }; \
  int GTEST_TEST_CLASS_NAME_(test_case_name, \
                             test_name)::gtest_registering_dummy_ = \
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::AddToRegistry(); \
  void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()



#define GTEST_TEST_TRAITS_(test_case_name, test_name, parent_class, parent_id,...)\
class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) : public parent_class {\
 public:\
  GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() {}\
 private:\
  virtual void TestBody();\
  GTA_TRAITS_MARKER(__VA_ARGS__) \
  static ::testing::TestInfo* const test_info_ GTEST_ATTRIBUTE_UNUSED_;\
  GTEST_DISALLOW_COPY_AND_ASSIGN_(\
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name));\
};\
\
::testing::TestInfo* const GTEST_TEST_CLASS_NAME_(test_case_name, test_name)\
  ::test_info_ =\
    ::testing::internal::MakeAndRegisterTestInfo(\
        #test_case_name, #test_name, NULL, NULL, \
        ::testing::internal::CodeLocation(__FILE__, __LINE__), \
        (parent_id), \
        parent_class::SetUpTestCase, \
        parent_class::TearDownTestCase, \
        new ::testing::internal::TestFactoryImpl<\
            GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>);\
void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()




#define TEST_TRAITS(test_case_name, test_name, ...)\
  GTEST_TEST_TRAITS_(test_case_name, test_name, \
              ::testing::Test, ::testing::internal::GetTestTypeId(), \
               __VA_ARGS__)


#define TEST_F_TRAITS(test_fixture, test_name, ...)\
  GTEST_TEST_TRAITS_(test_fixture, test_name, test_fixture, \
              ::testing::internal::GetTypeId<test_fixture>(), \
              __VA_ARGS__)


# define TYPED_TEST_TRAITS(CaseName, TestName, ...) \
  template <typename gtest_TypeParam_> \
  class GTEST_TEST_CLASS_NAME_(CaseName, TestName) \
      : public CaseName<gtest_TypeParam_> { \
   public:\
     GTEST_TEST_CLASS_NAME_(CaseName, TestName)() { \
       GTA_TRAITS_CALL(__VA_ARGS__) \
     }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    GTA_TRAITS_MARKER(__VA_ARGS__) \
  }; \
  bool gtest_##CaseName##_##TestName##_registered_ GTEST_ATTRIBUTE_UNUSED_ = \
      ::testing::internal::TypeParameterizedTest< \
          CaseName, \
          ::testing::internal::TemplateSel< \
              GTEST_TEST_CLASS_NAME_(CaseName, TestName)>, \
          GTEST_TYPE_PARAMS_(CaseName)>::Register(\
              "", ::testing::internal::CodeLocation(__FILE__, __LINE__), \
              #CaseName, #TestName, 0); \
  template <typename gtest_TypeParam_> \
  void GTEST_TEST_CLASS_NAME_(CaseName, TestName)<gtest_TypeParam_>::TestBody()


# define TYPED_TEST_P_TRAITS(CaseName, TestName, ...) \
  namespace GTEST_CASE_NAMESPACE_(CaseName) { \
  template <typename gtest_TypeParam_> \
  class TestName : public CaseName<gtest_TypeParam_> { \
   public:\
     TestName() {\
       GTA_TRAITS_CALL(__VA_ARGS__) \
	 }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    GTA_TRAITS_MARKER(__VA_ARGS__) \
  }; \
  static bool gtest_##TestName##_defined_ GTEST_ATTRIBUTE_UNUSED_ = \
      GTEST_TYPED_TEST_CASE_P_STATE_(CaseName).AddTestName(\
          __FILE__, __LINE__, #CaseName, #TestName); \
  } \
  template <typename gtest_TypeParam_> \
  void GTEST_CASE_NAMESPACE_(CaseName)::TestName<gtest_TypeParam_>::TestBody()
