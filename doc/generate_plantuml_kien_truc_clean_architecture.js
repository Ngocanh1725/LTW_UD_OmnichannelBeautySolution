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

// PlantUML Source Code for Clean Architecture 4 Layers
const pumlContent = `@startuml Kien_Truc_Clean_Architecture_4_Tang_Omni_Beauty
skinparam backgroundColor #FFFFFF
skinparam defaultFontName "Times New Roman"
skinparam defaultFontSize 12
skinparam roundCorner 8
skinparam shadowing false
skinparam ArrowColor #1B4F72
skinparam ArrowThickness 1.5
skinparam ArrowFontSize 11
skinparam packageStyle rectangle

' ========================================================
' TẦNG 1: PRESENTATION LAYER (GIAO DIỆN & ĐIỂM CHẠM)
' ========================================================
package "1. PRESENTATION LAYER (Omnichannel.Web)" as PresLayer #EBF5FB {
    rectangle "<b>Các thành phần giao diện & Điểm chạm</b>\\n- 40 Bộ điều khiển (Storefront Controllers & Admin Area Controllers)\\n- Giao diện Razor Views, ViewComponents, Partial Views\\n- Kênh thời gian thực SignalR Hubs (Realtime Notifications)\\n- Bộ lọc tùy biến (Action Filters, Exception & Authorization Filters)" as PresComp #D4E6F1
}

' ========================================================
' TẦNG 2: APPLICATION LAYER (NGHIỆP VỤ ỨNG DỤNG)
' ========================================================
package "2. APPLICATION LAYER (Omnichannel.Application)" as AppLayer #FEF9E7 {
    rectangle "<b>Các bản hợp đồng trừu tượng & Điều phối luồng nghiệp vụ</b>\\n- 25 Giao diện Dịch vụ (IOrderWorkflowService, IInventoryAllocationEngine, IPosShiftService...)\\n- 37 Bộ cấu trúc truyền tải dữ liệu (Data Transfer Objects - DTOs & ViewModels)\\n- Bộ điều phối luồng ca sử dụng (Business Rules Orchestrators)\\n- Tiêu chuẩn quy tắc nghiệp vụ (Business Rule Specifications & Enums)" as AppComp #FDEBD0
}

' ========================================================
' TẦNG 3: DOMAIN CORE LAYER (LÕI NGHIỆP VỤ NỘI TẠI)
' ========================================================
package "3. DOMAIN CORE LAYER (Omnichannel.Domain)" as DomainLayer #EAFAF1 {
    rectangle "<b>Lõi nghiệp vụ độc lập & Bất biến (Zero Dependencies)</b>\\n- 65 Thực thể thuần C# (POCO Entities: ProductBatches, StoreInventories, PosShifts, Orders...)\\n- Bất biến miền & Logic nội tại (GetAvailableQuantity, IsNearExpiration, ValidatePrice)\\n- Định nghĩa sự kiện nội miền (Domain Events) & Kiểu dữ liệu liệt kê (Enums)\\n- Cụm đối tượng tổng hợp (Aggregates & Value Objects)" as DomainComp #D5F5E3
}

' ========================================================
' TẦNG 4: INFRASTRUCTURE LAYER (HẠ TẦNG & TRUY XUẤT)
' ========================================================
package "4. INFRASTRUCTURE LAYER (Omnichannel.Infrastructure)" as InfraLayer #F4ECF7 {
    rectangle "<b>Hiện thực hóa hạ tầng, Dữ liệu & Tích hợp ngoài</b>\\n- Ngữ cảnh CSDL Entity Framework Core 8 (ApplicationDbContext & ChangeTracker Interceptor)\\n- 27 Dịch vụ cài đặt thực tế (OrderWorkflowService, InventoryAllocationEngine...)\\n- Cơ chế bảo mật phân quyền động PBAC & Bộ nhớ đệm (IMemoryCache)\\n- Cổng tích hợp dịch vụ ngoài (VietQR, Cổng thanh toán VNPay/MoMo, Vận chuyển GHN)" as InfraComp #E8DAEF
}

' ========================================================
' MỐI QUAN HỆ PHỤ THUỘC (THE DEPENDENCY RULE & DIP)
' ========================================================

PresComp -[#1B4F72,bold]-> AppComp : "phụ thuộc vào (references)\\n[Gọi thực thi ca sử dụng qua Interfaces]"

AppComp -[#1B4F72,bold]-> DomainComp : "phụ thuộc vào (references)\\n[Thao tác trực tiếp trên Domain Entities]"

InfraComp -[#27AE60,dashed,bold]-> AppComp : "triển khai thực tế (implements)\\n[Hiện thực hóa 25 Service Interfaces]"

InfraComp -[#1B4F72,dashed]-> DomainComp : "ánh xạ & bảo toàn (maps)\\n[Cấu hình Fluent API & Migration 65 bảng]"

PresComp .[#8E44AD,dashed].> InfraComp : "đăng ký thùng chứa phụ thuộc (IoC DI)\\n[Đăng ký Scoped Services tại Program.cs]"

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Kien_Truc_Clean_Architecture_4_Tang.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Kien_Truc_Clean_Architecture_4_Tang.png');
    const svgDest = path.join(__dirname, 'Kien_Truc_Clean_Architecture_4_Tang.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
