<#
.SYNOPSIS
    Publish IISWeb and copy it to a Windows / IIS host.

.DESCRIPTION
    Helper script intended to be run from a build machine that has
    .NET 8 SDK and the IIS management tools installed (so that
    Microsoft.Web.Administration.dll is reachable).

    The script:
      1. dotnet publish in Release / framework-dependent / win-x64
      2. Optionally robocopy the output to a target path
      3. Optionally stop the target App Pool, copy, then start it

.PARAMETER OutputDir
    Local publish directory (default: .\publish).

.PARAMETER Target
    UNC or local path of the IIS site folder, e.g. \\srv01\c$\inetpub\IISWeb

.PARAMETER AppPool
    Name of the IISWeb App Pool on the target server. If supplied AND a
    PSSession can reach the target, the pool is stopped before copy and
    started after.

.PARAMETER ComputerName
    Target machine name for remote App Pool stop/start. Required with -AppPool.

.EXAMPLE
    .\deploy.ps1 -Target C:\inetpub\IISWeb

.EXAMPLE
    .\deploy.ps1 -Target \\srv01\c$\inetpub\IISWeb -AppPool IISWeb-AppPool -ComputerName srv01
#>
[CmdletBinding()]
param(
    [string]$OutputDir   = (Join-Path $PSScriptRoot 'publish'),
    [string]$Target,
    [string]$AppPool,
    [string]$ComputerName
)

$ErrorActionPreference = 'Stop'

Write-Host "==> dotnet publish" -ForegroundColor Cyan
Push-Location $PSScriptRoot
try {
    dotnet publish IISWeb.csproj `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -o $OutputDir
} finally {
    Pop-Location
}

if (-not $Target) {
    Write-Host "Done. Output: $OutputDir" -ForegroundColor Green
    return
}

if ($AppPool -and $ComputerName) {
    Write-Host "==> Stopping App Pool '$AppPool' on $ComputerName" -ForegroundColor Cyan
    Invoke-Command -ComputerName $ComputerName -ScriptBlock {
        param($p)
        Import-Module WebAdministration
        if ((Get-WebAppPoolState -Name $p).Value -ne 'Stopped') {
            Stop-WebAppPool -Name $p
            Start-Sleep -Seconds 2
        }
    } -ArgumentList $AppPool
}

Write-Host "==> Robocopy to $Target" -ForegroundColor Cyan
$robocopyArgs = @(
    $OutputDir, $Target,
    '/MIR',
    '/XD', 'App_Data', 'logs',
    '/XF', 'appsettings.Development.json',
    '/R:2', '/W:2', '/NFL', '/NDL', '/NP'
)
& robocopy @robocopyArgs | Out-Null
# robocopy exits >=8 on errors; 0..7 are success-ish.
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}

if ($AppPool -and $ComputerName) {
    Write-Host "==> Starting App Pool '$AppPool' on $ComputerName" -ForegroundColor Cyan
    Invoke-Command -ComputerName $ComputerName -ScriptBlock {
        param($p)
        Import-Module WebAdministration
        Start-WebAppPool -Name $p
    } -ArgumentList $AppPool
}

Write-Host "==> Done" -ForegroundColor Green
