[![Build status](https://ci.appveyor.com/api/projects/status/8hdgmdy1ogqi606j/branch/master?svg=true)](https://ci.appveyor.com/project/csoltenborn/googletestadapter-u1cxh/branch/master)
[![Code coverage](https://codecov.io/gh/csoltenborn/GoogleTestAdapter/branch/master/graph/badge.svg)](https://codecov.io/gh/csoltenborn/GoogleTestAdapter)
[![Visual Studio Marketplace downloads](https://img.shields.io/badge/vs_marketplace-52k-blue.svg)](https://marketplace.visualstudio.com/items?itemName=ChristianSoltenborn.GoogleTestAdapter)
[![NuGet downloads](https://img.shields.io/nuget/dt/GoogleTestAdapter.svg?colorB=0c7dbe&label=nuget)](https://www.nuget.org/packages/GoogleTestAdapter)


### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework.

[![Donate to Google Test Adapter](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif "Donate to Google Test Adapter")](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=WWTKRV62UJW6C)

![Screenshot of Test Explorer](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/VsPackage.Shared/Resources/Screenshot.png "Screenshot of Test Explorer")

#### Features

* Sequential and [parallel](#parallelization) test execution
* [Traits](http://blogs.msdn.com/b/visualstudioalm/archive/2012/11/09/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1-using-traits-in-the-unit-test-explorer.aspx) support by means of [custom C++ macros](#trait_macros) and/or [trait assignment by regexes](#trait_regexes)
* Support for [value-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#value-parameterized-tests), [typed](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#typed-tests), and [type-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#type-parameterized-tests) tests
* Google Test's runtime behavior ([handling of exceptions](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#disabling-catching-test-thrown-exceptions), [break on assertion failure](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#turning-assertion-failures-into-break-points)) can be controlled via [VS options](#global_settings)
* Most important runtime options can be controlled via [toolbar](#toolbar) without entering VS's options
* Support for all Google Test command line options, including [test shuffling](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#shuffling-the-tests) and [test repetition](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#repeating-the-tests)
* [TFS support](#vstest_console) by means of [`VSTest.Console.exe`](https://msdn.microsoft.com/en-us/library/jj155800.aspx)
* [Support](#test_case_filters) for [test case filters](http://blogs.msdn.com/b/vikramagrawal/archive/2012/07/23/running-selective-unit-tests-in-vs-2012-rc-using-testcasefilter.aspx)
* Failed assertions and [SCOPED_TRACE](https://github.com/google/googletest/blob/master/googletest/docs/advanced.md#adding-traces-to-assertions)s are linked to their source locations
* Identification of crashed tests
* Test output can be piped to test console
* Exit code of test executables can be [reflected as an additional test](#evaluating_exit_code)
* Execution of [parameterized batch files](#test_setup_and_teardown) for test setup/teardown
* Automatic recognition of gtest executables (which can be overridden by using a [custom regex](#test_discovery_regex) or an indicator file)
* Settings can be [shared via source control](#solution_settings)
* Installable as Visual Studio extension or NuGet development dependency

#### History

* See [releases](https://github.com/csoltenborn/GoogleTestAdapter/releases)

#### Donations

In the last couple of months, I noticed that my private laptop certainly has a finite lifetime. Thinking about the requirements a new one has to stand up to, I realized that developing and supporting *Google Test Adapter* has in the last years been one of the major use cases of that laptop. Thus, I decided to take this as reason for from now on accepting donations :-)

Update: In the meantime, I have received a few donations, and some rather generous ones (thanks again to everybody - the new laptop is great :-) ), but since I'm still quite far away from my original donation goal of collecting half of the laptop's price, I will leave this section as is.

Therefore, if you would like to appreciate development and support of *Google Test Adapter*, **please consider to donate!** 

[![Donate to Google Test Adapter](https://www.paypalobjects.com/en_US/DE/i/btn/btn_donateCC_LG.gif "Donate to Google Test Adapter")](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=YWJX68LZWGN5S)

Please note that I will see your donations as appreciation of my work so far and a motivational factor for the future, but just to be sure, they do not result in any obligations on my part (e.g. prioritized support, your favorite feature, or even continued maintenance).


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

Note that due to Microsoft requiring VS extensions to support [asynchronous package loading](https://blogs.msdn.microsoft.com/visualstudio/2018/05/16/improving-the-responsiveness-of-critical-scenarios-by-updating-auto-load-behavior-for-extensions/), the last version of Google Test Adapter which supports Visual Studio 2012 is [0.14.4](https://github.com/csoltenborn/GoogleTestAdapter/releases/tag/v0.14.4).


#### <a name="gta_configuration"></a>Configuration

GTA provides different ways of configuration:
* <a name="global_settings"></a>The *Google Test Adapter* section of Visual Studio's *Tools/Options* (not available if installed via NuGet). These options are referred to as *global options* in the following.
* <a name="toolbar"></a>The GTA toolbar (not available if installed via NuGet). The most important runtime options (i.e., *Parallel test execution*, *Break on failure*, *Catch exceptions*, and *Print test output*) can also be set via a toolbar; this is equivalent to setting the according options via *Tools/Options/Google Test Adapter*.
* <a name="solution_settings"></a>Solution settings files (not available if run via [VsTest.Console.exe](https://msdn.microsoft.com/en-us/library/jj155800.aspx)). They are provided by means of an XML configuration file; this allows sharing of settings via source control. The configuration file must be placed in the same folder as the solution's `.sln` file, and must have the same name as that file, but with extension `.gta.runsettings`. E.g., if the solution file's name is `Foo.sln`, the settings file must be named `Foo.gta.runsettings`.
* Visual Studio user settings files. VS allows for the selection of [test settings](https://msdn.microsoft.com/en-us/library/jj635153.aspx) files via the *Test/Test Settings* menu, and to pass such settings files to `VsTest.Console.exe` via the `/Settings` parameter.
* Environment variable `GTA_FALLBACK_SETTINGS`. The settings file referred to from that environment variable will be used *only* if GTA fails to receive settings from the VS test framework; currently, this is only known to happen when running GTA through `VsTest.Console.exe` on VS 2017 V15.5 and later (see #184).

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

Note that due to the overriding hierarchy described above, you probably want to provide only a subset of the nodes in your configuration files. For instance, providing the node `<DebugMode>true</DebugMode>` in a shared solution settings file will make sure that all sharing developers will run GTA with debug output, no matter what the developer's individual settings at *Tools/Options/Google Test Adapter* are (and unless the developer has selected a test settings file via VS, which would override the solution setting).

For reference, see a settings file [AllTestSettings.gta.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Resources/AllTestSettings.gta.runsettings) containing all available settings, a more realistic solution settings file [SampleTests.gta.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/SampleTests/SampleTests.gta.runsettings) as delivered with the SampleTests solution, and a user settings file [NonDeterministic.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/SampleTests/NonDeterministic.runsettings) as used by GTA's end-to-end tests. The syntax of the GTA settings files (excluding the `<RunSettings>` node) is specified by [this schema](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/TestAdapter/GoogleTestAdapterSettings.xsd).

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

* <a name="trait_macros"></a>You can make use of the custom test macros provided in [GTA_Traits_1.8.0.h](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.8.0.h) (or [GTA_Traits_1.7.0.h](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits_1.7.0.h) if you are still on Google Test 1.7.0), which contain macros for all test types of the Google Test framework. The macros do not change behavior of the tests; they only add some information to the generated test code which encodes the traits assigned to the respective test. All GTA provided macros follow the same naming schema `<Google Test macro>_TRAITS`, where, obviously, `<Google Test macro>` is the name of the according macro in Google Test. Each test can be assigned up to 8 traits.
* <a name="trait_regexes"></a>Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option. For instance, the expression `.*///Size,Medium` assigns the trait *(Size,Medium)* to all tests.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase. For instance, the test declaration `TEST_P_TRAITS(ParameterizedTests, SimpleTraits, Size, Small)` will make sure that all test instances of test ParameterizedTest.SimpleTraits will be assigned the trait *(Size,Small)* (and override the Size trait assigned from the first phase).
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above. For instance, the expression `.*\[1.*\]///Size,Large` will make sure that all parameterized tests where the parameter starts with a 1 will be assigned the trait *(Size,Large)* (and override the traits assigned by phases 1 and 2).

Note that traits are assigned in an additive manner within each phase, and in an overriding manner between phases. For instance, if a test is assigned the traits *(Author,Foo)* and *(Author,Bar)* in phase 1, the test will have both traits. If the test is also assigned the trait *(Author,Baz)* in phases 2 or 3, it will only have that trait. See [test code](https://github.com/csoltenborn/GoogleTestAdapter/blob/fcc83220ceec9979710c2340f2378b0e8b430a60/GoogleTestAdapter/Core.Tests/GoogleTestDiscovererTraitTestsBase.cs) for examples.

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

However, note that Google Test as of V1.8.1 [uses some memory allocation](https://github.com/google/googletest/pull/1142) which is recognized by MS' leak detection mechanism as a leak (although it isn't), in particular for failing assertions. Some of these "false positives" have been fixed with the linked issue, making leak detection useful as long as all tests are green; however, and until this problem is fixed, the memory leak detection provided by GTA will result in a skipped exit code test in case `RUN_ALL_TESTS()` does not return `0`, but will report the leak in the test's error message. If you run into such problems, please report them against the [Google Test repository](https://github.com/google/googletest) if appropriate.

##### Known issues

* Visual Studio 2013: if a test project *FooTests* makes use of a `main()` method of a *BarTests* project (e.g. by referencing its `main.cpp`), the exit code test of *FooTests* will be grouped under *BarTests*' executable.


#### <a name="vstest_console"></a>Running tests from command line with `VSTest.Console.exe`

GTA can be used to run tests from the command line, which can be done making use of VS's [VSTest.Console.exe](https://msdn.microsoft.com/en-us/library/jj155800.aspx). GTA supports all the tool's command line options, including `/UseVsixExtensions` and `/TestAdapterPath`.

Note, however, that VSTest.Console.exe will not make use of GTA solution settings (if the solution containing the tests happens to use such settings). All settings to be used by VSTest.Console.exe need to be passed using the `/Settings` command line option. Note also that the `$(SolutionDir)` placeholder is neither available in the *Test setup/teardown batch file* options nor in the *Additional test execution parameters* option.

<a name="test_case_filters"></a>The tests to be run can be selected via the `/TestCaseFilter` option. Filters need to follow the syntax as described in this [blog entry](https://devblogs.microsoft.com/devops/running-selective-unit-tests-in-vs-2012-using-testcasefilter). GTA supports the following test properties:

* DisplayName
* FullyQualifiedName
* Type
* Author
* TestCategory
* Source (i.e., binary containing the test)
* CodeFilePath (i.e., source file containing the test)
* Class
* LineNumber
* Id 
* ExecutorUri

Additionally, traits can be used in test case filters. E.g., all tests having a `Duration` of `short` can be executed by means of the filter `/TestCaseFilter:"Duration=short"`.

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
* The test adapter is not able to find the pdb of your test executable, e.g. because it has been deleted or moved (and indicates that with a warning in the test output window). Rebuilding your solution should regenerate the pdb at an appropriate location.
* The test executable's project settings result in a pdb file not containing the information necessary to resolve source locations and traits (see [#46](https://github.com/csoltenborn/GoogleTestAdapter/issues/46)). Change the setting as indicated below and rebuild your solution.
  * VS 2015 and earlier: `Yes` or `Optimize for debugging (/DEBUG)`
  * VS 2017: `Generate debug information optimized for sharing and publishing (/DEBUG:FULL)`
* Option *Parse symbol information* is set to `false`, making GTA not parse that information out of the pdb file intentionally. The actual set of options used is potentially composed from VS options, a solution settings file, and a user settings file; the resulting set of options will be logged to the test output window if the *Print debug info* option is set to `true`.

##### Test discovery is slow/does not seem to finish
* Switch off *Parse symbol information*. You won't have source locations and traits from macros, but will still get clickable stack traces in case a test fails. This will avoid scanning the executables' `.pdb` files.
* Configure a regex matching your test executable, or create an `.is_google_test` file (see [above](#test_discovery_regex)). This will avoid scanning the binary for gtest indications.
* Make sure *Print debug info* and *Print test output* are `false`.

You might consider using GTA's project settings to switch off symbol parsing and binary scanning for problematic test executables only, thus compromising between speed of test discovery and build maintainability.

##### The Test Explorer window can not be opened after installing GTA
* Your MEF cache might have been corrupted. Please refer to [issue #172](https://github.com/csoltenborn/GoogleTestAdapter/issues/172) for help.

##### The Google Test Adapter extension is disabled when I start Visual Studio
* Your MEF cache might have been corrupted. Please refer to [issue #98](https://github.com/csoltenborn/GoogleTestAdapter/issues/98) for help.


### Development of Google Test Adapter

Please refer to our [wiki](https://github.com/csoltenborn/GoogleTestAdapter/wiki/Development-Knowledge-Base).


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
