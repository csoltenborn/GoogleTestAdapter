# VSIX Module for AppVeyor by Mads Kristensen
[cmdletbinding()]
param()

$vsixUploadEndpoint = "http://vsixgallery.com/api/upload"
#$vsixUploadEndpoint = "http://localhost:7035/api/upload"

function Vsix-PushArtifacts {
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=0,ValueFromPipeline=$true)]
        [string]$path = "./*.vsix",

        [switch]$publishToGallery
    )
    process {
        foreach($filePath in $path) {
            $fileName = (Get-ChildItem $filePath -Recurse)[0] # Instead of taking the first, support multiple vsix files

            if (Get-Command Update-AppveyorBuild -errorAction SilentlyContinue)
            {
                Write-Host ("Pushing artifact " + $fileName.Name + "...") -ForegroundColor Cyan -NoNewline
                Push-AppveyorArtifact ($fileName.FullName) -FileName $fileName.Name -DeploymentName "Latest build"
                Write-Host "OK" -ForegroundColor Green
            }

            if ($publishToGallery -and $fileName)
            {
                Vsix-PublishToGallery $fileName.FullName
            }
        }
    }
}

function Vsix-PublishToGallery{
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=0,ValueFromPipeline=$true)]
        [string[]]$path = "./*.vsix"
    )
    foreach($filePath in $path){
        if ($env:APPVEYOR_PULL_REQUEST_NUMBER){
            return
        }

        $repo = ""
        $issueTracker = ""

        if ($env:APPVEYOR_REPO_PROVIDER -contains "github"){
            [Reflection.Assembly]::LoadWithPartialName("System.Web") | Out-Null
            $repo = [System.Web.HttpUtility]::UrlEncode(("https://github.com/" + $env:APPVEYOR_REPO_NAME + "/"))
            $issueTracker = [System.Web.HttpUtility]::UrlEncode(($repo + "issues/"))
        }

        'Publish to VSIX Gallery...' | Write-Host -ForegroundColor Cyan -NoNewline

        $fileName = (Get-ChildItem $filePath -Recurse)[0] # Instead of taking the first, support multiple vsix files

        [string]$url = ($vsixUploadEndpoint + "?repo=" + $repo + "&issuetracker=" + $issueTracker)
        [byte[]]$bytes = [System.IO.File]::ReadAllBytes($fileName)

        try {
            $response = Invoke-WebRequest $url -Method Post -Body $bytes
            'OK' | Write-Host -ForegroundColor Green
        }
        catch{
            'FAIL' | Write-Error
            $_.Exception.Response.Headers["x-error"] | Write-Error
        }
    }
}

function Vsix-UpdateBuildVersion {
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=1,ValueFromPipelineByPropertyName=$true)]
        [Version[]]$version,
        [Parameter(Position=1,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
        $vsixFilePath,
        [switch]$updateOnPullRequests
    )
    process{
        if ($updateOnPullRequests -or !$env:APPVEYOR_PULL_REQUEST_NUMBER){

            foreach($ver in $version) {
                if (Get-Command Update-AppveyorBuild -errorAction SilentlyContinue)
                {
                    Write-Host "Updating AppVeyor build version..." -ForegroundColor Cyan -NoNewline
                    Update-AppveyorBuild -Version $ver | Out-Null
                    $ver | Write-Host -ForegroundColor Green
                }
            }
        }

        $vsixFilePath
    }
}

function Vsix-IncrementVsixVersion {
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=0,ValueFromPipeline=$true)]
        [string[]]$manifestFilePath = ".\source.extension.vsixmanifest",

        [Parameter(Position=1, Mandatory=0)]
        [int]$buildNumber = $env:APPVEYOR_BUILD_NUMBER,

        [ValidateSet("build","revision")]
        [Parameter(Position=2, Mandatory=0)]
        [string]$versionType = "build",

        [switch]$updateBuildVersion
    )
    process {
        foreach($manifestFile in $manifestFilePath)
        {
            "Incrementing VSIX version..." | Write-Host  -ForegroundColor Cyan -NoNewline
            $matches = (Get-ChildItem $manifestFile -Recurse)
            $vsixManifest = $matches[$matches.Count - 1] # Get the last one which matches the top most file in the recursive matches
            [xml]$vsixXml = Get-Content $vsixManifest

            $ns = New-Object System.Xml.XmlNamespaceManager $vsixXml.NameTable
            $ns.AddNamespace("ns", $vsixXml.DocumentElement.NamespaceURI) | Out-Null

            $attrVersion = ""

            if ($vsixXml.SelectSingleNode("//ns:Identity", $ns)){ # VS2012 format
                $attrVersion = $vsixXml.SelectSingleNode("//ns:Identity", $ns).Attributes["Version"]
            }
            elseif ($vsixXml.SelectSingleNode("//ns:Version", $ns)){ # VS2010 format
                $attrVersion = $vsixXml.SelectSingleNode("//ns:Version", $ns)
            }

            [Version]$version = $attrVersion.Value

            if (!$attrVersion.Value){
                $version = $attrVersion.InnerText
            }

            if ($versionType -eq "build"){
                $version = New-Object Version ([int]$version.Major),([int]$version.Minor),$buildNumber
            }
            elseif ($versionType -eq "revision"){
                $version = New-Object Version ([int]$version.Major),([int]$version.Minor),([System.Math]::Max([int]$version.Build, 0)),$buildNumber
            }

            $attrVersion.InnerText = $version

            $vsixXml.Save($vsixManifest) | Out-Null

            $version.ToString() | Write-Host -ForegroundColor Green

            if ($updateBuildVersion -and $env:APPVEYOR_BUILD_VERSION -ne $version.ToString())
            {
                Vsix-UpdateBuildVersion $version | Out-Null
            }

            # return the values to the pipeline
            New-Object PSObject -Property @{
                'vsixFilePath' = $vsixManifest
                'Version' = $version
            }
        }
    }
}

function Vsix-IncrementNuspecVersion {
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=0)]
        [string[]]$nuspecFilePath = ".\**\*.nuspec",

        [Parameter(Position=1, Mandatory=0)]
        [Version]$buildVersion = $env:APPVEYOR_BUILD_VERSION
    )
    process {
        foreach($nuspecFile in $nuspecFilePath)
        {
            "Incrementing Nuspec version..." | Write-Host  -ForegroundColor Cyan -NoNewline
            $matches = (Get-ChildItem $nuspecFile -Recurse)
            $nuspec = $matches[$matches.Count - 1] # Get the last one which matches the top most file in the recursive matches
            [xml]$vsixXml = Get-Content $nuspec

            $ns = New-Object System.Xml.XmlNamespaceManager $vsixXml.NameTable
            $ns.AddNamespace("ns", $vsixXml.DocumentElement.NamespaceURI) | Out-Null

            $elmVersion =  $vsixXml.SelectSingleNode("//ns:version", $ns)

            $elmVersion.InnerText = $buildVersion

            $vsixXml.Save($nuspec) | Out-Null

            $buildVersion.ToString() | Write-Host -ForegroundColor Green

            # return the values to the pipeline
            New-Object PSObject -Property @{
                'vsixFilePath' = $nuspec
                'Version' = $version
            }
        }
    }
}

function Vsix-TokenReplacement {
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=$true)]
        [string]$FilePath,

        [Parameter(Position=1, Mandatory=$true)]
        [string]$searchString,

        [Parameter(Position=2, Mandatory=$true)]
        [string]$replacement
    )
    process {

        $replacement = $replacement.Replace("{version}",  $env:APPVEYOR_BUILD_VERSION)

        "Replacing $searchString with $replacement..." | Write-Host -ForegroundColor Cyan -NoNewline

        $content = [string]::join([environment]::newline, (get-content $FilePath))
        $regex = New-Object System.Text.RegularExpressions.Regex $searchString
        
        $regex.Replace($content, $replacement) | Out-File $FilePath

		"OK" | Write-Host -ForegroundColor Green
    }
}

function Vsix-CreateChocolatyPackage {
    [cmdletbinding()]
    param (
        [Parameter(Position=0, Mandatory=0)]
        [string[]]$manifestFilePath = ".\source.extension.vsixmanifest",

        [Parameter(Position=1, Mandatory=1)]
        [string]$packageId
    )
    process {
        
        if ([String]::IsNullOrEmpty($pacakgeId)){
            $error = New-Object System.ArgumentNullException "packageID is null or empty"
        }

        foreach($manifestFile in $manifestFilePath)
        {
            "Creating Cholocatey package..." | Write-Host  -ForegroundColor Cyan -NoNewline
            $matches = (Get-ChildItem $manifestFile -Recurse)
            $vsixManifest = $matches[$matches.Count - 1] # Get the last one which matches the top most file in the recursive matches
            [xml]$vsixXml = Get-Content $vsixManifest

            $ns = New-Object System.Xml.XmlNamespaceManager $vsixXml.NameTable
            $ns.AddNamespace("ns", $vsixXml.DocumentElement.NamespaceURI) | Out-Null

            $id = ""
            $version = ""
            $author = ""
            $displayName = ""
            $description = ""
            $tags = ""
            $icon = ""
            $preview = ""

            if ($vsixXml.SelectSingleNode("//ns:Identity", $ns)){ # VS2012 format
                $id = $vsixXml.SelectSingleNode("//ns:Identity", $ns).Attributes["Id"].Value
                $version = $vsixXml.SelectSingleNode("//ns:Identity", $ns).Attributes["Version"].Value
                $author = $vsixXml.SelectSingleNode("//ns:Identity", $ns).Attributes["Publisher"].Value
                $displayName = $vsixXml.SelectSingleNode("//ns:DisplayName", $ns).InnerText
                $description = $vsixXml.SelectSingleNode("//ns:Description", $ns).InnerText
                $tags = $vsixXml.SelectSingleNode("//ns:Tags", $ns).InnerText
                $Icon = $vsixXml.SelectSingleNode("//ns:Tags", $ns).InnerText
                $PreviewImage = $vsixXml.SelectSingleNode("//ns:Tags", $ns).InnerText
            }
            elseif ($vsixXml.SelectSingleNode("//ns:Version", $ns)){ # VS2010 format
                $id = $vsixXml.SelectSingleNode("//ns:Identity", $ns).Attributes["Id"].Value
                $version = $vsixXml.SelectSingleNode("//ns:Version", $ns).InnerText
                $author = $vsixXml.SelectSingleNode("//ns:Author", $ns).InnerText
                $displayName = $vsixXml.SelectSingleNode("//ns:Name", $ns).InnerText
                $description = $vsixXml.SelectSingleNode("//ns:Description", $ns).InnerText
                $tags = $vsixXml.SelectSingleNode("//ns:Tags", $ns).InnerText
                $Icon = $vsixXml.SelectSingleNode("//ns:Tags", $ns).InnerText
                $PreviewImage = $vsixXml.SelectSingleNode("//ns:Tags", $ns).InnerText
            }

            
            [System.IO.DirectoryInfo]$folder = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), ".vsixbuild", "$id")

            [System.IO.Directory]::CreateDirectory($folder.FullName) | Out-Null

            $XmlWriter = New-Object System.XMl.XmlTextWriter(($folder.FullName + "\chocolatey.nuspec"), (New-Object System.Text.UTF8Encoding))
            $xmlWriter.Formatting = "Indented"
            $xmlWriter.Indentation = "4"

            $xmlWriter.WriteStartDocument()
            $xmlWriter.WriteStartElement("package")
            $XmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd")

            $xmlWriter.WriteStartElement("metadata")
            $XmlWriter.WriteElementString("id", $packageId)
            $XmlWriter.WriteElementString("version", $version)
            $XmlWriter.WriteElementString("title", $displayName)
            $XmlWriter.WriteElementString("description", $description)
            $XmlWriter.WriteElementString("authors", $author)
            $XmlWriter.WriteElementString("owners", $author)
            $XmlWriter.WriteElementString("licenseUrl", "http://vsixgallery.com/extension/" + $id + "/")
            $XmlWriter.WriteElementString("projectUrl", "http://vsixgallery.com/extension/" + $id + "/")
            $XmlWriter.WriteElementString("iconUrl", "http://vsixgallery.com/extensions/" + $id + "/icon.png")
            $XmlWriter.WriteEndElement() # metadata

            $XmlWriter.WriteStartElement("files")
            $XmlWriter.WriteStartElement("file")
            $XmlWriter.WriteAttributeString("src", "chocolateyInstall.ps1")
            $XmlWriter.WriteAttributeString("target", "tools")
            $XmlWriter.WriteEndElement() # file
            $XmlWriter.WriteEndElement() # files

            $xmlWriter.WriteEndElement() # package
            $xmlWriter.WriteEndDocument()

            $XmlWriter.Flush()
            $XmlWriter.Dispose()

            $sb = New-Object System.Text.StringBuilder
            $sb.AppendLine("`$name = `'" + $displayName + "`'") | Out-Null
            $sb.AppendLine("`$url = `'" + "http://vsixgallery.com/extensions/" + $id + "/" + $displayName + ".vsix`'") | Out-Null
            $sb.AppendLine("Install-ChocolateyVsixPackage `$name `$url") | Out-Null

            
            New-Item ($folder.FullName + "\chocolateyInstall.ps1") -type file -force -value $sb.ToString() | Out-Null

            Push-Location $folder.FullName

            try{
                & choco pack | Out-Null

                Write-Host "OK" -ForegroundColor Green

                if (Get-Command Update-AppveyorBuild -errorAction SilentlyContinue)
                {
                    $nupkg = Get-ChildItem $folder.FullName -Filter *.nupkg
                    Write-Host ("Pushing Chocolatey package " + $nupkg.Name + "...") -ForegroundColor Cyan -NoNewline
                    Push-AppveyorArtifact ($nupkg.FullName) -FileName $nupkg.Name -DeploymentName "Chocolatey package"
                    Write-Host "OK" -ForegroundColor Green
                }
            }
            finally{
                Pop-Location
            }
        }
    }
} 