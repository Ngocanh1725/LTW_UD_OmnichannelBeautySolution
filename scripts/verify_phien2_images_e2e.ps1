# ==============================================================================
# OMNICHANNEL BEAUTY SOLUTION - E2E IMAGE 404 SCANNER (PHIEN 2 & PHIEN 3)
# Quet toan bo URL anh tu DB + HTML render cua 13 trang Storefront & Admin
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$connString = "Server=localhost;Database=OmnichannelBeautyDb;User Id=sa;Password=12345678;TrustServerCertificate=True;MultipleActiveResultSets=True;"
$port = 5116
$baseUrl = "http://localhost:$port"
$webDir = Join-Path (Get-Location).Path "Omnichannel.Web"

Write-Host "=== KHOI DONG WEB SERVER (PORT $port) ===" -ForegroundColor Cyan
$proc = Start-Process -FilePath "dotnet" -ArgumentList "bin\Release\net8.0\Omnichannel.Web.dll" -WorkingDirectory $webDir -PassThru -WindowStyle Hidden

try {
    # Wait for server ready
    $ready = $false
    for ($i = 0; $i -lt 25; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            $resp = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing -TimeoutSec 3
            if ($resp.StatusCode -eq 200) {
                $ready = $true
                break
            }
        } catch {}
    }
    if (-not $ready) {
        throw "Web server failed to start on $baseUrl"
    }
    Write-Host "[OK] Web server ready at $baseUrl" -ForegroundColor Green

    # 1. Collect all local image URLs from DB
    Add-Type -AssemblyName "System.Data"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()

    $dbUrls = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    $queries = @(
        "SELECT ThumbnailUrl FROM dbo.Products WHERE ThumbnailUrl IS NOT NULL AND ThumbnailUrl LIKE '/images/%'",
        "SELECT SwatchImageUrl FROM dbo.ProductVariants WHERE SwatchImageUrl IS NOT NULL AND SwatchImageUrl LIKE '/images/%'",
        "SELECT ImageUrl FROM dbo.Banners WHERE ImageUrl IS NOT NULL AND ImageUrl LIKE '/images/%'",
        "SELECT ImageUrl FROM dbo.Categories WHERE ImageUrl IS NOT NULL AND ImageUrl LIKE '/images/%'"
    )
    foreach ($q in $queries) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $q
        $r = $cmd.ExecuteReader()
        while ($r.Read()) {
            $u = $r.GetString(0).Trim()
            if ($u) { [void]$dbUrls.Add($u) }
        }
        $r.Close()
    }

    # Get 5 PDP slugs
    $pdpSlugs = @()
    $cmdPdp = $conn.CreateCommand()
    $cmdPdp.CommandText = "SELECT TOP 5 Slug FROM dbo.Products WHERE IsActive = 1 ORDER BY Id DESC"
    $rPdp = $cmdPdp.ExecuteReader()
    while ($rPdp.Read()) {
        $pdpSlugs += $rPdp.GetString(0)
    }
    $rPdp.Close()
    $conn.Close()

    Write-Host "[DB] Thu thap duoc $($dbUrls.Count) URL anh local duy nhat tu Database." -ForegroundColor Cyan

    # 2. Crawl HTML pages (Storefront + Admin)
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.UseCookies = $true
    $handler.CookieContainer = New-Object System.Net.CookieContainer
    $handler.AllowAutoRedirect = $true
    $client = New-Object System.Net.Http.HttpClient($handler)
    $client.BaseAddress = [Uri]$baseUrl

    # Login as SuperAdmin via Demo Presenter API so /Admin/Product and /Admin/Pos render full HTML
    $jsonBody = New-Object System.Net.Http.StringContent('{"personaKey":"super_admin"}', [System.Text.Encoding]::UTF8, "application/json")
    [void]$client.PostAsync("/api/demo/quick-switch-persona", $jsonBody).GetAwaiter().GetResult()

    $pagesToScan = @(
        "/",
        "/san-pham",
        "/cart",
        "/checkout",
        "/clearance-fefo",
        "/so-sanh",
        "/Admin/Product",
        "/Admin/Pos"
    )
    foreach ($slug in $pdpSlugs) {
        $pagesToScan += "/san-pham/$slug"
    }

    $htmlUrls = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($page in $pagesToScan) {
        $res = $client.GetAsync($page).GetAwaiter().GetResult()
        $html = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        # Match <img ... src="..."> and <source ... srcset="...">
        $imgMatches = [regex]::Matches($html, '(?:src|srcset)=["''](/images/[^"''\s?]+)')
        $pageImgCount = 0
        foreach ($m in $imgMatches) {
            $u = $m.Groups[1].Value.Trim()
            if ($u -and -not $u.Contains('${') -and -not $u.Contains('{{')) {
                [void]$htmlUrls.Add($u)
                $pageImgCount++
            }
        }
        Write-Host ("  [PAGE] {0,-65} -> HTTP {1} ({2} img/source refs)" -f $page, [int]$res.StatusCode, $pageImgCount)
    }

    # Combine all unique image URLs
    $allUrls = [System.Collections.Generic.HashSet[string]]::new($dbUrls, [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($u in $htmlUrls) { [void]$allUrls.Add($u) }

    Write-Host "`n=== KIEM TRA HTTP STATUS CHO $($allUrls.Count) URL ANH DUY NHAT (DB + HTML) ===" -ForegroundColor Cyan
    $ok200 = 0
    $fail404 = @()
    foreach ($u in $allUrls) {
        $escapedUrl = ($u -split '/' | ForEach-Object { if ($_) { [Uri]::EscapeDataString([Uri]::UnescapeDataString($_)) } else { "" } }) -join '/'
        $respImg = $client.GetAsync($escapedUrl).GetAwaiter().GetResult()
        if ([int]$respImg.StatusCode -eq 200) {
            $ok200++
        } else {
            $fail404 += "$u -> HTTP $([int]$respImg.StatusCode)"
        }
    }

    Write-Host "  * Tong URL anh kiem tra : $($allUrls.Count) (DB: $($dbUrls.Count), HTML: $($htmlUrls.Count))" -ForegroundColor White
    Write-Host "  * So anh HTTP 200 OK    : $ok200" -ForegroundColor Green
    Write-Host "  * So anh HTTP 404/Loi   : $($fail404.Count)" -ForegroundColor ($(if ($fail404.Count -eq 0) { "Green" } else { "Red" }))

    if ($fail404.Count -gt 0) {
        foreach ($f in $fail404) {
            Write-Host "    [404] $f" -ForegroundColor Red
        }
        exit 1
    }
} finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
