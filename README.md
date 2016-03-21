[![Build status](https://ci.appveyor.com/api/projects/status/8hdgmdy1ogqi606j/branch/master?svg=true)](https://ci.appveyor.com/project/csoltenborn/googletestadapter-u1cxh/branch/master) [![Coverage Status](https://coveralls.io/repos/csoltenborn/GoogleTestAdapter/badge.svg?branch=master&service=github)](https://coveralls.io/github/csoltenborn/GoogleTestAdapter?branch=master)

### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework. It is based on the [Google Test Runner](https://github.com/markusl/GoogleTestRunner), a similar extension written in F#; we have ported the extension to C# and implemented various enhancements.

![Screenshot of test explorer](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/VsPackage/Resources/Screenshot.png "Screenshot of test explorer")

#### Features

* Sequential and parallel test execution
* [Traits](http://blogs.msdn.com/b/visualstudioalm/archive/2012/11/09/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1-using-traits-in-the-unit-test-explorer.aspx) support by means of custom C++ macros and/or trait assignment by regexes
* Support for [value-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#value-parameterized-tests), [typed](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#typed-tests), and [type-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#type-parameterized-tests) tests
* Google Test's runtime behavior ([handling of exceptions](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#disabling-catching-test-thrown-exceptions), [break on assertion failure](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#turning-assertion-failures-into-break-points)) can be controlled via VS options
* Most important runtime options can be controlled via toolbar without entering VS's options
* Support for all Google Test command line options, including [test shuffling](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#shuffling-the-tests) and [test repetition](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#repeating-the-tests)
* Support for [test case filters](http://blogs.msdn.com/b/vikramagrawal/archive/2012/07/23/running-selective-unit-tests-in-vs-2012-rc-using-testcasefilter.aspx)
* Failed assertions and [SCOPED_TRACE](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#adding-traces-to-assertions)s are linked to their source locations
* Identification of crashed tests
* Test output can be piped to test console
* Execution of parameterized batch files for test setup/teardown
* Test discovery using a custom regex (if needed)
* Settings can be shared via source control

#### History

* See [releases](https://github.com/csoltenborn/GoogleTestAdapter/releases)

### Usage

#### Installation

Google Test Adapter can be installed in two ways:

* Install through the Visual Studio Gallery at *Tools/Extensions and Updates* - search for *Google Test Adapter*. This will make sure that the extension is updated automatically
* Download and launch the [VSIX installer](https://github.com/csoltenborn/GoogleTestAdapter/releases/download/v0.4.1/GoogleTestAdapter-0.4.1.vsix) (which can also be downloaded from the [Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/94c02701-8043-4851-8458-34f137d10874))

After restarting VS, your tests will be displayed in the test explorer at build completion time. If no or not all tests show up, you can try one of the following options:

* Switch on *Debug mode* at *Tools/Options/Google Test Adapter/General*, which will show on the test console whether your test executables are found by GTA. If they are not, configure a *Test discovery regex* at the same place.
* If your project configuration contains references to DLLs which do not end up in the build directory (e.g. through *Project/Properties/Linker/Input/Additional Dependencies*), these DLLs will not be found when running your tests. Use option *Path extension* to add the directories containing these DLLs to the test executables' PATH variable.

#### Configuration

GTA is configured following Visual Studio's approach of configuration inheritance. There are three configuration levels:

1. Global options are configured in *Tools/Options/Google Test Adapter*.
2. Solution specific options override global options. They are provided by means of an XML configuration file; this allows sharing of settings via source control. The configuration file must be placed in the same folder as the solution's `.sln` file, and must have the same name as that file, but with extension `.gta.runsettings`. E.g., if the solution file's name is `Foo.sln`, the settings file must be named `Foo.gta.runsettings`. As a start, you can download a [sample solution test settings file](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Resources/AllTestSettings.gta.runsettings). A realistic example is provided as part of the SampleTests solution.
3. Finally, VS allows for the selection of [test settings](https://msdn.microsoft.com/en-us/library/jj635153.aspx) files via the *Test/Test Settings* menu. GTA test settings can be added to an existing `.runsettings` file by adding a `GoogleTestAdapter` node to the `RunSettings` node of the file; such settings override global and solution settings. A sample file `NonDeterministic.runsettings` is provided as part of the SampleTests solution.

Note that due to the overriding hierarchy described above, you probably want to provide only a subset of the nodes under `GoogleTestAdapter` in your configuration files. For instance, providing the node `<DebugMode>true</DebugMode>` in a shared solution settings file will make sure that all sharing developers will run GTA with debug output, no matter what the developer's individual settings at *Tools/Options/Google Test Adapter* are (and unless the developer has selected a test settings file via VS, which would override the solution setting).

The most important runtime options (i.e., *Parallel test execution*, *Break on failure*, and *Catch exceptions*) can also be set via a toolbar; this is equivalent to setting the according options via *Tools/Options/Google Test Adapter*.

#### Assigning traits to tests

GTA has full support for traits, which can be assigned to tests in two ways:

* You can make use of the custom test macros provided in [GTA_Traits.h](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestAdapter/Core/Resources/GTA_Traits.h), which contain macros for simple tests, tests with fixtures and parameterized tests, each with one, two, or three traits. 
* Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option. For instance, the expression `.*///Size,Medium` assigns the trait (Size,Medium) to all tests.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase. For instance, the test declaration `TEST_P_TRAITS1(ParameterizedTests, SimpleTraits, Size, Small)` will make sure that all test instances of test ParameterizedTest.SimpleTraits will be assigned the trait (Size,Small) (and override the Size trait assigned from the first phase).
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above. For instance, the expression `.*\[1.*\]///Size,Large` will make sure that all parameterized tests where the parameter starts with a 1 will be assigned the trait (Size,Large) (and override the traits assigned by phases 1 and 2).

#### Running tests from command line with `VSTest.Console.exe`

GTA can be used to run tests from the command line, which can be done making use of VS's [VSTest.Console.exe](https://msdn.microsoft.com/en-us/library/jj155800.aspx). GTA supports all the tool's command line options, including `/UseVsixExtensions` and `/TestAdapterPath`.

Note, however, that VSTest.Console.exe will not make use of GTA solution settings (if the solution containing the tests happens to use such settings). All settings to be used by VSTest.Console.exe need to be passed using the `/Settings` command line option. Note also that the `$(SolutionDir)` placeholder is neither available in the *Test setup/teardown batch file* options nor in the *Additional test execution parameters* option. Finally, note that GTA currently has issues with running X64 tests via VSTest.Console.exe (see [#21](https://github.com/csoltenborn/GoogleTestAdapter/issues/21)).

The tests to be run can be selected via the `/TestCaseFilter` option. Filters need to follow the syntax as described in this [blog entry](http://blogs.msdn.com/b/vikramagrawal/archive/2012/07/23/running-selective-unit-tests-in-vs-2012-rc-using-testcasefilter.aspx). GTA supports the following test properties:

* DisplayName
* FullyQualifiedName
* Source (i.e., binary containing the test)
* CodeFilePath (i.e., source file containing the test)
* LineNumber
* Id 
* ExecutorUri

Additionally, traits can be used in test case filters. E.g., all tests having a `TestCategory` of `Unit` can be executed by means of the filter `/TestCaseFilter:"TestCategory=Unit"`.

#### Parallelization

Tests are run sequentially by default. If parallel test execution is enabled, the tests will be distributed to the available cores of your machine. To support parallel test execution, additional command line parameters can be passed to the Google Test executables (note that this feature is not restricted to parallel test execution); they can then be parsed by the test code at run time and e.g. be used to improve test isolation.

If you need to perform some setup or teardown tasks in addition to the setup/teardown methods of your test code, you can do so by configuring test setup/teardown batch files, to which you can pass several values such as solution directory or test directory for exclusive usage of the tests.

Note that GTA remembers the durations of the executed tests to improve test scheduling for later test runs. The durations are stored in files with endings `.gta.testdurations` - make sure your version control system ignores these files.


### Building, testing, debugging

Google Test Adapter has been created using Visual Studio 2015 and Nuget, which are the only requirements for building GTA. Its main solution *GoogleTestAdapter* consists of a couple of projects:

* `Core` contains the main logic for discovering and running tests of the Google Test Framework
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


### Credits

#### People
* Markus Lindqvist, author of Google Test Runner
* Matthew Manela, author of Chutzpah Test Adapter

#### Tools
* [ReSharper](https://www.jetbrains.com/resharper/) - awesome VS extension for .NET development, including refactoring, static analysis etc.
  * thanks to [JetBrains](https://www.jetbrains.com/) for providing free licenses for our developers!
* [AppVeyor](http://www.appveyor.com/) - awesome .NET CI build services
  * thanks for providing free services and great support for open source projects!
* [Coveralls](https://coveralls.io/) - code coverage visualization facilities
  * thanks for providing free services for open source projects!
* [OpenCover](https://github.com/OpenCover/opencover) - open source .NET code coverage
* [Coveralls.net](https://github.com/csmacnz/coveralls.net) - uploads code coverage data to Coveralls
