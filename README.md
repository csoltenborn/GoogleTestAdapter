[![Build status](https://ci.appveyor.com/api/projects/status/8hdgmdy1ogqi606j/branch/master?svg=true)](https://ci.appveyor.com/project/csoltenborn/googletestadapter-u1cxh/branch/master) [![codecov](https://codecov.io/gh/csoltenborn/GoogleTestAdapter/branch/master/graph/badge.svg)](https://codecov.io/gh/csoltenborn/GoogleTestAdapter)


### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework.

![Screenshot of Test Explorer](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/VsPackage/Resources/Screenshot.png "Screenshot of Test Explorer")

#### Features

* Sequential and [parallel](#parallelization) test execution
* [Traits](http://blogs.msdn.com/b/visualstudioalm/archive/2012/11/09/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1-using-traits-in-the-unit-test-explorer.aspx) support by means of [custom C++ macros](#trait_macros) and/or [trait assignment by regexes](#trait_regexes)
* Support for [value-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#value-parameterized-tests), [typed](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#typed-tests), and [type-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#type-parameterized-tests) tests
* Google Test's runtime behavior ([handling of exceptions](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#disabling-catching-test-thrown-exceptions), [break on assertion failure](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#turning-assertion-failures-into-break-points)) can be controlled via [VS options](#global_settings)
* Most important runtime options can be controlled via [toolbar](#toolbar) without entering VS's options
* Support for all Google Test command line options, including [test shuffling](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#shuffling-the-tests) and [test repetition](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#repeating-the-tests)
* [TFS support](#vstest_console) by means of [`VSTest.Console.exe`](https://msdn.microsoft.com/en-us/library/jj155800.aspx)
* [Support](#test_case_filters) for [test case filters](http://blogs.msdn.com/b/vikramagrawal/archive/2012/07/23/running-selective-unit-tests-in-vs-2012-rc-using-testcasefilter.aspx)
* Failed assertions and [SCOPED_TRACE](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#adding-traces-to-assertions)s are linked to their source locations
* Identification of crashed tests
* Test output can be piped to test console
* Execution of [parameterized batch files](#test_setup_and_teardown) for test setup/teardown
* Test discovery using a [custom regex](#test_discovery_regex) (if needed)
* Settings can be [shared via source control](#solution_settings)
* Installable as Visual Studio extension or NuGet development dependency

#### History

* See [releases](https://github.com/csoltenborn/GoogleTestAdapter/releases)


### Usage

#### Installation

Google Test Adapter can be installed in three ways:

* Install through the Visual Studio Gallery at *Tools/Extensions and Updates* - search for *Google Test Adapter*. This will make sure that the extension is updated automatically
* Download and launch the [VSIX installer](https://github.com/csoltenborn/GoogleTestAdapter/releases/download/v0.9.0/GoogleTestAdapter-0.9.0.vsix) (which can also be downloaded from the [Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/94c02701-8043-4851-8458-34f137d10874))
* Add a NuGet dependency to the [Google test adapter nuget package](https://www.nuget.org/packages/GoogleTestAdapter/) to your Google Test projects. Note, however, that Visual Studio integration is limited this way: VS can discover and run tests, but no options or toolbar will be available; configuration is only possible through solution config files (see below).

After restarting VS, your tests will be displayed in the Test Explorer at build completion time. If no or not all tests show up, have a look at the [trouble shooting section](#trouble_shooting).

#### <a name="gta_configuration"></a>Configuration

GTA is configured following Visual Studio's approach of configuration inheritance. It provides different ways of configuration:
* <a name="global_settings"></a>The *Google Test Adapter* section of Visual Studio's *Tools/Options* (not available if installed via NuGet). These options are referred to as *global options* in the following.
* <a name="toolbar"></a>The GTA toolbar (not available if installed via NuGet). The most important runtime options (i.e., *Parallel test execution*, *Break on failure*, *Catch exceptions*, and *Print test output*) can also be set via a toolbar; this is equivalent to setting the according options via *Tools/Options/Google Test Adapter*.
* <a name="solution_settings"></a>Solution settings files (not available if run via [VsTest.Console.exe](https://msdn.microsoft.com/en-us/library/jj155800.aspx)). They are provided by means of an XML configuration file; this allows sharing of settings via source control. The configuration file must be placed in the same folder as the solution's `.sln` file, and must have the same name as that file, but with extension `.gta.runsettings`. E.g., if the solution file's name is `Foo.sln`, the settings file must be named `Foo.gta.runsettings`.
* Visual Studio user settings files. VS allows for the selection of [test settings](https://msdn.microsoft.com/en-us/library/jj635153.aspx) files via the *Test/Test Settings* menu. 

The format of solution and user settings files is the same: a `<GoogleTestAdapterSettings>` node contains the solution settings and the (possibly empty) set of project settings and is itself contained in a `<RunSettings>` node (which in the case of user settings files might contain additional, e.g. VS specific settings). In contrast to solution settings, each set of project settings additionally has a regular expression to be evaluated at test discovery and execution time.

The final settings to be used are computed in two stages:
1. The available global, solution file, and user file settings are merged into solution settings and a set of project settings. This is done in increasing priority, i.e., solution file settings override global settings, and user file settings override solution settings. Project settings of solution and user settings files are merged if they share the exact same regular expression.
2. At test discovery and execution time, each test executable's full path is matched against the project settings' regular expressions; the first matching project settings are used for the particular test executable. If no project settings are found, the solution settings are used.

Overall, given a test executable `mytests.exe`, the following settings apply to that executable in decreasing priority:
1. Project settings of a user settings file, the regular expression of which matches the full path of `mytests.exe`.
2. Project settings of a solution settings file, the regular expression of which matches the full path of `mytests.exe`.
3. Solution settings of a user settings file.
4. Solution settings of a solution settings file.
5. Global settings.

Note that due to the overriding hierarchy described above, you probably want to provide only a subset of the nodes in your configuration files. For instance, providing the node `<DebugMode>true</DebugMode>` in a shared solution settings file will make sure that all sharing developers will run GTA with debug output, no matter what the developer's individual settings at *Tools/Options/Google Test Adapter* are (and unless the developer has selected a test settings file via VS, which would override the solution setting).

For reference, see a settings file [AllTestSettings.gta.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Resources/AllTestSettings.gta.runsettings) containing all available settings, a more realistic solution settings file [SampleTests.gta.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/SampleTests/SampleTests.gta.runsettings) as delivered with the SampleTests solution, and a user settings file [NonDeterministic.runsettings](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/SampleTests/NonDeterministic.runsettings) as used by GTA's end-to-end tests.


#### Assigning traits to tests

GTA has full support for [traits](http://blogs.msdn.com/b/visualstudioalm/archive/2012/11/09/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1-using-traits-in-the-unit-test-explorer.aspx), which can be assigned to tests in two ways:

* <a name="trait_macros"></a>You can make use of the custom test macros provided in [GTA_Traits.h](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits.h), which contain macros for all test types of the Google Test framework. The macros do not change behavior of the tests; they only add some information to the generated test code which encodes the traits assigned to the respective test. All GTA provided macros follow the same naming schema `<Google Test macro>_TRAITS`, where, obviously, `<Google Test macro>` is the name of the according macro in Google Test. Each test can be assigned up to 8 traits.
* <a name="trait_regexes"></a>Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option. For instance, the expression `.*///Size,Medium` assigns the trait *(Size,Medium)* to all tests.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase. For instance, the test declaration `TEST_P_TRAITS(ParameterizedTests, SimpleTraits, Size, Small)` will make sure that all test instances of test ParameterizedTest.SimpleTraits will be assigned the trait *(Size,Small)* (and override the Size trait assigned from the first phase).
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above. For instance, the expression `.*\[1.*\]///Size,Large` will make sure that all parameterized tests where the parameter starts with a 1 will be assigned the trait *(Size,Large)* (and override the traits assigned by phases 1 and 2).

Note that traits are assigned in an additive manner within each phase, and in an overriding manner between phases. For instance, if a test is assigned the traits *(Author,Foo)* and *(Author,Bar)* in phase 1, the test will have both traits. If the test is also assigned the trait *(Author,Baz)* in phases 2 or 3, it will only have that trait. See [test code](https://github.com/csoltenborn/GoogleTestAdapter/blob/master/GoogleTestAdapter/Core.Tests/AbstractGoogleTestDiscovererTraitTests.cs) for examples.

#### <a name="vstest_console"></a>Running tests from command line with `VSTest.Console.exe`

GTA can be used to run tests from the command line, which can be done making use of VS's [VSTest.Console.exe](https://msdn.microsoft.com/en-us/library/jj155800.aspx). GTA supports all the tool's command line options, including `/UseVsixExtensions` and `/TestAdapterPath`.

Note, however, that VSTest.Console.exe will not make use of GTA solution settings (if the solution containing the tests happens to use such settings). All settings to be used by VSTest.Console.exe need to be passed using the `/Settings` command line option. Note also that the `$(SolutionDir)` placeholder is neither available in the *Test setup/teardown batch file* options nor in the *Additional test execution parameters* option. Finally, note that GTA currently has issues with running X64 tests via VSTest.Console.exe (see [#21](https://github.com/csoltenborn/GoogleTestAdapter/issues/21)).

<a name="test_case_filters"></a>The tests to be run can be selected via the `/TestCaseFilter` option. Filters need to follow the syntax as described in this [blog entry](http://blogs.msdn.com/b/vikramagrawal/archive/2012/07/23/running-selective-unit-tests-in-vs-2012-rc-using-testcasefilter.aspx). GTA supports the following test properties:

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


### <a name="trouble_shooting"></a>Trouble shooting

None or not all of my tests show up!
* <a name="test_discovery_regex"></a>Switch on *Debug mode* at *Tools/Options/Google Test Adapter/General*, which will show on the test console whether your test executables are found by GTA. If they are not, configure a *Test discovery regex* at the same place. In case of GTA installation via NuGet, do not forget to add the regex to the solution config file (which might be a good idea anyways).
* Your test executable can not run with command line option `--gtest_list_tests`, e.g. because it crashes. Make sure that your tests can be listed via command line; if they do not, debug your test executable, e.g. by making the according test project the startup project of your solution, and placing a breakpoint at the main method of your test executable.
* If your project configuration contains references to DLLs which do not end up in the build directory (e.g. through *Project/Properties/Linker/Input/Additional Dependencies*), these DLLs will not be found when running your tests. Use option *PATH extension* to add the directories containing these DLLs to the test executables' PATH variable.
* If your project happens to be a makefile project, there's a pitfall which will prevent GTA from discovering your tests: It appears that when importing a makefile project into VS, the *Output* setting of the project is populated from the makefile's content. However, if the makefile is edited later on such that the location of the generated test executable changes, VS does not find the test executable any more. One symptom of this is that your project can not be launched any more with `F5`. Make sure that the *Output* setting of the project is consistent with its makefile to avoid this problem. 

No source locations and traits are found for my tests!
* The test adapter is not able to find the pdb of your test executable, e.g. because it has been deleted or moved (and indicates that with a warning in the test output window). Rebuilding your solution should regenerate the pdb at an appropriate location.
* The test executable's project has the option *Linker/Debugging/Generate debug info* set to `No` or `Optimize for faster linking (/DEBUG:FASTLINK)`, resulting in a pdb not containing the information necessary to resolve source locations and traits (see [#46](https://github.com/csoltenborn/GoogleTestAdapter/issues/46)). Change the setting to `Yes` or `Optimize for debugging (/DEBUG)` and rebuild your solution.
* Option *Parse symbol information* is set to `false`, making GTA not parse that information out of the pdb file intentionally. The actual set of options used is potentially composed from VS options, a solution settings file, and a user settings file; the resulting set of options will be logged to the test output window if the *Print debug info* option is set to `true`.

The Google Test Adapter extension is disabled when I start Visual Studio!
* Your MEF cache might have been corrupted. Please refer to [this issue](https://github.com/csoltenborn/GoogleTestAdapter/issues/98) for help.


### Building, testing, debugging

Google Test Adapter has been created using Visual Studio 2015 and NuGet, which are the only requirements for building GTA. Its main solution *GoogleTestAdapter* consists of a couple of projects:

* `Core` contains the main logic for discovering and running tests of the Google Test Framework
* `Common` contains some infrastructure common to the other projects
* `DiaResolver` is the bridge to the Dia DLL used for finding tests in the binaries generated by the Google Test framework
* `TestAdapter` contains the integration into the VS unit testing framework for use in Visual Studio or `vstest.console.exe`
* `VsPackage` bundles everything into a Visual Studio Extension Package with an option page and .VSIX installer
* `*.Tests` contain the tests belonging to the respective project

#### Executing the tests

Many of the tests depend on the second solution *SampleTests*, which contains a couple of Google Test tests. Before any of the tests can be run, this second solution needs to be built in Debug mode for X86; this is done for you by a post-build event of project Core.Tests. Afterwards, the GTA tests can be run and should all pass.

For manually testing GTA, just start the GTA solution: A development instance of Visual Studio will be started with GTA installed. Use this instance to open the *SampleTests* solution (or any other solution containing Google Test tests).

#### Debugging GTA

Projects `TestAdapter` and `VsPackage` have debugging options pre-configured. `TestAdapter` will run the tests in the `SampleTests` solution using the command line tool for running tests (`vstest.console.exe`). `VsPackage` will start an [experimental instance of Visual Studio](https://msdn.microsoft.com/en-us/library/vstudio/bb166560.aspx) (`devenv.exe`) having the current build of GTA deployed.

Note that different parts of GTA will run in different processes which are spawned on demand:
* `devenv.exe` (running in IDE: `RunSettingsService`, `GoogleTestExtensionOptionsPage` and `GlobalRunSettingsProvider`)
* `vstest.console.exe` (running from command line: `RunSettingsService`)
* `te.processhost.managed.exe` (platform X86: `TestDiscoverer` and `TestExecutor`)
* `vstest.discoveryengine.exe` (platform X64: `TestDiscoverer`)
* `vstest.executionengine.exe` (platform X64: `TestExecutor`)

A convenient way to get your debugger attached is to use Microsoft's [Child Process Debugging Power Tool](https://visualstudiogallery.msdn.microsoft.com/a1141bff-463f-465f-9b6d-d29b7b503d7a). We have the `GoogleTestAdapter.ChildProcessDbgSettings` already precofigured for you. Alternatively, you can add [``System.Diagnostics.Debugger.Break()``](https://msdn.microsoft.com/en-US/library/system.diagnostics.debugger.break) statements in places of interest.

#### Contributions

Pull requests are welcome and will be reviewed carefully. Please make sure to include tests demonstrating the bug you fixed or covering the added functionality.


### External Links

[Basic tutorial for using Google Test with GTA in Visual Studio](https://usingcpp.wordpress.com/2016/07/23/google-test-and-visual-studio/)


### Credits

#### People
* Markus Lindqvist, author of Google Test Runner (see below)
* Matthew Manela, author of Chutzpah Test Adapter

#### Tools
* [Google Test Runner](https://github.com/markusl/GoogleTestRunner) - the starting point for GTA. We have ported the Google Test Runner extension from F# to C# and implemented various enhancements and refactorings.
* [ReSharper](https://www.jetbrains.com/resharper/) - awesome VS extension for .NET development, including refactoring, static analysis etc.
  * thanks to [JetBrains](https://www.jetbrains.com/) for providing free licenses for our developers!
  * note that JetBrains' [Resharper C++](https://www.jetbrains.com/resharper-cpp/) can also run tests written using Google Test
* [AppVeyor](http://www.appveyor.com/) - awesome .NET CI build services
  * thanks for providing free services and great support for open source projects!
* [Codecov](https://codecov.io/) - code coverage visualization facilities
  * thanks for providing free services for open source projects!
* [OpenCover](https://github.com/OpenCover/opencover) - open source .NET code coverage
