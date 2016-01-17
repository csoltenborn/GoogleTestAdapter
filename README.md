[![Build status](https://ci.appveyor.com/api/projects/status/8hdgmdy1ogqi606j/branch/master?svg=true)](https://ci.appveyor.com/project/csoltenborn/googletestadapter-u1cxh/branch/master) [![Coverage Status](https://coveralls.io/repos/csoltenborn/GoogleTestAdapter/badge.svg?branch=master&service=github)](https://coveralls.io/github/csoltenborn/GoogleTestAdapter?branch=master)

### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework. It is based on the [Google Test Runner](https://github.com/markusl/GoogleTestRunner), a similar extension written in F#; we have ported the extension to C# and implemented various enhancements.

![Screenshot of test explorer](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestExtension/GoogleTestAdapterVSIX/Resources/Screenshot.png "Screenshot of test explorer")

#### Features

* Sequential and parallel test execution
* [Traits](http://blogs.msdn.com/b/visualstudioalm/archive/2012/11/09/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1-using-traits-in-the-unit-test-explorer.aspx) support by means of custom C++ macros and/or trait assignment by regexes
* Full support for [value-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#value-parameterized-tests), [typed](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#typed-tests), and [type-parameterized](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#type-parameterized-tests) tests
* Full support for all Google Test command line options, including [test shuffling](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#shuffling-the-tests) and [test repetition](https://github.com/google/googletest/blob/master/googletest/docs/AdvancedGuide.md#repeating-the-tests)
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
* Download and launch the [VSIX installer](https://github.com/csoltenborn/GoogleTestAdapter/releases/download/v0.2.3/GoogleTestAdapter-0.2.3.vsix) (which can also be downloaded from the [Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/94c02701-8043-4851-8458-34f137d10874))

After restarting VS, your tests will be displayed in the test explorer at build completion time. If no or not all tests show up, switch on *Debug mode* at *Tools/Options/Google Test Adapter/General*, which will show on the test console whether your test executables are recognized by GTA. If they are not, configure a *Test discovery regex* at the same place.

#### Configuration

GTA is configured following Visual Studio's approach of configuration inheritance. There are three configuration levels:

1. Global options are configured in *Tools/Options/Google Test Adapter*.
2. Solution specific options override global options. They are provided by means of an XML configuration file; this allows sharing of settings via source control. The configuration file must be placed in the same folder as the solution's `.sln` file, and must have the same name as that file, but with extension `.gta.runsettings`. E.g., if the solution file's name is `Foo.sln`, the settings file must be named `Foo.gta.runsettings`. As a start, you can download a [sample solution test settings file](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestExtension/Resources/AllTestSettings.gta.runsettings). A realistic example is provided as part of the SampleGoogleTestTests solution.
3. Finally, VS allows for the selection of [test settings](https://msdn.microsoft.com/en-us/library/jj635153.aspx) files via the *Test/Test Settings* menu. GTA test settings can be added to an existing `.runsettings` file by adding a `GoogleTestAdapter` node to the `RunSettings` node of the file; such settings override global and solution settings. A sample file `NonDeterministic.runsettings` is provided as part of the SampleGoogleTestTests solution.

Note that due to the overriding hierarchy described above, you probably want to provide only a subset of the nodes under `GoogleTestAdapter` in your configuration files. For instance, providing the node `<DebugMode>true</DebugMode>` in a shared solution settings file will make sure that all sharing developers will run GTA with debug output, no matter what the developer's individual settings at *Tools/Options/Google Test Adapter* are (and unless the developer has selected a test settings file via VS, which would override the solution setting).

#### Assigning traits to tests

GTA has full support for traits, which can be assigned to tests in two ways:

1. You can make use of the custom test macros provided in [GTA_Traits.h](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestExtension/GoogleTestAdapter/Resources/GTA_Traits.h), which contain macros for simple tests, tests with fixtures and parameterized tests, each with one, two, or three traits. 
2. Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option. For instance, the expression `*///Size,Medium` assigns the trait (Size,Medium) to all tests.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase. For instance, the test declaration `TEST_P_TRAITS1(ParameterizedTests, SimpleTraits, Size, Small)` will make sure that all test instances of test ParameterizedTest.SimpleTraits will be assigned the trait (Size,Small) (and override the Size trait assigned from the first phase).
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above. For instance, the expression `*# GetParam() = 0*///Size,Large` will make sure that all parameterized tests where the parameter starts with a 0 will be assigned the trait (Size,Large) (and override the traits assigned by phases 1 and 2). 

#### Parallelization

Tests are run sequentially by default. If parallel test execution is enabled, the tests will be distributed to the available cores of your machine. To support parallel test execution, additional command line parameters can be passed to the Google Test executables (note that this feature is not restricted to parallel test execution); they can then be parsed by the test code at run time and e.g. be used to improve test isolation.

If you need to perform some setup or teardown tasks in addition to the setup/teardown methods of your test code, you can do so by configuring test setup/teardown batch files, to which you can pass several values such as solution directory or test directory for exclusive usage of the tests.


### Building, testing, debugging

Google Test Adapter has been created using Visual Studio 2015 and Nuget, which are the only requirements for building GTA. Its main solution *GoogleTestExtension* consists of three projects:

* GoogleTestAdapter contains the actual adapter code
* GoogleTestAdapterVSIX adds the VS Options page and some resources
* GoogleTestAdapterTests contains GTA's tests

#### Executing the tests

Many of the tests depend on the second solution *SampleTests*, which contains a couple of Google Test tests. Before the tests contained in GoogleTestAdapterTests can be run, the second solution needs to be built in Debug mode for X86; this is done for you by a post-build event of project GoogleTestAdapterTests. Afterwards, the GTA tests can be run and should all pass.

For manually testing GTA, just start the GTA solution: A development instance of Visual Studio will be started with GTA installed. Use this instance to open the *SampleGoogleTestTests* solution (or any other solution containing Google Test tests).

#### Debugging GTA

When you select *Debug/Start (Without) Debugging* an [experimental instance of Visual Studio](https://msdn.microsoft.com/en-us/library/vstudio/bb166560.aspx) will start and have the current build of GTA deployed. You can test and debug the extension here.

Note that test discovery and execution will not run as part of the Visual Studio process `devenv.exe`. It will spawn some of the following processes (depending on *Test/Test Settings/Default Processor Architecture*):
* `te.processhost.managed.exe` (test discovery and execution for X86)
* `vstest.discoveryengine.exe` (test discovery for X64)
* `vstest.executionengine.exe` (test execution for X64)

A convenient way to get your debugger attached is to set *Options/Advanced/Development mode* to true. Now you will get the chance to semi-automatically attach your debugger each time new GTA discovery or execution process is spawned (using the power of [just-in-time debugging](https://msdn.microsoft.com/en-us/library/5hs4b7a6.aspx)).


#### Contributions

Pull requests are welcome and will be reviewed carefully. Please make sure to include tests demonstrating the bug you fixed or covering the added functionality.


### Credits

#### People
* Markus Lindqvist, author of Google Test Runner
* Matthew Manela, author of Chutzpah Test Adapter

#### Tools
* [OpenCover](https://github.com/OpenCover/opencover) - open source .NET code coverage
* [AppVeyor](http://www.appveyor.com/) - awesome .NET CI build services
* [Coveralls](https://coveralls.io/) - code coverage visualization facilities
* [Coveralls.net](https://github.com/csmacnz/coveralls.net) - uploads code coverage data to Coveralls


### Contact

* Christian Soltenborn (first_name@last_name.de)
* Jonas Gefele (first_name@last_name.de)
