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

// PlantUML Source Code for Hinh 2.7
const pumlContent = `@startuml Hinh2_7_So_Do_Use_Case_Chi_Tiet_UC05
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
' ĐỊNH NGHĨA CÁC TÁC NHÂN (NGƯỜI DÙNG & ĐỘNG CƠ THUẬT TOÁN)
' ========================================================
actor "Khách hàng mua sắm\\n(Customer)" as Customer
actor "Chuyên viên tư vấn sắc đẹp\\n(Beauty Advisor)" as Advisor

actor "Động cơ Chẩn đoán da thông minh\\n(Skin Diagnostic Engine)" as DiagnosticEngine <<Engine>>
actor "Động cơ Phân tích Xung đột hoạt chất\\n(Ingredient Conflict Analyzer)" as ConflictEngine <<Engine>>
actor "CSDL Dược mỹ phẩm & Hồ sơ\\n(SQL: Ingredients & CustomerProfiles)" as DBService <<System>>

' ========================================================
' KHUNG RANH GIỚI CA SỬ DỤNG CHI TIẾT UC-05
' ========================================================
rectangle "PHÂN HỆ TƯ VẤN ROUTINE DA VÀ KIỂM TRA TƯƠNG KỴ HOẠT CHẤT" {

    usecase "UC-05: Tư vấn phác đồ chăm sóc da & Kiểm tra xung đột hoạt chất\\n(Skin Routine Recommendation & Ingredient Conflict Engine)" as UC05 #EBF5FB
    
    ' --- GIAI ĐOẠN 1: CHẨN ĐOÁN DA ---
    package "1. CHẨN ĐOÁN & PHÂN LOẠI DA LIỄU (DIAGNOSIS)" #FEF9E7 {
        usecase "UC-05.1: Thực hiện bài trắc nghiệm phân tích da\\n(Loại da: Dầu/Khô, Nhu cầu: Mụn/Nám, Tiền sử dị ứng)" as UC05_1 #FDEBD0
        usecase "UC-05.2: Tính điểm tương thích sinh học & Gợi ý sản phẩm\\n(Biocompatibility Scoring: Xếp hạng sản phẩm phù hợp)" as UC05_2 #FAD7A0
    }
    
    ' --- GIAI ĐOẠN 2: PHÂN TÍCH TƯƠNG KỴ HOẠT CHẤT ---
    package "2. RÀ SOÁT & CẢNH BÁO XUNG ĐỘNG HOẠT CHẤT (CONFLICT SCAN)" #FDEDEC {
        usecase "UC-05.3: Rà soát ma trận tương kỵ thành phần hóa mỹ phẩm\\n(Quét toàn bộ cặp hoạt chất có trong giỏ hàng/routine)" as UC05_3 #FADBD8
        usecase "UC-05.4: Cảnh báo tiền sử dị ứng thành phần cấp độ đỏ\\n(Allergy Alert: Khóa khuyến nghị sản phẩm nguy hiểm)" as UC05_4 #F5B7B1
        usecase "UC-05.5: Cảnh báo tương kỵ hai hoạt chất nồng độ cao\\n(Conflict Alert: Retinol + BHA, AHA + Vitamin C...)" as UC05_5 #F1948A
    }
    
    ' --- GIAI ĐOẠN 3: PHÂN TÁCH LỊCH TRÌNH & LƯU TRỮ ---
    package "3. TỰ ĐỘNG PHÂN TÁCH PHÁC ĐỒ & LƯU HỒ SƠ (ROUTINE BUILDER)" #EAFAF1 {
        usecase "UC-05.6: Tự động phân tách lịch trình sử dụng khoa học\\n(Chia sản phẩm Sáng / Tối và Dùng cách ngày an toàn)" as UC05_6 #D5F5E3
        usecase "UC-05.7: Đề xuất sản phẩm thay thế tương đương lành tính\\n(Gợi ý hoạt chất thay thế dịu nhẹ: Bakuchiol, Niacinamide)" as UC05_7 #A9DFBF
        usecase "UC-05.8: Lưu Routine vào Hồ sơ sắc đẹp & Đẩy vào Giỏ hàng\\n(Lưu lịch sử chẩn đoán vào bảng CustomerProfiles)" as UC05_8 #7DCEA0
    }
}

' ========================================================
' CÁC MỐI QUAN HỆ VÀ LIÊN KẾT CA SỬ DỤNG
' ========================================================

' Tác nhân Người dùng kích hoạt quy trình
Customer --> UC05
Customer --> UC05_1
Advisor --> UC05
Advisor --> UC05_1

' Luồng quan hệ Chẩn đoán da
UC05 ..> UC05_1 : <<include>>
UC05_1 ..> UC05_2 : <<include>>

' Luồng quan hệ Rà soát tương kỵ
UC05 ..> UC05_3 : <<include>>
UC05_3 <.. UC05_4 : <<extend>>\\n[Trùng thành phần dị ứng]
UC05_3 <.. UC05_5 : <<extend>>\\n[Xung đột cặp hoạt chất]

' Luồng phân tách phác đồ & Đề xuất thay thế
UC05_5 ..> UC05_6 : <<include>>\\n[Phân bổ thời gian]
UC05_4 ..> UC05_7 : <<include>>\\n[Đổi sản phẩm an toàn]
UC05 ..> UC05_8 : <<include>>

' Tương tác các Động cơ chuyên dụng & CSDL
UC05_1 --> DiagnosticEngine : 1. Nạp câu trả lời & Tính loại da
UC05_3 --> ConflictEngine : 2. Đối chiếu ma trận quy tắc da liễu
UC05_8 --> DBService : 3. Cập nhật bảng CustomerProfiles

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_2_7_So_Do_Use_Case_Chi_Tiet_UC05.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_2_7_So_Do_Use_Case_Chi_Tiet_UC05.png');
    const svgDest = path.join(__dirname, 'Hinh_2_7_So_Do_Use_Case_Chi_Tiet_UC05.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
