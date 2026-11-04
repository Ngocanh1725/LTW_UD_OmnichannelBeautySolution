$baseUrl = "http://localhost:5116"

# 1. Tìm real product slug từ trang chủ
$homeResp = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
$slugMatch = [regex]::Match($homeResp.Content, 'href="/san-pham/([^"#\?]+)"')
$productSlug = if ($slugMatch.Success) { $slugMatch.Groups[1].Value } else { "son-kem-li-romand-zero-velvet-tint-02" }

Write-Host "Product Slug detected: $productSlug"

$endpoints = @(
    @{ Name = "Trang chủ"; Path = "/" },
    @{ Name = "Tìm kiếm"; Path = "/tim-kiem" },
    @{ Name = "Danh mục trang điểm"; Path = "/category/trang-diem" },
    @{ Name = "Chi tiết sản phẩm"; Path = "/san-pham/$productSlug" },
    @{ Name = "Giỏ hàng"; Path = "/Cart" },
    @{ Name = "Thanh toán (Checkout)"; Path = "/Checkout" },
    @{ Name = "Admin Dashboard"; Path = "/Admin/Dashboard" }
)

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $true
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(30)

$report = @()

foreach ($ep in $endpoints) {
    $url = "$baseUrl$($ep.Path)"
    Write-Host "Đang đo: $($ep.Name) ($url)..." -NoNewline

    # Cold Request (Lần đầu)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $coldResp = $client.GetAsync($url).Result
    $sw.Stop()
    $coldMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 1)
    $statusCode = [int]$coldResp.StatusCode

    # 5 Warm Requests
    $warmTimes = @()
    for ($i = 0; $i -lt 5; $i++) {
        $sw.Restart()
        $warmResp = $client.GetAsync($url).Result
        $sw.Stop()
        $warmTimes += $sw.Elapsed.TotalMilliseconds
    }

    $avgWarm = [math]::Round(($warmTimes | Measure-Object -Average).Average, 1)
    $minWarm = [math]::Round(($warmTimes | Measure-Object -Minimum).Minimum, 1)
    $maxWarm = [math]::Round(($warmTimes | Measure-Object -Maximum).Maximum, 1)

    Write-Host " Xong! (Cold: ${coldMs}ms, Warm Avg: ${avgWarm}ms)"

    $report += [PSCustomObject]@{
        Name = $ep.Name
        Path = $ep.Path
        Status = $statusCode
        Cold_TTFB_ms = $coldMs
        Warm_Avg_ms = $avgWarm
        Warm_Min_ms = $minWarm
        Warm_Max_ms = $maxWarm
    }
}

Write-Host ""
Write-Host "=================== BẢNG KẾT QUẢ ĐO ĐẠC TTFB ==================="
$report | Format-Table -AutoSize
$report | Export-Csv -Path "scripts/ttfb_results.csv" -NoTypeInformation -Encoding utf8
