# Developing this fork of Google Test Adapter

This document is the hand-over for developers and coding agents continuing work on this fork
([snargit/GoogleTestAdapter](https://github.com/snargit/GoogleTestAdapter)). It complements the
[README](README.md) (user documentation, including the list of the fork's changes) and the original project's
[Development Knowledge Base](https://github.com/csoltenborn/GoogleTestAdapter/wiki/Development-Knowledge-Base), which
describes the code base as it was before the fork.

## Status (October 2026)

* Versions **0.19.0** and **0.20.0** have been published as pre-releases on GitHub (see
  `GoogleTestAdapter/VsPackage.GTA/Resources/ReleaseNotes/`).
* Supported: Visual Studio 2017 to 2026, .NET Framework 4.6. Visual Studio 2013/2015 support has been dropped.
* Known gap: the end-to-end tests (`VsPackage.Tests`, `VsPackage.Tests.Generated`, category `End to end`) compare the
  complete console output of test runs with expected files, which have not been updated for the fork's changes (e.g.
  stack traces, standard output) and thus fail; they are not run by CI. Updating them requires reviewing each difference.
* Open ideas (no concrete use case yet):
  * Discovering test executables which are not outputs of the solution's projects (e.g. built by scripts or another
    build system, or downloaded from CI), which would require GTA's own `ITestContainerDiscoverer`.
  * Regex rules for grouping tests by naming conventions in Test Explorer (the remaining part of upstream
    [#210](https://github.com/csoltenborn/GoogleTestAdapter/issues/210); typed and parameterized test instances are
    already grouped automatically).
* Deliberately not pursued: an execution wrapper (no use case left: VS's code coverage works, Dr. Memory is
  abandoned), WSL/Linux test executables (GTA only handles Windows PE executables; VS's CMake/CTest integration covers
  WSL), upstream issues #102 (data-driven tests), #155 (tests in DLLs), and #328 (VS 2019 hang, not reproducible), and
  `ITestExecutor2.ShouldAttachToTestHost` (which would stop VS attaching its debugger to `testhost.exe`, but exists only
  since a VS 2019 update, while the test platform's assembly version is 15.0.0.0 since VS 2017: implementing it would
  break GTA in VS 2017 and early VS 2019).

## Repository layout

| Path | Contents |
|---|---|
| `GoogleTestAdapter/GoogleTestAdapter.sln` | The adapter: `Common`, `Core` (discovery, execution, settings), `DiaResolver` (PE and PDB parsing), `TestAdapter` (VSTest adapter), `VsPackage.Shared` (shared project with the VS package code), `VsPackage.GTA`, `Packaging.GTA` (VSIX and NuGet package), and the test projects `Core.Tests`, `DiaResolver.Tests`, `TestAdapter.Tests`, `VsPackage.GTA.Tests.Unit`, `Tests.Common` |
| `SampleTests/SampleTests.sln` | C++ sample test executables used by GTA's tests; many of their tests **fail, crash, or hang on purpose** |
| `GoogleTestNuGet/Build.ps1` | Builds the Google Test NuGet packages (`Microsoft.googletest.*`) into `GoogleTestAdapter/Packages/`; run by the build of `Packaging.GTA` if they are missing |
| `ThirdParty/googletest` | Submodule, Google Test 1.18.0 |
| `GoogleTestAdapter/Core/Resources/GTA_Traits_*.h` | Trait macros headers for Google Test 1.7.0 to 1.18.0; `GTA_Traits.md` describes how to create and verify them |
| `NuGetPackages/` | Restored NuGet packages (not under source control) |
| `out/` | Build output (not under source control) |

`TestAdapterFlavor.props` selects the flavor (`GTA` or Microsoft's `TAfGT`); this fork only builds and ships `GTA`.

## Building

Prerequisites: Visual Studio 2022 or 2026 (or the corresponding Build Tools) with the workloads *Visual Studio
extension development* and *Desktop development with C++* (the latter provides the DIA SDK and the C++ toolset for the
sample tests), and the .NET Framework 4.6+ developer pack (provides `TlbImp.exe`). No other preparation is needed:

* NuGet packages (`packages.config`) are restored by Visual Studio or by MSBuild's `-restore` (see below) into
  `NuGetPackages/` (configured in `NuGet.config`).
* `DiaResolver.csproj` copies `msdia140.dll` (x86, x64, and arm64) from the DIA SDK of the Visual Studio installation
  building it and generates the interop assembly `dia2.dll` from it (properties `DiaSdkDir` and `TlbImpPath` override
  the locations). Which `msdia140.dll` is shipped thus depends on the Visual Studio version which first builds the
  project; both work. `DiaFactory` loads the x86 DLL in 32-bit processes, and the x64 or the arm64 DLL in 64-bit
  processes, whichever loads (an x64 process, also when emulated on ARM64 Windows, can not load the arm64 DLL, and vice
  versa).
* The VSIX projects use the VS SDK build tools from NuGet (`Microsoft.VSSDK.BuildTools`, version defined in
  `Common.props`) rather than those of the Visual Studio installation, so builds do not depend on the Visual Studio
  version (VS 2026's installed `CreatePkgDef.exe` misses a dependency and fails).
* The .NET Framework 4.6 and 4.6.1 projects (GTA, and the samples `ClrDotNetLibProject` and `ClrLibProject`) build
  against the reference assemblies from NuGet (`Microsoft.NETFramework.ReferenceAssemblies.net46`/`net461`, restored
  by `Packaging.GTA/packages.config`, applied by the root `Directory.Build.targets`), since current Visual Studio
  installations and the GitHub runners do not have these targeting packs anymore (error MSB3644). Only if the packages
  are missing (e.g. `SampleTests.sln` built on its own on a fresh clone) are the installed targeting packs used.
* The VSIX contains the Google Test project and item templates (`ProjectTemplates/`, `ItemTemplates/`, zipped by
  `Packaging.GTA`), the template wizard (`NewProjectWizard`, whose VS assemblies come from NuGet and are not part of
  the VSIX), and the three Google Test NuGet packages the wizard chooses from (folder `Packages` of the VSIX, found by
  NuGet via `repositoryId` in the template's `WizardData`, which must be GTA's VSIX id). `Packaging.GTA` runs
  `GoogleTestNuGet/Build.ps1` if the packages are missing in `GoogleTestAdapter/Packages/` (a few minutes, i.e. on
  clean checkouts and in CI); delete them to rebuild them, e.g. after updating Google Test. Their binaries (x86, x64, and ARM64)
  are built with the v143 toolset, so that VS 2022 projects can link them; where its build tools are missing for a
  platform (GitHub's VS 2026 images), `Build.ps1` uses v145 instead and says so, unless environment variable
  `GOOGLETEST_PACKAGES_REQUIRE_TOOLSET` is set (the CI job building the v143 packages for releases sets it). Environment
  variable `GOOGLETEST_PACKAGES_TOOLSET` builds all binaries with the given toolset (v145 for the VS 2026 variants of
  the VSIX); packages containing binaries not built with v143 are only enabled for v145 projects (parameter
  `SupportsV143` of `googletest.targets.tt`). The templates have their
  own `TemplateID`s, so they do not replace Microsoft's templates if TAfGT is installed.
* Configuration `Any CPU` of `GoogleTestAdapter.sln` also builds the sample tests (project `SampleTestsBuilder` builds
  `SampleTests/SampleTests.sln` for Debug and Release, Win32 and x64); GTA's tests use the binaries in
  `out/binaries/SampleTests/`. The samples' intermediate files are separated by platform and Visual Studio version,
  since object files of different compiler versions must not be mixed (whole program optimization fails with internal
  compiler errors).
* `Common.props` sets `IsTestProject` to `false` for all projects in folder `GoogleTestAdapter/` whose names do not end
  with `.Tests`, `.Tests.Unit`, or `.Tests.Generated`, so Test Explorer ignores GTA itself and the helpers of its tests
  (otherwise, the installed GTA runs helpers such as `FakeGtestDllApplication` and reports errors). New test projects
  must follow this naming (or set `IsTestProject` to `true`), or Test Explorer silently ignores them.

**In Visual Studio:** open `GoogleTestAdapter/GoogleTestAdapter.sln` and build configuration `Release|Any CPU` (or
`Debug|Any CPU`). On a fresh clone, the first build might stop with "NuGet package Microsoft.VSSDK.BuildTools is
missing" since packages are restored after the projects have been loaded - just build again.

**On the command line and in CI pipelines** (headless; one command, nothing interactive):

```bash
# Git Bash; without unsetting the proxy variables, MSBuild, vstest.console, VSIXInstaller and devenv fail with
# "Item has already been added. Key in dictionary: 'https_proxy'" (variables differing only in case) - this only
# affects machines defining these variables in different cases
unset https_proxy http_proxy no_proxy HTTPS_PROXY HTTP_PROXY NO_PROXY
cd GoogleTestAdapter
"/c/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/MSBuild.exe" GoogleTestAdapter.sln \
    -restore -p:RestorePackagesConfig=true -p:Configuration=Release -p:Platform="Any CPU" -m -v:m -nologo
```

`-restore -p:RestorePackagesConfig=true` restores the `packages.config` packages and re-evaluates the projects before
building, so a clean checkout builds in a single invocation (verified with VS 2022's and VS 2026's MSBuild). On CI
runners, locate MSBuild with `vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`.
`build_preparation.bat` (which also restores the packages, but asks for license acceptance unless `APPVEYOR` is
defined) is no longer needed.

**GitHub Actions** (`.github/workflows/build.yml`) builds with VS 2022 (runner `windows-2022`), VS 2026 (runner
`windows-2025-vs2026`), and VS 2026 on ARM64 (runner `windows-11-vs2026-arm`, which runs the tests in an ARM64 process
with `/Platform:ARM64`; the x86 and x64 sample tests are emulated). Each job provides a variant of the VSIX
(see the comment of the workflow's matrix): the VS 2022 one for VS 2017-2026 with Google Test packages built with v143,
and the VS 2026 ones restricted to VS 2026 (step "Restrict VSIX to VS 2026") with packages built with v145; releases
contain all three, GTA's NuGet package, and the Google Test packages of both toolsets as zip files. The workflow runs
for pushes to `master`, pull requests, tags `v*`, and manually. It checks out the repository
including the `ThirdParty/googletest` submodule, stamps the version `<version of VsPackage.nuspec>.<run number>`
(`SetVersion.ps1`), builds with this command, uploads the VSIX (`GoogleTestAdapter-<version>.vsix`) and NuGet package as
artifact `packages-<vs2022|vs2026>`, and runs the tests of `DiaResolver.Tests`, `Core.Tests`, `TestAdapter.Tests`, and
`VsPackage.GTA.Tests.Unit` (ignoring a timing-sensitive test with `IgnoreTests.ps1`; the TRX results are artifact
`test-results-<vs2022|vs2026>`). For a tag `v<version>`, which must match the version of `VsPackage.nuspec`, it creates
a draft prerelease with the packages built with VS 2022 and the release notes `<version>.md`, using the workflow's own
token. On the command line, call batch files with an explicit path (`call .\SetVersion.bat`): some machines do not
search the current directory for commands.

**Strong name keys:** the assemblies are signed with `Keys/Key_Release.snk` if it exists (pre-build events of the
projects; the file is ignored by Git), and with `Keys/Key_Debug.snk` (public key token `028d073f99e04687`) otherwise;
`NewProjectWizard` always uses its own `Key.snk` (token `1924acebdd4c8a75`, referenced by the project template). The
fork's release key (token `a5acb60cd53bf87d`; the original project's key is not available) is kept by the maintainer
and is the repository secret `RELEASE_KEY` (base64 of the `.snk`, e.g. `base64 -w0 Key_Release.snk`), which the
workflow writes to `Keys/Key_Release.snk` for tags only. Strong names are identities, not trust: nothing references
GTA's public key, so the key can be replaced (e.g. `sn -k 2048 Key_Release.snk`) without other changes.

## Testing

```bash
cd out/binaries/GoogleTestAdapter/Release
"/c/Program Files/Microsoft Visual Studio/2022/Professional/Common7/IDE/Extensions/TestPlatform/vstest.console.exe" \
    Core.Tests/GoogleTestAdapter.Core.Tests.dll DiaResolver.Tests/GoogleTestAdapter.DiaResolver.Tests.dll \
    TestAdapter.Tests/GoogleTestAdapter.TestAdapter.Tests.dll \
    VsPackage.GTA.Tests.Unit/GoogleTestAdapter.VsPackage.GTA.Tests.Unit.dll //Parallel
```

* As of 0.20.0, all 701 tests pass. Run the suite more than once after changes affecting timing or files: the test
  assemblies run in parallel and share the sample binaries, so tests must never rename or modify sample files (one
  test renaming a sample's .pdb was flaky for exactly this reason).
* VS 2026's `vstest.console.exe` (`.../18/Professional/...`) runs the tests as well.
* For end-to-end checks, run sample executables through `vstest.console.exe <exe> /TestAdapterPath:<repo>\out\binaries\GoogleTestAdapter\Release\Packaging.GTA`,
  optionally with `/Settings:<file>` containing `<GoogleTestAdapterSettings>` (e.g. `<OutputMode>Debug</OutputMode>`,
  `<TimestampMode>PrintTimestamp</TimestampMode>`), `/TestCaseFilter:` or `/ListTests`.
* Discovery runs inside `vstest.console.exe` itself (not in a separate test host) for .NET Framework adapters.

## Releasing and installing locally

1. Version numbers live in `GoogleTestAdapter/Packaging.GTA/VsPackage.nuspec` (also the version of the CI builds),
   `GoogleTestAdapter/VsPackage.GTA/ReleaseNotes/History.cs` (version and date), and the release notes
   `GoogleTestAdapter/VsPackage.GTA/Resources/ReleaseNotes/<version>.md` (registered as `Content` in
   `VsPackage.GTA.csproj`; a test checks that every version in `History.cs` has release notes). Pushing tag
   `v<version>` makes the CI create a draft release (see [Building](#building)).
2. Stamp the version for the build, build, and revert the stamp:
   ```bash
   cd GoogleTestAdapter
   powershell -NoProfile -ExecutionPolicy Bypass -File SetVersion.ps1 0.20.0.0
   # build as above
   git checkout -- '*/Properties/AssemblyInfo.cs' Packaging.TAfGT/source.extension.vsixmanifest ProjectTemplates/Test/GoogleTest/GoogleTest.vstemplate
   sed -i 's#Version="0.20.0.0" Language#Version="0.1.0.0" Language#' Packaging.GTA/source.extension.vsixmanifest
   ```
   The GTA vsixmanifest has intentional changes, so only its version is reverted.
3. The VSIX and NuGet package are `out/binaries/GoogleTestAdapter/Release/Packaging.GTA/Packaging.GTA.vsix` and
   `GoogleTestAdapter.<version>.nupkg`.
4. Install into a VS instance (VS must be closed; find the instance id with `vswhere.exe -all`):
   ```bash
   ide="/c/Program Files/Microsoft Visual Studio/18/Professional/Common7/IDE"
   MSYS_NO_PATHCONV=1 "$ide/VSIXInstaller.exe" /quiet /instanceIds:<instance id> 'C:\path\to\Packaging.GTA.vsix'
   MSYS_NO_PATHCONV=1 "$ide/devenv.exe" /updateconfiguration
   ```
   `MSYS_NO_PATHCONV=1` keeps Git Bash from turning `/updateconfiguration` into a file path.

## Diagnosing problems in Visual Studio

* **ActivityLog:** start VS as `devenv.exe <solution> /log` - the solution must come **first**, since `/log` takes an
  optional file name and would otherwise overwrite the solution file with the log. The log
  (`%APPDATA%\Microsoft\VisualStudio\<version>_<instance id>\ActivityLog.xml`, UTF-16) is only written when VS exits
  normally. `SetSite failed` with hr `0x80131513` is a `MissingMethodException`.
* **Hangs:** dump the process with `procdump -ma <pid> <file>` and inspect managed stacks with
  `dotnet-dump analyze <file>` (`clrstack -all`, `clrmodules`; works for .NET Framework processes). Two dumps a few
  minutes apart show whether threads are stuck. Hangs of VS 2026 during solution load observed in October 2026 were VS
  issues (network drives, file change notifications), not GTA's.
* **VS internals:** `ilspycmd -p -o <dir> <assembly>` decompiles VS assemblies, e.g. Test Explorer's
  (`Common7/IDE/CommonExtensions/Microsoft/TestWindow`) or the code coverage extension
  (`.../AnalyzeCodeCoverage/Microsoft.CodeCoverage.VisualStudio.Extension.dll`); this is how the facts below were
  established.

## Design decisions and how Visual Studio works

* **VS package** (`VsPackage.Shared/GoogleTestExtensionOptionsPage.cs`): an `AsyncPackage` built against the VS 2017
  SDK (`Microsoft.VisualStudio.Shell.15.0`, referenced with `Private=False`, i.e. provided by VS - never ship VS SDK
  assemblies in the VSIX, VS 2026 redirects them to its own versions). It does not use DTE: VS 2026's managed DTE
  implementation is incompatible with both the old EnvDTE interop assemblies and embedded interop types. Solution and
  configuration are determined with `IVsSolution`/`IVsSolutionBuildManager`.
* **Run settings** (`VsPackage.Shared/Settings/RunSettingsService.cs`): Test Explorer calls this MEF component on
  background threads for every discovery and run; it must never wait for the UI thread.
  `VisualStudioConfigurationTracker` keeps solution and configuration up to date on the UI thread. Exceptions must not
  escape, since Test Explorer then drops all of GTA's settings.
* **When tests are discovered:** Test Explorer's `ProjectOutputContainerDiscoverer` turns every project output into a
  container (unless `IsTestProject` is `false`) and rediscovers containers whose timestamps changed after each build;
  GTA's discoverer (`[FileExtension(".exe")]`) decides which executables are Google Test executables
  (`GoogleTestDiscoverer.IsGoogleTestExecutable`: indicator file `<exe>.is_google_test`, *Test discovery regex*,
  imported Google Test DLLs, imported Google Test functions - undecorated with dbghelp's `UnDecorateSymbolName` -, or
  Google Test's help text for static linking).
* **Discovery** (`Core/TestCases/TestCaseFactory.cs`): runs `--gtest_list_tests --gtest_output=xml:<temp file>`.
  Locations come from the .pdb (`TestCaseResolver`, which also provides traits and namespaces; symbols are indexed,
  matching used to be quadratic in the number of tests) or, as fallback, from the XML file (Google Test >= 1.8.1). CTest
  test properties are cached per settings instance (one discovery or run).
* **Fixture failures** (sample `SampleTests/FixtureTests`): Google Test prints failures of `SetUpTestSuite()` between
  a suite's header and its first `[ RUN ]`, and failures of `TearDownTestSuite()` between the suite's last test and its
  footer (`... (N ms total)`); `StreamingStandardOutputTestResultParser` tracks suites accordingly (setup results are
  kept per suite, since a test's result is only created when the next test starts), and `XmlTestResultParser`
  evaluates the nameless test cases Google Test writes for such failures. The `<suite>.TearDownTestSuite` test cases
  are created by discovery for fixtures with a `TearDownTestSuite()`/`TearDownTestCase()` symbol (`TestCaseResolver`;
  an empty method folded with Google Test's default by `/OPT:ICF` gets the location of the suite's first test). They
  are marked by a third field of `TestCaseMetaDataProperty` (`IsExitCodeTestCase` means "no meta data"), are not
  counted in the meta data's numbers of tests, are attached to the command line running their suite (a suite is run
  completely if only its `TearDownTestSuite` test is selected), and are kept with their suite by
  `ParallelTestRunner`.
* **Test Explorer hierarchy:** `TestAdapter/DataConversionExtensions.cs` sets the `TestCase.Hierarchy` property
  (project, namespace, class, test group); instances of typed and parameterized tests share class and group.
* **Code coverage** (`VsPackage.Shared/Settings/CodeCoverageRunSettings.cs`): VS only instruments the test
  executables (native child processes of the test host) with dynamic native instrumentation, which is off by default,
  or if they are linked with `/PROFILE`. GTA enables it in coverage runs and excludes its own assemblies. This relies
  on GTA's run settings service being called after VS's coverage service, which was verified in VS 2026; MEF does not
  guarantee the order.
* **Debugger engines:** `VsPackage.Shared/Debugging/VsDebuggerAttacher.cs` attaches native, legacy mixed mode
  (.NET Framework), or native plus CoreCLR (.NET Core/.NET 5+) engines.

## Adding an option

An option is wired through all of these (search for an existing option such as `TestTimeoutInSeconds`):
`Core/Settings/IGoogleTestAdapterSettings.cs` (property and merging), `Core/Settings/RunSettings.cs` (property and
`ShouldSerialize...`), `Core/Settings/SettingsWrapper.cs` (name, description, default, property),
`TestAdapter/GoogleTestAdapterSettings.xsd`, the options page in `VsPackage.Shared/OptionsPages/`,
`VsPackage.Shared/GoogleTestExtensionOptionsPage.cs` (`GetRunSettingsFromOptionPages`), `Tests.Common/TestsBase.cs`
(mock default), the README, and the release notes. Placeholders such as `$(SolutionDir)` are replaced by
`Core/Settings/PlaceholderReplacer.cs`.

## Conventions and pitfalls

* Source files use CRLF line endings; C# files usually start with a UTF-8 BOM. Keep both when editing.
* Shell tools mangle backslashes in Windows paths and regexes (`sed`, inline Python strings): prefer editing files
  directly, and check the result.
* Commit messages explain why a change is made; changes made for upstream issues reference them
  (e.g. `(upstream #309)`).
* Changes are pushed to and tagged on the fork only with the maintainer's approval; nothing is sent to the original
  project (csoltenborn/GoogleTestAdapter) without an explicit request.
* Many tests of the sample solution fail on purpose (e.g. `Failing.*`, `*Fails`, `Crash*`, most `MessageParserTests`
  and `OutputHandling` tests); failures in Test Explorer are only a problem if GTA itself reports errors.
