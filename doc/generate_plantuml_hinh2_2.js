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

// PlantUML Source Code for Hinh 2.2 (Notice double backslash \\n so it is preserved in PUML)
const pumlContent = `@startuml Hinh2_2_So_Do_Use_Case_Tong_Quat_Omni_Beauty
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
' ĐỊNH NGHĨA 6 TÁC NHÂN HỆ THỐNG
' ========================================================
actor "Quản trị viên tối cao\\n(SuperAdmin)" as SuperAdmin
actor "Quản lý chi nhánh\\n(StoreManager)" as StoreManager
actor "Thu ngân điểm bán lẻ\\n(Cashier)" as Cashier
actor "Nhân viên thủ kho\\n(WarehouseStaff)" as WarehouseStaff
actor "Biên tập viên CMS\\n(ContentEditor)" as ContentEditor
actor "Khách hàng mua sắm\\n(Customer)" as Customer

' ========================================================
' KHUNG HỆ THỐNG VÀ 17 GÓI PHÂN HỆ CHỨC NĂNG
' ========================================================
rectangle "HỆ THỐNG THƯƠNG MẠI ĐIỆN TỬ VÀ BÁN LẺ MỸ PHẨM ĐA KÊNH OMNI BEAUTY" {

    package "1. BÁN LẺ & GIAO DỊCH ĐA KÊNH" #FDEDEC {
        usecase "UC-POS: Quản lý ca bán lẻ & Két tiền mù" as UC_POS
        usecase "UC-ORDER: Xử lý đơn & Cấp phát kho 2 giai đoạn" as UC_ORDER
        usecase "UC-CHECKOUT: Quét mã vạch & Thanh toán đa kênh" as UC_CHECKOUT
    }

    package "2. QUẢN TRỊ KHO & VẬN HÀNH HẠN DÙNG (FEFO)" #FEF9E7 {
        usecase "UC-BATCH: Quản lý Lô NSX & Giám sát FEFO 3 cấp" as UC_BATCH
        usecase "UC-IMPORT: Nhập kho theo PO & In tem mã vạch" as UC_IMPORT
        usecase "UC-TRF: Điều chuyển kho liên chi nhánh 3 bước" as UC_TRF
        usecase "UC-STOCKTAKE: Kiểm kê & Cân bằng tồn tự động" as UC_STOCKTAKE
        usecase "UC-CLEARANCE: Xả kho cận date & Xuất hủy hàng" as UC_CLEARANCE
    }

    package "3. TRẢI NGHIỆM KHÁCH HÀNG & CHĂM SÓC DA (CRM)" #EAFAF1 {
        usecase "UC-SMARTFILTER: Khám phá sản phẩm qua Bộ lọc da" as UC_SMARTFILTER
        usecase "UC-AI: Trắc nghiệm phân tích da & Trợ lý Routine AI" as UC_AI
        usecase "UC-CRM: Quản lý Hồ sơ sắc đẹp & Lịch sử da" as UC_CRM
        usecase "UC-LOYALTY: Ví điểm thưởng & Thăng hạng VIP tự động" as UC_LOYALTY
    }

    package "4. QUẢN TRỊ NỘI DUNG SỐ & TIẾP THỊ (CMS & PROMO)" #F4ECF7 {
        usecase "UC-MENU: Quản trị Cây Menu điều hướng đa hình" as UC_MENU
        usecase "UC-CMS: Biên tập bài viết cẩm nang & Banner sự kiện" as UC_CMS
        usecase "UC-MODERATE: Kiểm duyệt đánh giá & Ảnh swatch thực tế" as UC_MODERATE
        usecase "UC-PROMO: Cấu hình Khuyến mãi, Coupon & Voucher" as UC_PROMO
    }

    package "5. TÀI CHÍNH, BÁO CÁO & GIÁM SÁT TOÀN CHUỖI" #EBF5FB {
        usecase "UC-DASHBOARD: Giám sát Dashboard & Cảnh báo rủi ro AI" as UC_DASHBOARD
        usecase "UC-FINANCE: Hạch toán Sổ cái kép & Đối soát COD/P&L" as UC_FINANCE
        usecase "UC-REPORT: Báo cáo Doanh thu & Tồn kho đa chi nhánh" as UC_REPORT
    }

    package "6. BẢO MẬT & HỆ THỐNG LÕI (PBAC & CORE)" #F2F4F4 {
        usecase "UC-PBAC: Cấu hình Ma trận phân quyền động 51 quyền" as UC_PBAC
        usecase "UC-SETTINGS: Quản lý Người dùng & Cấu hình tham số" as UC_SETTINGS
    }
}

' ========================================================
' LIÊN KẾT GIỮA TÁC NHÂN VÀ CÁC CA SỬ DỤNG (USE CASE)
' ========================================================

SuperAdmin --> UC_PBAC
SuperAdmin --> UC_SETTINGS
SuperAdmin --> UC_DASHBOARD
SuperAdmin --> UC_FINANCE
SuperAdmin --> UC_REPORT
SuperAdmin --> UC_TRF
SuperAdmin --> UC_PROMO

StoreManager --> UC_POS
StoreManager --> UC_ORDER
StoreManager --> UC_TRF
StoreManager --> UC_STOCKTAKE
StoreManager --> UC_FINANCE
StoreManager --> UC_REPORT
StoreManager --> UC_PROMO

Cashier --> UC_POS
Cashier --> UC_CHECKOUT
Cashier --> UC_ORDER
Cashier --> UC_LOYALTY
Cashier --> UC_CRM

WarehouseStaff --> UC_BATCH
WarehouseStaff --> UC_IMPORT
WarehouseStaff --> UC_TRF
WarehouseStaff --> UC_STOCKTAKE
WarehouseStaff --> UC_CLEARANCE
WarehouseStaff --> UC_REPORT

ContentEditor --> UC_MENU
ContentEditor --> UC_CMS
ContentEditor --> UC_MODERATE

Customer --> UC_SMARTFILTER
Customer --> UC_AI
Customer --> UC_ORDER
Customer --> UC_CHECKOUT
Customer --> UC_CRM
Customer --> UC_LOYALTY
Customer --> UC_MODERATE

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_2_2_So_Do_Use_Case_Tong_Quat.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_2_2_So_Do_Use_Case_Tong_Quat.png');
    const svgDest = path.join(__dirname, 'Hinh_2_2_So_Do_Use_Case_Tong_Quat.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
