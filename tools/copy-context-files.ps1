<#
.SYNOPSIS
Copies selected project files into a single bundle folder for sharing context.

.EXAMPLE
pwsh ./tools/copy-context-files.ps1 src/App.tsx src/components/Nav.tsx

.EXAMPLE
pwsh ./tools/copy-context-files.ps1 -DestinationRoot ./.codex/outputs/chatgpt-context -BundleName auth-discussion src/auth.ts README.md
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, ValueFromRemainingArguments = $true)]
    [string[]] $Files,

    [Parameter()]
    [string] $DestinationRoot = "./.codex/outputs/chatgpt-context",

    [Parameter()]
    [string] $BundleName = ("context-" + (Get-Date -Format "yyyyMMdd-HHmmss")),

    [Parameter()]
    [string] $BasePath = ".",

    [Parameter()]
    [switch] $Flat
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ExistingFile {
    param([string] $Path)

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $resolved) {
        throw "File not found: $Path"
    }

    $item = Get-Item -LiteralPath $resolved.ProviderPath
    if ($item.PSIsContainer) {
        throw "Expected a file but got a directory: $Path"
    }

    return $item.FullName
}

function Get-SafeRelativePath {
    param(
        [string] $FullPath,
        [string] $RootPath
    )

    $rootUri = [System.Uri]::new(($RootPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)) + [System.IO.Path]::DirectorySeparatorChar)
    $fileUri = [System.Uri]::new($FullPath)
    $relative = [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($fileUri).ToString())
    return $relative -replace "/", [System.IO.Path]::DirectorySeparatorChar
}

$resolvedBase = (Resolve-Path -LiteralPath $BasePath).ProviderPath
$resolvedDestinationRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DestinationRoot)
$bundlePath = Join-Path $resolvedDestinationRoot $BundleName
$filesPath = Join-Path $bundlePath "files"

New-Item -ItemType Directory -Path $filesPath -Force | Out-Null

$manifest = [System.Collections.Generic.List[object]]::new()
$usedFlatNames = @{}

foreach ($file in $Files) {
    $source = Resolve-ExistingFile -Path $file

    if ($Flat) {
        $name = Split-Path -Leaf $source
        if ($usedFlatNames.ContainsKey($name)) {
            $stem = [System.IO.Path]::GetFileNameWithoutExtension($name)
            $extension = [System.IO.Path]::GetExtension($name)
            $index = ++$usedFlatNames[$name]
            $name = "$stem-$index$extension"
        }
        else {
            $usedFlatNames[$name] = 1
        }

        $relativeTarget = $name
    }
    else {
        $relativeTarget = Get-SafeRelativePath -FullPath $source -RootPath $resolvedBase
        if ($relativeTarget.StartsWith("..")) {
            $drive = [System.IO.Path]::GetPathRoot($source).TrimEnd("\").Replace(":", "")
            $withoutRoot = $source.Substring([System.IO.Path]::GetPathRoot($source).Length)
            $relativeTarget = Join-Path "_outside-$drive" $withoutRoot
        }
    }

    $target = Join-Path $filesPath $relativeTarget
    $targetDirectory = Split-Path -Parent $target
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force

    $manifest.Add([pscustomobject]@{
        source = $source
        copiedTo = Get-SafeRelativePath -FullPath $target -RootPath $bundlePath
    })
}

$manifestPath = Join-Path $bundlePath "manifest.json"
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$zipPath = Join-Path $resolvedDestinationRoot "$BundleName.zip"
Compress-Archive -LiteralPath $bundlePath -DestinationPath $zipPath -Force
Remove-Item -LiteralPath $bundlePath -Recurse -Force

Write-Host "Copied $($manifest.Count) file(s)."
Write-Host "Zip: $zipPath"
