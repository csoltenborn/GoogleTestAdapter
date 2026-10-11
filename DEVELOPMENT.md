# Developing this fork of Google Test Adapter

This document is the hand-over for developers and coding agents continuing work on this fork
([snargit/GoogleTestAdapter](https://github.com/snargit/GoogleTestAdapter)). It complements the
[README](README.md) (user documentation, including the list of the fork's changes) and the original project's
[Development Knowledge Base](https://github.com/csoltenborn/GoogleTestAdapter/wiki/Development-Knowledge-Base), which
describes the code base as it was before the fork.

## Status (October 2026)

* Version **0.19.0** is complete (see `GoogleTestAdapter/VsPackage.GTA/Resources/ReleaseNotes/0.19.0.md`), but has not
  been tagged or published yet.
* Supported: Visual Studio 2017 to 2026, .NET Framework 4.6. Visual Studio 2013/2015 support has been dropped.
* Open ideas (no concrete use case yet):
  * Discovering test executables which are not outputs of the solution's projects (e.g. built by scripts or another
    build system, or downloaded from CI), which would require GTA's own `ITestContainerDiscoverer`.
  * Regex rules for grouping tests by naming conventions in Test Explorer (the remaining part of upstream
    [#210](https://github.com/csoltenborn/GoogleTestAdapter/issues/210); typed and parameterized test instances are
    already grouped automatically).
* Deliberately not pursued: an execution wrapper (no use case left: VS's code coverage works, Dr. Memory is
  abandoned), WSL/Linux test executables (GTA only handles Windows PE executables; VS's CMake/CTest integration covers
  WSL), upstream issues #102 (data-driven tests), #155 (tests in DLLs), and #328 (VS 2019 hang, not reproducible).

## Repository layout

| Path | Contents |
|---|---|
| `GoogleTestAdapter/GoogleTestAdapter.sln` | The adapter: `Common`, `Core` (discovery, execution, settings), `DiaResolver` (PE and PDB parsing), `TestAdapter` (VSTest adapter), `VsPackage.Shared` (shared project with the VS package code), `VsPackage.GTA`, `Packaging.GTA` (VSIX and NuGet package), and the test projects `Core.Tests`, `DiaResolver.Tests`, `TestAdapter.Tests`, `VsPackage.GTA.Tests.Unit`, `Tests.Common` |
| `SampleTests/SampleTests.sln` | C++ sample test executables used by GTA's tests; many of their tests **fail, crash, or hang on purpose** |
| `GoogleTestNuGet/Build.ps1` | Builds the Google Test NuGet packages (`Microsoft.googletest.*`) |
| `ThirdParty/googletest` | Submodule, Google Test 1.18.0 |
| `GoogleTestAdapter/Core/Resources/GTA_Traits_*.h` | Trait macros headers for Google Test 1.7.0 to 1.18.0; `GTA_Traits.md` describes how to create and verify them |
| `NuGetPackages/` | Restored NuGet packages (not under source control) |
| `out/` | Build output (not under source control) |

`TestAdapterFlavor.props` selects the flavor (`GTA` or Microsoft's `TAfGT`); this fork only builds and ships `GTA`.

## Building

Prerequisites: Visual Studio 2022 or later with the VS SDK and C++ workloads (the DIA SDK ships with VS), NuGet. Run
`build_preparation.bat` once (sets the GTA flavor, restores NuGet packages, copies the DIA DLLs, generates `dia2.dll`).

```bash
# Git Bash; without unsetting the proxy variables, MSBuild, vstest.console, VSIXInstaller and devenv fail with
# "Item has already been added. Key in dictionary: 'https_proxy'" (variables differing only in case)
unset https_proxy http_proxy no_proxy HTTPS_PROXY HTTP_PROXY NO_PROXY
cd GoogleTestAdapter
"/c/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/MSBuild.exe" GoogleTestAdapter.sln \
    -p:Configuration=Release -p:Platform="Any CPU" -m -v:m -nologo
```

The sample tests (`SampleTests/SampleTests.sln`) must be built for Debug and Release, Win32 and x64 (the Any CPU
configurations build the CLR samples); GTA's tests use the binaries in `out/binaries/SampleTests/`.

## Testing

```bash
cd out/binaries/GoogleTestAdapter/Release
"/c/Program Files/Microsoft Visual Studio/2022/Professional/Common7/IDE/Extensions/TestPlatform/vstest.console.exe" \
    Core.Tests/GoogleTestAdapter.Core.Tests.dll DiaResolver.Tests/GoogleTestAdapter.DiaResolver.Tests.dll \
    TestAdapter.Tests/GoogleTestAdapter.TestAdapter.Tests.dll \
    VsPackage.GTA.Tests.Unit/GoogleTestAdapter.VsPackage.GTA.Tests.Unit.dll //Parallel
```

* As of 0.19.0, all 672 tests pass. Run the suite more than once after changes affecting timing or files: the test
  assemblies run in parallel and share the sample binaries, so tests must never rename or modify sample files (one
  test renaming a sample's .pdb was flaky for exactly this reason).
* For end-to-end checks, run sample executables through `vstest.console.exe <exe> /TestAdapterPath:<repo>\out\binaries\GoogleTestAdapter\Release\Packaging.GTA`,
  optionally with `/Settings:<file>` containing `<GoogleTestAdapterSettings>` (e.g. `<OutputMode>Debug</OutputMode>`,
  `<TimestampMode>PrintTimestamp</TimestampMode>`), `/TestCaseFilter:` or `/ListTests`.
* Discovery runs inside `vstest.console.exe` itself (not in a separate test host) for .NET Framework adapters.

## Releasing and installing locally

1. Version numbers live in `appveyor.yml`, `GoogleTestAdapter/Packaging.GTA/VsPackage.nuspec`,
   `GoogleTestAdapter/VsPackage.GTA/ReleaseNotes/History.cs` (version and date), and the release notes
   `GoogleTestAdapter/VsPackage.GTA/Resources/ReleaseNotes/<version>.md` (registered as `Content` in
   `VsPackage.GTA.csproj`; a test checks that every version in `History.cs` has release notes).
2. Stamp the version for the build, build, and revert the stamp:
   ```bash
   cd GoogleTestAdapter
   powershell -NoProfile -ExecutionPolicy Bypass -File SetVersion.ps1 0.19.0.0
   # build as above
   git checkout -- '*/Properties/AssemblyInfo.cs' Packaging.TAfGT/source.extension.vsixmanifest ProjectTemplates/Test/GoogleTest/GoogleTest.vstemplate
   sed -i 's#Version="0.19.0.0" Language#Version="0.1.0.0" Language#' Packaging.GTA/source.extension.vsixmanifest
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
