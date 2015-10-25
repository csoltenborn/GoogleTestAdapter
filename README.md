[![Build status](https://ci.appveyor.com/api/projects/status/8hdgmdy1ogqi606j/branch/master?svg=true)](https://ci.appveyor.com/project/csoltenborn/googletestadapter-u1cxh/branch/master) [![Coverage Status](https://coveralls.io/repos/csoltenborn/GoogleTestAdapter/badge.svg?branch=master&service=github)](https://coveralls.io/github/csoltenborn/GoogleTestAdapter?branch=master)

### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework. It is based on the [Google Test Runner](https://github.com/markusl/GoogleTestRunner), a similar extension written in F#; we have ported the extension to C# and implemented various enhancements. 

![Screenshot of test explorer](https://raw.githubusercontent.com/csoltenborn/GoogleTestAdapter/master/GoogleTestExtension/GoogleTestAdapterVSIX/Resources/Screenshot.png "Screenshot of test explorer")

#### Features

* Sequential and parallel test execution
* [Traits](http://blogs.msdn.com/b/visualstudioalm/archive/2012/11/09/how-to-manage-unit-tests-in-visual-studio-2012-update-1-part-1-using-traits-in-the-unit-test-explorer.aspx) support by means of custom C++ macros and/or trait assignment by regexes
* Full support for [parameterized tests](https://code.google.com/p/googletest/wiki/AdvancedGuide#Value_Parameterized_Tests)
* Full support for all Google Test command line options, including [test shuffling](https://code.google.com/p/googletest/wiki/AdvancedGuide#Shuffling_the_Tests) and [test repetition](https://code.google.com/p/googletest/wiki/AdvancedGuide#Repeating_the_Tests)
* Identification of crashed tests
* Test output can be piped to test console
* Execution of parameterized batch files for test setup/teardown
* Test discovery using a custom regex (if needed)

#### History

* [0.1](https://github.com/csoltenborn/GoogleTestAdapter/releases/download/v0.1/GoogleTestAdapter.vsix) (10/25/2015) - initial release

### Usage

#### Installation

Google Test Adapter can be installed in two ways:

* Install through the Visual Studio Gallery at *Tools/Extensions and Updates* - search for *Google Test Adapter*. This will make sure that the extension is updated automatically
* Download and launch the [VSIX installer](https://github.com/csoltenborn/GoogleTestAdapter/releases/download/v0.1/GoogleTestAdapter.vsix)

After restarting VS, your tests will be displayed in the test explorer at build completion time. If no or not all tests show up, switch on *Debug mode* at *Tools/Options/Google Test Adapter/General*, which will show on the test console whether your test executables are recognized by GTA. If they are not, configure a *Test discovery regex* at the same place.

#### Configuration

GTA is configured through Visual Studio's standard options at *Tools/Options/Google Test Adapter*.

#### Assigning traits to tests

GTA has full support for traits, which can be assigned to tests in two ways:

1. You can make use of the custom test macros provided in GTA_Traits.h, which contain macros for simple tests, tests with fixtures and parameterized tests, each with one, two, or three traits. 
2. Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option. For instance, the expression `*///Size,Medium` assigns the trait (Size,Medium) to all tests.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase. For instance, the test declaration `TEST_P_TRAITS1(ParameterizedTests, SimpleTraits, Size, Small)` will make sure that all test instances of test ParameterizedTest.SimpleTraits will be assigned the trait (Size,Small) (and override the Size trait assigned from the first phase).
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above. For instance, the expression `*# param = 0*///Size,Large` will make sure that all parameterized tests where the parameter starts with a 0 will be assigned the trait (Size,Large) (and override the traits assigned by phases 1 and 2). 

#### Parallelization

Tests are run sequentially by default. If parallel test execution is enabled, the tests will be distributed to the available cores of your machine. To support parallel test execution, additional command line parameters can be passed to the Google Test executables (note that this feature is not restricted to parallel test execution); they can then be parsed by the test code at run time and e.g. be used to improve test isolation.

If you need to perform some setup or teardown tasks in addition to the setup/teardown methods of your test code, you can do so by configuring test setup/teardown batch files, to which you can pass several values such as solution directory or test directory for exclusive usage of the tests.


### Roadmap

The following tasks will be tackled in the months to come. Feel free to suggest other enhancements, or to provide pull requests providing some of the features listed below (see section *Contributions* below).

* Better parsing and displaying of parameter values in case of parameterized tests
* Allow settings per solution, including exchange between developers
* Smarter test scheduling
  * Reduce number of times executables are invoked where possible
  * introduce option to assign test resources to threads (scheduling would then make sure tests are not running at same time if competing for the same test resources)
* Performance improvements
  * Faster canceling of running tests by actively killing test processes
  * More fine-grained locking of resources  (e.g., synchronize updating of test duration files on file level)
  * Make use of smarter data structures e.g. in scheduling
* Provide more placeholders to be used with test parameters and setup/teardown batch files, e.g. project dir or executable


### Known Issues

Currently, the following issues are known to us - patches welcome!

* Exceptions when debugging tests
  * Symptoms: At the end of debugging a set of Google Test tests, Visual Studio catches exceptions of type `System.Runtime.InteropServices.InvalidComObjectException`, the messages of which contain "The object invoked has disconnected from its clients"
  * Reason: This seems to be due to a bug in *te.processhost.managed.exe*, to which VS attaches a debugger when debugging tests
  * Workaround 1: Select *Test/Test Settings/Default Processor Architecture/X64* - this lets VS use the older *vstest.executionengine.exe* which does not have this problem
  * Workaround 2: In VS, mark the exception as "Do not catch" when it occurs
* Not all test results are reported to VS
  * Symptoms: When running tests, GTA reports that n tests have been executed, but VS shows less test results in the test explorer
  * Reason: This seems to be due to inter process communication, but we do not have a lot of expertise in this area, and there is no official documentation of the VS test framework API
  * Workaround: Configure *Report waiting time* at *Tools/Options/Google Test Adapter/Advanced*
* *Run tests after build* does not work
  * Symptoms: Selecting the *Run tests after build* option does not or not always result in the tests being run after build completion. Instead, the following error message is printed to the test console: "The specified type member 'Stale' is not supported in LINQ to Entities. Only initializers, entity members, and entity navigation properties are supported."
  * Reason: We have no idea
  * Workaround: None so far
  
  
### Building, testing, debugging

Google Test Adapter has been created using Visual Studio 2015 and Nuget, which are the only requirements for building GTA. Its main solution *GoogleTestExtension* consists of three projects:

* GoogleTestAdapter contains the actual adapter code
* GoogleTestAdapterVSIX adds the VS Options page and some resources
* GoogleTestAdapterTests contains GTA's tests

#### Signing

GTA's DLLs have strong names, i.e., they are cryptographically signed. Our key is contained within the solution in an encrypted way and is decrypted for signing by means of pre-build events of the projects, the password being provided as an environment variable. This allows us to build the solution locally as well as on a CI server without revealing the password. Since you do not have access to the password, you have two options for building GTA:
* Build without signing: Remove 
  1. the "Sign the assembly" checks and the build events' key decryption steps from the projects' configurations
  2. the `PublicKey` part from the `InternalsVisibleTo` attribute of GoogleTestAdapter's `AssemblyInfo`
* Sign with own key: 
  1. Create a key with VS
  2. Encrypt that key with `aescrypt` (to be found in the Tools folder of the repository) and using your own password, and replace GoogleTestExtension\Key.aes with the result 
  3. Create environment variable `GTA_KEY_PASSWORD` and assign it your password as value.

#### Executing the tests

Many of the tests depend on the second solution *SampleGoogleTestTests*, which contains a couple of Google Test tests. Before the tests contained in GoogleTestAdapterTests can be run, the second solution needs to be built in Debug mode for X86; this is done for you by a post-build event of project GoogleTestAdapterTests. Afterwards, the GTA tests can be run and should all pass.

For manually testing GTA, just start the GTA solution: A development instance of Visual Studio will be started with GTA installed. Use this instance to open the *SampleGoogleTestTests* solution (or any other solution containing Google Test tests).

#### Debugging GTA

Note that test discovery as well as test execution will be performed in processes different from the VS one. Therefore, to debug GTA's discovery and execution code, you need to manually attach a debugger to these processes. To support this, the class `GoogleTestAdapter.Helpers.TestEnvironment` contains a member variable `DebugMode` defaulting to `false`. Set this to `true` and start the VS development instance. As soon as your code is triggered, a dialog shows the id of the process the code is executed in. Switch to the other VS instance, attach a debugger to the according process, and click the OK button of the dialog to continue execution of the GTA code. Your breakpoints will now be hit.

Alternatively, you can [configure Windows to automatically attach a debugger](https://msdn.microsoft.com/en-us/library/a329t4ed(v=vs.100).aspx) whenever the processes for test discovery or test execution are started.

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