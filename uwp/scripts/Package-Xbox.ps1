param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot '..\Bin-Weevils-Rewritten-Xbox.sln'

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Installer or vswhere.exe was not found.'
}

$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild was not found. Install Visual Studio with the UWP development workload.'
}

Write-Host "Building $Configuration|$Platform..."
& $msbuild $solution /restore /m /p:Configuration=$Configuration /p:Platform=$Platform /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

Write-Host 'Build complete. Check uwp\AppPackages and the project bin directory.'
