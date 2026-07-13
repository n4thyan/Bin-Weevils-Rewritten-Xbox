param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('x64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$uwpRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $uwpRoot 'Bin-Weevils-Rewritten-Xbox.sln'
$assetScript = Join-Path $PSScriptRoot 'Generate-PlaceholderAssets.ps1'
$signingDir = Join-Path $uwpRoot 'signing'
$pfxPath = Join-Path $signingDir 'local-test-signing.pfx'
$cerPath = Join-Path $signingDir 'BinWeevilsRewrittenXbox.cer'

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Installer or vswhere.exe was not found.'
}

$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild was not found. Install Visual Studio with the Universal Windows Platform development workload.'
}

Write-Host 'Generating application assets...'
& $assetScript

New-Item -ItemType Directory -Force -Path $signingDir | Out-Null
$passwordText = [Guid]::NewGuid().ToString('N')
$password = ConvertTo-SecureString $passwordText -AsPlainText -Force

Write-Host 'Creating a temporary local test certificate...'
$certificate = New-SelfSignedCertificate `
    -Type Custom `
    -Subject 'CN=BinWeevilsRewrittenXbox' `
    -KeyUsage DigitalSignature `
    -FriendlyName 'Bin Weevils Rewritten Xbox Local Test Certificate' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}')

try {
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $password | Out-Null
    Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null

    Write-Host "Building signed $Configuration|$Platform APPX..."
    & $msbuild $solution `
        /restore `
        /m `
        /p:Configuration=$Configuration `
        /p:Platform=$Platform `
        /p:AppxBundle=Never `
        /p:UapAppxPackageBuildMode=SideloadOnly `
        /p:AppxPackageSigningEnabled=true `
        /p:PackageCertificateKeyFile=$pfxPath `
        /p:PackageCertificatePassword=$passwordText

    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item $pfxPath -Force -ErrorAction SilentlyContinue
    if ($certificate) {
        Remove-Item "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }
}

Write-Host 'Build complete.'
Write-Host "APPX output: $(Join-Path $uwpRoot 'AppPackages')"
Write-Host "Public test certificate: $cerPath"
