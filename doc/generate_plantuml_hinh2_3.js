const fs = require('fs');
const zlib = require('zlib');
const https = require('https');
const path = require('path');

// PlantUML custom base64 encoder on Buffer
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
                reject(new Error(`Failed to download ${url}: HTTP ${res.statusCode}`));
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

// PlantUML Source Code for Hinh 2.3
const pumlContent = `@startuml Hinh2_3_So_Do_Use_Case_Chi_Tiet_UC01
left to right direction
skinparam backgroundColor #FFFFFF
skinparam defaultFontName "Times New Roman"
skinparam defaultFontSize 12
skinparam roundCorner 8
skinparam shadowing false
skinparam ArrowColor #1B4F72
skinparam ArrowThickness 1.2
skinparam ActorBorderColor #1B4F72
skinparam ActorBackgroundColor #EBF5FB
skinparam packageStyle rectangle

skinparam usecaseBackgroundColor #F4F6F7
skinparam usecaseBorderColor #2C3E50
skinparam usecaseFontSize 11
skinparam usecaseBorderThickness 1

' ========================================================
' ĐỊNH NGHĨA CÁC TÁC NHÂN (CHÍNH & HỆ THỐNG HỖ TRỢ)
' ========================================================
actor "Quản trị viên tối cao\\n(SuperAdmin)" as SuperAdmin

actor "Dịch vụ Bộ nhớ đệm\\n(PermissionCacheService)" as CacheService <<System>>
actor "Hệ quản trị CSDL\\n(SQL Server Database)" as DBService <<System>>

' ========================================================
' KHUNG RANH GIỚI CA SỬ DỤNG CHI TIẾT UC-01
' ========================================================
rectangle "PHÂN HỆ BẢO MẬT VÀ PHÂN QUYỀN HẠT NHÂN ĐỘNG (DYNAMIC PBAC)" {

    usecase "UC-01: Cấu hình Ma trận phân quyền động\\n(Dynamic Permission Matrix)" as UC01 #EBF5FB
    
    usecase "UC-01.1: Xem lưới ma trận 51 quyền\\n(Gom nhóm 17 phân hệ x 6 vai trò)" as UC01_1
    
    usecase "UC-01.2: Cập nhật phân quyền theo Vai trò\\n(Role Matrix Configuration)" as UC01_2
    
    usecase "UC-01.3: Ghi đè quyền người dùng cá nhân\\n(User Permission Override)" as UC01_3
    
    usecase "UC-01.3a: Cấp quyền riêng biệt\\n(Grant: IsGranted = true)" as UC01_3a #EAFAF1
    usecase "UC-01.3b: Tước bỏ quyền vai trò\\n(Revoke: IsGranted = false)" as UC01_3b #FDEDEC
    
    usecase "UC-01.4: Tự động xóa bộ nhớ đệm phân quyền\\n(Invalidate User Permission Cache)" as UC01_4 #FEF9E7
    
    usecase "UC-01.5: Ghi nhật ký kiểm toán an ninh\\n(Security Audit Logging)" as UC01_5 #F4ECF7
}

' ========================================================
' CÁC MỐI QUAN HỆ VÀ LIÊN KẾT CA SỬ DỤNG
' ========================================================

' Tác nhân SuperAdmin kích hoạt ca sử dụng chính
SuperAdmin --> UC01

' Quan hệ <<include>> bắt buộc trong quy trình UC-01
UC01 ..> UC01_1 : <<include>>
UC01 ..> UC01_4 : <<include>>
UC01 ..> UC01_5 : <<include>>

' Hai nhánh phân quyền cốt lõi mở rộng từ UC-01
UC01 <.. UC01_2 : <<extend>>\\n[Chế độ Vai trò]
UC01 <.. UC01_3 : <<extend>>\\n[Chế độ Cá nhân]

' Các trường hợp ghi đè quyền cá nhân
UC01_3 <.. UC01_3a : <<extend>>\\n[Ủy quyền thêm]
UC01_3 <.. UC01_3b : <<extend>>\\n[Hạn chế quyền]

' Tác vụ tương tác với các hệ thống hỗ trợ
UC01_4 --> CacheService : 1. Xóa sạch Cache\\n(Key: Perm_UserId)
UC01_2 --> DBService : 2. Cập nhật bảng RolePermissions
UC01_3 --> DBService : 3. Cập nhật bảng UserPermissions
UC01_5 --> DBService : 4. Ghi bản ghi AuditLogs

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_2_3_So_Do_Use_Case_Chi_Tiet_UC01.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_2_3_So_Do_Use_Case_Chi_Tiet_UC01.png');
    const svgDest = path.join(__dirname, 'Hinh_2_3_So_Do_Use_Case_Chi_Tiet_UC01.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
