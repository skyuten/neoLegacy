#Requires -Version 5.1
<#
.SYNOPSIS
    Builds zips for client and server, updates their Nightly GitHub releases, and archives locally.
.DESCRIPTION
    1. Fetches the latest commit hash.
    2. Zips client (x64\Release) and server (x64\Minecraft.Server\Release) builds into the archive folder.
    3. Updates the Nightly release: deletes old assets, uploads client zip + exe + pdb, updates title.
    4. Updates the Nightly-Dedicated-Server release: deletes old assets, uploads server zip, updates title.
.NOTES
    Requires GITHUB_TOKEN environment variable to be set.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Configuration ---
$RepoOwner       = "itsRevela"
$RepoName        = "MinecraftConsoles"
$ReleaseTag      = "Nightly"
$ReleaseDir      = Join-Path $PSScriptRoot "x64\Release"
$ZipName         = "LCEWindows64.zip"
$ServerReleaseTag  = "Nightly-Dedicated-Server"
$ServerReleaseDir  = Join-Path $PSScriptRoot "x64\Minecraft.Server\Release"
$ServerZipName     = "LCEServerWindows64.zip"
$ArchiveRoot     = "C:\Users\rexma\Documents\Minecraft\itsRevelaReleases"
$ApiBase         = "https://api.github.com/repos/$RepoOwner/$RepoName"

$Token = $env:GITHUB_TOKEN
if (-not $Token) {
    Write-Error "GITHUB_TOKEN environment variable is not set. Generate a token at https://github.com/settings/tokens with 'repo' scope."
    exit 1
}

$Headers = @{
    Authorization = "token $Token"
    Accept        = "application/vnd.github+json"
}

# --- Step 1: Get latest commit hash (needed for archive folder and title) ---
Write-Host "==> Fetching latest commit hash..." -ForegroundColor Cyan

$latestCommit = Invoke-RestMethod -Uri "$ApiBase/commits?per_page=1" -Headers $Headers -Method Get
$fullHash = $latestCommit[0].sha
$shortHash = $fullHash.Substring(0, 7)
Write-Host "    Latest commit: $shortHash"

# --- Step 2: Create archive folder and zip directly into it ---
Write-Host "==> Creating $ZipName in archive folder..." -ForegroundColor Cyan

$dateStr = (Get-Date).ToString("MM-dd-yyyy")
$archiveFolder = Join-Path $ArchiveRoot "${dateStr}_${shortHash}"

if (-not (Test-Path $archiveFolder)) {
    New-Item -ItemType Directory -Path $archiveFolder -Force | Out-Null
}

$ZipPath = Join-Path $archiveFolder $ZipName

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$topLevelFolder = "LCEWindows64"

$fs = [System.IO.File]::Open($ZipPath, [System.IO.FileMode]::Create)
try {
    $zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)

    try {
        $basePath = (Resolve-Path $ReleaseDir).Path

        # Add empty directories so they exist when extracted
        Get-ChildItem -Path $basePath -Recurse -Directory | ForEach-Object {
            $dirFullPath = $_.FullName
            $relativePath = $dirFullPath.Substring($basePath.Length).TrimStart('\','/')
            $entryName = ($topLevelFolder + "/" + ($relativePath -replace '\\','/') + "/")
            # Creating an entry with a trailing slash makes an empty directory in the zip
            $zip.CreateEntry($entryName) | Out-Null
        }

        Get-ChildItem -Path $basePath -Recurse -File | ForEach-Object {
            $fullPath = $_.FullName

            if ($_.Extension -ieq ".pch" -or $_.Extension -ieq ".zip") {
                return
            }

            $relativePath = $fullPath.Substring($basePath.Length).TrimStart('\','/')
            $entryName = ($topLevelFolder + "/" + ($relativePath -replace '\\','/'))

            Write-Host "    Adding: $entryName"

            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $fullPath,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }
}
finally {
    $fs.Dispose()
}

Write-Host "    Created: $ZipPath" -ForegroundColor Green

# --- Step 2b: Zip the server build into the same archive folder ---
Write-Host "==> Creating $ServerZipName in archive folder..." -ForegroundColor Cyan

$ServerZipPath = Join-Path $archiveFolder $ServerZipName

if (Test-Path $ServerZipPath) {
    Remove-Item $ServerZipPath -Force
}

$serverTopLevelFolder = "LCEServerWindows64"

$sfs = [System.IO.File]::Open($ServerZipPath, [System.IO.FileMode]::Create)
try {
    $szip = New-Object System.IO.Compression.ZipArchive($sfs, [System.IO.Compression.ZipArchiveMode]::Create)

    try {
        $serverBasePath = (Resolve-Path $ServerReleaseDir).Path

        # Add empty directories so they exist when extracted
        Get-ChildItem -Path $serverBasePath -Recurse -Directory | ForEach-Object {
            $dirFullPath = $_.FullName
            $relativePath = $dirFullPath.Substring($serverBasePath.Length).TrimStart('\','/')
            $entryName = ($serverTopLevelFolder + "/" + ($relativePath -replace '\\','/') + "/")
            $szip.CreateEntry($entryName) | Out-Null
        }

        Get-ChildItem -Path $serverBasePath -Recurse -File | ForEach-Object {
            $fullPath = $_.FullName

            if ($_.Extension -ieq ".pch" -or $_.Extension -ieq ".zip") {
                return
            }

            $relativePath = $fullPath.Substring($serverBasePath.Length).TrimStart('\','/')
            $entryName = ($serverTopLevelFolder + "/" + ($relativePath -replace '\\','/'))

            Write-Host "    Adding: $entryName"

            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $szip,
                $fullPath,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        $szip.Dispose()
    }
}
finally {
    $sfs.Dispose()
}

Write-Host "    Created: $ServerZipPath" -ForegroundColor Green

# --- Step 3: Get the Nightly release info ---
Write-Host "==> Fetching Nightly release info..." -ForegroundColor Cyan

$release = Invoke-RestMethod -Uri "$ApiBase/releases/tags/$ReleaseTag" -Headers $Headers -Method Get
$releaseId = $release.id
$currentTitle = $release.name
Write-Host "    Release ID: $releaseId"
Write-Host "    Current title: $currentTitle"

# --- Step 4: Delete existing assets ---
Write-Host "==> Deleting old assets..." -ForegroundColor Cyan

foreach ($asset in $release.assets) {
    Write-Host "    Deleting: $($asset.name) (ID: $($asset.id))"
    Invoke-RestMethod -Uri "$ApiBase/releases/assets/$($asset.id)" -Headers $Headers -Method Delete
}

# --- Step 5: Upload new assets ---
Write-Host "==> Uploading new assets..." -ForegroundColor Cyan

$uploadBase = "https://uploads.github.com/repos/$RepoOwner/$RepoName/releases/$releaseId/assets"

$filesToUpload = @(
    @{ Path = $ZipPath;                                         Name = $ZipName;                ContentType = "application/zip" }
    @{ Path = Join-Path $ReleaseDir "Minecraft.Client.exe";     Name = "Minecraft.Client.exe";  ContentType = "application/octet-stream" }
    @{ Path = Join-Path $ReleaseDir "Minecraft.Client.pdb";     Name = "Minecraft.Client.pdb";  ContentType = "application/octet-stream" }
)

foreach ($file in $filesToUpload) {
    $filePath = $file.Path
    if (-not (Test-Path $filePath)) {
        Write-Error "File not found: $filePath"
        exit 1
    }

    $uploadUrl = "$uploadBase`?name=$($file.Name)"
    $fileBytes = [System.IO.File]::ReadAllBytes($filePath)
    $sizeMB = [math]::Round($fileBytes.Length / 1MB, 2)
    Write-Host "    Uploading: $($file.Name) ($sizeMB MB)..."

    Invoke-RestMethod -Uri $uploadUrl -Headers @{
        Authorization = "token $Token"
        Accept        = "application/vnd.github+json"
        "Content-Type" = $file.ContentType
    } -Method Post -Body $fileBytes | Out-Null

    Write-Host "    Uploaded: $($file.Name)" -ForegroundColor Green
}

# --- Step 6: Update release title with latest commit hash ---
Write-Host "==> Updating release title..." -ForegroundColor Cyan

# Replace the old 7-char hash in the title with the new one
# Title format: "Latest:  8bd6690 (+Hardcore Mode)"
$newTitle = $currentTitle -replace '(?<=Client:\s{1,4})[0-9a-f]{7}', $shortHash

if ($newTitle -eq $currentTitle -and $currentTitle -notmatch $shortHash) {
    # Fallback if regex didn't match — just set a reasonable title
    $newTitle = "Latest:  $shortHash"
    Write-Host "    Warning: Could not parse existing title format, using fallback." -ForegroundColor Yellow
}

Write-Host "    New title: $newTitle"

$body = @{ name = $newTitle } | ConvertTo-Json
Invoke-RestMethod -Uri "$ApiBase/releases/$releaseId" -Headers $Headers -Method Patch -Body $body -ContentType "application/json" | Out-Null
Write-Host "    Title updated." -ForegroundColor Green

# --- Step 7: Get the Nightly-Dedicated-Server release info ---
Write-Host "==> Fetching Nightly-Dedicated-Server release info..." -ForegroundColor Cyan

$serverRelease = Invoke-RestMethod -Uri "$ApiBase/releases/tags/$ServerReleaseTag" -Headers $Headers -Method Get
$serverReleaseId = $serverRelease.id
$serverCurrentTitle = $serverRelease.name
Write-Host "    Release ID: $serverReleaseId"
Write-Host "    Current title: $serverCurrentTitle"

# --- Step 8: Delete existing server release assets ---
Write-Host "==> Deleting old server assets..." -ForegroundColor Cyan

foreach ($asset in $serverRelease.assets) {
    Write-Host "    Deleting: $($asset.name) (ID: $($asset.id))"
    Invoke-RestMethod -Uri "$ApiBase/releases/assets/$($asset.id)" -Headers $Headers -Method Delete
}

# --- Step 9: Upload server zip ---
Write-Host "==> Uploading server assets..." -ForegroundColor Cyan

$serverUploadBase = "https://uploads.github.com/repos/$RepoOwner/$RepoName/releases/$serverReleaseId/assets"

if (-not (Test-Path $ServerZipPath)) {
    Write-Error "File not found: $ServerZipPath"
    exit 1
}

$serverUploadUrl = "$serverUploadBase`?name=$ServerZipName"
$serverFileBytes = [System.IO.File]::ReadAllBytes($ServerZipPath)
$serverSizeMB = [math]::Round($serverFileBytes.Length / 1MB, 2)
Write-Host "    Uploading: $ServerZipName ($serverSizeMB MB)..."

Invoke-RestMethod -Uri $serverUploadUrl -Headers @{
    Authorization  = "token $Token"
    Accept         = "application/vnd.github+json"
    "Content-Type" = "application/zip"
} -Method Post -Body $serverFileBytes | Out-Null

Write-Host "    Uploaded: $ServerZipName" -ForegroundColor Green

# --- Step 10: Update server release title with latest commit hash ---
Write-Host "==> Updating server release title..." -ForegroundColor Cyan

$serverNewTitle = $serverCurrentTitle -replace '(?<=Server:\s{1,4})[0-9a-f]{7}', $shortHash

if ($serverNewTitle -eq $serverCurrentTitle -and $serverCurrentTitle -notmatch $shortHash) {
    $serverNewTitle = "Server: $shortHash"
    Write-Host "    Warning: Could not parse existing title format, using fallback." -ForegroundColor Yellow
}

Write-Host "    New title: $serverNewTitle"

$serverBody = @{ name = $serverNewTitle } | ConvertTo-Json
Invoke-RestMethod -Uri "$ApiBase/releases/$serverReleaseId" -Headers $Headers -Method Patch -Body $serverBody -ContentType "application/json" | Out-Null
Write-Host "    Title updated." -ForegroundColor Green

# --- Done ---
Write-Host ""
Write-Host "==> Nightly releases updated successfully!" -ForegroundColor Green
Write-Host "    Commit:         $shortHash"
Write-Host "    Client title:   $newTitle"
Write-Host "    Client assets:  $ZipName, Minecraft.Client.exe, Minecraft.Client.pdb"
Write-Host "    Server title:   $serverNewTitle"
Write-Host "    Server assets:  $ServerZipName"
Write-Host "    Archive:        $archiveFolder"
