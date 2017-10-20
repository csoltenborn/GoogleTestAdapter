param(
	[Parameter(Mandatory=$true)]
	[ValidateSet("GTA","TAfGT")]
	[string] $flavor
)

$solutionFile = ".\GoogleTestAdapter\GoogleTestAdapter.sln"
$gta_guids = @(
  "E6276CAD-E4C3-4B25-876A-65B265EBFF1A",
  "17F4B73F-E4D3-4E40-98FC-788B1D0F8225",
  "87F26371-0005-4301-9C49-A6DF4F06B81C",
  "4735D8CC-FA30-432D-854C-2984A7DA5DD2"
)
$tafgt_guids = @(
	"55294B5F-A075-43F2-B0E9-2B11925E8B91",
	"9041BDED-FA1B-4C17-B7EA-7B750C470C23",
	"B3AEAD11-8EA3-4AB0-9DB0-643BFAAEB9B2",
	"483FE0C7-4E8D-4591-BE45-EAC6B2EA5F4F"
)

$guids = if ($flavor -eq "GTA") { $tafgt_guids } else { $gta_guids }
$guids_regex = [string]::Join('|', $guids)

$is_processing_project = $false
(Get-Content $solutionFile) | ForEach-Object {
  if ($is_processing_project) {
    if ($_ -like "EndProject") {
      $is_processing_project = $false
    }
  } elseif ($_ -match $guids_regex) {
    if ($_ -like "Project*") {
      $is_processing_project = $true
    }
  } else {
    # Add the line to the new sln file if it isn't related to one of our projects
    $_
  }
} | Set-Content $solutionFile