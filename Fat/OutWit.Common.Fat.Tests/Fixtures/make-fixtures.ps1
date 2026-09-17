<#
.SYNOPSIS
    Regenerates the reference FAT and exFAT images in WSL.

.PARAMETER Images
    Names of the images to rebuild; all of them when omitted.

.PARAMETER Distribution
    The WSL distribution with dosfstools, exfatprogs and mtools installed.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Images = @(),

    [string] $Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'make_fixtures.py'
$linuxPath = (& wsl.exe -d $Distribution -- wslpath -a ($script -replace '\\', '/')).Trim()
if ($LASTEXITCODE -ne 0 -or -not $linuxPath) {
    throw "Cannot reach WSL distribution '$Distribution'."
}

& wsl.exe -d $Distribution -- python3 $linuxPath @Images
if ($LASTEXITCODE -ne 0) {
    throw "make_fixtures.py failed with exit code $LASTEXITCODE."
}
