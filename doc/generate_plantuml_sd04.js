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

const pumlContent = `@startuml SD04_InterBranch_Stock_Transfer
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
skinparam DividerFontColor #1B4F72
skinparam DividerFontSize 12
skinparam DividerFontStyle bold
autonumber

actor "Quản lý Chi nhánh Nhận\\n(Showroom Ba Đình)" as Receiver #E8F8F5
actor "Thủ kho / Quản lý Xuất\\n(Tổng kho Miền Nam)" as Dispatcher #FEF9E7
participant "StockTransferController\\n(Web Presentation)" as TransferCtrl #D4E6F1

box "Application & Infrastructure Core" #EAF2F8
    participant "StockTransferService\\n(Điều chuyển kho)" as TransferSvc #D4E6F1
    participant "InventoryAllocation\\nEngine" as AllocEngine #D4E6F1
end box

database "SQL Server Database\\n(ApplicationDbContext)" as DB #D1F2EB

== BƯỚC 1: LẬP ĐỀ XUẤT YÊU CẦU ĐIỀU CHUYỂN (STATUS = PENDING) ==

Receiver -> TransferCtrl: Lập phiếu đề xuất chuyển 50 hộp kem B5\\n(POST /Admin/StockTransfer/Create)
activate TransferCtrl

TransferCtrl -> TransferSvc: CreateTransferRequestAsync(dto)
activate TransferSvc

TransferSvc -> DB: INSERT StockTransfers\\n(FromStore='WH-MN', ToStore='SH-BD', Status='PENDING')
activate DB
DB --> TransferSvc: Khởi tạo TransferId = 12, Mã phiếu 'TRF-2026-0012'
deactivate DB

TransferSvc -> DB: INSERT StockTransferDetails\\n(TransferId=12, ProductId=10, RequestedQty=50)

TransferSvc --> TransferCtrl: Phiếu chuyển tạo thành công: Mã 'TRF-2026-0012'
deactivate TransferSvc

TransferCtrl --> Receiver: Hiển thị phiếu đề xuất ở trạng thái CHỜ DUYỆT (PENDING)
deactivate TransferCtrl

== BƯỚC 2: PHÊ DUYỆT VÀ XUẤT KHO VẬN CHUYỂN THEO FEFO (STATUS = SHIPPED) ==

Dispatcher -> TransferCtrl: Duyệt yêu cầu & bấm "Xuất kho vận chuyển"\\n(POST /Admin/StockTransfer/Dispatch)
activate TransferCtrl

TransferCtrl -> TransferSvc: DispatchTransferAsync(transferId=12)
activate TransferSvc

TransferSvc -> AllocEngine: AllocateBatchesFEFOAsync(storeId='WH-MN', productId=10, qty=50)
activate AllocEngine

AllocEngine -> DB: Truy vấn tồn kho lô theo FEFO (ExpDate ASC) tại Tổng kho WH-MN
activate DB
DB --> AllocEngine: Cấp phát 2 lô cận hạn: Lô #LOT-01 (30 sp) & Lô #LOT-02 (20 sp)
deactivate DB

AllocEngine --> TransferSvc: Cấp phát thành công danh sách lô xuất kho
deactivate AllocEngine

TransferSvc -> DB: 1. Mở giao dịch CSDL (BeginTransactionAsync)
activate DB

TransferSvc -> DB: 2. UPDATE StoreInventories: Giảm PhysicalQuantity tại Kho Miền Nam (-50)
TransferSvc -> DB: 3. UPDATE ProductBatches: Trừ số lượng tồn từng Lô (#LOT-01: -30, #LOT-02: -20)
TransferSvc -> DB: 4. INSERT InventoryLogs: Loại 'TRANSFER_OUT', Kho WH-MN, Mã 'TRF-2026-0012'
TransferSvc -> DB: 5. UPDATE StockTransfers: Set Status='SHIPPED', TotalSentQty=50, ShippedAt=UTC

TransferSvc -> DB: 6. Xác nhận giao dịch nguyên tử (CommitTransactionAsync)
DB --> TransferSvc: Dữ liệu xuất kho cam kết thành công
deactivate DB

TransferSvc --> TransferCtrl: Xuất kho thành công, hàng bắt đầu lưu thông trên đường
deactivate TransferSvc

TransferCtrl --> Dispatcher: In phiếu xuất kho vận chuyển kiêm lệnh điều động (IN TRANSIT)
deactivate TransferCtrl

== BƯỚC 3: TIẾP NHẬN, ĐỐI SOÁT VÀ XÁC NHẬN NHẬP KHO ĐÍCH (STATUS = COMPLETED) ==

Receiver -> TransferCtrl: Kiểm đếm thực nhận đủ 50 hộp nguyên vẹn -> Bấm "Xác nhận nhập kho"\\n(POST /Admin/StockTransfer/Receive)
activate TransferCtrl

TransferCtrl -> TransferSvc: ReceiveTransferAsync(transferId=12, receivedQty=50)
activate TransferSvc

TransferSvc -> DB: 1. Mở giao dịch CSDL (BeginTransactionAsync)
activate DB

TransferSvc -> DB: 2. UPDATE StoreInventories: Tăng PhysicalQuantity tại Showroom Ba Đình (+50)
TransferSvc -> DB: 3. Tăng tồn hoặc ghi nhận lô hàng chuyển giao tại chi nhánh đích (#LOT-01: +30, #LOT-02: +20)
TransferSvc -> DB: 4. INSERT InventoryLogs: Loại 'TRANSFER_IN', Kho SH-BD, Mã 'TRF-2026-0012'
TransferSvc -> DB: 5. UPDATE StockTransfers: Set Status='COMPLETED', TotalReceivedQty=50, ReceivedAt=UTC

TransferSvc -> DB: 6. Xác nhận giao dịch nguyên tử (CommitTransactionAsync)
DB --> TransferSvc: Cam kết nhập kho thành công
deactivate DB

TransferSvc --> TransferCtrl: Hoàn tất toàn bộ quy trình điều chuyển liên chi nhánh
deactivate TransferSvc

TransferCtrl --> Receiver: Thông báo nhập kho thành công, tồn kho tại Ba Đình đã sẵn sàng mở bán
deactivate TransferCtrl

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_3_4_So_Do_Tuan_Tu_SD04.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_3_4_So_Do_Tuan_Tu_SD04.png');
    const svgDest = path.join(__dirname, 'Hinh_3_4_So_Do_Tuan_Tu_SD04.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
