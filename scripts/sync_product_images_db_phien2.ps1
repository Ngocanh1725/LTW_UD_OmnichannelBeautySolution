param(
    [switch]$Apply = $false
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$workspaceRoot = Split-Path $PSScriptRoot -Parent
$connStr = "Server=localhost;Database=OmnichannelBeautyDb;Trusted_Connection=True;TrustServerCertificate=True;"
$sqlFile = Join-Path $PSScriptRoot "update_product_images_phien2.sql"
$productsImgDir = Join-Path $workspaceRoot "Omnichannel.Web\wwwroot\images\products"

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

try {
    # 1. Đọc danh sách file chuẩn trong Omnichannel.Web\wwwroot\images\products\**
    $canonicalFiles = Get-ChildItem -Path $productsImgDir -Recurse -File | Where-Object {
        $_.Extension -match '^\.(jpg|jpeg|png|webp)$'
    }
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $fileMap = @{}
    foreach ($f in $canonicalFiles) {
        $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
        $hash = ([System.BitConverter]::ToString($sha256.ComputeHash($bytes))).Replace("-", "").ToLowerInvariant()
        $ext = $f.Extension.ToLowerInvariant()
        $ct = switch ($ext) {
            ".png"  { "image/png" }
            ".webp" { "image/webp" }
            default { "image/jpeg" }
        }
        $fileMap[$f.Name] = @{
            FullName    = $f.FullName
            Bytes       = $bytes
            FileSize    = $bytes.Length
            Hash        = $hash
            ContentType = $ct
        }
    }

    # 2. Truy vấn SELECT COUNT hiện trạng trên DB (READ-ONLY)
    $cmdCheck = $conn.CreateCommand()
    $cmdCheck.CommandText = @"
SELECT
    (SELECT COUNT(*) FROM dbo.Products WHERE ThumbnailUrl NOT LIKE N'/images/products/%' OR ThumbnailUrl = N'/images/product-placeholder.svg' OR ThumbnailUrl IS NULL) AS ProductsToUpdate,
    (SELECT COUNT(*) FROM dbo.OrderReturnRequests WHERE EvidenceUrlsJson LIKE N'%/images/products/serum-b5.jpg%') AS OrderReturnRequestsToUpdate,
    (SELECT COUNT(*) FROM dbo.ProductVariants WHERE SwatchImageUrl IS NOT NULL AND SwatchImageUrl NOT LIKE N'/images/products/%') AS ProductVariantsToUpdate,
    (SELECT COUNT(*) FROM dbo.ProductReviews WHERE ImagesJson LIKE N'%/images/products/serum-b5.jpg%') AS ProductReviewsToUpdate;
"@
    $reader = $cmdCheck.ExecuteReader()
    $counts = @{}
    if ($reader.Read()) {
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $counts[$reader.GetName($i)] = $reader.GetValue($i)
        }
    }
    $reader.Close()

    # Kiểm tra số dòng MediaAssets lệch ChecksumHash so với thư mục chuẩn A
    $cmdMedia = $conn.CreateCommand()
    $cmdMedia.CommandText = "SELECT Id, FileName, ChecksumHash, FileSize FROM dbo.MediaAssets WHERE Category = N'PRODUCT'"
    $rMedia = $cmdMedia.ExecuteReader()
    $mediaRows = @()
    while ($rMedia.Read()) {
        $mediaRows += [PSCustomObject]@{
            Id           = [int]$rMedia["Id"]
            FileName     = [string]$rMedia["FileName"]
            ChecksumHash = [string]$rMedia["ChecksumHash"]
            FileSize     = [long]$rMedia["FileSize"]
        }
    }
    $rMedia.Close()

    $mediaToSyncBinary = @()
    $mediaMissingInDb = @()
    foreach ($kv in $fileMap.GetEnumerator()) {
        $fn = $kv.Key
        $info = $kv.Value
        $existing = $mediaRows | Where-Object { $_.FileName -ieq $fn } | Select-Object -First 1
        if ($null -eq $existing) {
            $mediaMissingInDb += $fn
        } elseif ($existing.ChecksumHash -ne $info.Hash) {
            $mediaToSyncBinary += [PSCustomObject]@{
                Id       = $existing.Id
                FileName = $fn
                OldHash  = $existing.ChecksumHash
                NewHash  = $info.Hash
                OldSize  = $existing.FileSize
                NewSize  = $info.FileSize
            }
        }
    }

    Write-Host "=========================================================================" -ForegroundColor Cyan
    Write-Host " [PRE-CHECK SELECT COUNT] SO DONG DU LIEU BI ANH HUONG TRONG DB" -ForegroundColor Cyan
    Write-Host "=========================================================================" -ForegroundColor Cyan
    Write-Host ("  1. dbo.Products can cap nhat ThumbnailUrl & ImagesJson : {0} dong" -f $counts.ProductsToUpdate) -ForegroundColor Yellow
    Write-Host ("  2. dbo.MediaAssets can dong bo BinaryData & Checksum   : {0} dong (dong bo byte tu thu muc A)" -f $mediaToSyncBinary.Count) -ForegroundColor Yellow
    Write-Host ("  3. dbo.OrderReturnRequests (EvidenceUrlsJson gay)      : {0} dong" -f $counts.OrderReturnRequestsToUpdate) -ForegroundColor Yellow
    Write-Host ("  4. dbo.ProductVariants (SwatchImageUrl)                : {0} dong" -f $counts.ProductVariantsToUpdate) -ForegroundColor Green
    Write-Host ("  5. dbo.ProductReviews (ImagesJson)                     : {0} dong" -f $counts.ProductReviewsToUpdate) -ForegroundColor Green
    Write-Host "=========================================================================" -ForegroundColor Cyan

    if (-not $Apply) {
        Write-Host "[READ-ONLY MODE] Chua thuc thi UPDATE (chay voi tham so -Apply sau khi duoc duyet)." -ForegroundColor Green
        exit 0
    }

    Write-Host "[APPLY MODE] Dang thuc thi dong bo BinaryData MediaAssets va script SQL trong Transaction..." -ForegroundColor Magenta
    $tx = $conn.BeginTransaction()
    try {
        foreach ($item in $mediaToSyncBinary) {
            $info = $fileMap[$item.FileName]
            $uCmd = $conn.CreateCommand()
            $uCmd.Transaction = $tx
            $uCmd.CommandText = @"
UPDATE dbo.MediaAssets
SET Data = @Data,
    FileSize = @FileSize,
    ChecksumHash = @Hash,
    ContentType = @ContentType
WHERE Id = @Id;
"@
            [void]$uCmd.Parameters.Add("@Data", [System.Data.SqlDbType]::VarBinary, -1)
            $uCmd.Parameters["@Data"].Value = $info.Bytes
            [void]$uCmd.Parameters.AddWithValue("@FileSize", [long]$info.FileSize)
            [void]$uCmd.Parameters.AddWithValue("@Hash", [string]$info.Hash)
            [void]$uCmd.Parameters.AddWithValue("@ContentType", [string]$info.ContentType)
            [void]$uCmd.Parameters.AddWithValue("@Id", [int]$item.Id)
            [void]$uCmd.ExecuteNonQuery()
        }
        $tx.Commit()
        Write-Host ("  -> Da cap nhat BinaryData + ChecksumHash cho {0} dong dbo.MediaAssets." -f $mediaToSyncBinary.Count) -ForegroundColor Green
    } catch {
        $tx.Rollback()
        throw
    }

    # Thực thi script SQL chính (đã có BEGIN TRANSACTION .. COMMIT TRANSACTION bên trong)
    $sqlContent = Get-Content -Path $sqlFile -Raw -Encoding UTF8
    $sqlCmd = $conn.CreateCommand()
    $sqlCmd.CommandTimeout = 120
    $sqlCmd.CommandText = $sqlContent
    [void]$sqlCmd.ExecuteNonQuery()
    Write-Host "  -> Da thuc thi thanh cong scripts/update_product_images_phien2.sql!" -ForegroundColor Green
}
finally {
    $conn.Close()
}
