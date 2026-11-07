# scripts/test_permission_matrix.ps1
# Kiểm thử tự động Ma Trận Phân Quyền 19 tài khoản x 15 URL Quản trị bằng HttpClient

$baseUrl = "http://localhost:5116"

$accounts = @(
    @{ Username = "superadmin";   Password = "Admin@123";    Role = "SuperAdmin (Lv 100)";     Dept = "BAN_GIAM_DOC" },
    @{ Username = "admin";        Password = "Admin@123456"; Role = "SuperAdmin (Lv 100)";     Dept = "BAN_GIAM_DOC" },
    @{ Username = "ad1";          Password = "Admin@123";    Role = "UiAdmin (Lv 50)";         Dept = "PHONG_GIAO_DIEN_CMS" },
    @{ Username = "ad2";          Password = "Admin@123";    Role = "DataAdmin (Lv 50)";       Dept = "PHONG_DU_LIEU_SAN_PHAM" },
    @{ Username = "ad3";          Password = "Admin@123";    Role = "HrAdmin (Lv 50)";         Dept = "PHONG_NHAN_SU" },
    @{ Username = "ad4";          Password = "Admin@123";    Role = "SalesAdmin (Lv 50)";      Dept = "PHONG_SALE" },
    @{ Username = "sale01";       Password = "Admin@123";    Role = "SalesStaff (Lv 10)";      Dept = "PHONG_SALE" },
    @{ Username = "sale02";       Password = "Admin@123";    Role = "SalesStaff (Lv 10)";      Dept = "PHONG_SALE" },
    @{ Username = "mkt01";        Password = "Admin@123";    Role = "MarketingStaff (Lv 10)";  Dept = "PHONG_MARKETING" },
    @{ Username = "hr01";         Password = "Admin@123";    Role = "HrStaff (Lv 10)";         Dept = "PHONG_NHAN_SU" },
    @{ Username = "data01";       Password = "Admin@123";    Role = "DataStaff (Lv 10)";       Dept = "PHONG_DU_LIEU_SAN_PHAM" },
    @{ Username = "manager_hq";   Password = "Admin@123";    Role = "StoreManager (Lv 30)";    Dept = "CUA_HANG_BAN_LE (Q1)" },
    @{ Username = "manager_cg";   Password = "Admin@123";    Role = "StoreManager (Lv 30)";    Dept = "CUA_HANG_BAN_LE (CG)" },
    @{ Username = "cashier01";    Password = "Admin@123";    Role = "Cashier (Lv 10)";         Dept = "CUA_HANG_BAN_LE (Q1)" },
    @{ Username = "cashier02";    Password = "Admin@123";    Role = "Cashier (Lv 10)";         Dept = "CUA_HANG_BAN_LE (CG)" },
    @{ Username = "warehouse01";  Password = "Admin@123";    Role = "WarehouseStaff (Lv 10)";  Dept = "PHONG_KHO_VAN (Q1)" },
    @{ Username = "warehouse02";  Password = "Admin@123";    Role = "WarehouseStaff (Lv 10)";  Dept = "PHONG_KHO_VAN (CG)" },
    @{ Username = "editor01";     Password = "Admin@123";    Role = "ContentEditor (Lv 10)";   Dept = "PHONG_GIAO_DIEN_CMS" },
    @{ Username = "customer01";   Password = "Admin@123";    Role = "Customer (Lv 0)";         Dept = "Storefront" }
)

$endpoints = @(
    @{ Name = "Dashboard";   Path = "/Admin/Dashboard";            KeyPerm = "DASHBOARD_VIEW" },
    @{ Name = "Order";       Path = "/Admin/Order";                KeyPerm = "ORDER_VIEW" },
    @{ Name = "Customer";    Path = "/Admin/Customer";             KeyPerm = "CUSTOMER_VIEW" },
    @{ Name = "Product";     Path = "/Admin/Product";              KeyPerm = "PRODUCT_VIEW" },
    @{ Name = "Inventory";   Path = "/Admin/Inventory";            KeyPerm = "INVENTORY_VIEW" },
    @{ Name = "Banner";      Path = "/Admin/Banner";               KeyPerm = "CMS_BANNER_MANAGE" },
    @{ Name = "UiSection";   Path = "/Admin/UiSection";            KeyPerm = "UI_SECTION_MANAGE / THEME_MANAGE" },
    @{ Name = "Menu";        Path = "/Admin/Menu";                 KeyPerm = "CMS_MENU_MANAGE" },
    @{ Name = "User";        Path = "/Admin/User";                 KeyPerm = "USER_VIEW" },
    @{ Name = "Department";  Path = "/Admin/Department";           KeyPerm = "DEPARTMENT_VIEW" },
    @{ Name = "Delegation";  Path = "/Admin/Delegation";           KeyPerm = "SuperAdmin / CanDelegateFurther" },
    @{ Name = "Permission";  Path = "/Admin/PermissionManagement"; KeyPerm = "PERMISSION_DELEGATE / PERMISSION_MANAGE" },
    @{ Name = "Finance";     Path = "/Admin/Finance";              KeyPerm = "FINANCE_PNL_VIEW" },
    @{ Name = "Report";      Path = "/Admin/Report";               KeyPerm = "REPORT_SALES" },
    @{ Name = "Settings";    Path = "/Admin/Settings";             KeyPerm = "SETTINGS_VIEW" }
)

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  BẮT ĐẦU KIỂM THỬ MA TRẬN PHÂN QUYỀN: 19 TÀI KHOẢN X 15 URL QUẢN TRỊ ADMIN" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

$matrixResults = @()

foreach ($acc in $accounts) {
    Write-Host "`n--> Đang kiểm tra: $($acc.Username) [$($acc.Role)]" -ForegroundColor Yellow
    
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.CookieContainer = [System.Net.CookieContainer]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)

    if ($acc.Username -eq "customer01") {
        # Đăng nhập storefront
        $res = $client.GetAsync("$baseUrl/Account/Login").GetAwaiter().GetResult()
        $html = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $token = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
        
        $form = [System.Collections.Generic.Dictionary[string, string]]::new()
        $form["Username"] = $acc.Username
        $form["Password"] = $acc.Password
        $form["RememberMe"] = "false"
        $form["__RequestVerificationToken"] = $token
        $content = [System.Net.Http.FormUrlEncodedContent]::new($form)
        $null = $client.PostAsync("$baseUrl/Account/Login", $content).GetAwaiter().GetResult()
    } else {
        # Đăng nhập Admin Portal
        $res = $client.GetAsync("$baseUrl/admin/auth/login").GetAwaiter().GetResult()
        $html = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $token = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
        
        $form = [System.Collections.Generic.Dictionary[string, string]]::new()
        $form["Username"] = $acc.Username
        $form["Password"] = $acc.Password
        $form["RememberMe"] = "false"
        $form["__RequestVerificationToken"] = $token
        $content = [System.Net.Http.FormUrlEncodedContent]::new($form)
        $loginRes = $client.PostAsync("$baseUrl/admin/auth/login", $content).GetAwaiter().GetResult()
        if ([int]$loginRes.StatusCode -eq 302) {
            Write-Host "    [OK] Đăng nhập Admin thành công." -ForegroundColor Green
        } else {
            Write-Warning "    [FAIL] Đăng nhập thất bại ($([int]$loginRes.StatusCode))."
        }
    }

    $rowResult = [ordered]@{
        Username = $acc.Username
        Role     = $acc.Role
    }

    foreach ($ep in $endpoints) {
        $targetUrl = "$baseUrl$($ep.Path)"
        $resp = $client.GetAsync($targetUrl).GetAwaiter().GetResult()
        $code = [int]$resp.StatusCode
        $loc = if ($resp.Headers.Location) { $resp.Headers.Location.ToString() } else { "" }

        $statusDisplay = ""
        if ($code -eq 200) {
            $statusDisplay = "200"
        } elseif ($code -eq 302) {
            if ($loc -like "*access-denied*") {
                $statusDisplay = "403"
            } elseif ($loc -like "*/auth/login*" -or $loc -like "*/Account/Login*") {
                $statusDisplay = "302"
            } elseif ($loc -like "*/Admin/Finance/ProfitLoss*") {
                # Follow redirect một lần đến ProfitLoss
                $subUrl = if ($loc.StartsWith("http")) { $loc } else { "$baseUrl$loc" }
                $subResp = $client.GetAsync($subUrl).GetAwaiter().GetResult()
                if ([int]$subResp.StatusCode -eq 200) {
                    $statusDisplay = "200"
                } else {
                    $statusDisplay = "403"
                }
            } else {
                $statusDisplay = "302"
            }
        } elseif ($code -eq 403) {
            $statusDisplay = "403"
        } else {
            $statusDisplay = "$code"
        }

        $rowResult[$ep.Name] = $statusDisplay
    }

    $matrixResults += [PSCustomObject]$rowResult
}

Write-Host "`n================================================================================" -ForegroundColor Cyan
Write-Host "                     KẾT QUẢ KIỂM THỬ MA TRẬN PHÂN QUYỀN                       " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

$matrixResults | Format-Table -AutoSize

# Xuất bảng Markdown
$mdHeader = "| Tài khoản | Vai trò | Dash | Order | Cust | Prod | Inv | Banner | UiSec | Menu | User | Dept | Deleg | Perm | Fin | Report | Sett |"
$mdSep    = "|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|"
$mdRows = @($mdHeader, $mdSep)

foreach ($r in $matrixResults) {
    $vals = @(
        $r.Username,
        $r.Role,
        ($r.Dashboard -eq "200" ? "✅ 200" : "⛔ " + $r.Dashboard),
        ($r.Order -eq "200" ? "✅ 200" : "⛔ " + $r.Order),
        ($r.Customer -eq "200" ? "✅ 200" : "⛔ " + $r.Customer),
        ($r.Product -eq "200" ? "✅ 200" : "⛔ " + $r.Product),
        ($r.Inventory -eq "200" ? "✅ 200" : "⛔ " + $r.Inventory),
        ($r.Banner -eq "200" ? "✅ 200" : "⛔ " + $r.Banner),
        ($r.UiSection -eq "200" ? "✅ 200" : "⛔ " + $r.UiSection),
        ($r.Menu -eq "200" ? "✅ 200" : "⛔ " + $r.Menu),
        ($r.User -eq "200" ? "✅ 200" : "⛔ " + $r.User),
        ($r.Department -eq "200" ? "✅ 200" : "⛔ " + $r.Department),
        ($r.Delegation -eq "200" ? "✅ 200" : "⛔ " + $r.Delegation),
        ($r.Permission -eq "200" ? "✅ 200" : "⛔ " + $r.Permission),
        ($r.Finance -eq "200" ? "✅ 200" : "⛔ " + $r.Finance),
        ($r.Report -eq "200" ? "✅ 200" : "⛔ " + $r.Report),
        ($r.Settings -eq "200" ? "✅ 200" : "⛔ " + $r.Settings)
    )
    $mdRows += ("| " + ($vals -join " | ") + " |")
}

$mdContent = $mdRows -join "`n"
Set-Content -Path "doc/MA_TRAN_THUC_TE_TEST_RUN.md" -Value $mdContent -Encoding UTF8
Write-Host "`nĐã xuất ma trận kết quả thực tế vào: doc/MA_TRAN_THUC_TE_TEST_RUN.md" -ForegroundColor Green
