# Notes for coding agents

Read [DEVELOPMENT.md](DEVELOPMENT.md) before making changes: it describes the project's status and open ideas, how to
build, test, release, and install GTA, how to diagnose problems in Visual Studio, and the design decisions and pitfalls
which are not obvious from the code.

The most important points:

* Unset the proxy environment variables before building or testing (see DEVELOPMENT.md).
* Run the complete test suite after changes, and keep it passing.
* Many sample tests fail on purpose.
* Don't push, tag, or contact the original project without the maintainer's approval.
