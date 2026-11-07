# scripts/test_hr_performance.ps1
# Kiểm thử hiệu năng trang danh sách nhân sự với 1.000 bản ghi giả lập

$baseUrl = "http://localhost:5116"
$connStr = "Server=localhost;Database=OmnichannelBeautyDb;User Id=sa;Password=12345678;TrustServerCertificate=True;"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "         KIỂM THỬ HIỆU NĂNG DANH SÁCH NHÂN SỰ VỚI 1.000 BẢN GHI GIẢ LẬP          " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

# 1. Đăng nhập SuperAdmin
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$handler.CookieContainer = [System.Net.CookieContainer]::new()
$client = [System.Net.Http.HttpClient]::new($handler)

$res = $client.GetAsync("$baseUrl/admin/auth/login").GetAwaiter().GetResult()
$html = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
$token = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value

$form = [System.Collections.Generic.Dictionary[string, string]]::new()
$form["Username"] = "superadmin"
$form["Password"] = "Admin@123"
$form["RememberMe"] = "false"
$form["__RequestVerificationToken"] = $token
$null = $client.PostAsync("$baseUrl/admin/auth/login", [System.Net.Http.FormUrlEncodedContent]::new($form)).GetAwaiter().GetResult()

# 2. Xóa dữ liệu rác cũ nếu có
$conn = [System.Data.SqlClient.SqlConnection]::new($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "DELETE FROM Users WHERE Username LIKE 'perf_staff_%'"
$deletedCount = $cmd.ExecuteNonQuery()
if ($deletedCount -gt 0) {
    Write-Host "Đã dọn dẹp $deletedCount bản ghi kiểm thử cũ." -ForegroundColor Gray
}

# 3. Tạo 1.000 bản ghi nhân sự giả lập
Write-Host "Đang tạo 1.000 bản ghi nhân sự giả lập bằng SQL batch..." -ForegroundColor Yellow
$swSeed = [System.Diagnostics.Stopwatch]::StartNew()

$seedSql = @"
DECLARE @i INT = 1;
BEGIN TRANSACTION;
WHILE @i <= 1000
BEGIN
    INSERT INTO Users (Username, FullName, Email, PasswordHash, PhoneNumber, UserType, DepartmentId, IsActive, IsDeleted, CreatedAt, EmployeeCode, SecurityStamp, JobTitle)
    VALUES (
        CONCAT('perf_staff_', @i),
        CONCAT('Nhân Viên Giả Lập ', @i),
        CONCAT('perf_', @i, '@canmakebeauty.vn'),
        'AQAAAAIAAYagAAAAEI0kL...',
        CONCAT('0912', RIGHT(CONCAT('000000', @i), 6)),
        2,
        3, -- PHONG_SALE
        1,
        0,
        DATEADD(minute, -@i, GETUTCDATE()),
        CONCAT('EMP_PERF_', RIGHT(CONCAT('000000', @i), 6)),
        NEWID(),
        'Chuyên Viên Giả Lập'
    );
    SET @i = @i + 1;
END;
COMMIT TRANSACTION;
"@
$cmd.CommandText = $seedSql
$cmd.ExecuteNonQuery()
$swSeed.Stop()

$cmd.CommandText = "SELECT COUNT(1) FROM Users WHERE UserType = 2 AND IsDeleted = 0"
$totalStaff = [int]$cmd.ExecuteScalar()
Write-Host "  -> Đã tạo 1.000 nhân viên trong $($swSeed.ElapsedMilliseconds) ms. Tổng nhân sự hiện tại: $totalStaff" -ForegroundColor Green

# 4. Đo thời gian phản hồi trang danh sách nhân sự (/Admin/User)
Write-Host "`nĐang tiến hành đo thời gian phản hồi (5 lần đo liên tiếp)..." -ForegroundColor Yellow

$testUrls = @(
    "$baseUrl/Admin/User?page=1&pageSize=20",
    "$baseUrl/Admin/User?page=2&pageSize=20",
    "$baseUrl/Admin/User?page=1&pageSize=50",
    "$baseUrl/Admin/User?keyword=perf_staff_500",
    "$baseUrl/Admin/User?departmentId=3&pageSize=20"
)

$times = @()

foreach ($u in $testUrls) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response = $client.GetAsync($u).GetAwaiter().GetResult()
    $sw.Stop()
    $status = [int]$response.StatusCode
    $elapsed = $sw.ElapsedMilliseconds
    $times += $elapsed

    $label = $u.Substring($u.IndexOf("?"))
    Write-Host "  GET /Admin/User$label -> Status $status, Thời gian: $elapsed ms" -ForegroundColor $(if ($elapsed -lt 300) { "Green" } else { "Red" })
}

$avg = ($times | Measure-Object -Average).Average
$min = ($times | Measure-Object -Minimum).Minimum
$max = ($times | Measure-Object -Maximum).Maximum

Write-Host "`n================================================================================" -ForegroundColor Cyan
Write-Host "                         KẾT QUẢ ĐO HIỆU NĂNG NHÂN SỰ                           " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Tổng số nhân sự trong CSDL: $totalStaff bản ghi" -ForegroundColor White
Write-Host "  Thời gian nhanh nhất: $min ms" -ForegroundColor Green
Write-Host "  Thời gian chậm nhất : $max ms" -ForegroundColor Yellow
Write-Host "  Thời gian trung bình: [ $avg ms ] (Tiêu chuẩn yêu cầu: < 300 ms)" -ForegroundColor $(if ($avg -lt 300) { "Green" } else { "Red" })

if ($avg -lt 300) {
    Write-Host "`n  --> [XÁC NHẬN ĐẠT CHUẨN]: Thời gian phản hồi $avg ms < 300 ms!" -ForegroundColor Green
} else {
    Write-Host "`n  --> [CẢNH BÁO]: Thời gian phản hồi vượt quá 300 ms!" -ForegroundColor Red
}

# 5. Dọn dẹp dữ liệu giả lập
Write-Host "`nĐang dọn dẹp 1.000 bản ghi nhân sự giả lập..." -ForegroundColor Gray
$cmd.CommandText = "DELETE FROM Users WHERE Username LIKE 'perf_staff_%'"
$cleanupCount = $cmd.ExecuteNonQuery()
$conn.Close()
Write-Host "Đã xóa an toàn $cleanupCount bản ghi kiểm thử khỏi cơ sở dữ liệu." -ForegroundColor Green
