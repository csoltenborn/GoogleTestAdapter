#pragma once

#include "gtest\gtest.h"


#define GTA_TRAIT_(name, value) \
  name##__GTA__##value##_GTA_TRAIT


#define TEST_P_TRAITS1(test_case_name, test_name, name, value) \
  class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) \
      : public test_case_name { \
   public: \
    GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() {} \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name, value)() {} \
   private: \
    static int AddToRegistry() { \
      GTA_TRAIT_(name, value)(); \
      ::testing::UnitTest::GetInstance()->parameterized_test_registry(). \
          GetTestCasePatternHolder<test_case_name>(\
              #test_case_name, __FILE__, __LINE__)->AddTestPattern(\
                  #test_case_name, \
                  #test_name, \
                  new ::testing::internal::TestMetaFactory< \
                      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>()); \
      return 0; \
    } \
    static int gtest_registering_dummy_; \
    GTEST_DISALLOW_COPY_AND_ASSIGN_(\
        GTEST_TEST_CLASS_NAME_(test_case_name, test_name)); \
  }; \
  int GTEST_TEST_CLASS_NAME_(test_case_name, \
                             test_name)::gtest_registering_dummy_ = \
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::AddToRegistry(); \
  void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()


#define TEST_P_TRAITS2(test_case_name, test_name, name1, value1, name2, value2) \
  class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) \
      : public test_case_name { \
   public: \
    GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() {} \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
    static void GTA_TRAIT_(name2, value2)() {} \
   private: \
    static int AddToRegistry() { \
      GTA_TRAIT_(name1, value1)(); \
      GTA_TRAIT_(name2, value2)(); \
      ::testing::UnitTest::GetInstance()->parameterized_test_registry(). \
          GetTestCasePatternHolder<test_case_name>(\
              #test_case_name, __FILE__, __LINE__)->AddTestPattern(\
                  #test_case_name, \
                  #test_name, \
                  new ::testing::internal::TestMetaFactory< \
                      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>()); \
      return 0; \
    } \
    static int gtest_registering_dummy_; \
    GTEST_DISALLOW_COPY_AND_ASSIGN_(\
        GTEST_TEST_CLASS_NAME_(test_case_name, test_name)); \
  }; \
  int GTEST_TEST_CLASS_NAME_(test_case_name, \
                             test_name)::gtest_registering_dummy_ = \
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::AddToRegistry(); \
  void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()


#define TEST_P_TRAITS3(test_case_name, test_name, name1, value1, name2, value2, name3, value3) \
  class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) \
      : public test_case_name { \
   public: \
    GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() {} \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
    static void GTA_TRAIT_(name2, value2)() {} \
    static void GTA_TRAIT_(name3, value3)() {} \
   private: \
    static int AddToRegistry() { \
      GTA_TRAIT_(name1, value1)(); \
      GTA_TRAIT_(name2, value2)(); \
      GTA_TRAIT_(name3, value3)(); \
      ::testing::UnitTest::GetInstance()->parameterized_test_registry(). \
          GetTestCasePatternHolder<test_case_name>(\
              #test_case_name, __FILE__, __LINE__)->AddTestPattern(\
                  #test_case_name, \
                  #test_name, \
                  new ::testing::internal::TestMetaFactory< \
                      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>()); \
      return 0; \
    } \
    static int gtest_registering_dummy_; \
    GTEST_DISALLOW_COPY_AND_ASSIGN_(\
        GTEST_TEST_CLASS_NAME_(test_case_name, test_name)); \
  }; \
  int GTEST_TEST_CLASS_NAME_(test_case_name, \
                             test_name)::gtest_registering_dummy_ = \
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::AddToRegistry(); \
  void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()



#define GTEST_TEST_TRAITS1_(test_case_name, test_name, name, value, parent_class, parent_id)\
class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) : public parent_class {\
 public:\
  GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() { GTA_TRAIT_(name, value)(); }\
 private:\
  virtual void TestBody();\
  static void GTA_TRAIT_(name, value)() {} \
  static ::testing::TestInfo* const test_info_ GTEST_ATTRIBUTE_UNUSED_;\
  GTEST_DISALLOW_COPY_AND_ASSIGN_(\
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name));\
};\
\
::testing::TestInfo* const GTEST_TEST_CLASS_NAME_(test_case_name, test_name)\
  ::test_info_ =\
    ::testing::internal::MakeAndRegisterTestInfo(\
        #test_case_name, #test_name, NULL, NULL, \
        (parent_id), \
        parent_class::SetUpTestCase, \
        parent_class::TearDownTestCase, \
        new ::testing::internal::TestFactoryImpl<\
            GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>);\
void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()


#define GTEST_TEST_TRAITS2_(test_case_name, test_name, name1, value1, name2, value2, parent_class, parent_id)\
class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) : public parent_class {\
 public:\
  GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() { GTA_TRAIT_(name1, value1)(); GTA_TRAIT_(name2, value2)(); }\
 private:\
  virtual void TestBody();\
  static void GTA_TRAIT_(name1, value1)() {} \
  static void GTA_TRAIT_(name2, value2)() {} \
  static ::testing::TestInfo* const test_info_ GTEST_ATTRIBUTE_UNUSED_;\
  GTEST_DISALLOW_COPY_AND_ASSIGN_(\
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name));\
};\
\
::testing::TestInfo* const GTEST_TEST_CLASS_NAME_(test_case_name, test_name)\
  ::test_info_ =\
    ::testing::internal::MakeAndRegisterTestInfo(\
        #test_case_name, #test_name, NULL, NULL, \
        (parent_id), \
        parent_class::SetUpTestCase, \
        parent_class::TearDownTestCase, \
        new ::testing::internal::TestFactoryImpl<\
            GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>);\
void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()


#define GTEST_TEST_TRAITS3_(test_case_name, test_name, name1, value1, name2, value2, name3, value3, parent_class, parent_id)\
class GTEST_TEST_CLASS_NAME_(test_case_name, test_name) : public parent_class {\
 public:\
  GTEST_TEST_CLASS_NAME_(test_case_name, test_name)() { GTA_TRAIT_(name1, value1)(); GTA_TRAIT_(name2, value2)(); GTA_TRAIT_(name3, value3)(); }\
 private:\
  virtual void TestBody();\
  static void GTA_TRAIT_(name1, value1)() {} \
  static void GTA_TRAIT_(name2, value2)() {} \
  static void GTA_TRAIT_(name3, value3)() {} \
  static ::testing::TestInfo* const test_info_ GTEST_ATTRIBUTE_UNUSED_;\
  GTEST_DISALLOW_COPY_AND_ASSIGN_(\
      GTEST_TEST_CLASS_NAME_(test_case_name, test_name));\
};\
\
::testing::TestInfo* const GTEST_TEST_CLASS_NAME_(test_case_name, test_name)\
  ::test_info_ =\
    ::testing::internal::MakeAndRegisterTestInfo(\
        #test_case_name, #test_name, NULL, NULL, \
        (parent_id), \
        parent_class::SetUpTestCase, \
        parent_class::TearDownTestCase, \
        new ::testing::internal::TestFactoryImpl<\
            GTEST_TEST_CLASS_NAME_(test_case_name, test_name)>);\
void GTEST_TEST_CLASS_NAME_(test_case_name, test_name)::TestBody()


#define TEST_TRAITS1(test_case_name, test_name, name, value)\
  GTEST_TEST_TRAITS1_(test_case_name, test_name, name, value, \
              ::testing::Test, ::testing::internal::GetTestTypeId())


#define TEST_F_TRAITS1(test_fixture, test_name, name, value)\
  GTEST_TEST_TRAITS1_(test_fixture, test_name, name, value, test_fixture, \
              ::testing::internal::GetTypeId<test_fixture>())


#define TEST_TRAITS2(test_case_name, test_name, name1, value1, name2, value2)\
  GTEST_TEST_TRAITS2_(test_case_name, test_name, name1, value1, name2, value2, \
              ::testing::Test, ::testing::internal::GetTestTypeId())


#define TEST_F_TRAITS2(test_fixture, test_name, name1, value1, name2, value2)\
  GTEST_TEST_TRAITS2_(test_fixture, test_name, name1, value1, name2, value2, test_fixture, \
              ::testing::internal::GetTypeId<test_fixture>())


#define TEST_TRAITS3(test_case_name, test_name, name1, value1, name2, value2, name3, value3)\
  GTEST_TEST_TRAITS3_(test_case_name, test_name, name1, value1, name2, value2, name3, value3, \
              ::testing::Test, ::testing::internal::GetTestTypeId())


#define TEST_F_TRAITS3(test_fixture, test_name, name1, value1, name2, value2, name3, value3)\
  GTEST_TEST_TRAITS3_(test_fixture, test_name, name1, value1, name2, value2, name3, value3, test_fixture, \
              ::testing::internal::GetTypeId<test_fixture>())

# define TYPED_TEST_TRAITS1(CaseName, TestName, name1, value1) \
  template <typename gtest_TypeParam_> \
  class GTEST_TEST_CLASS_NAME_(CaseName, TestName) \
      : public CaseName<gtest_TypeParam_> { \
   public:\
     GTEST_TEST_CLASS_NAME_(CaseName, TestName)() { GTA_TRAIT_(name1, value1)(); }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
  }; \
  bool gtest_##CaseName##_##TestName##_registered_ GTEST_ATTRIBUTE_UNUSED_ = \
      ::testing::internal::TypeParameterizedTest< \
          CaseName, \
          ::testing::internal::TemplateSel< \
              GTEST_TEST_CLASS_NAME_(CaseName, TestName)>, \
          GTEST_TYPE_PARAMS_(CaseName)>::Register(\
              "", #CaseName, #TestName, 0); \
  template <typename gtest_TypeParam_> \
  void GTEST_TEST_CLASS_NAME_(CaseName, TestName)<gtest_TypeParam_>::TestBody()


# define TYPED_TEST_TRAITS2(CaseName, TestName, name1, value1, name2, value2) \
  template <typename gtest_TypeParam_> \
  class GTEST_TEST_CLASS_NAME_(CaseName, TestName) \
      : public CaseName<gtest_TypeParam_> { \
   public:\
     GTEST_TEST_CLASS_NAME_(CaseName, TestName)() { GTA_TRAIT_(name1, value1)(); GTA_TRAIT_(name2, value2)(); }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
    static void GTA_TRAIT_(name2, value2)() {} \
  }; \
  bool gtest_##CaseName##_##TestName##_registered_ GTEST_ATTRIBUTE_UNUSED_ = \
      ::testing::internal::TypeParameterizedTest< \
          CaseName, \
          ::testing::internal::TemplateSel< \
              GTEST_TEST_CLASS_NAME_(CaseName, TestName)>, \
          GTEST_TYPE_PARAMS_(CaseName)>::Register(\
              "", #CaseName, #TestName, 0); \
  template <typename gtest_TypeParam_> \
  void GTEST_TEST_CLASS_NAME_(CaseName, TestName)<gtest_TypeParam_>::TestBody()


# define TYPED_TEST_TRAITS3(CaseName, TestName, name1, value1, name2, value2, name3, value3) \
  template <typename gtest_TypeParam_> \
  class GTEST_TEST_CLASS_NAME_(CaseName, TestName) \
      : public CaseName<gtest_TypeParam_> { \
   public:\
     GTEST_TEST_CLASS_NAME_(CaseName, TestName)() { GTA_TRAIT_(name1, value1)(); GTA_TRAIT_(name2, value2)(); GTA_TRAIT_(name3, value3)(); }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
    static void GTA_TRAIT_(name2, value2)() {} \
    static void GTA_TRAIT_(name3, value3)() {} \
  }; \
  bool gtest_##CaseName##_##TestName##_registered_ GTEST_ATTRIBUTE_UNUSED_ = \
      ::testing::internal::TypeParameterizedTest< \
          CaseName, \
          ::testing::internal::TemplateSel< \
              GTEST_TEST_CLASS_NAME_(CaseName, TestName)>, \
          GTEST_TYPE_PARAMS_(CaseName)>::Register(\
              "", #CaseName, #TestName, 0); \
  template <typename gtest_TypeParam_> \
  void GTEST_TEST_CLASS_NAME_(CaseName, TestName)<gtest_TypeParam_>::TestBody()


# define TYPED_TEST_P_TRAITS1(CaseName, TestName, name1, value1) \
  namespace GTEST_CASE_NAMESPACE_(CaseName) { \
  template <typename gtest_TypeParam_> \
  class TestName : public CaseName<gtest_TypeParam_> { \
   public:\
     TestName() { GTA_TRAIT_(name1, value1)(); }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
  }; \
  static bool gtest_##TestName##_defined_ GTEST_ATTRIBUTE_UNUSED_ = \
      GTEST_TYPED_TEST_CASE_P_STATE_(CaseName).AddTestName(\
          __FILE__, __LINE__, #CaseName, #TestName); \
  } \
  template <typename gtest_TypeParam_> \
  void GTEST_CASE_NAMESPACE_(CaseName)::TestName<gtest_TypeParam_>::TestBody()


# define TYPED_TEST_P_TRAITS2(CaseName, TestName, name1, value1, name2, value2) \
  namespace GTEST_CASE_NAMESPACE_(CaseName) { \
  template <typename gtest_TypeParam_> \
  class TestName : public CaseName<gtest_TypeParam_> { \
   public:\
     TestName() { GTA_TRAIT_(name1, value1)(); GTA_TRAIT_(name2, value2)(); }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
    static void GTA_TRAIT_(name2, value2)() {} \
  }; \
  static bool gtest_##TestName##_defined_ GTEST_ATTRIBUTE_UNUSED_ = \
      GTEST_TYPED_TEST_CASE_P_STATE_(CaseName).AddTestName(\
          __FILE__, __LINE__, #CaseName, #TestName); \
  } \
  template <typename gtest_TypeParam_> \
  void GTEST_CASE_NAMESPACE_(CaseName)::TestName<gtest_TypeParam_>::TestBody()


# define TYPED_TEST_P_TRAITS3(CaseName, TestName, name1, value1, name2, value2, name3, value3) \
  namespace GTEST_CASE_NAMESPACE_(CaseName) { \
  template <typename gtest_TypeParam_> \
  class TestName : public CaseName<gtest_TypeParam_> { \
   public:\
     TestName() { GTA_TRAIT_(name1, value1)(); GTA_TRAIT_(name2, value2)(); GTA_TRAIT_(name3, value3)(); }\
   private: \
    typedef CaseName<gtest_TypeParam_> TestFixture; \
    typedef gtest_TypeParam_ TypeParam; \
    virtual void TestBody(); \
    static void GTA_TRAIT_(name1, value1)() {} \
    static void GTA_TRAIT_(name2, value2)() {} \
    static void GTA_TRAIT_(name3, value3)() {} \
  }; \
  static bool gtest_##TestName##_defined_ GTEST_ATTRIBUTE_UNUSED_ = \
      GTEST_TYPED_TEST_CASE_P_STATE_(CaseName).AddTestName(\
          __FILE__, __LINE__, #CaseName, #TestName); \
  } \
  template <typename gtest_TypeParam_> \
  void GTEST_CASE_NAMESPACE_(CaseName)::TestName<gtest_TypeParam_>::TestBody()
