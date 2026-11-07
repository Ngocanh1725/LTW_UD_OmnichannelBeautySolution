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

// PlantUML Source Code for Hinh 2.4
const pumlContent = `@startuml Hinh2_4_So_Do_Use_Case_Chi_Tiet_UC02
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
' ĐỊNH NGHĨA CÁC TÁC NHÂN (CHÍNH, BÊN NGOÀI & HỆ THỐNG)
' ========================================================
actor "Khách hàng mua sắm\\n(Customer)" as Customer

actor "Cổng thanh toán điện tử\\n(VietQR / VNPay / MoMo)" as PaymentGateway <<External>>
actor "Dịch vụ Nền hẹn giờ\\n(OrderCleanupWorker)" as BackgroundWorker <<Service>>
actor "Hệ thống Quản lý Tồn kho & CSDL\\n(SQL Server: ProductBatches & Orders)" as DBService <<System>>

' ========================================================
' KHUNG RANH GIỚI CA SỬ DỤNG CHI TIẾT UC-02
' ========================================================
rectangle "PHÂN HỆ ĐẶT HÀNG & CẤP PHÁT TỒN KHO 2 GIAI ĐOẠN (2-PHASE DEDUCT)" {

    usecase "UC-02: Đặt hàng trực tuyến & Trừ kho FEFO 2 giai đoạn\\n(2-Phase Inventory Allocation)" as UC02 #EBF5FB
    
    ' --- GIAI ĐOẠN 1: KHÓA TỒN TẠM TÍNH (SOFT RESERVE) ---
    package "GIAI ĐOẠN 1: KHÓA TỒN TẠM TÍNH (SOFT RESERVE - 15 PHÚT)" #FEF9E7 {
        usecase "UC-02.1: Nhập địa chỉ & Chọn phương thức thanh toán\\n(VietQR / Thẻ / COD)" as UC02_1
        usecase "UC-02.2: Khóa giữ chỗ Lô FEFO cận hạn nhất\\n(Soft Reserve: ReservedQuantity += Qty)" as UC02_2 #FAD7A0
        usecase "UC-02.3: Tạo đơn hàng Chờ thanh toán\\n(OrderStatus = PENDING, Payment = UNPAID)" as UC02_3
        usecase "UC-02.4: Sinh mã VietQR động kèm mã giao dịch\\n(Payload: Mã Đơn + Số Tiền)" as UC02_4
    }
    
    ' --- GIAI ĐOẠN 2: CHỐT TRỪ KHO VẬT LÝ (HARD DEDUCT) ---
    package "GIAI ĐOẠN 2: TRỪ KHO THỰC TẾ (HARD DEDUCT KHI TRẢ TIỀN)" #EAFAF1 {
        usecase "UC-02.5: Tiếp nhận & Xác thực Webhook/IPN\\n(Kiểm tra chữ ký số bảo mật HMAC-SHA512)" as UC02_5 #D5F5E3
        usecase "UC-02.6: Trừ cứng tồn kho vật lý & Hủy giữ chỗ\\n(Physical -= Qty, Reserved -= Qty)" as UC02_6 #A9DFBF
        usecase "UC-02.7: Chuyển đơn sang Đang xử lý & Tích điểm VIP\\n(Ghi sổ cái kế toán & Bắn Realtime SignalR)" as UC02_7 #7DCEA0
    }
    
    ' --- NGOẠI LỆ & QUÁ HẠN THANH TOÁN ---
    package "XỬ LÝ QUÁ HẠN & NGOẠI LỆ TRANH CHẤP" #FDEDEC {
        usecase "UC-02.8: Tự động hủy đơn & Hoàn trả tồn khả dụng\\n(Release Soft Reserve: Reserved -= Qty sau 15p)" as UC02_8 #F5B7B1
        usecase "UC-02.9: Cảnh báo hết hàng khi có xung đột đồng thời\\n(Optimistic Concurrency: RowVersion check)" as UC02_9 #F1948A
    }
}

' ========================================================
' CÁC MỐI QUAN HỆ VÀ LIÊN KẾT CA SỬ DỤNG
' ========================================================

' Khách hàng kích hoạt ca sử dụng Đặt hàng
Customer --> UC02
Customer --> UC02_1

' Luồng quan hệ <<include>> của Giai đoạn 1 (Soft Reserve)
UC02 ..> UC02_1 : <<include>>
UC02 ..> UC02_2 : <<include>>
UC02 ..> UC02_3 : <<include>>
UC02 ..> UC02_4 : <<include>>

' Luồng quan hệ của Giai đoạn 2 (Hard Deduct) do Webhook thanh toán kích hoạt
PaymentGateway --> UC02_5
UC02_5 ..> UC02_6 : <<include>>\\n[Giao dịch ACID]
UC02_6 ..> UC02_7 : <<include>>

' Luồng hủy đơn do dịch vụ nền định kỳ kích hoạt
BackgroundWorker --> UC02_8
UC02 <.. UC02_8 : <<extend>>\\n[Quá hạn 15 phút]
UC02 <.. UC02_9 : <<extend>>\\n[Hết tồn khả dụng]

' Tương tác CSDL & Tồn kho
UC02_2 --> DBService : 1. Tăng ReservedQuantity trên ProductBatches
UC02_6 --> DBService : 2. Trừ PhysicalQuantity & Ghi InventoryLogs
UC02_8 --> DBService : 3. Giảm ReservedQuantity (Khôi phục khả dụng)

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_2_4_So_Do_Use_Case_Chi_Tiet_UC02.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_2_4_So_Do_Use_Case_Chi_Tiet_UC02.png');
    const svgDest = path.join(__dirname, 'Hinh_2_4_So_Do_Use_Case_Chi_Tiet_UC02.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
