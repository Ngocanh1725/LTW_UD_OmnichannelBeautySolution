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

// PlantUML Source Code for Hinh 2.6
const pumlContent = `@startuml Hinh2_6_So_Do_Use_Case_Chi_Tiet_UC04
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
' ĐỊNH NGHĨA CÁC TÁC NHÂN LIÊN QUAN ĐẾN ĐIỀU CHUYỂN
' ========================================================
actor "Quản lý chi nhánh nhận\\n(StoreManager - Destination)" as DestManager
actor "Quản trị viên / QL kho xuất\\n(SuperAdmin / Source Manager)" as SourceApprover
actor "Thủ kho kho xuất\\n(WarehouseStaff - Source)" as SourceStaff
actor "Thủ kho chi nhánh nhận\\n(WarehouseStaff - Destination)" as DestStaff

actor "Hệ quản trị CSDL & Sổ cái\\n(SQL Server: StockTransfers & Ledgers)" as DBService <<System>>

' ========================================================
' KHUNG RANH GIỚI CA SỬ DỤNG CHI TIẾT UC-04
' ========================================================
rectangle "PHÂN HỆ ĐIỀU CHUYỂN KHO LIÊN CHI NHÁNH QUA BA BƯỚC (3-STEP TRANSFER)" {

    usecase "UC-04: Điều chuyển hàng hóa liên chi nhánh bảo toàn tồn kho\\n(3-Step Inter-Branch Stock Transfer)" as UC04 #EBF5FB
    
    ' --- BƯỚC 1: ĐỀ XUẤT & PHÊ DUYỆT (PENDING) ---
    package "BƯỚC 1: ĐỀ XUẤT & PHÊ DUYỆT LỆNH CHUYỂN (STATUS = PENDING)" #FEF9E7 {
        usecase "UC-04.1: Lập phiếu đề xuất điều chuyển kho\\n(Chọn kho xuất, kho đích, mã hàng, số lượng)" as UC04_1 #FDEBD0
        usecase "UC-04.2: Thẩm định tồn kho & Phê duyệt lệnh xuất\\n(Chuyển trạng thái phiếu sang APPROVED)" as UC04_2 #FAD7A0
        usecase "UC-04.2a: Từ chối yêu cầu điều chuyển\\n(Nhập lý do từ chối: Status = REJECTED)" as UC04_2a #FADBD8
    }
    
    ' --- BƯỚC 2: XUẤT VẬN CHUYỂN THEO FEFO (IN TRANSIT) ---
    package "BƯỚC 2: XUẤT VẬN CHUYỂN THEO FEFO (STATUS = SHIPPED / IN TRANSIT)" #E8F8F5 {
        usecase "UC-04.3: Chỉ định lô xuất theo giải thuật FEFO\\n(Lựa chọn Batch có hạn dùng gần nhất)" as UC04_3 #D1F2EB
        usecase "UC-04.4: Xuất kho xe tải & Trừ tồn vật lý kho nguồn\\n(Physical -= Qty tại Kho xuất, Status = IN_TRANSIT)" as UC04_4 #A3E4D7
        usecase "UC-04.5: In lệnh điều chuyển kiêm vận đơn nội bộ\\n(In phiếu giao nhận kèm mã tra cứu TransferCode)" as UC04_5 #76D7C4
    }
    
    ' --- BƯỚC 3: TIẾP NHẬN & HOÀN TẤT (COMPLETED) ---
    package "BƯỚC 3: TIẾP NHẬN & HOÀN TẤT TỒN KHO (STATUS = COMPLETED)" #EAFAF1 {
        usecase "UC-04.6: Kiểm đếm thực tế hàng hóa tại kho đích\\n(Quét mã vạch kiểm tra số lượng và chất lượng)" as UC04_6 #D5F5E3
        usecase "UC-04.7: Lập biên bản sự cố hao hụt / hư hỏng\\n(Hạch toán tổn thất vận chuyển vào Sổ cái)" as UC04_7 #FDEDEC
        usecase "UC-04.8: Xác nhận nhập kho & Ghi tăng tồn kho đích\\n(Physical += ActualQty, Status = COMPLETED)" as UC04_8 #A9DFBF
    }
}

' ========================================================
' CÁC MỐI QUAN HỆ VÀ LIÊN KẾT CA SỬ DỤNG
' ========================================================

' 1. Tác nhân Chi nhánh nhận lập phiếu
DestManager --> UC04
DestManager --> UC04_1

' 2. Tác nhân Kho nguồn phê duyệt
SourceApprover --> UC04_2
SourceApprover --> UC04_2a

' 3. Tác nhân Thủ kho nguồn đóng gói & xuất
SourceStaff --> UC04_3
SourceStaff --> UC04_4
SourceStaff --> UC04_5

' 4. Tác nhân Thủ kho đích tiếp nhận
DestStaff --> UC04_6
DestStaff --> UC04_8

' Luồng quan hệ Bước 1 (Đề xuất & Duyệt)
UC04 ..> UC04_1 : <<include>>
UC04 ..> UC04_2 : <<include>>
UC04_2 <.. UC04_2a : <<extend>>\\n[Không đủ hàng / Từ chối]

' Luồng quan hệ Bước 2 (Xuất hàng)
UC04 ..> UC04_3 : <<include>>
UC04_3 ..> UC04_4 : <<include>>
UC04_4 ..> UC04_5 : <<include>>

' Luồng quan hệ Bước 3 (Nhập kho)
UC04 ..> UC04_6 : <<include>>
UC04_6 <.. UC04_7 : <<extend>>\\n[Hư hại / Thiếu hàng]
UC04_6 ..> UC04_8 : <<include>>

' Tác động dữ liệu CSDL
UC04_1 --> DBService : 1. Tạo bản ghi StockTransfers (PENDING)
UC04_4 --> DBService : 2. Trừ PhysicalStock kho nguồn & Status = IN_TRANSIT
UC04_7 --> DBService : 3. Ghi bút toán tổn thất FinancialLedgers (nếu có)
UC04_8 --> DBService : 4. Tăng PhysicalStock kho đích & Status = COMPLETED

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_2_6_So_Do_Use_Case_Chi_Tiet_UC04.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_2_6_So_Do_Use_Case_Chi_Tiet_UC04.png');
    const svgDest = path.join(__dirname, 'Hinh_2_6_So_Do_Use_Case_Chi_Tiet_UC04.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
