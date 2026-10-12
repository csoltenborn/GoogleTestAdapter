[![Build status](https://ci.appveyor.com/api/projects/status/8hdgmdy1ogqi606j/branch/master?svg=true)](https://ci.appveyor.com/project/csoltenborn/googletestadapter-u1cxh/branch/master)
[![Code coverage](https://codecov.io/gh/csoltenborn/GoogleTestAdapter/branch/master/graph/badge.svg)](https://codecov.io/gh/csoltenborn/GoogleTestAdapter)
[![Visual Studio Marketplace downloads](https://img.shields.io/badge/vs_marketplace-52k-blue.svg)](https://marketplace.visualstudio.com/items?itemName=ChristianSoltenborn.GoogleTestAdapter)
[![NuGet downloads](https://img.shields.io/nuget/dt/GoogleTestAdapter.svg?colorB=0c7dbe&label=nuget)](https://www.nuget.org/packages/GoogleTestAdapter)

# About this fork

This fork of [Google Test Adapter](https://github.com/csoltenborn/GoogleTestAdapter) supports **Visual Studio 2017 to Visual Studio 2026** (Visual Studio 2013 and 2015 are no longer supported), and treats CMake projects as first-class citizens alongside Visual Studio projects. Note that the downloads linked in the [installation section](#installation) (Visual Studio Marketplace, NuGet, and the original project's GitHub releases) refer to the original version 0.18.0, which contains none of the changes below.

Changes compared to the original project:

* **Visual Studio 2022 and 2026**
  * The VSIX can be installed into VS 2022 (17.x) and VS 2026 (18.x); GTA and its sample projects build with the v143/v145 toolsets
  * GTA requires VS 2017 or later and .NET Framework 4.6; its VS package is an async package built against the VS 2017 SDK, since VS 2022 and later no longer ship the assemblies of older SDKs
  * Updated to Google Test 1.18.0 (including the Google Test NuGet packages, which now require C++17 and support the v143/v145 toolsets; the rt-static packages are now actually built against the static CRT)
  * The sample projects are built against Google Test 1.18.0 as provided by the `ThirdParty/googletest` submodule (instead of fused copies of Google Test 1.7.0 and 1.8.1); support for building against older versions has been dropped
  * The Google Test project template uses the default platform toolset of the installed Visual Studio
  * GTA warns if Microsoft's *Test Adapter for Google Test* is installed, since it [prevents GTA from being used](#tafgt_conflict)
  * Test Explorer groups tests by their C++ namespace (e.g. `outer::inner`, or `(anonymous namespace)`) when grouping by namespace, rather than putting all tests into an empty namespace; tests in the global namespace and tests without source location are still shown without namespace (upstream [#342](https://github.com/csoltenborn/GoogleTestAdapter/issues/342), [#365](https://github.com/csoltenborn/GoogleTestAdapter/issues/365))
  * Test Explorer shows the instances of typed and parameterized tests below a common node, e.g. `InstantiationName/ParameterizedTests.Simple/0` and `ParameterizedTests.Simple/1` below `ParameterizedTests` → `Simple`, and `TypedTests/0.CanIterate` and `TypedTests/1.CanIterate` below `TypedTests` → `CanIterate`; test names, filters, and test selection are not affected (upstream [#210](https://github.com/csoltenborn/GoogleTestAdapter/issues/210))
  * [Test case filters](#test_case_filters) support the properties `ClassName` (the test suite), `Name` (the test's name within its suite), and `Namespace` (the C++ namespace), as known from MSTest; the documented list of supported filter properties has been corrected, and there is a [command line recipe](#vstest_console_recipe) for running selected tests e.g. by coding agents
  * Visual Studio's code coverage (*Test → Analyze Code Coverage*) covers Google Test executables: their code is only instrumented if they are linked with `/PROFILE` or if dynamic native instrumentation is enabled, which is off by default; for coverage runs, GTA thus enables dynamic native instrumentation (VS 2022 and later, unless configured explicitly in a `.runsettings` file or in VS's code coverage options) and excludes its own assemblies from the coverage results
  * Option *Debugger engine* has new choice *Managed (.NET Core, .NET 5+) and native* for debugging into .NET Core and .NET 5+ code, since *Managed and native* only supports the .NET Framework (upstream [#317](https://github.com/csoltenborn/GoogleTestAdapter/issues/317))
* **[CMake projects](#cmake)**
  * The CTest test properties `LABELS` (assigned as `Label` traits), `DISABLED` (tests reported as skipped), `TIMEOUT` (tests killed after their timeout), `WORKING_DIRECTORY`, and `ENVIRONMENT` are honored, both within Visual Studio and with `VSTest.Console.exe`; this can be switched off with option *Use CTest test properties*
  * The [solution settings file](#solution_settings) and `$(SolutionDir)` are supported if a folder has been opened instead of a solution (Open Folder mode)
  * Test executables linking Google Test or Google Mock as DLLs are discovered, whatever the DLLs are named (e.g. `my_gtest.dll`) and also if they are delay-loaded: GTA recognizes the functions of Google Test's core the executables import (from namespace `testing::internal`, and classes `testing::UnitTest` and `testing::Test`), in addition to the DLL names `gtest`, `gtest_main`, `gmock`, and `gmock_main` (optionally with debug postfix `d`)
  * Option *Test timeout in s* (`<TestTimeoutInSeconds>`, default 0, i.e. no timeout) kills a test executable if a test runs longer than the timeout; the test fails with a timeout message, and the remaining tests are run (as for CTest's `TIMEOUT`, which takes precedence)
* **Test executables built elsewhere**
  * Option *Source path mapping* (`<SourcePathMapping>`) maps the source paths of executables built e.g. by CI or within a container to local paths, so that test locations and the stack traces of failures are navigable: `build path=>local path` pairs are separated by `//||//`, e.g. `C:\agent\_work\1\s=>$(SolutionDir)`; a path is mapped if it starts with the build path (case-insensitive, `\` and `/` are equivalent), the first matching pair wins. With option *Map only missing source paths* (`<MapOnlyMissingSourcePaths>`), only paths of files which do not exist are mapped. The mapping does not affect the debugger, which takes source paths from the .pdb (see the solution's *Debug Source Files* settings)
* **Bug fixes**
  * Test executables with paths not representable in the ANSI code page (e.g. containing Chinese characters on a Western system) are discovered (based on upstream PR [#288](https://github.com/csoltenborn/GoogleTestAdapter/pull/288))
  * Test suites with dots in their names no longer cause tests of other suites to be run (upstream [#330](https://github.com/csoltenborn/GoogleTestAdapter/pull/330))
  * Test case filters on traits with multiple values (e.g. `Label=slow`) now match
  * New [trait macros](#trait_macros) headers for Google Test 1.10.0 to 1.18.0, which `GTA_Traits_1.8.0.h` does not compile with (upstream #349); [`GTA_Traits.md`](GoogleTestAdapter/Core/Resources/GTA_Traits.md) describes how to create the header for other versions of Google Test
  * Test results are no longer lost if Google Test writes test names with non-ASCII characters (e.g. umlauts) to its XML result file in the system's ANSI code page instead of the declared UTF-8, which happens with recent versions of Google Test if the tests are not compiled with `/utf-8` (e.g., when debugging tests with debugger engine *VsTest framework*)
  * Tests compiled with `/utf-8` are now supported: GTA decodes the output of test executables as UTF-8 (falling back to the system's ANSI code page), and test filters also match test names with non-ASCII characters if these are UTF-8 encoded (new sample project `Utf8Tests`)
  * The error message of a crashed test contains the exit code of the test executable, e.g. `!! This test has probably CRASHED !! (exit code -1073741819, i.e. 0xC0000005)` (upstream #358)
  * Canceling a test run kills the test executable including all processes it has started (by means of a job object); test executables are also killed if the test host dies, and a cancel request arriving while a test executable is about to be started is no longer missed (upstream #337)
  * Tests skipped with `GTEST_SKIP()` are no longer reported as passed if their results are taken from the result XML file (e.g. when debugging tests); the skip message is reported as the result's error message (with the location of `GTEST_SKIP()` as stack trace), i.e. it is shown in Test Explorer and is available to Copilot
  * Results of crashed and timed out tests have a stack trace pointing to the test's source location, so that Copilot and other tools can find the test
  * Stack trace entries of failed assertions are labeled with the test's name (e.g. `at MySuite.MyTest in C:\src\MyTests.cpp:line 42` instead of `at MyTests.cpp:42 in …`), as for .NET tests, so that tools can relate them to the test
  * The output of failed, skipped, crashed, and timed out tests is reported as the result's standard output (as it already was for passed tests), and the error message starts with the first failure rather than with the test's output; since Google Test's console output does not allow to tell where a failure message ends, the standard output contains the complete output including the failure messages, and output printed after a failure is still part of the error message
  * Failures without source location (e.g. `unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.`) are recognized, i.e. the error message no longer contains the test's output before the failure nor the `unknown file: error:` prefix
  * A missing .pdb file of a test executable is reported as a warning (rather than only in debug mode), since its tests have neither traits nor namespaces then
  * Tests have source locations even without .pdb file, with option *Parse symbol information* switched off, or if they have been registered at runtime with `::testing::RegisterTest()`: GTA takes them from the test list Google Test (1.8.1 or later) writes with `--gtest_list_tests --gtest_output=xml` (upstream [#309](https://github.com/csoltenborn/GoogleTestAdapter/issues/309)); locations found in the .pdb are still preferred, since only these come with traits and namespaces
  * More reliable killing of test processes on cancellation
  * The Visual Studio version is also identified if tests are run by a 32-bit test host of the 64-bit VS 2022 or later (e.g. for x86 test executables): GTA no longer warns that it could not identify the Visual Studio version, and no longer slows down the reporting of test results as needed for VS 2015 and earlier
  * The Google Test NuGet packages link the debug or release libraries depending on the project's *Use Debug Libraries* setting, so they also work with configurations not named `Debug` or `Release` (upstream #356); the choice can be overridden with property `<package name with dashes>-LibraryConfiguration`
  * The Google Test NuGet packages contain the [trait macros](#trait_macros) header for their version of Google Test as `GTA_Traits.h` (upstream #316)


# Looking for a Maintainer

As you might have noticed, this project is not maintained any more (I have moved to different technologies and are not even working as a developer any more). Since it still has a few features which the [MS clone](https://github.com/microsoft/TestAdapterForGoogleTest) (which is still developed and delivered with Visual Studio) does not support, there still seems to be some interest in the project. Thus, if anybody wants to take over this project, please get in touch - I will certainly try to get you up and running!


### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework.

![Screenshot of Test Explorer](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/VsPackage.Shared/Resources/Screenshot.png "Screenshot of Test Explorer")

#### Features

* Sequential and [parallel](#parallelization) test execution
* [Traits](https://devblogs.microsoft.com/devops/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1using-traits-in-the-unit-test-explorer/) support by means of [custom C++ macros](#trait_macros) and/or [trait assignment by regexes](#trait_regexes)
* Support for [value-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#value-parameterized-tests), [typed](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#typed-tests), and [type-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#type-parameterized-tests) tests
* Google Test's runtime behavior ([handling of exceptions](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#disabling-catching-test-thrown-exceptions), [break on assertion failure](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#turning-assertion-failures-into-break-points)) can be controlled via [VS options](#global_settings)
* Most important runtime options can be controlled via [toolbar](#toolbar) without entering VS's options
* Support for all Google Test command line options, including [test shuffling](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#shuffling-the-tests) and [test repetition](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#repeating-the-tests)
* [TFS support](#vstest_console) by means of [`VSTest.Console.exe`](https://learn.microsoft.com/en-us/visualstudio/test/vstest-console-options)
* [Support](#test_case_filters) for [test case filters](https://github.com/microsoft/vstest/blob/main/docs/filter.md)
* Failed assertions and [SCOPED_TRACE](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#adding-traces-to-assertions)s are linked to their source locations
* Identification of crashed tests, and of hanging tests by means of option *Test timeout in s*
* Test output can be piped to test console
* Exit code of test executables can be [reflected as an additional test](#evaluating_exit_code)
* Execution of [parameterized batch files](#test_setup_and_teardown) for test setup/teardown
* Support for [CMake projects](#cmake), including the test properties `LABELS`, `TIMEOUT`, `DISABLED`, `WORKING_DIRECTORY`, and `ENVIRONMENT`
* Automatic recognition of gtest executables (which can be overridden by using a [custom regex](#test_discovery_regex) or an indicator file)
* Settings can be [shared via source control](#solution_settings)
* Installable as Visual Studio extension or NuGet development dependency

#### History

* See [releases](https://github.com/csoltenborn/GoogleTestAdapter/releases)

### Usage

#### Installation

[![Download from Visual Studio Marketplace](https://img.shields.io/badge/vs_marketplace-v0.18.0-blue.svg)](https://marketplace.visualstudio.com/items?itemName=ChristianSoltenborn.GoogleTestAdapter)
[![Download from NuGet](https://img.shields.io/nuget/vpre/GoogleTestAdapter.svg?colorB=0c7dbe&label=nuget)](https://www.nuget.org/packages/GoogleTestAdapter)
[![Download from GitHub](https://img.shields.io/github/release/csoltenborn/GoogleTestAdapter/all.svg?colorB=0c7dbe&label=github)](https://github.com/csoltenborn/GoogleTestAdapter/releases)

Google Test Adapter can be installed in three ways:

* Install through the Visual Studio Marketplace at *Tools/Extensions and Updates* - search for *Google Test Adapter*.
* Download and launch the VSIX installer from either the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=ChristianSoltenborn.GoogleTestAdapter) or [GitHub](https://github.com/csoltenborn/GoogleTestAdapter/releases/download/v0.18.0/GoogleTestAdapter-0.18.0.vsix)
* Add a NuGet dependency to the [Google test adapter nuget package](https://www.nuget.org/packages/GoogleTestAdapter/) to your Google Test projects. Note, however, that Visual Studio integration is limited this way: VS can discover and run tests, but no debugging, options or toolbar will be available; configuration is only possible through solution config files (see below).

After restarting VS, your tests will be displayed in the Test Explorer at build completion time. If no or not all tests show up, have a look at the [trouble shooting section](#trouble_shooting).

<a name="tafgt_conflict"></a>Note that Visual Studio's C++ workloads may install Microsoft's *Test Adapter for Google Test* (TAfGT), which is a fork of Google Test Adapter. Both adapters ship assemblies with identical names, so the test platform loads only one of them, and which one is not predictable; if TAfGT wins, your tests are discovered and run by TAfGT, and GTA's options and features (e.g. support for CMake test properties) do not apply. GTA warns at startup if TAfGT is installed. To use GTA, remove the *Test Adapter for Google Test* individual component using the Visual Studio Installer.

Note that due to Microsoft requiring VS extensions to support [asynchronous package loading](https://devblogs.microsoft.com/visualstudio/improving-the-responsiveness-of-critical-scenarios-by-updating-auto-load-behavior-for-extensions/), the last version of Google Test Adapter which supports Visual Studio 2012 is [0.14.4](https://github.com/csoltenborn/GoogleTestAdapter/releases/tag/v0.14.4).


#### <a name="gta_configuration"></a>Configuration

GTA provides different ways of configuration:
* <a name="global_settings"></a>The *Google Test Adapter* section of Visual Studio's *Tools/Options* (not available if installed via NuGet). These options are referred to as *global options* in the following.
* <a name="toolbar"></a>The GTA toolbar (not available if installed via NuGet). The most important runtime options (i.e., *Parallel test execution*, *Break on failure*, *Catch exceptions*, and *Print test output*) can also be set via a toolbar; this is equivalent to setting the according options via *Tools/Options/Google Test Adapter*.
* <a name="solution_settings"></a>Solution settings files (not available if run via [VsTest.Console.exe](https://learn.microsoft.com/en-us/visualstudio/test/vstest-console-options)). They are provided by means of an XML configuration file; this allows sharing of settings via source control. The configuration file must be placed in the same folder as the solution's `.sln` file, and must have the same name as that file, but with extension `.gta.runsettings`. E.g., if the solution file's name is `Foo.sln`, the settings file must be named `Foo.gta.runsettings`. If a folder has been opened instead of a solution (e.g., a CMake project), the settings file must be placed in that folder and be named after it (e.g., `Foo.gta.runsettings` for folder `Foo`); alternatively, the folder may contain exactly one file with extension `.gta.runsettings`. In this case, `$(SolutionDir)` refers to the opened folder.
* Visual Studio user settings files. VS allows for the selection of [test settings](https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file) files via the *Test/Test Settings* menu, and to pass such settings files to `VsTest.Console.exe` via the `/Settings` parameter.
* Environment variable `GTA_FALLBACK_SETTINGS`. The settings file referred to from that environment variable will be used *only* if GTA does not receive settings from the VS test framework, which is the case when running GTA through `VsTest.Console.exe` without passing a settings file containing GTA settings (see #184 and [running tests from the command line](#vstest_console_recipe)).

The format of solution and user settings files is the same: a `<GoogleTestAdapterSettings>` node contains the solution settings and the (possibly empty) set of project settings and is itself contained in a `<RunSettings>` node (which in the case of user settings files might contain additional, e.g. VS specific settings). In contrast to solution settings, each set of project settings additionally has a regular expression to be evaluated at test discovery and execution time.

The final settings to be used are computed by merging the available settings, vaguely following Visual Studio's approach of configuration inheritance. Merging is done in two stages:
1. The available global, solution file, and user file settings are merged into solution settings and a set of project settings. This is done in increasing priority, i.e., solution file settings override global settings, and user file settings override solution settings. Project settings of solution and user settings files are merged if they share the exact same regular expression.
2. At test discovery and execution time, each test executable's full path is matched against the project settings' regular expressions; the first matching project settings are used for the particular test executable. If no project settings are found, the solution settings are used.

Overall, given a test executable `mytests.exe`, the following settings apply to that executable in decreasing priority:
1. Project settings of a user settings file, the regular expression of which matches the full path of `mytests.exe`.
2. Project settings of a solution settings file, the regular expression of which matches the full path of `mytests.exe`.
3. Solution settings of a user settings file.
4. Solution settings of a solution settings file.
5. Global settings.

<a name="skip_origin_check"></a>Option *Skip check of file origin* (*Tools/Options/Google Test Adapter/General/Security*) is an exception: for security reasons, it can only be set via the global options and is ignored if provided by solution or user settings files (in particular, it can not be set if GTA has been installed via NuGet or is run via `VSTest.Console.exe`). If it is not set, GTA does not run test executables which Windows considers to originate from another computer (e.g. downloaded files which have not been unblocked, or files located on a network drive), but reports an error instead.

Note that due to the overriding hierarchy described above, you probably want to provide only a subset of the nodes in your configuration files. For instance, providing the node `<DebugMode>true</DebugMode>` in a shared solution settings file will make sure that all sharing developers will run GTA with debug output, no matter what the developer's individual settings at *Tools/Options/Google Test Adapter* are (and unless the developer has selected a test settings file via VS, which would override the solution setting).

For reference, see a settings file [AllTestSettings.gta.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Resources/AllTestSettings.gta.runsettings) containing all available settings, a more realistic solution settings file [SampleTests.gta.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/SampleTests/SampleTests.gta.runsettings) as delivered with the SampleTests solution, and a user settings file [NonDeterministic.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/SampleTests/NonDeterministic.runsettings) as used by GTA's end-to-end tests. The syntax of the GTA settings files (excluding the `<RunSettings>` node) is specified by [this schema](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/TestAdapter/GoogleTestAdapterSettings.xsd).

##### <a name="gtest_options"></a>Passing Google Test options to the test executables

Google Test options not provided by GTA (e.g., `--gtest_output` for generating test reports) can be passed to the test executables by means of option *Additional test execution parameters*. For instance, the following solution settings file makes each test executable write its test results to an XML file within the executable's folder:

```XML
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <GoogleTestAdapterSettings>
    <SolutionSettings>
      <Settings>
        <AdditionalTestExecutionParam>--gtest_output=xml:$(ExecutableDir)\$(Executable)_$(ThreadId).xml</AdditionalTestExecutionParam>
      </Settings>
    </SolutionSettings>
  </GoogleTestAdapterSettings>
</RunSettings>
```

Note that
* the `<GoogleTestAdapterSettings>` node must not have an `xmlns` attribute (e.g. as added by an editor making use of GTA's schema); otherwise, the settings are silently ignored,
* the additional parameters are appended to the parameters GTA passes to the executable; since GTA itself makes use of `--gtest_output` for obtaining test results, overriding it might result in GTA reporting fewer test results in rare cases (e.g., if a test executable crashes, or if tests are debugged with debugger engine *VsTest framework*),
* an executable might be run more than once within a single test run (e.g., in case of [parallel test execution](#parallelization), or if the tests to be run do not fit onto a single command line), and each run overwrites the file of the previous one; the `$(ThreadId)` placeholder at least prevents clashes between runs in parallel, and
* the file is also overwritten by subsequent test runs, e.g. when running a single test from the Test Explorer.

If test results are needed for reporting purposes (e.g. on a build server), consider making `VSTest.Console.exe` write them, e.g. by means of option `/Logger:trx`.

##### <a name="settings_helper_files"></a>Settings helper files
GTA does not provide direct access to VS project settings such as *Project* > *Properties* > *Debugging* > *Environment*. Additionally, when run as NuGet dependency, GTA does not have access to information such as solution dir or Platform/Configuration a test executable has been built with.

To overcome these problems, GTA supports so-called *settings helper files* which provide that information to GTA. Helper files are usually generated as part of the build, e.g. within a post-build event, which might look like this:

```
echo SolutionPath=$(SolutionPath)::GTA::SolutionDir=$(SolutionDir)::GTA::PlatformName=$(PlatformName)::GTA::ConfigurationName=$(ConfigurationName)::GTA::TheTarget=$(TargetFileName) > $(TargetPath).gta_settings_helper
```

This command generates a file `$(TargetPath).gta_settings_helper` (where `$(TargetPath)` is the path of the final test executable) containing key/value pairs separated by `::GTA::`. At file generation time, the VS macros will be replaced by the actual values, which can then in turn be used as placeholders within the GTA settings. In particular, the helper file generated by the above command will
* make sure that the `$(SolutionDir)`, `$(PlatformName)`, and `$(ConfigurationName)` placeholders will be available even if the runtime environment does not provide them (CI) and
* will provide the value of the VS `$(TargetFileName)` macro to GTA, where it can be referred to as the `$(TheTarget)` placeholder

Note that the settings helper files need to be located within the same folder as their corresponding test executable, and must have the test executable's name concatenated with the `.gta_settings_helper` ending (make sure to ignore these files in version control if necessary).


#### Assigning traits to tests

GTA has full support for [traits](https://devblogs.microsoft.com/devops/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1using-traits-in-the-unit-test-explorer/), which can be assigned to tests in two ways:

* <a name="trait_macros"></a>You can make use of the custom test macros provided in the header matching your version of Google Test, which contains macros for all test types of the Google Test framework: [GTA_Traits_1.7.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.7.0.h) (Google Test 1.7.0), [GTA_Traits_1.8.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.8.0.h) (1.8.x), [GTA_Traits_1.10.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.10.0.h) (1.10.0), [GTA_Traits_1.11.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.11.0.h) (1.11.0), [GTA_Traits_1.12.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.12.0.h) (1.12.x), [GTA_Traits_1.13.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.13.0.h) (1.13.0 and 1.14.0), [GTA_Traits_1.15.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.15.0.h) (1.15.x and 1.16.0), or [GTA_Traits_1.18.0.h](https://raw.githubusercontent.com/snargit/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.18.0.h) (1.17.0 and 1.18.0). If your project uses one of the fork's Google Test NuGet packages (`Microsoft.googletest.*`), the header matching the package's Google Test version is part of the package and can be included as `#include "GTA_Traits.h"`. For other versions, [GTA_Traits.md](GoogleTestAdapter/Core/Resources/GTA_Traits.md) describes how to create an according header. The macros do not change behavior of the tests; they only add some information to the generated test code which encodes the traits assigned to the respective test. All GTA provided macros follow the same naming schema `<Google Test macro>_TRAITS`, where, obviously, `<Google Test macro>` is the name of the according macro in Google Test. Each test can be assigned up to 8 traits.
* <a name="trait_regexes"></a>Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option. For instance, the expression `.*///Size,Medium` assigns the trait *(Size,Medium)* to all tests.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase. For instance, the test declaration `TEST_P_TRAITS(ParameterizedTests, SimpleTraits, Size, Small)` will make sure that all test instances of test ParameterizedTest.SimpleTraits will be assigned the trait *(Size,Small)* (and override the Size trait assigned from the first phase).
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above. For instance, the expression `.*\[1.*\]///Size,Large` will make sure that all parameterized tests where the parameter starts with a 1 will be assigned the trait *(Size,Large)* (and override the traits assigned by phases 1 and 2).

Note that traits are assigned in an additive manner within each phase, and in an overriding manner between phases. For instance, if a test is assigned the traits *(Author,Foo)* and *(Author,Bar)* in phase 1, the test will have both traits. If the test is also assigned the trait *(Author,Baz)* in phases 2 or 3, it will only have that trait. See [test code](https://github.com/csoltenborn/GoogleTestAdapter/blob/fcc83220ceec9979710c2340f2378b0e8b430a60/GoogleTestAdapter/Core.Tests/GoogleTestDiscovererTraitTestsBase.cs) for examples.

#### <a name="cmake"></a>CMake projects

GTA discovers and runs the tests of executables built by CMake just like those of Visual Studio projects, e.g. when a CMake project has been opened as a folder in Visual Studio, or when running the tests via [`VSTest.Console.exe`](#vstest_console). If a `CMakeCache.txt` file is found within the folder of a test executable or one of its parent folders, GTA asks CTest (i.e., `ctest --show-only=json-v1`, using the `ctest` executable of the CMake that has generated the build tree, and the build configuration found in the executable's path for multi-config generators) for the test properties of the executable's tests, as set by `add_test()`/`set_tests_properties()` or by the `PROPERTIES` argument of `gtest_discover_tests()`/`gtest_add_tests()`. CTest tests are matched with the executable's Google Test tests by the executable and the `--gtest_filter` of the CTest test (which makes `TEST_PREFIX` and `TEST_SUFFIX` irrelevant), or by the CTest test's name. The following test properties are supported:

* `LABELS`: Each label is assigned to the test as a trait *(Label,&lt;label&gt;)*, allowing to group tests by label in the Test Explorer and to select tests by label via test case filters (e.g. `/TestCaseFilter:"Label=fast"`).
* `DISABLED`: Disabled tests are not run, but reported as skipped (as CTest does).
* `TIMEOUT`: If a test runs longer than its timeout, the test executable is killed and the test fails with a timeout message; the remaining tests of the executable are run afterwards. Timeouts are not applied while debugging tests. A test's `TIMEOUT` takes precedence over option *Test timeout in s*, which applies the same to all tests.
* `WORKING_DIRECTORY` and `ENVIRONMENT`: The tests are run with the working directory and environment variables CTest would use (by default, CTest uses the build folder as working directory). A working directory configured for GTA wins over CMake's if it differs from the default; environment variables configured for GTA override CMake's.

When running tests of a CMake project opened as a folder, Visual Studio additionally passes the `WORKING_DIRECTORY` and `ENVIRONMENT` test properties to the test adapter; these are used if CTest's test properties are not available.

Asking CTest for test properties can be switched off by means of option *Use CTest test properties* (`<UseCTestTestProperties>false</UseCTestTestProperties>` in a settings file), e.g. for selected executables via project settings; in this case, only the test properties provided by Visual Studio are used.

#### <a name="evaluating_exit_code"></a>Evaluating the test executable's exit code
If option *Exit code test case* is non-empty, an additional test case will be generated per text executable (referred to as *exit code test* in the following), and that exit code test will pass if the test executable's exit code is 0. This allows to reflect some additional result as a test case; for instance, the test executable might be built such that it performs memory leak detection at shutdown (see below for [example](#evaluating_exit_code_leak_example)); the result of that check can then be seen within VS as the result of the according additional test.

<a name="evaluating_exit_code_tokens"></a>A couple of tokens can used as part of a test executable's output; if GTA sees theses tokens, it will act accordingly:
* `GTA_EXIT_CODE_OUTPUT_BEGIN`: This token will make GTA capture the following output and add it to the exit code test as error message.
* `GTA_EXIT_CODE_OUTPUT_END`: This token will stop GTA from adding the following output to the error message. If it is not provided, GTA will capture the complete remaining output as error message of the exit code test.
* `GTA_EXIT_CODE_SKIP`: This token will make the exit code test have outcome *Skipped*. This can e.g. be useful if a particular check is only perfomed in Debug mode, or to provide a general warning that something has gone wrong without making the exit code test fail.

Note that a test executable might be run more than once by GTA (e.g., if tests are run in parallel, or if the selection of tests to be run results in command lines too long for a single test run). In this case, the exit codes and respective outputs of a test exectutable are aggregated as follows:
* The exit code test will be reported as 
  * skipped if all runs of that executable have been reported as skipped,
  * failed if at least one test run has been reported as failed, and
  * passed otherwise.
* The exit code reported will be the one with the greatest absolute value; e.g., if the exit codes have been -2, 0, and 1, the reported exit code will be -2.
* Captured outputs will all go into the single exit code test's error message.

##### <a name="evaluating_exit_code_leak_example"></a>Example usage: Memory leak detection

An example usage of the *Exit code test case* can be found as part of the SampleTests solution: [Project *MemoryLeakTests*](https://github.com/csoltenborn/GoogleTestAdapter/tree/master/SampleTests/LeakCheckTests) makes use of MS' memory leak detection facilities and reports the results to VS via an exit code test. The approach can easily be re-used for other Google Test projects:
* add files [`gta_leak_detection.h`](https://github.com/csoltenborn/GoogleTestAdapter/tree/master/SampleTests/LeakCheckTests/gta_leak_detection.h) and [`gta_leak_detection.cpp`](https://github.com/csoltenborn/GoogleTestAdapter/tree/master/SampleTests/LeakCheckTests/gta_leak_detection.cpp) to the project
* in the project's `main` method, return the result of `gta_leak_detection::PerformLeakDetection(argc, argv, RUN_ALL_TESTS())` (instead of the result of `RUN_ALL_TESTS()`, see [example](https://github.com/csoltenborn/GoogleTestAdapter/tree/master/SampleTests/LeakCheckTests/main.cpp))
* set the following GTA options (probably through the settings file):
  * `<ExitCodeTestCase>MemoryLeakTest</ExitCodeTestCase>` (enables evaluation of the executable's exit code; feel free to choose another name)
  * `<AdditionalTestExecutionParam>-is_run_by_gta</AdditionalTestExecutionParam>` (makes sure the test executable is aware of being run by GTA)

However, note that older versions of Google Test (at least up to V1.8.1) [use some memory allocation](https://github.com/google/googletest/pull/1142) which is recognized by MS' leak detection mechanism as a leak (although it isn't), in particular for failing assertions; with Google Test 1.18.0, failing assertions no longer cause such "false positives". To be on the safe side, the memory leak detection provided by GTA will result in a skipped exit code test in case leaks are detected and `RUN_ALL_TESTS()` does not return `0`, but will report the leak in the test's error message. If you run into such problems, please report them against the [Google Test repository](https://github.com/google/googletest) if appropriate.

##### Known issues

* Visual Studio 2013: if a test project *FooTests* makes use of a `main()` method of a *BarTests* project (e.g. by referencing its `main.cpp`), the exit code test of *FooTests* will be grouped under *BarTests*' executable.


#### <a name="vstest_console"></a>Running tests from command line with `VSTest.Console.exe`

GTA can be used to run tests from the command line, which can be done making use of VS's [VSTest.Console.exe](https://learn.microsoft.com/en-us/visualstudio/test/vstest-console-options). GTA supports all the tool's command line options, including `/UseVsixExtensions` and `/TestAdapterPath`.

Note, however, that VSTest.Console.exe will not make use of GTA solution settings (if the solution containing the tests happens to use such settings). All settings to be used by VSTest.Console.exe need to be passed using the `/Settings` command line option (or environment variable `GTA_FALLBACK_SETTINGS`, see [below](#vstest_console_recipe)). Note also that the `$(SolutionDir)` placeholder is neither available in the *Test setup/teardown batch file* options nor in the *Additional test execution parameters* option.

<a name="test_case_filters"></a>The tests to be run can be selected via the `/TestCaseFilter` option. Filters need to follow the syntax as described in the [VSTest documentation](https://github.com/microsoft/vstest/blob/main/docs/filter.md). GTA supports the following test properties:

* FullyQualifiedName (i.e., `<suite>.<test>`, e.g. `MyParamSuite/MyFixture.Test/0` for a parameterized test)
* DisplayName (the same as FullyQualifiedName, plus the (type) parameter for parameterized and typed tests)
* ClassName (i.e., the test suite, e.g. `MyParamSuite/MyFixture`)
* Name (i.e., the test's name within its suite, e.g. `Test/0`)
* Namespace (i.e., the C++ namespace containing the test, e.g. `outer::inner` or `(anonymous namespace)`; empty for tests in the global namespace and tests without source location)
* Source (i.e., binary containing the test)
* CodeFilePath (i.e., source file containing the test)
* LineNumber
* Id
* ExecutorUri

Additionally, traits can be used in test case filters. E.g., all tests having a `Duration` of `short` can be executed by means of the filter `/TestCaseFilter:"Duration=short"`. If a trait has the same name as `ClassName`, `Name`, or `Namespace`, the trait is used.

<a name="vstest_console_recipe"></a>For example, to run tests of a single test executable (e.g. from a script or by a coding agent):

```
vstest.console.exe path\to\MyTests.exe /TestAdapterPath:<GTA directory> /TestCaseFilter:"ClassName=MySuite" /Logger:trx
```

* `vstest.console.exe` can be found in `<VS installation directory>\Common7\IDE\Extensions\TestPlatform` (or use a *Developer Command Prompt*). If GTA is installed as a VSIX, `/UseVsixExtensions:true` can be used instead of `/TestAdapterPath`; otherwise, `<GTA directory>` is the folder containing `GoogleTestAdapter.TestAdapter.dll`.
* GTA settings (e.g. the solution's settings file) are not picked up automatically; pass them with `/Settings:<settings file>`, or set environment variable `GTA_FALLBACK_SETTINGS` to the settings file's path, which is used whenever no GTA settings are passed with `/Settings` (convenient for agents, which then do not need to know about the file).
* By default, GTA recognizes Google Test executables by scanning them; if a *Regex for test discovery* is configured, only executables matching it are considered.
* Without a .pdb file of the test executable, the `Namespace` filter property and traits assigned with the [trait macros](#trait_macros) are not available, and tests only have source locations (i.e., `CodeFilePath` and `LineNumber`) if Google Test 1.8.1 or later is used.
* `/ListTests` lists the tests of the executable without running them.
* Use `/TestCaseFilter:"FullyQualifiedName=MySuite.MyTest"` to run a single test, `ClassName=MySuite` to run a test suite, `Namespace=my::ns` to run all tests of a namespace, and `~` instead of `=` for substring matches (e.g. `FullyQualifiedName~MyTest`). Conditions can be negated with `!=` or `!~` and combined with `&` and `|` (see the [VSTest documentation](https://github.com/microsoft/vstest/blob/main/docs/filter.md)).
* The console output contains the error message and a stack trace of the form `at <suite>.<test> in <full path>:line <line>` (one entry per failed assertion) for each failed test; `/Logger:trx` additionally writes these to a `.trx` file in the `TestResults` folder (use `/ResultsDirectory` to change the folder), and `/Logger:"console;verbosity=detailed"` also prints the output of the tests.

#### <a name="parallelization"></a>Parallelization

Tests are run sequentially by default. If parallel test execution is enabled, the tests will be distributed to the available cores of your machine. To support parallel test execution, additional command line parameters can be passed to the Google Test executables (note that this feature is not restricted to parallel test execution); they can then be parsed by the test code at run time and e.g. be used to improve test isolation.

GTA remembers the durations of the executed tests to improve test scheduling for later test runs. The durations are stored in files with endings `.gta.testdurations` - make sure your version control system ignores these files.

Note that since VS 2015 update 1, VS allows for the parallel execution of tests (again); since update 2, Test Explorer has an own *Run tests in parallel* button, and VsTest.Console.exe suppports a new command line option */Parallel*. Neither button nor command line option has any effect on test execution with GTA.

#### <a name="test_setup_and_teardown"></a>Test setup and teardown

If you need to perform some setup or teardown tasks in addition to the setup/teardown methods of your test code, you can do so by configuring test setup/teardown batch files, to which you can pass several values such as solution directory or test directory for exclusive usage of the tests.


### <a name="gta_feature_support"></a>Feature availability

GTA runs in three different environments:
* Within Visual Studio and installed via VSIX (i.e., through *Extensions and updates* or by downloading and executing the VSIX file)
* Within Visual Studio and installed via NuGet (i.e., pulled via a project's NuGet dependencies)
* Within `VsTestConsole.exe` (making use of the `/UseVsixExtensions:true` or the `/TestAdapterPath:<dir>` options)

For technical reasons, not all features are available in all environments by default; refer to the table below for details.

| Feature | VS/VSIX | VS/NuGet | VsTest.Console
|--- |:---:|:---:|:---:
| Test discovery | yes | yes | yes
| Test execution | yes | yes | yes
| Test debugging | yes | no | -
| Configuration via | | |
| - VS Options | yes | no | -
| - VS Toolbar | yes | no | -
| - Solution test config file | yes | no | no
| - User test config file | yes<sup>[1](#vs_settings)</sup> | yes<sup>[1](#vs_settings)</sup> | yes<sup>[2](#test_settings)</sup>
| Placeholders | | |
| - `$(SolutionDir)` | yes | yes<sup>[3](#helper_files), [4](#also_test_execution)</sup> | yes<sup>[3](#helper_files)</sup>
| - `$(PlatformName)` | yes | yes<sup>[3](#helper_files)</sup> | yes<sup>[3](#helper_files)</sup>
| - `$(ConfigurationName)` | yes | yes<sup>[3](#helper_files)</sup> | yes<sup>[3](#helper_files)</sup>
| - `$(ExecutableDir)` | yes | yes | yes
| - `$(Executable)` | yes | yes | yes
| - `$(TestDir)`<sup>[5](#only_test_execution)</sup> | yes | yes | yes
| - `$(ThreadId)`<sup>[5](#only_test_execution)</sup> | yes | yes | yes
| - Additional placeholders | yes<sup>[3](#helper_files)</sup> | yes<sup>[3](#helper_files)</sup> | yes<sup>[3](#helper_files)</sup>
| - Environment variables | yes | yes | yes

<a name="vs_settings">1</a>: Via *Test/Test Settings/Select Test Settings File*<br>
<a name="test_settings">2</a>: Via `/Settings` option<br>
<a name="helper_files">3</a>: If [*settings helper files*](#settings_helper_files) are provided<br>
<a name="also_test_execution">4</a>: During test execution, placeholders are available even without settings helper files<br>
<a name="only_test_execution">5</a>: Only during test execution; placeholders are removed in discovery mode


### External resources

* [Basic tutorial](https://computingonplains.wordpress.com/google-test-and-visual-studio/) for using Google Test with GTA in Visual Studio


### <a name="trouble_shooting"></a>Trouble shooting

##### General advice
* Make sure that your test executable can be run on the console from within the folder it is located at. Try both with and without gtest option `--gtest_list_tests`.
* Enable options *Print debug info* and *Print test output* at *Tools/Options/Google Test Adapter/General* to understand what's going wrong.
* In case of performance problems, additionally enable option *Timestamp output*.

##### None or not all of my tests show up
* <a name="test_discovery_regex"></a>The *Debug mode* will show on the test console whether your test executables are recognized by GTA. If they are not, you have two options:
  * If your test executable is `..\FooTests.exe`, make sure that a file `..\FooTests.exe.is_google_test` exists.
  * Configure a *Test discovery regex*. In case of GTA installation via NuGet, do not forget to add the regex to the solution config file (which might be a good idea anyways). 
* Your test executable can not run with command line option `--gtest_list_tests`. Make sure that your tests can be listed via command line; if they do not, debug your test executable, e.g. by making the according test project the startup project of your solution, and placing a breakpoint at the main method of your test executable. Here are some reasons why your tests might not be listed:
  * The test executable crashes.
  * Encoding issues prevent Google Test from properly reading the command line arguments (see e.g. [issue #149](https://github.com/csoltenborn/GoogleTestAdapter/issues/149)).
* If your project configuration contains references to DLLs which do not end up in the build directory (e.g. through *Project/Properties/Linker/Input/Additional Dependencies*), these DLLs will not be found when running your tests. Use option *PATH extension* to add the directories containing these DLLs to the test executables' PATH variable.
* By default, test discovery for an executable is canceled after 30s. If in need, change this with option *Test discovery timeout in s*. Also consider moving expensive initialization code to the appropriate gtest setup facilities (e.g. [global](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#global-set-up-and-tear-down), [per test suite](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#sharing-resources-between-tests-in-the-same-test-suite), or [per test](https://github.com/google/googletest/blob/master/googletest/docs/primer.md#test-fixtures-using-the-same-data-configuration-for-multiple-tests).).
* For security reasons, Visual Studio's test discovery process runs with rather limited permissions. This might cause problems if your code needs those permissions already during test discovery (e.g. if your code [tries to open a file in write mode](https://github.com/csoltenborn/GoogleTestAdapter/issues/168#issuecomment-331725168)). Make sure that such code is executed at the appropriate gtest setup facilities (see above).
* If your project happens to be a makefile project, there's a pitfall which will prevent GTA from discovering your tests: It appears that when importing a makefile project into VS, the *Output* setting of the project is populated from the makefile's content. However, if the makefile is edited later on such that the location of the generated test executable changes, VS does not find the test executable any more. One symptom of this is that your project can not be launched any more with `F5`. Make sure that the *Output* setting of the project is consistent with its makefile to avoid this problem. 

In general, you can identify issues with your test executables by debugging them within VS: make the according test project the startup project of your solution, add command line arguments as desired, place breakpoints at points of interest and launch your test executable with *F5*.

##### No source locations and traits are found for my tests
* With Google Test 1.8.1 or later, tests have source locations even without a pdb (since Google Test provides them), but traits and namespaces still require the pdb.
* The test adapter is not able to find the pdb of your test executable, e.g. because it has been deleted or moved (and indicates that with a warning in the test output window). Rebuilding your solution should regenerate the pdb at an appropriate location.
* The test executable's project settings result in a pdb file not containing the information necessary to resolve source locations and traits (see [#46](https://github.com/csoltenborn/GoogleTestAdapter/issues/46)). Change the setting as indicated below and rebuild your solution.
  * VS 2015 and earlier: `Yes` or `Optimize for debugging (/DEBUG)`
  * VS 2017: `Generate debug information optimized for sharing and publishing (/DEBUG:FULL)`
* Option *Parse symbol information* is set to `false`, making GTA not parse that information out of the pdb file intentionally. The actual set of options used is potentially composed from VS options, a solution settings file, and a user settings file; the resulting set of options will be logged to the test output window if the *Print debug info* option is set to `true`.

##### Test discovery is slow/does not seem to finish
* Switch off *Parse symbol information*. You won't have traits from macros and namespaces (and no source locations if you use Google Test 1.8.0 or earlier), but will still get clickable stack traces in case a test fails. This will avoid scanning the executables' `.pdb` files.
* Configure a regex matching your test executable, or create an `.is_google_test` file (see [above](#test_discovery_regex)). This will avoid scanning the binary for gtest indications.
* Make sure *Print debug info* and *Print test output* are `false`.

You might consider using GTA's project settings to switch off symbol parsing and binary scanning for problematic test executables only, thus compromising between speed of test discovery and build maintainability.

##### My tests are run, but GTA's options do not seem to have any effect
* Microsoft's *Test Adapter for Google Test* is probably installed and runs your tests instead of GTA (see [installation](#tafgt_conflict)). Remove it using the Visual Studio Installer.

##### Debugging fails with "Debugging is only possible if GoogleTestAdapter has been installed into Visual Studio"
* If the test output reads *Test Adapter for Google Test: Test execution starting...* (rather than *Google Test Adapter: ...*), your tests are run by Microsoft's *Test Adapter for Google Test*, not by GTA (see [installation](#tafgt_conflict)); this error is then reported by that adapter and can not be fixed by GTA. Remove the *Test Adapter for Google Test* component using the Visual Studio Installer.
* Otherwise, GTA's Visual Studio package has not been loaded (e.g. because GTA has been installed via NuGet only). Install GTA into Visual Studio, or set option *Debugger engine* to *VsTest framework* (`<DebuggerKind>VsTestFramework</DebuggerKind>`), which does not need GTA's package.

##### The Test Explorer window can not be opened after installing GTA
* Your MEF cache might have been corrupted. Please refer to [issue #172](https://github.com/csoltenborn/GoogleTestAdapter/issues/172) for help.

##### The Google Test Adapter extension is disabled when I start Visual Studio
* Your MEF cache might have been corrupted. Please refer to [issue #98](https://github.com/csoltenborn/GoogleTestAdapter/issues/98) for help.


### Development of Google Test Adapter

See [DEVELOPMENT.md](DEVELOPMENT.md) for building, testing, releasing, and the design of this fork, and the original project's [wiki](https://github.com/csoltenborn/GoogleTestAdapter/wiki/Development-Knowledge-Base) for the code base before the fork.


### Credits

#### People

Google Test Adapter is written and maintained by [Christian Soltenborn](https://github.com/csoltenborn). 

The first version of GTA was a slightly enhanced C# port of the F# [Google Test Runner](https://github.com/markusl/GoogleTestRunner), written by [Markus Lindqvist](https://github.com/markusl). We have also learned a lot from the JavaScript test runner [Chutzpah](https://github.com/mmanela/chutzpah), written by [Matthew Manela](https://github.com/mmanela).

GTA has benefited from all kinds of contributions, be it feature requests, bug reports, code snippets, testing of new features and bugfixes, or even pull requests: 
* [Jonas Gefele](https://github.com/jgefele) has been a regular and significant contributor up to version 0.10.1 (with more to come in the future, as I hope :-))
* Most (hopefully all) other contributors are mentioned in the according [release notes](https://github.com/csoltenborn/GoogleTestAdapter/releases).

#### Organizations
* My employer [Connext Communication GmbH](https://connext.de/) for supporting testing of GTA by providing virtual machines with different Visual Studio installations.

#### Tools
* [ReSharper](https://www.jetbrains.com/resharper/) - awesome VS extension for .NET development, including refactoring, static analysis etc.
  * thanks to [JetBrains](https://www.jetbrains.com/) for providing free licenses for our developers!
  * note that JetBrains' [Resharper C++](https://www.jetbrains.com/resharper-cpp/) can also run tests written using Google Test
* [AppVeyor](http://www.appveyor.com/) - awesome .NET CI build services
  * thanks for providing free services and fantastic support for open source projects!
* [Codecov](https://codecov.io/) - code coverage visualization facilities
  * thanks for providing free services for open source projects!
* [OzCode](https://oz-code.com/) - VS extension for enhanced debugging experience
  * thanks to [CodeValue](http://www.codevalue.net/) for providing free licenses for our developers!
* [CommonMark.NET](https://github.com/Knagis/CommonMark.NET) - open source Markdown to HTML converter
* [OpenCover](https://github.com/OpenCover/opencover) - open source .NET code coverage
