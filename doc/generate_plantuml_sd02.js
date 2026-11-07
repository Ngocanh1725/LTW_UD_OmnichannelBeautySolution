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

const pumlContent = `@startuml SD02_FEFO_Two_Phase_Reservation
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

actor "Khách hàng\\n(Customer)" as Customer #E8F8F5
participant "CheckoutController\\n(Web Presentation)" as CheckoutCtrl #D4E6F1

box "Application & Infrastructure Core" #EAF2F8
    participant "OrderWorkflowService" as OrderSvc #D4E6F1
    participant "InventoryAllocation\\nEngine" as AllocEngine #D4E6F1
    participant "PaymentService\\n(VNPay Gateway)" as PaymentSvc #D4E6F1
end box

database "SQL Server Database\\n(ApplicationDbContext)" as DB #D1F2EB
actor "Cổng Thanh toán\\nVNPay Sandbox" as VNPayGate #FDEDEC

== GIAI ĐOẠN 1: KHÓA GIỮ CHỖ TỒN KHO TẠM TÍNH (SOFT RESERVE THEO THUẬT TOÁN FEFO) ==

Customer -> CheckoutCtrl: Bấm "Xác nhận Thanh toán Đặt hàng"\\n(POST /Checkout/ProcessOrder)
activate CheckoutCtrl

CheckoutCtrl -> OrderSvc: CreateOrderAsync(checkoutDto)
activate OrderSvc

OrderSvc -> AllocEngine: ReserveInventoryAsync(storeId=1, items)
activate AllocEngine

AllocEngine -> DB: Truy vấn StoreInventories & ProductBatches\\n(Sắp xếp hạn dùng ExpDate ASC - Ưu tiên lô cận date nhất)
activate DB
DB --> AllocEngine: Dữ liệu Lô #LOT-26A (Hạn 15/12/2026, Tồn khả dụng = 5)
deactivate DB

AllocEngine -> AllocEngine: Kiểm tra AvailableQuantity >= Yêu cầu\\n(5 >= 2 -> Đủ điều kiện cấp phát)

AllocEngine -> DB: Cập nhật StoreInventories:\\nTăng ReservedQuantity += 2 (PhysicalQuantity giữ nguyên)
activate DB
DB --> AllocEngine: Xác nhận ghi nhận Soft Reserve thành công
deactivate DB

AllocEngine --> OrderSvc: Cấp phát thành công: Chỉ định Lô #LOT-26A (Số lượng: 2)
deactivate AllocEngine

OrderSvc -> DB: INSERT Orders (Trạng thái Status='PENDING', PaymentStatus='UNPAID')
activate DB
DB --> OrderSvc: Khởi tạo OrderId = 2048
deactivate DB

OrderSvc -> DB: INSERT OrderDetails (OrderId=2048, ProductBatchId=LOT-26A, Quantity=2)

OrderSvc -> PaymentSvc: CreatePaymentUrl(orderId=2048, amount=850.000 đ)
activate PaymentSvc
PaymentSvc --> OrderSvc: Trả về Payment URL kèm chữ ký số bảo mật SecureHash
deactivate PaymentSvc

OrderSvc --> CheckoutCtrl: Kết quả khởi tạo đơn hàng thành công kèm VNPay Payment URL
deactivate OrderSvc

CheckoutCtrl --> Customer: Điều hướng trình duyệt chuyển sang Cổng thanh toán VNPay
deactivate CheckoutCtrl

== GIAI ĐOẠN 2: CHỐT TRỪ KHO VẬT LÝ KHI THANH TOÁN THÀNH CÔNG (HARD DEDUCT) ==

Customer -> VNPayGate: Nhập OTP xác thực & hoàn tất giao dịch thanh toán
activate VNPayGate

VNPayGate -> CheckoutCtrl: Gửi bản tin Webhook IPN Server-to-Server\\n(GET /Checkout/VnpayIpn?vnp_ResponseCode=00&vnp_SecureHash=...)
deactivate VNPayGate
activate CheckoutCtrl

CheckoutCtrl -> PaymentSvc: ProcessVnpayCallbackAsync(queryParams)
activate PaymentSvc

PaymentSvc -> PaymentSvc: Xác thực chữ ký điện tử HMAC-SHA512\\n(So khớp vnp_SecureHash với Secret Key)
PaymentSvc --> CheckoutCtrl: Chữ ký hợp lệ, giao dịch thành công (ResponseCode = '00')
deactivate PaymentSvc

CheckoutCtrl -> OrderSvc: ConfirmPaymentAndAllocateInventoryAsync(orderId=2048)
activate OrderSvc

OrderSvc -> DB: 1. Mở giao dịch CSDL (BeginTransactionAsync)
activate DB

OrderSvc -> DB: 2. UPDATE StoreInventories:\\nGiảm PhysicalQuantity -= 2 và Giảm ReservedQuantity -= 2
OrderSvc -> DB: 3. UPDATE ProductBatches:\\nGiảm số lượng tồn thực tế của Lô #LOT-26A -= 2
OrderSvc -> DB: 4. INSERT InventoryLogs:\\nLoại 'SALE_ONLINE', Tham chiếu 'ORD-2048', Lô #LOT-26A
OrderSvc -> DB: 5. UPDATE Orders:\\nChuyển Status='PROCESSING', PaymentStatus='PAID'

OrderSvc -> DB: 6. Xác nhận giao dịch nguyên tử (CommitTransactionAsync)
DB --> OrderSvc: Cam kết dữ liệu thành công (ACID Transaction)
deactivate DB

OrderSvc --> CheckoutCtrl: Hoàn tất chốt kho vật lý và gạch nợ đơn hàng
deactivate OrderSvc

CheckoutCtrl --> VNPayGate: Phản hồi HTTP 200 OK\\n({"RspCode":"00", "Message":"Confirm Success"})
deactivate CheckoutCtrl

CheckoutCtrl --> Customer: Điều hướng trang cảm ơn (Hiển thị đơn hàng ORD-2048 thành công)

@enduml
`;

async function main() {
    const pumlPath = path.join(__dirname, 'Hinh_3_2_So_Do_Tuan_Tu_SD02.puml');
    fs.writeFileSync(pumlPath, pumlContent, 'utf8');
    console.log("Saved PUML file:", pumlPath);

    const compressed = zlib.deflateRawSync(Buffer.from(pumlContent, 'utf8'));
    const encoded = encode64(compressed);
    const pngUrl = `https://www.plantuml.com/plantuml/png/${encoded}`;
    const svgUrl = `https://www.plantuml.com/plantuml/svg/${encoded}`;

    console.log("\n--- PLANTUML URLS ---");
    console.log("PNG URL:", pngUrl);
    console.log("SVG URL:", svgUrl);

    const pngDest = path.join(__dirname, 'Hinh_3_2_So_Do_Tuan_Tu_SD02.png');
    const svgDest = path.join(__dirname, 'Hinh_3_2_So_Do_Tuan_Tu_SD02.svg');

    console.log("\nDownloading PNG to:", pngDest);
    await downloadFile(pngUrl, pngDest);
    console.log("PNG downloaded successfully! Size:", fs.statSync(pngDest).size, "bytes");

    console.log("Downloading SVG to:", svgDest);
    await downloadFile(svgUrl, svgDest);
    console.log("SVG downloaded successfully! Size:", fs.statSync(svgDest).size, "bytes");
}

main().catch(console.error);
