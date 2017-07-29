$gta_guids = @(
  "E6276CAD-E4C3-4B25-876A-65B265EBFF1A",
  "17F4B73F-E4D3-4E40-98FC-788B1D0F8225",
  "87F26371-0005-4301-9C49-A6DF4F06B81C",
  "4735D8CC-FA30-432D-854C-2984A7DA5DD2"
)

$sln = Get-Content .\GoogleTestAdapter\GoogleTestAdapter.sln
$is_gta_project = $false
$gta_guids_regex = [string]::Join('|', $gta_guids)

$sln | ForEach-Object {
  if ($is_gta_project) {
    if ($_ -like "EndProject") {
      $is_gta_project = $false
    }
  } elseif ($_ -match $gta_guids_regex) {
    if ($_ -like "Project*") {
      $is_gta_project = $true
    }
  } else {
    # Add the line to the new sln file if it isn't related to GTA projects
    $_
  }
} | Set-Content GoogleTestAdapter\GoogleTestAdapter.sln