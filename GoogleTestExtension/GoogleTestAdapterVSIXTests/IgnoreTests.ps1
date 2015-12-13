[CmdletBinding()]
param (
	[parameter(Mandatory=$true)]
	[string[]] $tests
)


foreach ($test in $tests) {
	$data = $test -split "::"
	$testclass = $data[0]
	$testname = $data[1]

	$regex = '\[(.*TestMethod.*)\](\s*public\s+void\s+)' + $testname
	$replacement = '[$1,Ignore]$2' + $testname

	$testcode = [IO.File]::ReadAllText($testclass)
	$testcode -replace $regex, $replacement | Set-Content $testclass
}