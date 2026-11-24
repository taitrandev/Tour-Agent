<#
.SYNOPSIS
    Gom toan bo quy trinh Build & Test 4 du an Backend (.NET 10) vao 1 lenh duy nhat.
.DESCRIPTION
    Tu dong xac dinh duong dan Solution TourAgent.slnx, build toan bo src projects,
    phat sinh Swagger / api-index va thuc thi toan bo 132 unit/architecture tests.
.PARAMETER Filter
    Loc test theo ten (vi du: -Filter "TourTests")
.PARAMETER NoBuild
    Bo qua buoc build neu code vua moi build xong (-NoBuild)
.EXAMPLE
    pwsh scripts/test-backend.ps1
    pwsh scripts/test-backend.ps1 -Filter "SearchTours"
#>
param(
    [string]$Filter = "",
    [switch]$NoBuild = $false
)

$ErrorActionPreference = "Stop"

# Xac dinh thu muc root cua repository tu vi tri script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$slnPath = Join-Path $repoRoot "dotnet-backend-core-main\TourAgent.slnx"

if (-not (Test-Path $slnPath)) {
    Write-Error "Khong tim thay file solution tai: $slnPath"
    exit 1
}

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "🚀 Dang kiem tra toan dien Backend TourAgent (.NET 10)" -ForegroundColor Cyan
Write-Host "📂 Solution: $slnPath" -ForegroundColor DarkGray
Write-Host "=====================================================" -ForegroundColor Cyan

$argsList = @("test", $slnPath)

if ($NoBuild) {
    $argsList += "--no-build"
}

if ($Filter) {
    $argsList += "--filter", $Filter
}

$startTime = [System.DateTime]::Now
& dotnet @argsList
$exitCode = $LASTEXITCODE
$elapsed = [System.DateTime]::Now - $startTime

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host "✅ TAT CA TESTS PASSED (Thoi gian: $($elapsed.TotalSeconds.ToString('F2'))s)" -ForegroundColor Green
    Write-Host "=====================================================" -ForegroundColor Green
} else {
    Write-Host "=====================================================" -ForegroundColor Red
    Write-Host "❌ CO TEST THAT BAI HOAC BUILD ERROR (Exit Code: $exitCode)" -ForegroundColor Red
    Write-Host "=====================================================" -ForegroundColor Red
}

exit $exitCode

