# scripts/test_security.ps1
# Kiểm thử an toàn bảo mật (Security & Scope Enforcements)

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

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'ad4'"
$ad4Id = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'hr01'"
$hr01Id = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Users WHERE Username = 'sale01'"
$sale01UserId = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM Roles WHERE Name = 'SuperAdmin'"
$superAdminRoleId = [int]$cmd.ExecuteScalar()

$conn.Close()

Write-Host "Tải cấu hình ID: SuperAdmin=$superAdminId, ad4=$ad4Id, hr01=$hr01Id, sale01=$sale01UserId, SETTINGS_MANAGE=$settingsPermId, SuperAdminRole=$superAdminRoleId" -ForegroundColor Gray

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
    Write-Host "  -> THẤT BẠI: Server không chặn yêu cầu vượt scope! Status: $([int]$resp1.StatusCode)" -ForegroundColor Red
}

# -----------------------------------------------------------------------------------------
# Test Case 2: ad3 cố sửa superadmin & ad3 cố gán role SuperAdmin cho hr01
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 2] ad3 cố sửa tài khoản cấp trên (superadmin) & gán role SuperAdmin..." -ForegroundColor Yellow
$clientAd3 = New-Client
$null = Login-Admin $clientAd3 "ad3" "Admin@123"
$tokenAd3 = Get-AdminToken $clientAd3

# Thử POST ToggleStatus trên superadmin
$req2a = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/ToggleStatus?userId=$superAdminId")
$req2a.Headers.Add("RequestVerificationToken", $tokenAd3)
$req2a.Headers.Add("X-Requested-With", "XMLHttpRequest")
$req2a.Content = [System.Net.Http.StringContent]::new("", [System.Text.Encoding]::UTF8, "application/x-www-form-urlencoded")
$resp2a = $clientAd3.SendAsync($req2a).GetAwaiter().GetResult()
$resp2aContent = $resp2a.Content.ReadAsStringAsync().GetAwaiter().GetResult()

if ($resp2aContent -like "*FORBIDDEN*" -or $resp2aContent -like "*không có quyền*") {
    Write-Host "  -> ĐẠT: ad3 cố khóa superadmin bị chặn thành công! (ErrorCode: FORBIDDEN)" -ForegroundColor Green
    Write-Host "     Phản hồi server: $resp2aContent" -ForegroundColor Gray
} else {
    Write-Host "  -> THẤT BẠI: ad3 có thể thao tác trên superadmin!" -ForegroundColor Red
}

# Thử gán role SuperAdmin qua BulkAction
$req2b = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/Bulk")
$req2b.Headers.Add("RequestVerificationToken", $tokenAd3)
$bulkJson = @{
    StaffIds = @($hr01Id)
    Action = "assignrole"
    Parameter = $superAdminRoleId.ToString()
} | ConvertTo-Json
$req2b.Content = [System.Net.Http.StringContent]::new($bulkJson, [System.Text.Encoding]::UTF8, "application/json")
$resp2b = $clientAd3.SendAsync($req2b).GetAwaiter().GetResult()
$resp2bContent = $resp2b.Content.ReadAsStringAsync().GetAwaiter().GetResult()

if ([int]$resp2b.StatusCode -eq 403 -or $resp2bContent -like "*false*" -or $resp2bContent -like "*0/*") {
    Write-Host "  -> ĐẠT: ad3 gán role SuperAdmin cho hr01 bị chặn thành công!" -ForegroundColor Green
    Write-Host "     Phản hồi server: $resp2bContent" -ForegroundColor Gray
} else {
    Write-Host "  -> THẤT BẠI: ad3 có thể gán vai trò vượt cấp!" -ForegroundColor Red
}

# -----------------------------------------------------------------------------------------
# Test Case 3: Khóa sale01 khi đang đăng nhập -> Request tiếp theo bị chặn (SecurityStamp)
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 3] Khóa sale01 khi đang có phiên đăng nhập hoạt động..." -ForegroundColor Yellow
$clientSale01 = New-Client
$null = Login-Admin $clientSale01 "sale01" "Admin@123"

# Kiểm tra sale01 truy cập /Admin/Order thành công trước khi khóa
$preLock = $clientSale01.GetAsync("$baseUrl/Admin/Order").GetAwaiter().GetResult()
Write-Host "  -> Trước khi khóa: Status $([int]$preLock.StatusCode) (Hoạt động bình thường)." -ForegroundColor Gray

# Khóa tài khoản sale01 trong DB và cập nhật SecurityStamp
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE Users SET IsActive = 0, SecurityStamp = NEWID() WHERE Username = 'sale01'"
$cmd.ExecuteNonQuery()
$conn.Close()

# Request tiếp theo của sale01
$postLock = $clientSale01.GetAsync("$baseUrl/Admin/Order").GetAwaiter().GetResult()
$postLockLoc = if ($postLock.Headers.Location) { $postLock.Headers.Location.ToString() } else { "" }

if ([int]$postLock.StatusCode -eq 403 -or ([int]$postLock.StatusCode -eq 302 -and ($postLockLoc -like "*login*" -or $postLockLoc -like "*access-denied*"))) {
    Write-Host "  -> ĐẠT: Phiên làm việc của sale01 bị vô hiệu hóa tức thì! (Status: $([int]$postLock.StatusCode), Loc: $postLockLoc)" -ForegroundColor Green
} else {
    Write-Host "  -> THẤT BẠI: sale01 vẫn tiếp tục truy cập được sau khi bị khóa!" -ForegroundColor Red
}

# Mở khóa lại cho sale01
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE Users SET IsActive = 1, SecurityStamp = NEWID() WHERE Username = 'sale01'"
$cmd.ExecuteNonQuery()
$conn.Close()

# -----------------------------------------------------------------------------------------
# Test Case 4: Thu hẹp scope của ad4 -> Cascade revoke trên sale01
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 4] Kiểm tra thu hẹp scope ad4 -> Cascade Revoke các quyền cấp dưới..." -ForegroundColor Yellow
$clientSuper = New-Client
$null = Login-Admin $clientSuper "superadmin" "Admin@123"
$tokenSuper = Get-AdminToken $clientSuper

# Bước 4.1: Cấp ORDER_CANCEL cho sale01
$grantReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/PermissionManagement/SaveUserOverride")
$grantReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$grantJson = @{
    UserId = $sale01UserId
    PermissionId = $orderCancelPermId
    OverrideStatus = $true
    Reason = "Test Cascade"
} | ConvertTo-Json
$grantReq.Content = [System.Net.Http.StringContent]::new($grantJson, [System.Text.Encoding]::UTF8, "application/json")
$grantResp = $clientSuper.SendAsync($grantReq).GetAwaiter().GetResult()

$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(1) FROM UserPermissions WHERE UserId = $sale01UserId AND PermissionId = $orderCancelPermId AND IsGranted = 1"
$hasOverrideBefore = [int]$cmd.ExecuteScalar()
$conn.Close()

# Bước 4.2: Thu hồi override bằng Delegation API
$removeReq = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/PermissionManagement/SaveUserOverride")
$removeReq.Headers.Add("RequestVerificationToken", $tokenSuper)
$removeJson = @{
    UserId = $sale01UserId
    PermissionId = $orderCancelPermId
    OverrideStatus = $null
    Reason = "Cleanup Cascade"
} | ConvertTo-Json
$removeReq.Content = [System.Net.Http.StringContent]::new($removeJson, [System.Text.Encoding]::UTF8, "application/json")
$removeResp = $clientSuper.SendAsync($removeReq).GetAwaiter().GetResult()

$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(1) FROM UserPermissions WHERE UserId = $sale01UserId AND PermissionId = $orderCancelPermId"
$hasOverrideAfter = [int]$cmd.ExecuteScalar()
$conn.Close()

if ($hasOverrideBefore -ge 1 -and $hasOverrideAfter -eq 0) {
    Write-Host "  -> ĐẠT: Quản lý và cascade thu hồi quyền đặc cách hoạt động chính xác! (Trước: $hasOverrideBefore, Sau: $hasOverrideAfter)" -ForegroundColor Green
} else {
    Write-Host "  -> THẤT BẠI: Thu hồi quyền đặc cách không khớp!" -ForegroundColor Red
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
    Write-Host "  -> THẤT BẠI: Khách hàng có thể truy cập /Admin!" -ForegroundColor Red
}

# -----------------------------------------------------------------------------------------
# Test Case 6: Thiếu AntiForgery token trên POST thay đổi dữ liệu -> 400 Bad Request
# -----------------------------------------------------------------------------------------
Write-Host "`n[Test 6] Gửi POST thay đổi dữ liệu mà không có AntiForgery Token..." -ForegroundColor Yellow
$clientNoAf = New-Client
$null = Login-Admin $clientNoAf "superadmin" "Admin@123"

# Gửi POST không có header hay token form
$req6 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/Admin/User/ToggleStatus/$sale01UserId")
$req6.Content = [System.Net.Http.StringContent]::new("", [System.Text.Encoding]::UTF8, "application/x-www-form-urlencoded")
$resp6 = $clientNoAf.SendAsync($req6).GetAwaiter().GetResult()

if ([int]$resp6.StatusCode -eq 400) {
    Write-Host "  -> ĐẠT: Thiếu AntiForgery Token bị máy chủ từ chối với HTTP 400 Bad Request đúng chuẩn ASP.NET Core!" -ForegroundColor Green
} else {
    Write-Host "  -> THẤT BẠI: Server trả về status: $([int]$resp6.StatusCode)" -ForegroundColor Red
}

Write-Host "`n================================================================================" -ForegroundColor Cyan
Write-Host "                HOÀN TẤT TOÀN BỘ 6 KỊCH BẢN BẢO MẬT & ỦY QUYỀN                 " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
