# ==============================================================================
# OMNICHANNEL BEAUTY SOLUTION - TAI ANH THUC TE CHINH XAC & DOI TEN THEO SAN PHAM
# Tu dong tai anh that tu mang, kiem tra Magic Bytes va doi ten trung khop ten san pham
# ==============================================================================

param (
    [string]$TargetDirectory = "Real_Products_Images"
)

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Continue"

$scriptDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($scriptDir)) {
    $scriptDir = (Get-Location).Path
}
if ((Split-Path $scriptDir -Leaf) -eq "scripts") {
    $workspaceRoot = Split-Path $scriptDir -Parent
} else {
    $workspaceRoot = $scriptDir
}

$fullTargetDir = Join-Path $workspaceRoot $TargetDirectory
if (-not (Test-Path $fullTargetDir)) {
    New-Item -ItemType Directory -Path $fullTargetDir -Force | Out-Null
}

$registryFile = Join-Path $workspaceRoot "Omnichannel.Infrastructure\Data\product_image_registry.json"
if (-not (Test-Path $registryFile)) {
    Write-Host "[ERROR] Khong tim thay tep registry tai: $registryFile" -ForegroundColor Red
    exit 1
}

$items = Get-Content $registryFile -Raw -Encoding UTF8 | ConvertFrom-Json
$products = $items | Where-Object { $_.categorySubfolder -like "products/*" }

Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "     OMNICHANNEL BEAUTY SOLUTION - REAL PRODUCT IMAGE DOWNLOADER & RENAMER       " -ForegroundColor Magenta
Write-Host "     Tai anh thuc te tu mang & Doi ten chuan xac tuong dong theo ten san pham     " -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "  -> Thu muc dich luu tru anh: $fullTargetDir" -ForegroundColor White
Write-Host "  -> Tong so san pham can tai va doi ten: $($products.Count) san pham." -ForegroundColor White
Write-Host ""

$reportList = @()
$successCount = 0
$failCount = 0
$index = 0

foreach ($p in $products) {
    $index++
    $targetFileName = $p.fileName
    $destinationPath = Join-Path $fullTargetDir $targetFileName
    $url = $p.directImageUrl
    $prodName = $p.name

    Write-Host ("[$index/$($products.Count)] " + $prodName) -ForegroundColor White
    Write-Host ("      URL goc   : " + $url) -ForegroundColor DarkGray
    Write-Host ("      Ten file  : " + $targetFileName) -ForegroundColor Cyan

    $downloaded = $false
    $retries = 3

    # Kiem tra neu da co trong thu muc products hoac tren dia va dung luong hop le
    $sourceInWeb = Join-Path $workspaceRoot ("Omnichannel.Web\wwwroot\images\" + $p.categorySubfolder + "\" + $targetFileName)
    if (Test-Path $sourceInWeb) {
        Copy-Item $sourceInWeb -Destination $destinationPath -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $destinationPath) {
        $existingBytes = [System.IO.File]::ReadAllBytes($destinationPath)
        if ($existingBytes.Length -ge 8192) {
            $downloaded = $true
            Write-Host "      [CACHED] Da xac thuc anh tren he thong ($([math]::Round($existingBytes.Length/1024, 1)) KB)." -ForegroundColor Green
        }
    }

    while (-not $downloaded -and $retries -gt 0) {
        try {
            $wc = New-Object System.Net.WebClient
            $wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36")
            $wc.DownloadFile($url, $destinationPath)
            $wc.Dispose()

            # Kiem tra Magic Bytes
            $bytes = [System.IO.File]::ReadAllBytes($destinationPath)
            if ($bytes.Length -ge 8192) {
                $isJpeg = ($bytes[0] -eq 0xFF -and $bytes[1] -eq 0xD8 -and $bytes[2] -eq 0xFF)
                $isPng  = ($bytes.Length -ge 8 -and $bytes[0] -eq 0x89 -and $bytes[1] -eq 0x50 -and $bytes[2] -eq 0x4E -and $bytes[3] -eq 0x47)
                $isWebp = ($bytes.Length -ge 12 -and $bytes[0] -eq 0x52 -and $bytes[1] -eq 0x49 -and $bytes[2] -eq 0x46 -and $bytes[3] -eq 0x46 -and $bytes[8] -eq 0x57 -and $bytes[9] -eq 0x45 -and $bytes[10] -eq 0x42 -and $bytes[11] -eq 0x50)

                if ($isJpeg -or $isPng -or $isWebp) {
                    $downloaded = $true
                    Write-Host "      [DOWNLOAD OK] Tai ve & doi ten thanh cong ($([math]::Round($bytes.Length/1024, 1)) KB)." -ForegroundColor Green
                } else {
                    Remove-Item $destinationPath -Force -ErrorAction SilentlyContinue
                    Write-Host "      [INVALID] Tep khong phai anh hop le (Magic Bytes sai)." -ForegroundColor Yellow
                }
            } else {
                Remove-Item $destinationPath -Force -ErrorAction SilentlyContinue
                Write-Host "      [TOO SMALL] Tep qua nho (< 8 KB)." -ForegroundColor Yellow
            }
        } catch {
            Write-Host ("      [RETRY] Loi: " + $_.Exception.Message) -ForegroundColor Yellow
            Remove-Item $destinationPath -Force -ErrorAction SilentlyContinue
        }

        if (-not $downloaded) {
            $retries--
            Start-Sleep -Milliseconds 800
        }
    }

    if ($downloaded) {
        $finalSize = (Get-Item $destinationPath).Length
        $successCount++
        $reportList += [PSCustomObject]@{
            No = $index
            SKU = $p.sku
            ProductName = $prodName
            RenamedFile = $targetFileName
            SizeKB = [math]::Round($finalSize / 1024, 1)
            Status = "SUCCESS"
        }
    } else {
        $failCount++
        $reportList += [PSCustomObject]@{
            No = $index
            SKU = $p.sku
            ProductName = $prodName
            RenamedFile = $targetFileName
            SizeKB = 0
            Status = "FAILED"
        }
    }
}

# Dong bo ban sao vao wwwroot/images/real_products
$webDir = Join-Path $workspaceRoot "Omnichannel.Web\wwwroot\images\real_products"
if (-not (Test-Path $webDir)) {
    New-Item -ItemType Directory -Path $webDir -Force | Out-Null
}
Copy-Item (Join-Path $fullTargetDir "*.*") -Destination $webDir -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "                         BANG TONG KET KET QUA                                   " -ForegroundColor Magenta
Write-Host "=================================================================================" -ForegroundColor Magenta
$reportList | Format-Table -AutoSize No, SKU, RenamedFile, SizeKB, Status | Out-String | Write-Host

$passRate = [math]::Round(($successCount / [math]::Max(1, $products.Count)) * 100, 1)
Write-Host "  Tong so san pham xu ly       : $($products.Count)" -ForegroundColor White
Write-Host "  Tai va doi ten thanh cong    : $successCount ($passRate%)" -ForegroundColor Green
Write-Host "  That bai                     : $failCount" -ForegroundColor $(if ($failCount -eq 0) { [ConsoleColor]::Green } else { [ConsoleColor]::Red })
Write-Host "  Thu muc luu tru chinh        : $fullTargetDir" -ForegroundColor Cyan
Write-Host "  Thu muc dong bo web          : $webDir" -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Magenta

if ($failCount -eq 0) {
    Write-Host "   *** HOAN TAT: 100% ANH THUC TE DA DUOC TAI VA DOI TEN CHUAN XAC TUONG DONG *** " -ForegroundColor Green
    exit 0
} else {
    Write-Host "   *** CO MOT SO ANH TAI THAT BAI, VUI LONG KIEM TRA ***" -ForegroundColor Red
    exit 1
}
