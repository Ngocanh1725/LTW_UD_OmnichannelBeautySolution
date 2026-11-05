# ==============================================================================
# OMNICHANNEL BEAUTY SOLUTION - PHIEN E2 EXTENDED SMOKE TEST & 2-CYCLE VERIFICATION
# ==============================================================================

param(
    [string]$BaseUrl = "http://localhost:5116"
)

Add-Type -AssemblyName "System.Net.Http"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Continue"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  CANMAKE BEAUTY (OmnichannelBeautySolution) - EXTENDED SMOKE TEST & 2-CYCLE DEMO" -ForegroundColor Cyan
Write-Host "  Target URL: $BaseUrl" -ForegroundColor Gray
Write-Host "================================================================================" -ForegroundColor Cyan

$handler = New-Object System.Net.Http.HttpClientHandler
$cookieContainer = New-Object System.Net.CookieContainer
$handler.CookieContainer = $cookieContainer
$handler.AllowAutoRedirect = $true
$client = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromSeconds(30)

$failedRoutes = 0
$totalRoutes = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Path,
        [int[]]$ExpectedStatusCodes = @(200),
        [string]$Method = "GET",
        [System.Net.Http.HttpContent]$Content = $null
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $status = 0
    $length = 0
    try {
        if ($Method -eq "GET") {
            $response = $client.GetAsync("$BaseUrl$Path").Result
        } else {
            if ($null -eq $Content) {
                $Content = New-Object System.Net.Http.StringContent("{}", [System.Text.Encoding]::UTF8, "application/json")
            }
            $response = $client.PostAsync("$BaseUrl$Path", $Content).Result
        }
        $sw.Stop()
        $status = [int]$response.StatusCode
        $bytes = $response.Content.ReadAsByteArrayAsync().Result
        $length = $bytes.Length
    } catch {
        $sw.Stop()
        $status = 500
    }

    $isPass = $ExpectedStatusCodes -contains $status
    $color = if ($isPass) { "Green" } else { "Red" }
    $mark = if ($isPass) { "[PASS]" } else { "[FAIL]" }

    Write-Host ("{0,-7} {1,-40} | Status: {2,-3} | Time: {3,5}ms | Size: {4,8} B" -f $mark, $Name, $status, $sw.ElapsedMilliseconds, $length) -ForegroundColor $color
    
    $script:totalRoutes++
    if (-not $isPass -or $status -ge 500) {
        $script:failedRoutes++
    }
}

Write-Host "`n--- 1. SMOKE TEST ROUTE CONG KHAI (STOREFRONT VA TINH NANG MOI) ---" -ForegroundColor Yellow

Test-Endpoint "Trang chu (Home)" "/"
Test-Endpoint "Danh muc San pham" "/san-pham"
Test-Endpoint "Catalog tong" "/Catalog"
Test-Endpoint "Danh muc Cham soc da" "/danh-muc/cham-soc-da"
Test-Endpoint "Tim kiem san pham" "/tim-kiem?q=serum"
Test-Endpoint "Instant Search Suggest API (C1)" "/api/search/suggest?q=serum"
Test-Endpoint "Tu van da Routine Quiz" "/tu-van-da"
Test-Endpoint "AI Beauty Advisor" "/ai-beauty-advisor"
Test-Endpoint "Phan tich da AI (C8)" "/phan-tich-da"
Test-Endpoint "So sanh san pham (C3)" "/so-sanh"
Test-Endpoint "Khay so sanh API (C3)" "/api/compare/quick-items"
Test-Endpoint "Clearance FEFO can date" "/clearance-fefo"
Test-Endpoint "Tra cuu don vang lai (C6)" "/tra-cuu-don-hang"
Test-Endpoint "Gio hang (Cart)" "/cart"
Test-Endpoint "Thanh toan (Checkout)" "/checkout"
Test-Endpoint "Robots.txt" "/robots.txt"
Test-Endpoint "Sitemap.xml" "/sitemap.xml"
Test-Endpoint "Dang nhap Khach hang" "/Account/Login"
Test-Endpoint "Dang nhap Quan tri" "/admin/auth/login"
Test-Endpoint "Trang thai Demo HUD (E1)" "/api/demo/status"

Write-Host "`n--- 2. KIEM THU KICH BAN DEMO 2 VONG LIEN TIEP (CYCLE 1 VA CYCLE 2) ---" -ForegroundColor Yellow

for ($cycle = 1; $cycle -le 2; $cycle++) {
    Write-Host "`n======================= [DEMO CYCLE $cycle / 2] =======================" -ForegroundColor Magenta

    # Buoc A: Chuyen vai Khach VIP (Le Ngoc Linh)
    Write-Host "-> Buoc 1: Chuyen vai Khach VIP (customer_vip)..." -ForegroundColor Cyan
    Test-Endpoint "Switch to customer_vip" "/api/demo/quick-switch-persona?persona=customer" @(200) "POST"
    
    # Kiem tra quyen Khach hang
    Test-Endpoint "Customer Dashboard & Canmake Rewards (C7)" "/Customer/Dashboard"
    Test-Endpoint "Customer Orders" "/Customer/Orders"
    Test-Endpoint "Customer Wishlist" "/Customer/Wishlist"

    # Buoc B: Nap gio hang tuong ky AI
    Write-Host "-> Buoc 2: Nap gio hang tuong ky hoat chat AI (Retinol + BHA)..." -ForegroundColor Cyan
    Test-Endpoint "Seed Cart Conflict" "/api/demo/seed-cart-conflict" @(200) "POST"
    Test-Endpoint "Kiem tra Gio hang sau nap" "/cart"

    # Buoc C: Chuyen vai Thu ngan Showroom 1 (POS)
    Write-Host "-> Buoc 3: Chuyen vai Thu ngan POS (cashier01 - Showroom 1)..." -ForegroundColor Cyan
    Test-Endpoint "Switch to cashier01" "/api/demo/quick-switch-persona?persona=cashier" @(200) "POST"
    Test-Endpoint "Man hinh POS" "/Admin/Pos"
    Test-Endpoint "POS Visual Catalog" "/Admin/Pos/GetVisualCatalog"
    Test-Endpoint "POS Shift Status" "/Admin/Pos/GetShiftStatus"
    Test-Endpoint "POS Pickup Orders (D1)" "/Admin/Pos/PendingPickupOrders"

    # Buoc D: Chuyen vai SuperAdmin
    Write-Host "-> Buoc 4: Chuyen vai SuperAdmin (Dieu hanh chuoi)..." -ForegroundColor Cyan
    Test-Endpoint "Switch to superadmin" "/api/demo/quick-switch-persona?persona=superadmin" @(200) "POST"
    Test-Endpoint "Admin Decision Center (D2)" "/Admin/Dashboard"
    Test-Endpoint "Admin Order Management" "/Admin/Order"
    Test-Endpoint "Admin Warehouse FEFO" "/Admin/Warehouse"
    Test-Endpoint "Admin Stock Transfer" "/Admin/StockTransfer"
    Test-Endpoint "Admin Promotion" "/Admin/Promotion"
    Test-Endpoint "Admin Loyalty" "/Admin/Loyalty"
    Test-Endpoint "Admin Product Reviews (C4)" "/Admin/Review"
    Test-Endpoint "Admin Permission PBAC" "/Admin/PermissionManagement"
    Test-Endpoint "Xuat CSV Khach RFM (D2)" "/Admin/Dashboard/ExportRfmSegment?segment=VIP"

    # Buoc E: Reset Demo Data
    Write-Host "-> Buoc 5: 1-Click Reset Demo Data..." -ForegroundColor Cyan
    Test-Endpoint "Reset Demo Data" "/api/demo/reset-data" @(200) "POST"
}

Write-Host "`n================================================================================" -ForegroundColor Cyan
Write-Host "                              TONG KET NGHIEM THU                               " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Tong so luot kiem thu route/tac vu: $totalRoutes"
Write-Host "  So luot loi (500 hoac khong dung ma mong muon): $failedRoutes" -ForegroundColor $(if ($failedRoutes -eq 0) { "Green" } else { "Red" })

if ($failedRoutes -eq 0) {
    Write-Host "`n  >>> [KET QUA: PASS 100%] TOAN BO HE THONG SAN SANG DEMO! <<<" -ForegroundColor Green
} else {
    Write-Host "`n  >>> [KET QUA: FAIL] Co $failedRoutes loi can khac phuc! <<<" -ForegroundColor Red
}
