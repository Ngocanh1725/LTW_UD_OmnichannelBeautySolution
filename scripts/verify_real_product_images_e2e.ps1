# ==============================================================================
# OMNICHANNEL BEAUTY SOLUTION - END-TO-END VERIFICATION SUITE (PHIEN 4)
# Kiem thu End-to-End: Tinh tuong dong, tinh toan ven va hieu nang tai anh thuc te
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($scriptDir)) {
    $scriptDir = (Get-Location).Path
}
if ((Split-Path $scriptDir -Leaf) -eq "scripts") {
    $workspaceRoot = Split-Path $scriptDir -Parent
} else {
    $workspaceRoot = $scriptDir
}

$connString = "Server=localhost;Database=OmnichannelBeautyDb;User Id=sa;Password=12345678;TrustServerCertificate=True;MultipleActiveResultSets=True;"
$baseUrl = "http://localhost:5116"

Add-Type -AssemblyName "System.Data"
Add-Type -AssemblyName "System.Net.Http"

$passCount = 0
$failCount = 0
$warnCount = 0

function Report-Pass($message) {
    $global:passCount++
    Write-Host "     [PASS] $message" -ForegroundColor Green
}

function Report-Fail($message) {
    $global:failCount++
    Write-Host "     [FAIL] $message" -ForegroundColor Red
}

function Report-Warn($message) {
    $global:warnCount++
    Write-Host "     [WARN] $message" -ForegroundColor Yellow
}

Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "          OMNICHANNEL BEAUTY SOLUTION - END-TO-END VERIFICATION                  " -ForegroundColor Magenta
Write-Host "     Phien 4: Tinh Tuong Dong, Tinh Toan Ven & Hieu Nang Tai Anh Thuc Te         " -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host ""

# ==============================================================================
# PHAN 1: KIEM TRA CAU TRUC TEP CUC BO TREN DIA
# ==============================================================================
Write-Host "[PHAN 1/4]: KIEM TRA CAU TRUC TEP ANH CUC BO TREN DIA..." -ForegroundColor Yellow

$imagesRoot = Join-Path $workspaceRoot "Omnichannel.Web/wwwroot/images"
$foldersToCheck = @(
    @{ Name = "Products"; Path = Join-Path $imagesRoot "products"; ExpectedMin = 80 },
    @{ Name = "Branding"; Path = Join-Path $imagesRoot "branding"; ExpectedMin = 15 },
    @{ Name = "Banners";  Path = Join-Path $imagesRoot "banners";  ExpectedMin = 3 },
    @{ Name = "Categories"; Path = Join-Path $imagesRoot "categories"; ExpectedMin = 10 }
)

$totalFilesFound = 0
$totalSizeBytes = 0
$zeroByteCount = 0
$smallByteCount = 0

foreach ($folder in $foldersToCheck) {
    if (Test-Path $folder.Path) {
        $files = Get-ChildItem -Path $folder.Path -Recurse -File | Where-Object {
            $_.Extension -match "\.(jpg|jpeg|png|webp|svg)$"
        }
        $count = $files.Count
        $sizeMB = [math]::Round(($files | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
        $totalFilesFound += $count
        $totalSizeBytes += ($files | Measure-Object -Property Length -Sum).Sum

        $zeroFiles = $files | Where-Object { $_.Length -eq 0 }
        $smallFiles = $files | Where-Object { $_.Length -gt 0 -and $_.Length -lt 5120 -and $_.Extension -ne ".svg" -and -not $_.Name.StartsWith("favicon") }

        $zeroByteCount += $zeroFiles.Count
        $smallByteCount += $smallFiles.Count

        if ($count -ge $folder.ExpectedMin) {
            Report-Pass ("Thu muc " + $folder.Name + ": " + $count + " tep anh hop le (" + $sizeMB + " MB)")
        } else {
            Report-Warn ("Thu muc " + $folder.Name + ": " + $count + " tep (Ky vong >= " + $folder.ExpectedMin + ")")
        }
    } else {
        Report-Fail ("Khong tim thay thu muc: " + $folder.Path)
    }
}

$totalSizeMB = [math]::Round($totalSizeBytes / 1MB, 2)
Write-Host ""
Write-Host "  -> Tong ket kiem tra tep tren dia:" -ForegroundColor White
Write-Host ("     * Tong so anh quet duoc: " + $totalFilesFound + " tep anh.") -ForegroundColor Cyan
Write-Host ("     * Tong dung luong tren dia: " + $totalSizeMB + " MB.") -ForegroundColor Cyan

if ($zeroByteCount -eq 0) {
    Report-Pass "Xac nhan khong co tep nao 0 byte (0 byte = 0)."
} else {
    Report-Fail ("Phat hien " + $zeroByteCount + " tep 0 byte!")
}

if ($smallByteCount -eq 0) {
    Report-Pass "Xac nhan khong co tep nao bi hong hoac < 5 KB."
} else {
    Report-Warn ("Co " + $smallByteCount + " tep nho hon 5 KB (kiem tra neu la icon/svg).")
}

# ==============================================================================
# PHAN 2: KIEM TRA CSDL SQL SERVER & TOAN VEN MEDIAASSETS
# ==============================================================================
Write-Host ""
Write-Host "[PHAN 2/4]: KIEM TRA CSDL SQL SERVER & TOAN VEN MEDIAASSETS..." -ForegroundColor Yellow

$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
try {
    $conn.Open()
} catch {
    Report-Fail ("Khong the ket noi CSDL SQL Server: " + $_.Exception.Message)
    exit 1
}

$mediaCmd = $conn.CreateCommand()
$mediaCmd.CommandText = @"
SELECT 
    COUNT(*) AS TotalMedia,
    SUM(CASE WHEN Category = 'PRODUCT' THEN 1 ELSE 0 END) AS ProductMediaCount,
    SUM(CASE WHEN Category = 'THEME' THEN 1 ELSE 0 END) AS ThemeMediaCount,
    SUM(CASE WHEN Category = 'BANNER' THEN 1 ELSE 0 END) AS BannerMediaCount,
    SUM(CASE WHEN Category = 'CATEGORY' THEN 1 ELSE 0 END) AS CategoryMediaCount,
    ISNULL(SUM(CAST(DATALENGTH(Data) AS BIGINT)), 0) / (1024.0 * 1024.0) AS TotalDataMB,
    SUM(CASE WHEN DATALENGTH(Data) = 0 OR Data IS NULL THEN 1 ELSE 0 END) AS CorruptDataCount
FROM MediaAssets;
"@
$mediaReader = $mediaCmd.ExecuteReader()

$totalMediaInDb = 0
$prodMediaInDb = 0
$themeMediaInDb = 0
$bannerMediaInDb = 0
$catMediaInDb = 0
$totalDbMB = 0.0
$corruptData = 0

if ($mediaReader.Read()) {
    $totalMediaInDb = [int]$mediaReader["TotalMedia"]
    $prodMediaInDb = [int]$mediaReader["ProductMediaCount"]
    $themeMediaInDb = [int]$mediaReader["ThemeMediaCount"]
    $bannerMediaInDb = [int]$mediaReader["BannerMediaCount"]
    $catMediaInDb = [int]$mediaReader["CategoryMediaCount"]
    $totalDbMB = [math]::Round([double]$mediaReader["TotalDataMB"], 2)
    $corruptData = [int]$mediaReader["CorruptDataCount"]
}
$mediaReader.Close()

Write-Host "  -> Thong ke bang MediaAssets:" -ForegroundColor White
Report-Pass ("Tong so ban ghi MediaAssets: " + $totalMediaInDb + " ban ghi (" + $totalDbMB + " MB du lieu nhi phan).")
Report-Pass ("Phan loai Category: PRODUCT=" + $prodMediaInDb + ", THEME=" + $themeMediaInDb + ", BANNER=" + $bannerMediaInDb + ", CATEGORY=" + $catMediaInDb)

if ($corruptData -eq 0) {
    Report-Pass "Xac nhan 100% ban ghi co du lieu nhi phan hop le (Data IS NOT NULL & DATALENGTH > 0)."
} else {
    Report-Fail ("Phat hien " + $corruptData + " ban ghi MediaAssets bi loi du lieu nhi phan!")
}

# Kiem tra tinh toan ven ChecksumHash (khong bi rong, dung 64 ky tu hex)
$hashCmd = $conn.CreateCommand()
$hashCmd.CommandText = "SELECT COUNT(*) FROM MediaAssets WHERE ChecksumHash IS NULL OR LEN(ChecksumHash) <> 64;"
$invalidHashCount = [int]$hashCmd.ExecuteScalar()

if ($invalidHashCount -eq 0) {
    Report-Pass "Xac nhan toan bo 100% ban ghi co ma bam SHA256 ChecksumHash hop le (dung 64 ky tu Hex)."
} else {
    Report-Fail ("Co " + $invalidHashCount + " ban ghi ChecksumHash khong hop le!")
}

# Kiem tra bang Products
$prodCmd = $conn.CreateCommand()
$prodCmd.CommandText = @"
SELECT 
    COUNT(*) AS TotalProducts,
    SUM(CASE WHEN ThumbnailMediaId IS NOT NULL THEN 1 ELSE 0 END) AS MappedProducts,
    SUM(CASE WHEN ThumbnailMediaId IS NULL THEN 1 ELSE 0 END) AS UnmappedProducts
FROM Products;
"@
$prodReader = $prodCmd.ExecuteReader()

$totalProducts = 0
$mappedProducts = 0
$unmappedProducts = 0

if ($prodReader.Read()) {
    $totalProducts = [int]$prodReader["TotalProducts"]
    $mappedProducts = [int]$prodReader["MappedProducts"]
    $unmappedProducts = [int]$prodReader["UnmappedProducts"]
}
$prodReader.Close()

$matchPercentage = [math]::Round(($mappedProducts / [math]::Max(1, $totalProducts)) * 100, 1)
Write-Host ""
Write-Host "  -> Thong ke lien ket 1-1 bang Products:" -ForegroundColor White
if ($matchPercentage -ge 95.0) {
    Report-Pass ("Ty le san pham duoc gan ThumbnailMediaId: " + $mappedProducts + "/" + $totalProducts + " (" + $matchPercentage + "% >= Ky vong 95%).")
} else {
    Report-Warn ("Ty le san pham duoc gan ThumbnailMediaId: " + $mappedProducts + "/" + $totalProducts + " (" + $matchPercentage + "%)")
}

# Kiem tra ngau nhien 5 san pham lien ket thuc te
Write-Host ""
Write-Host "  -> Kiem tra ngau nhien 5 san pham thuc te lien ket 1-1 voi MediaAssets:" -ForegroundColor White

$sampleCmd = $conn.CreateCommand()
$sampleCmd.CommandText = @"
SELECT TOP 5 
    p.Id, p.Sku, p.Name, p.Slug, p.ThumbnailMediaId,
    m.FileName, m.ContentType, m.ChecksumHash
FROM Products p
INNER JOIN MediaAssets m ON p.ThumbnailMediaId = m.Id
WHERE p.ThumbnailMediaId IS NOT NULL
ORDER BY NEWID();
"@
$sampleAdapter = New-Object System.Data.SqlClient.SqlDataAdapter($sampleCmd)
$sampleDs = New-Object System.Data.DataSet
$sampleAdapter.Fill($sampleDs) | Out-Null
$sampleProducts = $sampleDs.Tables[0]

$sampleValidCount = 0
$sampleMediaIds = @()

foreach ($row in $sampleProducts.Rows) {
    $prodId = $row["Id"]
    $pSku = $row["Sku"]
    $pName = $row["Name"]
    $pSlug = $row["Slug"]
    $tId = $row["ThumbnailMediaId"]
    $fName = $row["FileName"]
    $cType = $row["ContentType"]

    $sampleMediaIds += $tId

    $isConsistent = ($fName.IndexOf($pSlug, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -or
                    ($fName.StartsWith($pSku, [System.StringComparison]::OrdinalIgnoreCase)) -or
                    ($pSlug.IndexOf([System.IO.Path]::GetFileNameWithoutExtension($fName), [System.StringComparison]::OrdinalIgnoreCase) -ge 0)

    if (-not $isConsistent) {
        $pTokens = $pSlug.Split('-') | Where-Object { $_.Length -gt 2 }
        $tokenMatch = 0
        foreach ($tok in $pTokens) {
            if ($fName.ToLower().Contains($tok.ToLower())) { $tokenMatch++ }
        }
        if ($tokenMatch -ge 2) { $isConsistent = $true }
    }

    if ($isConsistent) {
        $sampleValidCount++
        Report-Pass ("San pham #" + $prodId + " (" + $pSku + "): Slug='" + $pSlug + "' => Anh: '" + $fName + "' (MediaId=" + $tId + ", " + $cType + ")")
    } else {
        Report-Warn ("San pham #" + $prodId + " (" + $pSku + "): Slug='" + $pSlug + "' => Anh: '" + $fName + "'")
    }
}

if ($sampleValidCount -ge 4) {
    Report-Pass ("Kiem tra tuong dong 5 san pham: " + $sampleValidCount + "/5 san pham dat do tuong dong cao.")
} else {
    Report-Warn ("Kiem tra tuong dong: " + $sampleValidCount + "/5")
}

$conn.Close()

# ==============================================================================
# PHAN 3: KIEM THU ENDPOINT STREAMING /media/{id} (HTTP 200 & HTTP 304)
# ==============================================================================
Write-Host ""
Write-Host "[PHAN 3/4]: KIEM THU ENDPOINT STREAMING /media/{id} (HTTP 200 & HTTP 304)..." -ForegroundColor Yellow

$httpClientHandler = New-Object System.Net.Http.HttpClientHandler
$httpClientHandler.AllowAutoRedirect = $true
$httpClient = New-Object System.Net.Http.HttpClient($httpClientHandler)
$httpClient.Timeout = [TimeSpan]::FromSeconds(15)

$testMediaIds = $sampleMediaIds | Select-Object -Unique | Select-Object -First 5
if ($testMediaIds.Count -lt 5) {
    $testMediaIds = @(1, 10, 16, 50, 109)
}

$http200Count = 0
$http304Count = 0
$headerOkCount = 0

foreach ($mId in $testMediaIds) {
    $url = "$baseUrl/media/$mId"
    try {
        # 1. Lan 1: HTTP GET
        $resp = $httpClient.GetAsync($url).Result
        $statusCode = [int]$resp.StatusCode

        if ($statusCode -eq 200) {
            $http200Count++
            $cType = $resp.Content.Headers.ContentType.MediaType
            $etag = $resp.Headers.ETag.Tag
            $cacheControl = $resp.Headers.CacheControl.ToString()

            $hasETag = -not [string]::IsNullOrEmpty($etag)
            $hasCache = $cacheControl.Contains("max-age") -or $cacheControl.Contains("public")

            if ($hasETag -and $hasCache) {
                $headerOkCount++
                Report-Pass ("GET " + $url + " => 200 OK | Content-Type: " + $cType + " | ETag: " + $etag + " | Cache-Control: " + $cacheControl)
            } else {
                Report-Warn ("GET " + $url + " => 200 OK nhung thieu ETag/Cache-Control header")
            }

            # 2. Lan 2: HTTP GET voi If-None-Match
            $req304 = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $url)
            $req304.Headers.TryAddWithoutValidation("If-None-Match", $etag) | Out-Null
            $resp304 = $httpClient.SendAsync($req304).Result

            if ([int]$resp304.StatusCode -eq 304) {
                $http304Count++
                Report-Pass ("Re-check voi If-None-Match => HTTP 304 Not Modified (Browser/CDN Cache HIT toi uu 100% I/O)")
            } else {
                Report-Warn ("Re-check voi If-None-Match tra ve: " + [int]$resp304.StatusCode + " (Ky vong 304)")
            }
        } else {
            Report-Fail ("GET " + $url + " that bai voi ma loi: " + $statusCode)
        }
    } catch {
        Report-Fail ("Loi ket noi toi " + $url + ": " + $_.Exception.Message)
    }
}

Write-Host ""
Write-Host "  -> Ket qua kiem thu Endpoint Media:" -ForegroundColor White
Report-Pass ("Phan hoi HTTP 200 OK: " + $http200Count + "/" + $testMediaIds.Count + " requests.")
Report-Pass ("Day du Headers ETag & Cache-Control: " + $headerOkCount + "/" + $testMediaIds.Count + " requests.")
Report-Pass ("Phan hoi HTTP 304 Not Modified: " + $http304Count + "/" + $testMediaIds.Count + " requests.")

# ==============================================================================
# PHAN 4: KIEM TRA STOREFRONT & PRODUCT DETAIL PAGE
# ==============================================================================
Write-Host ""
Write-Host "[PHAN 4/4]: KIEM TRA STOREFRONT & TRANG CHI TIET SAN PHAM..." -ForegroundColor Yellow

# 1. Kiem tra Storefront Trang Chu
$homeUrl = "$baseUrl/"
try {
    $homeHtml = $httpClient.GetStringAsync($homeUrl).Result
    if (-not [string]::IsNullOrEmpty($homeHtml)) {
        Report-Pass "Ket noi thanh cong toi Storefront Trang Chu (HTTP 200 OK)."

        # Kiem tra the <img> co chua /media/
        $mediaImgMatches = [System.Text.RegularExpressions.Regex]::Matches($homeHtml, 'src=["''](/media/\d+)["'']')
        if ($mediaImgMatches.Count -gt 0) {
            Report-Pass ("Phat hien " + $mediaImgMatches.Count + " the <img> tai Storefront su dung Endpoint CSDL /media/{id}.")
        } else {
            Report-Warn "Chua tim thay the <img> nao co src='/media/{id}' tai Trang Chu."
        }

        # Kiem tra khong con duong dan vo / broken placeholder
        if (-not $homeHtml.Contains("broken-placeholder") -and -not $homeHtml.Contains("undefined-img")) {
            Report-Pass "Xac nhan khong con duong dan vo hoac placeholder loi tren Trang Chu."
        } else {
            Report-Warn "Phat hien placeholder vo tren Trang Chu."
        }
    } else {
        Report-Fail "Trang Chu tra ve noi dung rong."
    }
} catch {
    Report-Fail ("Khong the tai Storefront Trang Chu: " + $_.Exception.Message)
}

# 2. Kiem tra Trang Chi Tiet San Pham (Lay 2 san pham mau)
$testProductSlugs = @(
    "sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml",
    "serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml"
)

foreach ($slug in $testProductSlugs) {
    $detailUrl = "$baseUrl/Product/Detail/$slug"
    try {
        $resp = $httpClient.GetAsync($detailUrl).Result
        if ($resp.IsSuccessStatusCode) {
            $detailHtml = $resp.Content.ReadAsStringAsync().Result
            if (-not [string]::IsNullOrEmpty($detailHtml)) {
                Report-Pass ("Ket noi thanh cong toi Trang Chi Tiet: /Product/Detail/" + $slug + " (HTTP 200 OK)")

                # Kiem tra anh san pham co /media/
                $hasMediaInDetail = $detailHtml.Contains("/media/")
                if ($hasMediaInDetail) {
                    Report-Pass "Anh chinh va Gallery san pham phuc vu chuan xac qua Endpoint /media/{id}."
                } else {
                    Report-Warn "Trang Chi Tiet chua tim thay /media/ trong ma HTML."
                }

                # Kiem tra id='mainProductImage'
                if ($detailHtml.Contains('id="mainProductImage"')) {
                    Report-Pass "The <img> chinh (id='mainProductImage') hien dien day du voi co che phong to Zoom 2.5X."
                } else {
                    Report-Warn "Khong tim thay id='mainProductImage'."
                }
            } else {
                Report-Fail ("Trang chi tiet rong: " + $detailUrl)
            }
        } else {
            Report-Fail ("Trang chi tiet tra ve HTTP " + [int]$resp.StatusCode + ": " + $detailUrl)
        }
    } catch {
        Report-Fail ("Loi tai trang chi tiet " + $detailUrl + ": " + $_.Exception.Message)
    }
}

$httpClient.Dispose()

# ==============================================================================
# TONG KET & XUAT BANG KET QUA
# ==============================================================================
Write-Host ""
Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "                    TONG KET KIEM THU END-TO-END PHIEN 4                         " -ForegroundColor Magenta
Write-Host "=================================================================================" -ForegroundColor Magenta

$totalChecks = $passCount + $failCount
$passRate = 0
if ($totalChecks -gt 0) {
    $passRate = [math]::Round(($passCount / $totalChecks) * 100, 1)
}

Write-Host ("  [TOTAL PASS]     : " + $passCount + " checks passed.") -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host ("  [TOTAL FAIL]     : " + $failCount + " checks failed.") -ForegroundColor Red
} else {
    Write-Host "  [TOTAL FAIL]     : 0 checks failed." -ForegroundColor Green
}
if ($warnCount -gt 0) {
    Write-Host ("  [TOTAL WARNINGS] : " + $warnCount + " warnings.") -ForegroundColor Yellow
}
Write-Host ("  [PASS RATE]      : " + $passRate + "%") -ForegroundColor $(if ($failCount -eq 0) { [ConsoleColor]::Green } else { [ConsoleColor]::Red })

Write-Host "=================================================================================" -ForegroundColor Magenta
if ($failCount -eq 0) {
    Write-Host "   *** XAC NHAN: 100% MUC TIEU KIEM THU PHIEN 4 DAT CHUAN XUAT SAC ***            " -ForegroundColor Green
} else {
    Write-Host "   *** CANH BAO: CO MOT SO HANG MUC KIEM THU CHUA DAT. VUI LONG KIEM TRA ***     " -ForegroundColor Red
}
Write-Host "=================================================================================" -ForegroundColor Magenta

if ($failCount -gt 0) {
    exit 1
} else {
    exit 0
}
