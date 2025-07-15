
[CmdletBinding()]
Param
(
	[Parameter()]
		[ValidateNotNull()]
		[AllowEmptyCollection()]
			$AssemblyPaths = @((Join-Path $PSScriptRoot '../../Assemblies/1.6')),

	[Parameter()]
		[ValidateNotNull()]
			$Configuration = 'Debug'
)

$Mod = & (Join-Path $PSScriptRoot 'ModMetadata.ps1')
$Branch = git branch --show-current
$BuildPath = Join-Path $PSScriptRoot 'Build'
$ModPath = Join-Path $PSScriptRoot 'Mod'
$BuiltModPath = Join-Path $BuildPath "$Branch/$($Mod.Name)"
$BuildBinPath = Join-Path $BuildPath "bin/$Branch"
$BuildObjPath = Join-Path $BuildPath "obj/$Branch"

dotnet `
	build `
	"-c=$Configuration" `
	"-o=$BuildBinPath" `
	"-p:ObjPath=$BuildObjPath" `
	"-p:AssemblyPath=$([String]::Join(';', $AssemblyPaths))" `
	(Join-Path $ModPath "Source/$($Mod.Name)")

if ($LASTEXITCODE -ne 0)
{
	return
}

New-Item -ItemType Directory -Force -Path $BuiltModPath

Copy-Item -Recurse -Force -PassThru -Path (Join-Path $ModPath '*') -Destination $BuiltModPath

New-Item -ItemType Directory -Force -Path (Join-Path $BuiltModPath 'Assemblies')

Copy-Item -Force -PassThru -Path (Join-Path $BuildBinPath "$($Mod.Name).*") -Destination (Join-Path $BuiltModPath 'Assemblies')

Remove-Item -Recurse -Force -LiteralPath (Join-Path $BuiltModPath "Source/$($Mod.Name)/obj")

