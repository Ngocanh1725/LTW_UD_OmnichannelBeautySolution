# scripts/test_security.ps1
# Kiểm thử an toàn bảo mật (Security & Scope Enforcements - Phiên 8)

$baseUrl = "http://localhost:5116"
$connStr = "Server=localhost;Database=OmnichannelBeautyDb;User Id=sa;Password=12345678;TrustServerCertificate=True;"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "                KIỂM THỬ AN TOÀN BẢO MẬT & ENFORCEMENT RBAC                    " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

function New-Client {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.CookieContainer = [System.Net.CookieContainer]::new()
    return [System.Net.Http.HttpClient]::new($handler)
}

function Login-Admin([System.Net.Http.HttpClient]$client, [string]$user, [string]$pass) {
    $res = $client.GetAsync("$baseUrl/admin/auth/login").GetAwaiter().GetResult()
    $html = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $token = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
    
    $form = [System.Collections.Generic.Dictionary[string, string]]::new()
    $form["Username"] = $user
    $form["Password"] = $pass
    $form["RememberMe"] = "false"
    $form["__RequestVerificationToken"] = $token
    $content = [System.Net.Http.FormUrlEncodedContent]::new($form)
    $null = $client.PostAsync("$baseUrl/admin/auth/login", $content).GetAwaiter().GetResult()
    return $token
}

function Get-AdminToken([System.Net.Http.HttpClient]$client) {
    $page = $client.GetAsync("$baseUrl/Admin/User").GetAwaiter().GetResult()
    $html = $page.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    return [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
}

# Lấy ID từ DB
$conn = [System.Data.SqlClient.SqlConnection]::new($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()

$cmd.CommandText = "SELECT Id FROM Permissions WHERE Code = 'SETTINGS_MANAGE'"
$settingsPermId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Permissions WHERE Code = 'ORDER_CANCEL'"
$orderCancelPermId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'superadmin'"
$superAdminId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'ad1'"
$ad1Id = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'ad4'"
$ad4Id = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'hr01'"
$hr01Id = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'sale01'"
$sale01UserId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Roles WHERE Name = 'SuperAdmin'"
$superAdminRoleId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Roles WHERE Name = 'SalesStaff'"
$salesStaffRoleId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Roles WHERE Name = 'MarketingStaff'"
$mktStaffRoleId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'sale02'"
$sale02UserId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'mkt01'"
$mkt01UserId = [int]$cmd.ExecuteScalar()

# Đảm bảo sale01, sale02, mkt01 đang có Role mặc định và IsActive = 1 trước khi kiểm thử
$cmd.CommandText = @"
UPDATE Users SET IsActive = 1, IsDeleted = 0 WHERE Id IN ($sale01UserId, $sale02UserId, $mkt01UserId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = $sale01UserId AND RoleId = $salesStaffRoleId)
    INSERT INTO UserRoles (UserId, RoleId, AssignedByUserId, AssignedAt) VALUES ($sale01UserId, $salesStaffRoleId, $superAdminId, GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = $sale02UserId AND RoleId = $salesStaffRoleId)
    INSERT INTO UserRoles (UserId, RoleId, AssignedByUserId, AssignedAt) VALUES ($sale02UserId, $salesStaffRoleId, $superAdminId, GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = $mkt01UserId AND RoleId = $mktStaffRoleId)
    INSERT INTO UserRoles (UserId, RoleId, AssignedByUserId, AssignedAt) VALUES ($mkt01UserId, $mktStaffRoleId, $superAdminId, GETUTCDATE());
"@
$cmd.ExecuteNonQuery() | Out-Null

$conn.Close()

Write-Host "Tải cấu hình ID: SuperAdmin=$superAdminId, ad1=$ad1Id, ad4=$ad4Id, hr01=$hr01Id, sale01=$sale01UserId, SETTINGS_MANAGE=$settingsPermId, ORDER_CANCEL=$orderCancelPermId, SuperAdminRole=$superAdminRoleId" -ForegroundColor Gray

# -----------------------------------------------------------------------------------------
# Test Case 1: ad4 gửi POST giả mạo cấp quyền vượt scope (SETTINGS_MANAGE) cho sale01
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 1] ad4 gửi POST cấp quyền vượt scope (SETTINGS_MANAGE) cho sale01..." -ForegroundColor Yellow
$clientAd4 = New-Client
$null = Login-Admin $clientAd4 "ad4" "Admin@123"
$tokenAd4 = Get-AdminToken $clientAd4

$postJson = @{
    UserId = $sale01UserId
    PermissionId = $settingsPermId
    OverrideStatus = $true
    Reason = "Hack vượt cấp"
} | ConvertTo-Json

$req1 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/PermissionManagement/SaveUserOverride")
$req1.Headers.Add("RequestVerificationToken", $tokenAd4)
$req1.Content = [System.Net.Http.StringContent]::new($postJson, [System.Text.Encoding]::UTF8, "application/json")
$resp1 = $clientAd4.SendAsync($req1).GetAwaiter().GetResult()
$resp1Content = $resp1.Content.ReadAsStringAsync().GetAwaiter().GetResult()

if ([int]$resp1.StatusCode -eq 403 -or $resp1Content -like "*false*") {
    Write-Host "  -> ĐẠT: Yêu cầu bị chặn 403 / Thất bại đúng như thiết kế! (Status: $([int]$resp1.StatusCode))" -ForegroundColor Green
    Write-Host "     Phản hồi server: $resp1Content" -ForegroundColor Gray
} else {
    throw "THẤT BẠI: Server không chặn yêu cầu vượt scope! Status: $([int]$resp1.StatusCode)"
}

# -----------------------------------------------------------------------------------------
# Test Case 2: ad3 cố sửa superadmin, ad1, ad4 & cố gán role SuperAdmin cho hr01
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 2] ad3 cố sửa superadmin, ad1, ad4 & cố gán role SuperAdmin cho hr01..." -ForegroundColor Yellow
$clientAd3 = New-Client
$null = Login-Admin $clientAd3 "ad3" "Admin@123"
$tokenAd3 = Get-AdminToken $clientAd3

foreach ($target in @(
    @{ Name = "superadmin"; Id = $superAdminId },
    @{ Name = "ad1";        Id = $ad1Id },
    @{ Name = "ad4";        Id = $ad4Id }
)) {
    # Thử GET trang Edit và POST ToggleStatus
    $editTry = $clientAd3.GetAsync("$baseUrl/Admin/User/Edit/$($target.Id)").GetAwaiter().GetResult()
    $reqToggle = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/ToggleStatus/$($target.Id)")
    $reqToggle.Headers.Add("RequestVerificationToken", $tokenAd3)
    $reqToggle.Headers.Add("X-Requested-With", "XMLHttpRequest")
    $reqToggle.Content = [System.Net.Http.StringContent]::new("", [System.Text.Encoding]::UTF8, "application/x-www-form-urlencoded")
    $respToggle = $clientAd3.SendAsync($reqToggle).GetAwaiter().GetResult()
    $respToggleContent = $respToggle.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    if (([int]$editTry.StatusCode -eq 403 -or [int]$editTry.StatusCode -eq 302) -and ($respToggleContent -like "*FORBIDDEN*" -or $respToggleContent -like "*false*")) {
        Write-Host "  -> ĐẠT: ad3 cố sửa/khóa $($target.Name) bị chặn thành công! (GET Edit=$([int]$editTry.StatusCode), POST Toggle=$respToggleContent)" -ForegroundColor Green
    } else {
        throw "THẤT BẠI: ad3 có thể thao tác trên $($target.Name)!"
    }
}

# Thử gán role SuperAdmin qua BulkAction cho hr01
$req2b = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/Bulk")
$req2b.Headers.Add("RequestVerificationToken", $tokenAd3)
$bulkJson = @{
    StaffIds = @($hr01Id)
    Action = "assignrole"
    TargetRoleId = $superAdminRoleId
} | ConvertTo-Json
$req2b.Content = [System.Net.Http.StringContent]::new($bulkJson, [System.Text.Encoding]::UTF8, "application/json")
$resp2b = $clientAd3.SendAsync($req2b).GetAwaiter().GetResult()
$resp2bContent = $resp2b.Content.ReadAsStringAsync().GetAwaiter().GetResult()

if ([int]$resp2b.StatusCode -eq 403 -or $resp2bContent -like '*"succeededCount":0*' -or $resp2bContent -like "*false*") {
    Write-Host "  -> ĐẠT: ad3 gán role SuperAdmin cho hr01 bị chặn thành công! ($resp2bContent)" -ForegroundColor Green
} else {
    throw "THẤT BẠI: ad3 có thể gán vai trò vượt cấp! $resp2bContent"
}

# -----------------------------------------------------------------------------------------
# Test Case 3: Khóa sale01 khi đang đăng nhập -> Request tiếp theo bị chặn (SecurityStamp)
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 3] Khóa sale01 khi đang có phiên đăng nhập hoạt động..." -ForegroundColor Yellow
$clientSale01 = New-Client
$null = Login-Admin $clientSale01 "sale01" "Admin@123"

$preLock = $clientSale01.GetAsync("$baseUrl/Admin/Order").GetAwaiter().GetResult()
Write-Host "  -> Trước khi khóa: Status $([int]$preLock.StatusCode) (Hoạt động bình thường)." -ForegroundColor Gray

# Khóa tài khoản sale01 qua API của SuperAdmin (tự động đổi SecurityStamp + xóa cache)
$clientSuper = New-Client
$null = Login-Admin $clientSuper "superadmin" "Admin@123"
$tokenSuper = Get-AdminToken $clientSuper

# Làm mới cache quyền cho sale02, mkt01 qua Bulk Unlock
$syncReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/Bulk")
$syncReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$syncJson = @{ StaffIds = @($sale02UserId, $mkt01UserId); Action = "unlock" } | ConvertTo-Json
$syncReq.Content = [System.Net.Http.StringContent]::new($syncJson, [System.Text.Encoding]::UTF8, "application/json")
$null = $clientSuper.SendAsync($syncReq).GetAwaiter().GetResult()

$lockReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/ToggleStatus/$sale01UserId")
$lockReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$lockReq.Headers.Add("X-Requested-With", "XMLHttpRequest")
$lockReq.Content = [System.Net.Http.StringContent]::new("", [System.Text.Encoding]::UTF8, "application/x-www-form-urlencoded")
$null = $clientSuper.SendAsync($lockReq).GetAwaiter().GetResult()

# Request tiếp theo của sale01
$postLock = $clientSale01.GetAsync("$baseUrl/Admin/Order").GetAwaiter().GetResult()
$postLockLoc = if ($postLock.Headers.Location) { $postLock.Headers.Location.ToString() } else { "" }

if ([int]$postLock.StatusCode -eq 403 -or ([int]$postLock.StatusCode -eq 302 -and ($postLockLoc -like "*login*" -or $postLockLoc -like "*access-denied*"))) {
    Write-Host "  -> ĐẠT: Phiên làm việc của sale01 bị vô hiệu hóa tức thì qua SecurityStamp! (Status: $([int]$postLock.StatusCode), Loc: $postLockLoc)" -ForegroundColor Green
} else {
    throw "THẤT BẠI: sale01 vẫn tiếp tục truy cập được sau khi bị khóa!"
}

# Mở khóa lại cho sale01
$unlockReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/ToggleStatus/$sale01UserId")
$unlockReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$unlockReq.Headers.Add("X-Requested-With", "XMLHttpRequest")
$unlockReq.Content = [System.Net.Http.StringContent]::new("", [System.Text.Encoding]::UTF8, "application/x-www-form-urlencoded")
$null = $clientSuper.SendAsync($unlockReq).GetAwaiter().GetResult()

# -----------------------------------------------------------------------------------------
# Test Case 4: Thu hẹp scope của ad4 -> Cascade revoke trên sale01
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 4] Kiểm tra thu hẹp scope ad4 -> Cascade Revoke quyền ORDER_CANCEL trên sale01..." -ForegroundColor Yellow

# Bước 4.0: Đảm bảo Scope chuẩn 18 quyền & 2 phòng ban của ad4 trước khi test
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT Id FROM Permissions WHERE Code IN (
    'DASHBOARD_VIEW','DASHBOARD_ANALYTICS','ORDER_VIEW','ORDER_CREATE','ORDER_EDIT','ORDER_CANCEL','ORDER_CONFIRM',
    'CUSTOMER_VIEW','CUSTOMER_CREATE','CUSTOMER_EDIT','CUSTOMER_PROFILE_VIEW','PROMOTION_MANAGE','COUPON_MANAGE',
    'VOUCHER_MANAGE','LOYALTY_VIEW','LOYALTY_ADJUST_POINTS','LOYALTY_TIER_MANAGE','REPORT_SALES'
)
"@
$reader = $cmd.ExecuteReader()
$origAd4Perms = @()
while ($reader.Read()) { $origAd4Perms += [int]$reader.GetValue(0) }
$reader.Close()

$cmd.CommandText = "SELECT Id FROM Departments WHERE Code IN ('PHONG_SALE','PHONG_MARKETING')"
$reader2 = $cmd.ExecuteReader()
$origAd4Depts = @()
while ($reader2.Read()) { $origAd4Depts += [int]$reader2.GetValue(0) }
$reader2.Close()
$conn.Close()

$initScopeReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/Delegation/SaveScope")
$initScopeReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$initScopeJson = @{
    AdminUserId = $ad4Id
    CanCreateUser = $false
    CanEditUser = $true
    CanDeleteUser = $false
    CanResetPassword = $true
    CanAssignRole = $true
    CanDelegateFurther = $false
    MaxGrantLevel = 50
    PermissionIds = $origAd4Perms
    DepartmentIds = $origAd4Depts
    StoreIds = @()
} | ConvertTo-Json
$initScopeReq.Content = [System.Net.Http.StringContent]::new($initScopeJson, [System.Text.Encoding]::UTF8, "application/json")
$null = $clientSuper.SendAsync($initScopeReq).GetAwaiter().GetResult()

# Bước 4.1: ad4 cấp quyền ORDER_CANCEL cho sale01
$grantByAd4Req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/PermissionManagement/SaveUserOverride")
$grantByAd4Req.Headers.Add("RequestVerificationToken", $tokenAd4)
$grantByAd4Json = @{
    UserId = $sale01UserId
    PermissionId = $orderCancelPermId
    OverrideStatus = $true
    Reason = "ad4 cấp ORDER_CANCEL cho trưởng ca sale01"
} | ConvertTo-Json
$grantByAd4Req.Content = [System.Net.Http.StringContent]::new($grantByAd4Json, [System.Text.Encoding]::UTF8, "application/json")
$grantByAd4Resp = $clientAd4.SendAsync($grantByAd4Req).GetAwaiter().GetResult()

$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(1) FROM UserPermissions WHERE UserId = $sale01UserId AND PermissionId = $orderCancelPermId AND IsGranted = 1"
$hasOverrideBefore = [int]$cmd.ExecuteScalar()
$conn.Close()

# Bước 4.2: SuperAdmin thu hẹp Scope của ad4 (bỏ ORDER_CANCEL khỏi PermissionIds của ad4)
$narrowedPerms = @($origAd4Perms | Where-Object { $_ -ne $orderCancelPermId })
$narrowReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/Delegation/SaveScope")
$narrowReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$narrowJson = @{
    AdminUserId = $ad4Id
    CanCreateUser = $false
    CanEditUser = $true
    CanDeleteUser = $false
    CanResetPassword = $true
    CanAssignRole = $true
    CanDelegateFurther = $false
    MaxGrantLevel = 50
    PermissionIds = $narrowedPerms
    DepartmentIds = $origAd4Depts
    StoreIds = @()
} | ConvertTo-Json
$narrowReq.Content = [System.Net.Http.StringContent]::new($narrowJson, [System.Text.Encoding]::UTF8, "application/json")
$narrowResp = $clientSuper.SendAsync($narrowReq).GetAwaiter().GetResult()
$narrowRespContent = $narrowResp.Content.ReadAsStringAsync().GetAwaiter().GetResult()

# Kiểm tra trong DB xem quyền ORDER_CANCEL của sale01 đã bị cascade-revoke chưa
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(1) FROM UserPermissions WHERE UserId = $sale01UserId AND PermissionId = $orderCancelPermId AND IsGranted = 1"
$hasOverrideAfter = [int]$cmd.ExecuteScalar()
$conn.Close()

# Bước 4.3: Khôi phục lại Scope chuẩn cho ad4
$restoreScopeReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/Delegation/SaveScope")
$restoreScopeReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$restoreScopeJson = @{
    AdminUserId = $ad4Id
    CanCreateUser = $false
    CanEditUser = $true
    CanDeleteUser = $false
    CanResetPassword = $true
    CanAssignRole = $true
    CanDelegateFurther = $false
    MaxGrantLevel = 50
    PermissionIds = $origAd4Perms
    DepartmentIds = $origAd4Depts
    StoreIds = @()
} | ConvertTo-Json
$restoreScopeReq.Content = [System.Net.Http.StringContent]::new($restoreScopeJson, [System.Text.Encoding]::UTF8, "application/json")
$null = $clientSuper.SendAsync($restoreScopeReq).GetAwaiter().GetResult()

if ($hasOverrideBefore -eq 1 -and $hasOverrideAfter -eq 0) {
    Write-Host "  -> ĐẠT: Khi thu hẹp scope của ad4, quyền ORDER_CANCEL đã cấp cho sale01 bị Cascade Revoke tự động! (Trước: $hasOverrideBefore, Sau: $hasOverrideAfter)" -ForegroundColor Green
    Write-Host "     Phản hồi SaveScope: $narrowRespContent" -ForegroundColor Gray
} else {
    throw "THẤT BẠI: Cascade revoke không khớp! (Before=$hasOverrideBefore, After=$hasOverrideAfter)"
}

# -----------------------------------------------------------------------------------------
# Test Case 5: Khách hàng truy cập /Admin -> Bị chuyển hướng hoặc 403
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 5] Khách hàng (customer01) cố tình truy cập /Admin..." -ForegroundColor Yellow
$clientCust = New-Client
$resCust = $clientCust.GetAsync("$baseUrl/Account/Login").GetAwaiter().GetResult()
$htmlCust = $resCust.Content.ReadAsStringAsync().GetAwaiter().GetResult()
$tokenCust = [regex]::Match($htmlCust, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
$formCust = [System.Collections.Generic.Dictionary[string, string]]::new()
$formCust["Username"] = "customer01"
$formCust["Password"] = "Admin@123"
$formCust["RememberMe"] = "false"
$formCust["__RequestVerificationToken"] = $tokenCust
$null = $clientCust.PostAsync("$baseUrl/Account/Login", [System.Net.Http.FormUrlEncodedContent]::new($formCust)).GetAwaiter().GetResult()

$adminTry = $clientCust.GetAsync("$baseUrl/Admin/Dashboard").GetAwaiter().GetResult()
$adminTryLoc = if ($adminTry.Headers.Location) { $adminTry.Headers.Location.ToString() } else { "" }

if ([int]$adminTry.StatusCode -eq 403 -or ([int]$adminTry.StatusCode -eq 302 -and $adminTryLoc -like "*login*")) {
    Write-Host "  -> ĐẠT: Khách hàng bị chặn tuyệt đối khỏi phân hệ Admin! Status: $([int]$adminTry.StatusCode), Loc: $adminTryLoc" -ForegroundColor Green
} else {
    throw "THẤT BẠI: Khách hàng có thể truy cập /Admin!"
}

# -----------------------------------------------------------------------------------------
# Test Case 6: Thiếu AntiForgery token trên POST thay đổi dữ liệu -> 400 Bad Request
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 6] Gửi POST thay đổi dữ liệu mà không có AntiForgery Token..." -ForegroundColor Yellow
$clientNoAf = New-Client
$null = Login-Admin $clientNoAf "superadmin" "Admin@123"

$req6 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/ToggleStatus/$sale01UserId")
$req6.Content = [System.Net.Http.StringContent]::new("", [System.Text.Encoding]::UTF8, "application/x-www-form-urlencoded")
$resp6 = $clientNoAf.SendAsync($req6).GetAwaiter().GetResult()

if ([int]$resp6.StatusCode -eq 400) {
    Write-Host "  -> ĐẠT: Thiếu AntiForgery Token bị máy chủ từ chối với HTTP 400 Bad Request đúng chuẩn ASP.NET Core!" -ForegroundColor Green
} else {
    throw "THẤT BẠI: Server trả về status: $([int]$resp6.StatusCode)"
}

Write-Host "`n================================================================================" -ForegroundColor Cyan
Write-Host "                HOÀN TẤT TOÀN BỘ 6 KỊCH BẢN BẢO MẬT & ỦY QUYỀN                 " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
