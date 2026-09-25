<#
  Sets up MyTools on this computer. Double-click Setup.cmd to run it.

    -Yes      answer yes to every question (for AI assistants and scripts)
    -Remove   take MyTools out of Civil 3D (your files stay where they are)

  Works in Windows PowerShell 5.1 and PowerShell 7. No admin rights needed.
#>
param(
    [switch]$Yes,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_NOLOGO = '1'
$repo = Split-Path -Parent $PSScriptRoot
$pluginFolder = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\MyTools.bundle'
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'

function Write-Step($text) { Write-Host ''; Write-Host $text -ForegroundColor Cyan }
function Write-Ok($text) { Write-Host "  OK  $text" -ForegroundColor Green }
function Write-Note($text) { Write-Host "      $text" }
function Write-Problem($text) { Write-Host "  !!  $text" -ForegroundColor Yellow }
function Stop-Setup($text) {
    Write-Host ''
    Write-Host "  Setup stopped: $text" -ForegroundColor Red
    exit 1
}
function Confirm-Step($question) {
    if ($Yes) { return $true }
    $answer = Read-Host "      $question [Y/n]"
    return ($answer -eq '' -or $answer -match '^(y|yes)$')
}

if ($Remove) {
    if (Test-Path $pluginFolder) {
        Remove-Item $pluginFolder -Recurse -Force
        Write-Ok 'MyTools is out of Civil 3D. Restart Civil 3D to finish.'
    } else {
        Write-Ok 'MyTools was not installed in Civil 3D.'
    }
    exit 0
}

Write-Host 'MyTools setup' -ForegroundColor Cyan
Write-Host "Folder: $repo"

# 1. Which Civil 3D -------------------------------------------------------------

Write-Step '1. Looking for Civil 3D'

$programFiles = $env:ProgramW6432
if (-not $programFiles) { $programFiles = $env:ProgramFiles }
$installed = @()
foreach ($y in 2027, 2026, 2025, 2024) {
    if (Test-Path (Join-Path $programFiles "Autodesk\AutoCAD $y\C3D")) { $installed += $y }
}

$props = Get-Content (Join-Path $repo 'Directory.Build.props') -Raw
$props = [regex]::Replace($props, '<!--.*?-->', '', 'Singleline')
$pinned = [regex]::Match($props, '<Civil3DYear>\s*(\d{4})\s*</Civil3DYear>')

if ($pinned.Success) {
    $year = [int]$pinned.Groups[1].Value
    Write-Ok "Using Civil 3D $year (set in Directory.Build.props)."
} elseif ($installed.Count -gt 0) {
    $year = $installed[0]
    Write-Ok ("Found Civil 3D " + ($installed -join ', ') + ". Using $year.")
    if ($installed.Count -gt 1) {
        Write-Note 'To use another one, put its year in Directory.Build.props and run setup again.'
    }
} else {
    $year = 2026
    Write-Problem 'No Civil 3D 2024 to 2027 found in Program Files. Building for 2026 anyway.'
    Write-Note 'If you use a different version, put its year in Directory.Build.props.'
}

# 2. .NET SDK -------------------------------------------------------------------

Write-Step '2. Checking the .NET SDK (free Microsoft tool that builds your commands)'

$needMajor = 8
if ($year -ge 2027) { $needMajor = 10 }

function Test-Sdk($exe) {
    if (-not $exe -or -not (Test-Path $exe)) { return $false }
    try { $sdks = & $exe --list-sdks 2>$null } catch { return $false }
    foreach ($line in $sdks) {
        if ($line -match '^(\d+)\.' -and [int]$Matches[1] -ge $needMajor) { return $true }
    }
    return $false
}

# scripts\build.cmd uses the copy in your user folder when there is one, unless
# build\use-system-dotnet exists. Setup writes that file when the user copy is too
# old but a machine-wide SDK (for example one IT installed) is new enough.
$useSystemMarker = Join-Path $repo 'build\use-system-dotnet'
$dotnet = $null
$pathDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (Test-Sdk $localDotnet) {
    $dotnet = $localDotnet
} elseif ($pathDotnet -and (Test-Sdk $pathDotnet.Source)) {
    $dotnet = $pathDotnet.Source
    if (Test-Path $localDotnet) {
        New-Item -ItemType Directory -Force -Path (Split-Path $useSystemMarker) | Out-Null
        Set-Content -Path $useSystemMarker -Value 'build.cmd uses the dotnet on PATH' -Encoding ASCII
    }
}

if (-not $dotnet) {
    Write-Note ".NET SDK $needMajor or newer isn't set up for your account yet."
    if (-not (Confirm-Step 'Download and install it now? It goes in your user folder, no admin needed.')) {
        Stop-Setup "install the .NET $needMajor SDK (or ask IT to), then run setup again."
    }
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        # Company proxies often want your Windows sign-in.
        if ([Net.WebRequest]::DefaultWebProxy) {
            [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultNetworkCredentials
        }
        $installer = Join-Path $env:TEMP 'dotnet-install.ps1'
        Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer -UseBasicParsing
        & $installer -Channel "$needMajor.0" -InstallDir (Split-Path $localDotnet) -NoPath -ProxyUseDefaultCredentials
    } catch {
        Stop-Setup ("the .NET SDK download failed (" + $_.Exception.Message + "). Ask IT to install the .NET $needMajor SDK, then run setup again.")
    }
    if (-not (Test-Sdk $localDotnet)) {
        Stop-Setup "the .NET SDK didn't install. Ask IT to install the .NET $needMajor SDK, then run setup again."
    }
    $dotnet = $localDotnet
}
if ($dotnet -eq $localDotnet -and (Test-Path $useSystemMarker)) { Remove-Item $useSystemMarker -Force }
Write-Ok ".NET SDK ready ($dotnet)"

# 3. Build ----------------------------------------------------------------------

Write-Step '3. Building your tools (the first time downloads Autodesk''s API files)'

& $dotnet build (Join-Path $repo 'MyTools.sln') -nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Stop-Setup 'the build failed (errors above). Open Claude in this folder and ask it to fix the build.'
}
$builtFor = (& $dotnet msbuild (Join-Path $repo 'src\MyTools\MyTools.csproj') -nologo -getProperty:Civil3DYear | Out-String).Trim()
if ($builtFor -ne "$year") {
    Stop-Setup "the build picked Civil 3D $builtFor but setup expected $year. Put <Civil3DYear>$year</Civil3DYear> in Directory.Build.props and run setup again."
}
Write-Ok "Built for Civil 3D $builtFor"

# 4. Add to Civil 3D ------------------------------------------------------------

Write-Step '4. Adding MyTools to Civil 3D'

$bundle = Join-Path $repo 'build\MyTools.bundle'
New-Item -ItemType Directory -Force -Path (Join-Path $pluginFolder 'Contents') | Out-Null
Copy-Item (Join-Path $bundle 'PackageContents.xml') $pluginFolder -Force
Copy-Item (Join-Path $bundle 'Contents\repo-path.txt') (Join-Path $pluginFolder 'Contents') -Force

$newLoader = Join-Path $bundle 'Contents\MyTools.Loader.dll'
$oldLoader = Join-Path $pluginFolder 'Contents\MyTools.Loader.dll'
$sameLoader = (Test-Path $oldLoader) -and ((Get-FileHash $oldLoader).Hash -eq (Get-FileHash $newLoader).Hash)
if (-not $sameLoader) {
    try {
        Copy-Item $newLoader $oldLoader -Force
    } catch {
        Stop-Setup 'Civil 3D is using the old loader. Close Civil 3D and run setup again.'
    }
}
Write-Ok "Installed in $pluginFolder"

# 5. Done -----------------------------------------------------------------------

Write-Step 'All set. Next:'
$restart = ''
if (Get-Process -Name acad -ErrorAction SilentlyContinue) { $restart = ' (close it and open it again)' }
Write-Note "1. Start Civil 3D$restart. If it asks about loading MyTools, choose Always Load."
Write-Note '2. Type MYTOOLS in Civil 3D to see your tools.'
Write-Note '3. Open the Claude app, go to Code, and pick this folder:'
Write-Note "   $repo"
Write-Note '   Then tell it what you want a command to do.'
if (-not (Get-Command claude -ErrorAction SilentlyContinue)) {
    Write-Note '   (No Claude app yet? Ask IT whether your company plan includes Claude Code.)'
}
exit 0
