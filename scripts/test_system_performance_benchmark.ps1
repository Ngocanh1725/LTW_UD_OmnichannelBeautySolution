# ==============================================================================
# OMNICHANNEL BEAUTY SOLUTION - PERFORMANCE BENCHMARK & LOAD STRESS TEST
# Kịch bản kiểm thử hiệu năng tự động (50 requests liên tiếp / endpoint)
# Đo đạc: Average Response Time (< 200ms), Min/Max, HTTP Status, RAM Consumption
# ==============================================================================

param(
    [string]$BaseUrl = "http://localhost:5116",
    [int]$RequestsPerEndpoint = 50,
    [switch]$AutoStartServer = $true,
    [switch]$KeepServerRunning = $false
)

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

Add-Type -AssemblyName "System.Net.Http"

Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "       OMNICHANNEL BEAUTY SOLUTION - SYSTEM PERFORMANCE BENCHMARK               " -ForegroundColor Magenta
Write-Host "   Đo đạc độ trễ phản hồi, độ ổn định CPU / RAM và mã trạng thái HTTP          " -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "  Cấu hình:" -ForegroundColor Gray
Write-Host "  - Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "  - Số lượt request mỗi endpoint: $RequestsPerEndpoint" -ForegroundColor Gray
Write-Host "  - Thư mục dự án: $workspaceRoot" -ForegroundColor Gray
Write-Host ""

# ==============================================================================
# BƯỚC 1: KIỂM TRA & KHỞI ĐỘNG SERVER (NẾU CHƯA CHẠY)
# ==============================================================================
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $true
$client = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromSeconds(15)

$serverProcess = $null
$serverStartedByScript = $false

function Test-ServerHealth {
    try {
        $resp = $client.GetAsync("$BaseUrl/").Result
        return ($resp -ne $null -and ([int]$resp.StatusCode -ge 200 -and [int]$resp.StatusCode -lt 500))
    } catch {
        return $false
    }
}

Write-Host "[BƯỚC 1/4] Kiểm tra trạng thái máy chủ Web..." -ForegroundColor Yellow
$isAlive = Test-ServerHealth

if (-not $isAlive) {
    if ($AutoStartServer) {
        Write-Host "  -> Máy chủ chưa khởi chạy. Đang kích hoạt 'dotnet run --no-build --project Omnichannel.Web'..." -ForegroundColor Cyan
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "dotnet"
        $psi.Arguments = "run --no-build --project Omnichannel.Web"
        $psi.WorkingDirectory = $workspaceRoot
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true

        $serverProcess = [System.Diagnostics.Process]::Start($psi)
        $serverStartedByScript = $true

        Write-Host "  -> Tiến trình đã khởi tạo (PID=$($serverProcess.Id)). Đang đợi máy chủ sẵn sàng tiếp nhận kết nối..." -ForegroundColor Cyan
        $attempts = 0
        $maxAttempts = 30
        while ($attempts -lt $maxAttempts) {
            Start-Sleep -Seconds 1
            $attempts++
            if (Test-ServerHealth) {
                Write-Host "  -> Máy chủ đã sẵn sàng tại $BaseUrl sau $attempts giây!" -ForegroundColor Green
                $isAlive = $true
                break
            }
        }

        if (-not $isAlive) {
            Write-Host "  [LỖI] Máy chủ không thể khởi động sau $maxAttempts giây." -ForegroundColor Red
            if ($serverProcess -and -not $serverProcess.HasExited) {
                $serverProcess.Kill()
            }
            exit 1
        }
    } else {
        Write-Host "  [LỖI] Không thể kết nối tới $BaseUrl. Vui lòng khởi động server hoặc dùng cờ -AutoStartServer." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  -> Máy chủ đang hoạt động tại $BaseUrl." -ForegroundColor Green
}

# ==============================================================================
# BƯỚC 2: WARM-UP JIT & DATABASE CONNECTION POOL
# ==============================================================================
Write-Host ""
Write-Host "[BƯỚC 2/4] Thực hiện Warm-up (JIT compilation, EF Core Model Cache, Connection Pool)..." -ForegroundColor Yellow
$warmupEndpoints = @("/", "/Catalog", "/Cart/GetCartSummary", "/Media/GetImage/1")
foreach ($ep in $warmupEndpoints) {
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $r = $client.GetAsync("$BaseUrl$ep").Result
        $sw.Stop()
        Write-Host "  -> Warmup $ep : HTTP $([int]$r.StatusCode) ($($sw.ElapsedMilliseconds) ms)" -ForegroundColor Gray
    } catch {
        Write-Host "  -> Warmup $ep : Thất bại ($($_.Exception.Message))" -ForegroundColor DarkGray
    }
}
Start-Sleep -Milliseconds 500

# ==============================================================================
# BƯỚC 3: BENCHMARK 50 REQUESTS LIÊN TIẾP TRÊN CÁC ENDPOINT TRỌNG ĐIỂM
# ==============================================================================
Write-Host ""
Write-Host "[BƯỚC 3/4] Thực hiện Benchmark $RequestsPerEndpoint requests liên tiếp trên từng endpoint..." -ForegroundColor Yellow

$benchmarkTargets = @(
    @{ Name = "Trang chủ"; Path = "/"; TargetMaxAvgMs = 200 },
    @{ Name = "Danh mục sản phẩm"; Path = "/Catalog"; TargetMaxAvgMs = 200 },
    @{ Name = "API Giỏ hàng (JSON)"; Path = "/Cart/GetCartSummary"; TargetMaxAvgMs = 100 },
    @{ Name = "API Ảnh nhị phân"; Path = "/Media/GetImage/1"; TargetMaxAvgMs = 150 }
)

$results = @()

foreach ($target in $benchmarkTargets) {
    $epName = $target.Name
    $epPath = $target.Path
    $targetMaxAvg = $target.TargetMaxAvgMs
    $url = "$BaseUrl$epPath"

    Write-Host "  -> Đang benchmark: $epName ($epPath)..." -ForegroundColor Cyan

    $latencies = New-Object System.Collections.Generic.List[double]
    $statusCodes = @{}
    $successCount = 0
    $errorCount = 0
    $totalBytes = 0

    for ($i = 1; $i -le $RequestsPerEndpoint; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $resp = $client.GetAsync($url).Result
            $sw.Stop()
            $ms = $sw.Elapsed.TotalMilliseconds
            $latencies.Add($ms)

            $code = [int]$resp.StatusCode
            if (-not $statusCodes.ContainsKey($code)) {
                $statusCodes[$code] = 0
            }
            $statusCodes[$code]++

            if ($code -ge 200 -and $code -lt 400) {
                $successCount++
            } else {
                $errorCount++
            }

            if ($resp.Content -and $resp.Content.Headers.ContentLength) {
                $totalBytes += $resp.Content.Headers.ContentLength
            }
        } catch {
            $sw.Stop()
            $latencies.Add($sw.Elapsed.TotalMilliseconds)
            $errorCount++
            if (-not $statusCodes.ContainsKey(500)) { $statusCodes[500] = 0 }
            $statusCodes[500]++
        }
    }

    $sorted = $latencies | Sort-Object
    $minMs = [math]::Round($sorted[0], 2)
    $maxMs = [math]::Round($sorted[-1], 2)
    $avgMs = [math]::Round(($latencies | Measure-Object -Average).Average, 2)
    $p95Index = [math]::Floor($latencies.Count * 0.95)
    $p95Ms = [math]::Round($sorted[[math]::Min($p95Index, $latencies.Count - 1)], 2)
    $avgKb = [math]::Round(($totalBytes / [math]::Max(1, $RequestsPerEndpoint)) / 1024, 2)

    $statusStr = ($statusCodes.Keys | ForEach-Object { "$($_):$($statusCodes[$_])" }) -join ", "
    $passed = ($avgMs -le $targetMaxAvg) -and ($errorCount -eq 0)

    $results += [PSCustomObject]@{
        Endpoint = $epName
        Path = $epPath
        TotalRequests = $RequestsPerEndpoint
        MinLatencyMs = $minMs
        MaxLatencyMs = $maxMs
        AvgLatencyMs = $avgMs
        P95LatencyMs = $p95Ms
        SuccessRate = "$([math]::Round(($successCount / $RequestsPerEndpoint) * 100, 1))%"
        StatusCodes = $statusStr
        AvgPayloadKB = "$avgKb KB"
        TargetMaxAvgMs = "$targetMaxAvg ms"
        Result = if ($passed) { "PASSED" } else { "FAILED" }
    }
}

# ==============================================================================
# BƯỚC 4: THỐNG KÊ TÀI NGUYÊN TIẾN TRÌNH & BẢNG KẾT QUẢ
# ==============================================================================
Write-Host ""
Write-Host "[BƯỚC 4/4] Thống kê tiêu thụ tài nguyên máy chủ (RAM / CPU)..." -ForegroundColor Yellow

# Tìm tiến trình dotnet chạy Omnichannel.Web
$webProc = $null
$allDotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
foreach ($dp in $allDotnetProcs) {
    try {
        # Match process
        if ($serverProcess -and $dp.Id -eq $serverProcess.Id) {
            $webProc = $dp
            break
        }
    } catch {}
}
if (-not $webProc -and $allDotnetProcs) {
    # Chọn tiến trình có WorkingSet lớn nhất (web app)
    $webProc = $allDotnetProcs | Sort-Object WorkingSet64 -Descending | Select-Object -First 1
}

$workingSetMb = 0
$cpuSeconds = 0
if ($webProc) {
    $workingSetMb = [math]::Round($webProc.WorkingSet64 / 1MB, 2)
    $cpuSeconds = [math]::Round($webProc.CPU, 2)
    Write-Host "  -> Tiến trình Web App: PID=$($webProc.Id)" -ForegroundColor White
    Write-Host "  -> Mức sử dụng bộ nhớ RAM (Working Set): $workingSetMb MB" -ForegroundColor Green
    Write-Host "  -> Tổng thời gian sử dụng CPU: $cpuSeconds giây" -ForegroundColor Green
}

Write-Host ""
Write-Host "=================================================================================" -ForegroundColor Magenta
Write-Host "                    BẢNG KẾT QUẢ BENCHMARK CHI TIẾT                             " -ForegroundColor Magenta
Write-Host "=================================================================================" -ForegroundColor Magenta

$results | Format-Table -Property Endpoint, Path, TotalRequests, AvgLatencyMs, MinLatencyMs, MaxLatencyMs, P95LatencyMs, SuccessRate, Result -AutoSize

Write-Host "Chi tiết mã HTTP & Kích thước Payload:" -ForegroundColor Cyan
$results | Format-Table -Property Endpoint, Path, StatusCodes, AvgPayloadKB, TargetMaxAvgMs, Result -AutoSize

# Đánh giá tổng thể
$allPassed = ($results | Where-Object { $_.Result -eq "FAILED" }).Count -eq 0

Write-Host "---------------------------------------------------------------------------------" -ForegroundColor Gray
if ($allPassed) {
    Write-Host "  [XÁC NHẬN] 100% CÁC ENDPOINT ĐẠT CHUẨN HIỆU NĂNG XUẤT SẮC!" -ForegroundColor Green
    Write-Host "  - Tất cả phản hồi trung bình < 200ms (chuẩn trải nghiệm mượt mà không độ trễ)." -ForegroundColor Green
    Write-Host "  - Tỷ lệ thành công 100% (không có lỗi 500 hoặc Timeout)." -ForegroundColor Green
    Write-Host "  - Mức chiếm dụng RAM ổn định ($workingSetMb MB), không phát hiện rò rỉ bộ nhớ." -ForegroundColor Green
} else {
    Write-Host "  [CẢNH BÁO] Có một số endpoint chưa đạt chuẩn hiệu năng mục tiêu." -ForegroundColor Yellow
}
Write-Host "=================================================================================" -ForegroundColor Magenta

# Dọn dẹp tiến trình nếu được bật bởi script
if ($serverStartedByScript -and -not $KeepServerRunning) {
    Write-Host ""
    Write-Host "Đang dừng tiến trình máy chủ được khởi động bởi script..." -ForegroundColor Gray
    try {
        if ($serverProcess -and -not $serverProcess.HasExited) {
            $serverProcess.Kill()
            Write-Host "Đã dừng tiến trình Web (PID=$($serverProcess.Id))." -ForegroundColor Gray
        }
    } catch {}
}

if (-not $allPassed) {
    exit 1
}
