const fs = require('fs');
const zlib = require('zlib');
const https = require('https');
const path = require('path');

function encode64(data) {
    let r = '';
    for (let i = 0; i < data.length; i += 3) {
        if (i + 2 == data.length) {
            r += append3bytes(data[i], data[i + 1], 0);
        } else if (i + 1 == data.length) {
            r += append3bytes(data[i], 0, 0);
        } else {
            r += append3bytes(data[i], data[i + 1], data[i + 2]);
        }
    }
    return r;
}

function append3bytes(b1, b2, b3) {
    let c1 = b1 >> 2;
    let c2 = ((b1 & 0x3) << 4) | (b2 >> 4);
    let c3 = ((b2 & 0xF) << 2) | (b3 >> 6);
    let c4 = b3 & 0x3F;
    return encode6bit(c1 & 0x3F) + encode6bit(c2 & 0x3F) + encode6bit(c3 & 0x3F) + encode6bit(c4 & 0x3F);
}

function encode6bit(b) {
    if (b < 10) return String.fromCharCode(48 + b);
    b -= 10;
    if (b < 26) return String.fromCharCode(65 + b);
    b -= 26;
    if (b < 26) return String.fromCharCode(97 + b);
    b -= 26;
    if (b == 0) return '-';
    if (b == 1) return '_';
    return '?';
}

function downloadFile(url, destPath) {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(destPath);
        https.get(url, (res) => {
            if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                return https.get(res.headers.location, (redRes) => {
                    redRes.pipe(file);
                    file.on('finish', () => file.close(resolve));
                }).on('error', reject);
            }
            if (res.statusCode !== 200) {
                let errBody = '';
                res.on('data', d => errBody += d);
                res.on('end', () => reject(new Error(`Failed to download ${url}: HTTP ${res.statusCode} - ${errBody.slice(0, 200)}`)));
                return;
            }
            res.pipe(file);
            file.on('finish', () => {
                file.close(resolve);
            });
        }).on('error', (err) => {
            fs.unlink(destPath, () => {});
            reject(err);
        });
    });
}

const pumlContent = `@startuml SD01_Dynamic_PBAC_Resolution
skinparam backgroundColor #FFFFFF
skinparam defaultFontName "Times New Roman"
skinparam defaultFontSize 12
skinparam roundCorner 6
skinparam shadowing false
skinparam ArrowColor #1B4F72
skinparam ArrowThickness 1.2
skinparam SequenceLifeLineBorderColor #1B4F72
skinparam SequenceLifeLineBackgroundColor #EBF5FB
skinparam SequenceGroupBorderColor #2C3E50
skinparam SequenceGroupBackgroundColor #F8F9F9
skinparam SequenceGroupHeaderFontColor #1B4F72
skinparam SequenceGroupFontSize 11
autonumber

actor "Nhân sự / Quản lý" as User #E8F8F5
participant "Browser / Client" as Client #EBF5FB

box "Presentation Layer (Omnichannel.Web)" #EAF2F8
    participant "CookieAuthMiddleware" as CookieAuth #D4E6F1
    participant "DynamicPermission\\nPolicyProvider" as PolicyProvider #D4E6F1
    participant "PermissionAuthorization\\nHandler" as AuthHandler #D4E6F1
end box

box "Infrastructure Layer (Omnichannel.Infrastructure)" #E8F8F5
    participant "PermissionService" as PermService #D1F2EB
    participant "IMemoryCache\\n(L1 Cache)" as Cache #D1F2EB
    database "SQL Server\\nDatabase" as DB #D1F2EB
end box

User -> Client: Bấm chọn menu "Quản lý Chuyển kho"
activate Client

Client -> CookieAuth: HTTP GET /Admin/StockTransfer (Kèm Auth Cookie)
activate CookieAuth

CookieAuth -> CookieAuth: 1. Giải mã Cookie bảo mật\\n2. Tái tạo ClaimsPrincipal (UserId=4, Role='WarehouseStaff')
CookieAuth -> PolicyProvider: GetPolicyAsync("PERM:TRANSFER:VIEW")
activate PolicyProvider

PolicyProvider -> PolicyProvider: Khởi tạo Policy động với\\nPermissionRequirement("TRANSFER_VIEW")
PolicyProvider --> AuthHandler: Chuyển giao context kiểm tra quyền
deactivate PolicyProvider

activate AuthHandler
AuthHandler -> PermService: GetEffectivePermissionsAsync(userId=4)
activate PermService

PermService -> Cache: TryGetValue("UserPerms:4", out cachedPerms)
activate Cache

alt #EDF7ED Trường hợp 1: Cache HIT (Đã lưu tập quyền trong RAM)
    Cache --> PermService: Trả về HashSet<string> quyền hữu hiệu (< 1ms)
else #FFF9E6 Trường hợp 2: Cache MISS (Chưa nạp hoặc bị xóa do đổi quyền)
    Cache --> PermService: Trả về null
    deactivate Cache
    
    PermService -> DB: 1. Truy vấn quyền từ vai trò (UserRoles -> RolePermissions)
    activate DB
    DB --> PermService: Danh sách quyền cơ sở từ vai trò
    deactivate DB
    
    PermService -> DB: 2. Truy vấn can thiệp quyền cá nhân (UserPermissions Overrides)
    activate DB
    DB --> PermService: Danh sách can thiệp (IsGranted: 1=Cấp, 0=Tước)
    deactivate DB
    
    PermService -> PermService: Hòa trộn tập quyền\\n(Áp dụng luật Deny tước quyền ưu tiên tuyệt đối)
    PermService -> Cache: Set("UserPerms:4", effectivePerms, TTL=30 phút)
    activate Cache
    deactivate Cache
end

PermService --> AuthHandler: Trả về tập quyền hữu hiệu của User #4
deactivate PermService

alt #EDF7ED Kiểm tra thấy mã quyền "TRANSFER_VIEW" tồn tại
    AuthHandler -> AuthHandler: context.Succeed(requirement)
    AuthHandler --> CookieAuth: Cấp phép truy cập thành công (Success)
    CookieAuth --> Client: HTTP 200 OK (Kèm HTML màn hình Quản lý điều chuyển kho)
    Client --> User: Hiển thị giao diện màn hình Điều chuyển kho
else #FDEDEC Không sở hữu quyền hạn "TRANSFER_VIEW"
    AuthHandler -> AuthHandler: context.Fail()
    AuthHandler --> CookieAuth: Từ chối truy cập (Access Denied)
    CookieAuth --> Client: HTTP 403 Forbidden (Redirect /Account/AccessDenied)
    Client --> User: Hiển thị thông báo "Bạn không có quyền truy cập tính năng này"
end

deactivate AuthHandler
deactivate CookieAuth
deactivate Client

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_3_1_So_Do_Tuan_Tu_SD01.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_3_1_So_Do_Tuan_Tu_SD01.png');
    const svgDest = path.join(__dirname, 'Hinh_3_1_So_Do_Tuan_Tu_SD01.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
