### Google Test Adapter

Google Test Adapter (GTA) is a Visual Studio 2015 extension providing test discovery and execution of C++ tests written with the [Google Test](https://github.com/google/googletest) framework. It is based on the [Google Test Runner](https://github.com/markusl/GoogleTestRunner), a similar extension written in F#; we have ported the extension to C# and implemented various enhancements. 

![Screenshot of test explorer](Screenshot.png)

#### Features

* Linear and parallel test execution
* Traits support by means of custom C++ macros and/or trait assignment by regexes
* Full support of parameterized tests
* Full support of all Google Test command line options, including test shuffling and test repetition
* Identification of crashed tests
* Test output can be piped to test console
* Execution of parameterized batch files for test setup/teardown
* Test discovery using a custom regex (if needed)

#### History

* 0.1 (10/01/2015) - initial release

### Usage

#### Installation

Google Test Adapter can be installed in two ways:

* Install through the Visual Studio Gallery - search for *Google Test Adapter*
* Download the VSIX installer

After restarting VS, your tests will be displayed in the test explorer at build completion. If they don't, switch on *Debug mode* at *Tools/Options/Google Test Adapter/General*, which will show on the test console whether your test executables are recognized by GTA. If they don't, configure a *Test discovery regex* at the same place.

#### Assigning traits to tests

GTA has full support for traits, which can be assigned to tests in two ways:

1. You can make use of the custom test macros provided in GTA_Traits.h, which contain macros for simple tests, tests with fixtures and parameterized tests with one, two, or three traits. 
2. Combinations of regular expressions and traits can be specified under the GTA options: If a test's name matches one of these regular expressions, the according trait is assigned to that test. 

More precisely, traits are assigned to tests in three phases:

1. Traits are assigned to tests which match one of the regular expressions specified in the *traits before* option.
2. Traits added to tests via test macros are assigned to the according tests, overriding traits from the first phase (e.g., if test Foo has been assigned a trait (Size,Small) in the first phase, and its macro assigns the trait (Size,Medium), the test will carry the (and only the) trait (Size,Medium)
3. Traits are assigned to tests which match one of the regular expressions specified in the *traits after* option, overriding traits from phases 1 and 2 as described above.

#### Parallelization

Tests are run sequentially by default. If parallel test execution is enabled, the tests will be distributed to the available cores of your machine. To support parallel test execution, additional command line parameters can be passed to the Google Test executables (note that this feature is not restricted to parallel test execution); they can then be parsed by the test code at run time and e.g. be used to improve test isolation.

If you need to perform some setup or teardown tasks in addition to the setup/teardown methods of your test code, you can do so by configuring test setup/teardown batch files, to which you can pass several values such as solution directory or test directory for exclusive usage of the tests.


### Roadmap

The following tasks will be tackled in the months to come. Feel free to suggest other enhancements, or to provide pull requests providing some of the features listed below (see section *Contributing* below).

* 0.2 (planned for 1/1/2016)
  * Better parsing and displaying of parameter values in case of parameterized tests
  * Save settings into XML file, e.g. within solution dir, to allow easy exchange of settings via developers
  * Provide more placeholders to be used with test parameters and setup/teardown batch files (project dir? executable?)
  * Performance improvements
    * Parallel test discovery
	* Faster canceling of running tests by actively killing test processes
  * Setup CI build
  * Improve test coverage
* 0.3 (second quarter of 2016)
  * Smarter test scheduling
    * Reduce number of times executables are invoked
	* introduce minimum duration per thread (executing 8 tests each lasting 1ms is much faster on 1 thread than on 8 threads)
    * introduce option to assign test ressources to threads
	  *	scheduling would then make sure tests are not running at same time if competing for the same test resources
  * Performance improvements
    * More fine-grained locking of resources  (e.g., synchronize updating of test duration files on file level)
	* Make use of smarter data structures e.g. in scheduling
  * Improve test coverage

	
### Known Issues

Currently, the following issues are known to us - patches welcome!

* Exceptions when debugging tests
  * Symptoms: At the end of debugging a set of Google Test tests, Visual Studio catches exceptions of the type TODO
  * Reason: This seems to be due to a bug in *te.processhost.managed.exe*, to which VS attaches a debugger when debugging tests
  * Workaround 1: Select *Test/Test Settings/Default Processor Architecture/X64* - this lets VS use the older *vstest.executionengine.exe* which does not have this problem
  * Workaround 2: In VS, mark the exception as "Do not catch" when it occurs
* Not all test results are reported to VS
  * Symptoms: When running tests, GTA reports that n tests have been executed, but VS shows less test results in the test explorer
  * Reason: This seems to be due to inter process communication, but we do not have a lot of expertise in this area, and there is no official documentation of the VS test framework API.
  * Workaround: Configure *Report waiting time* at *Tools/Options/Google Test Adapter/Advanced*
* *Run tests after build* does not work
  * Symptoms: Selecting the *Run tests after build* option does not or not always result in the tests being run after build completion. Instead, the following error message is printed to the test console: "The specified type member 'Stale' is not supported in LINQ to Entities. Only initializers, entity members, and entity navigation properties are supported."
  * Reason: We have no idea
  * Workaround: None so far
  
  
### Building, testing, debugging

Google Test Adapter has been created using Visual Studio 2015 and Nuget, which are the only requirements for building GTA. Its main solution *Google Test Extension* consists of three projects:

* GoogleTestAdapter contains the actual adapter code
* GoogleTestAdapterVSIX adds the VS Options page and some resources
* GoogleTestAdapterTests contains GTA's tests

#### Executing the tests

Many of the tests depend on the second solution 'SampleGoogleTestTests', which contains a couple of Google Test tests. Before the tests contained in GoogleTestAdapterTests can be run, the second solution needs to be built in Debug mode for X86. Afterwards, the GTA tests can be run and should all pass.

For manually testing GTA, just start the GTA solution: A development instance of Visual Studio will be started with GTA installed. Use this instance to open the SampleGoogleTestTests solution (or any other solution containing Google Test tests).

#### Debugging GTA

Note that test discovery as well as test execution will be performed in processes different to the VS one. Therefore, to debug GTA's discovery and execution code, you need to manually attach a debugger to these processes. To support this, the class GoogleTestAdapter.Helpers.TestEnvironment contains a member variable DebugMode defaulting to false. Set this to true and start the VS development instance. As soon as your code is triggered, a dialog is opened and shows the id of the process the code is executed in. Switch to the other VS instance, attach a debugger to the according process, and click the OK button of the dialog to continue execution of the GTA code. Your breakpoints will now be hit.

#### Contributions

Pull requests are welcome and will be reviewed carefully. Please make sure to include tests demonstrating the bug you fixed or covering the added functionality.


### Credits

* Markus Lindqvist, author of Google Test Runner
* Matthew Manela, author of Chutzpah Test Adapter