Building the generated tests takes ages; therefore, the executable and pdb are checked in.

To rebuild:
* Execute T4 template `GoogleTestAdapter/SampleTestsBuilder/CppLoadTests.tt`
* Add resulting file `GoogleTestAdapter/SampleTestsBuilder/CppLoadTests.cpp` to project `SampleTests/LoadTests`
* Build project `SampleTests/LoadTests`