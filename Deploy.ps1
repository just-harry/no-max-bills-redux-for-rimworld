
[CmdletBinding()]
Param
(
	[Parameter()]
		[ValidateNotNull()]
			$AssemblyPathsForVersion = {Param ($V) @((Join-Path $PSScriptRoot '../../Assemblies' $V))},

	[Parameter()]
		[ValidateNotNull()]
			$LastestVersion = '1.6',

	[Parameter()]
		[ValidateNotNull()]
		[AllowEmptyCollection()]
			$OldVersions = @(),

	[Parameter()]
		[ValidateNotNull()]
			$BuildOptions = @{
				Configuration = 'Release'
			}
)

$Mod = & (Join-Path $PSScriptRoot 'ModMetadata.ps1')
$BuildPath = Join-Path $PSScriptRoot 'Build'
$ModPath = $PSScriptRoot
$BuiltModPath = Join-Path $BuildPath "$($Mod.Name)"

$OriginalBranch = git branch --show-current

function Build-Version ($Version)
{
	git checkout $Version

	& $PSScriptRoot/Build.ps1 -AssemblyPaths (& $AssemblyPathsForVersion $Version) @BuildOptions

	$VersionFolder = Join-Path $BuiltModPath $Version

	New-Item -Force -ItemType Directory -Path $VersionFolder
	Copy-Item -Recurse -Force -Path (Join-Path $BuildPath $Version "$($Mod.Name)/*") -Destination $VersionFolder

	$VersionFolder
}

try
{
	$LatestVersionFolder = (Build-Version $LastestVersion)[-1]
	Remove-Item -Recurse -Force -LiteralPath (Join-Path $BuiltModPath 'About') -ErrorAction Ignore
	Move-Item -Force -LiteralPath (Join-Path $LatestVersionFolder 'About') -Destination $BuiltModPath

	foreach ($Version in $OldVersions)
	{
		$VersionFolder = (Build-Version $Version)[-1]
		Remove-Item -Recurse -Force -LiteralPath (Join-Path $VersionFolder 'About')
	}
}
finally
{
	git checkout $OriginalBranch
}

