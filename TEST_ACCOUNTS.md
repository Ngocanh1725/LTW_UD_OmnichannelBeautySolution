# DANH SÁCH TÀI KHOẢN VÀ MẬT KHẨU KIỂM THỬ HỆ THỐNG
## Omnichannel Beauty Solution - Hệ Thống Bán Lẻ Mỹ Phẩm Đa Kênh

Tài liệu này tổng hợp toàn bộ danh sách tài khoản, mật khẩu, vai trò và phạm vi chi nhánh được khởi tạo sẵn (Seeded Data) trong hệ thống để phục vụ công tác kiểm thử (Manual Test, UAT, Integration Test).

---

## 1. Bảng Tra Cứu Nhanh Tài Khoản (Quick Reference)

> [!TIP]
> **THUYẾT TRÌNH 1-CLICK VỚI DEMO PRESENTER HUD (`Alt + D`):**  
> Khi `DemoMode:Enabled == true`, bạn có thể bấm tổ hợp phím **`Alt + D`** (hoặc nút tròn góc trái dưới) trên bất kỳ màn hình nào để chuyển vai tức thì giữa **Khách VIP (Lê Ngọc Linh)**, **Thu ngân Showroom 1 (cashier01)**, và **SuperAdmin** trong **< 50ms** mà không cần gõ tài khoản hay mật khẩu!

> **Mật khẩu mặc định toàn hệ thống:** `Admin@123`  
> *(Riêng tài khoản `admin` có thể sử dụng cả `Admin@123456` và có nút **Điền nhanh** tại trang đăng nhập).*

| STT | Username | Mật khẩu | Vai trò (Role) | Chi nhánh phân bổ | Họ và tên | Loại TK |
|:---:|:---|:---|:---|:---|:---|:---:|
| **1** | `superadmin` | `Admin@123` | **SuperAdmin** | Toàn hệ thống (Global) | Nguyễn Quản Trị | Quản trị |
| **2** | `admin` | `Admin@123456` / `Admin@123` | **SuperAdmin** | Toàn hệ thống (Global) | Quản Trị Viên Kế Thừa | Quản trị |
| **3** | `manager_hq` | `Admin@123` | **StoreManager** | `HQ-001` (Kho Tổng TP.HCM) | Trần Quản Lý HQ | Nhân viên |
| **4** | `manager_cg` | `Admin@123` | **StoreManager** | `SH-CG` (Showroom Cầu Giấy) | Lê Quản Lý CG | Nhân viên |
| **5** | `cashier01` | `Admin@123` | **Cashier** | `HQ-001` (Kho Tổng TP.HCM) | Phạm Thu Ngân 01 | Nhân viên |
| **6** | `cashier02` | `Admin@123` | **Cashier** | `SH-CG` (Showroom Cầu Giấy) | Hoàng Thu Ngân 02 | Nhân viên |
| **7** | `warehouse01` | `Admin@123` | **WarehouseStaff** | `HQ-001` (Kho Tổng TP.HCM) | Đặng Thủ Kho 01 | Nhân viên |
| **8** | `warehouse02` | `Admin@123` | **WarehouseStaff** | `SH-CG` (Showroom Cầu Giấy) | Vũ Thủ Kho 02 | Nhân viên |
| **9** | `editor01` | `Admin@123` | **ContentEditor** | Toàn hệ thống (CMS) | Bùi Biên Tập | Nhân viên |
| **10** | `customer01` | `Admin@123` | **Customer** | Khách hàng cá nhân | Ngọc Khách Hàng 01 | Khách hàng |
| **11** | `customer02` | `Admin@123` | **Customer** | Khách hàng cá nhân | Mai Khách Hàng 02 | Khách hàng |
| **12** | `customer_vip` | `Admin@123` | **Customer** | Khách hàng cá nhân | Linh VIP Diamond | Khách hàng |

---

## 2. Chi Tiết Tài Khoản Theo Vai Trò & Kịch Bản Kiểm Thử

### 2.1. Quản Trị Viên Hệ Thống (SuperAdmin)
* **Tài khoản chính:** `superadmin` / `Admin@123`
* **Tài khoản thay thế:** `admin` / `Admin@123456` hoặc `Admin@123`
* **Email:** `superadmin@omnichannelbeauty.vn`
* **Phạm vi (Scope):** Toàn quyền (God Mode Bypass). Xem và quản trị tất cả các chi nhánh mà không bị giới hạn `StoreId`.
* **Kịch bản kiểm thử khuyến nghị:**
  - Truy cập Dashboard tổng quan toàn chuỗi (`/Admin/Dashboard`).
  - Quản lý phân quyền, vai trò, tài khoản người dùng (`/Admin/User`, `/Admin/Role`).
  - Cấu hình hệ thống chung, hạng thành viên, tỷ lệ tích điểm Loyalty.
  - Phê duyệt các lệnh điều chuyển hàng giữa các kho (`/Admin/StockTransfer`).
  - Xem sổ cái tài chính, báo cáo P&L toàn bộ chuỗi cửa hàng.

---

### 2.2. Quản Lý Chi Nhánh (Store Manager)
* **Tài khoản 1:** `manager_hq` / `Admin@123` (Chi nhánh `HQ-001` - Kho Tổng & Showroom Quận 1, TP.HCM)
* **Tài khoản 2:** `manager_cg` / `Admin@123` (Chi nhánh `SH-CG` - Showroom Cầu Giấy, Hà Nội)
* **Phạm vi (Scope):** Giới hạn trong chi nhánh được gán.
* **Kịch bản kiểm thử khuyến nghị:**
  - Kiểm tra tính năng **cách ly dữ liệu (Store Isolation)**: Quản lý Cầu Giấy không sửa được số liệu hoặc đơn hàng của Kho Tổng HQ.
  - Xem báo cáo doanh thu theo ca, báo cáo xuất nhập tồn cục bộ tại chi nhánh.
  - Tạo phiếu đề xuất điều chuyển kho giữa 2 chi nhánh (`TRANSFER_CREATE`).
  - Điều chỉnh tồn kho kiểm kê phát sinh lệch (`INVENTORY_ADJUST`).
  - Quản lý và duyệt voucher, chương trình ưu đãi cấp cửa hàng.

---

### 2.3. Thu Ngân Bán Lẻ (Cashier - POS)
* **Tài khoản 1:** `cashier01` / `Admin@123` (Showroom HQ - TP.HCM)
* **Tài khoản 2:** `cashier02` / `Admin@123` (Showroom Cầu Giấy - Hà Nội)
* **Phạm vi (Scope):** Màn hình bán lẻ POS (`/Admin/Pos`).
* **Kịch bản kiểm thử khuyến nghị:**
  - Mở ca làm việc (Open Shift), nhập tiền mặt đầu ca.
  - Tìm kiếm sản phẩm bằng mã vạch (Barcode/SKU), thêm vào giỏ hàng.
  - Tìm kiếm khách hàng bằng số điện thoại để tích điểm hoặc áp dụng hạng thành viên.
  - Áp mã giảm giá Voucher/Coupon, thanh toán (Tiền mặt, Chuyển khoản QR, Thẻ).
  - In hóa đơn bán lẻ (`PrintReceipt.cshtml`).
  - Đóng ca làm việc (Close Shift), đối soát doanh thu thực thu so với hệ thống.

---

### 2.4. Nhân Viên Thủ Kho (Warehouse Staff)
* **Tài khoản 1:** `warehouse01` / `Admin@123` (Kho Tổng HQ - TP.HCM)
* **Tài khoản 2:** `warehouse02` / `Admin@123` (Kho Showroom Cầu Giấy)
* **Phạm vi (Scope):** Quản lý kho, tồn kho theo phương pháp FEFO (First Expired, First Out).
* **Kịch bản kiểm thử khuyến nghị:**
  - Nhập hàng từ nhà cung cấp theo Lô (Batch/Lot) và Hạn sử dụng (Expiry Date).
  - Kiểm tra cảnh báo hàng cận date (Near-expiry alerts).
  - Thực hiện quy trình kiểm kê kho định kỳ (Stocktake).
  - Xác nhận nhận hàng điều chuyển kho khi xe giao tới (`TRANSFER_RECEIVE`).

---

### 2.5. Biên Tập Viên Nội Dung (Content Editor - CMS)
* **Tài khoản:** `editor01` / `Admin@123`
* **Email:** `editor01@omnichannelbeauty.vn`
* **Phạm vi (Scope):** Giao diện CMS & Marketing.
* **Kịch bản kiểm thử khuyến nghị:**
  - Đăng bài viết chia sẻ bí quyết làm đẹp, cẩm nang chăm sóc da (`/Admin/Post`).
  - Cấu hình banner sự kiện ngoài trang chủ (`/Admin/Banner`).
  - Quản trị hệ thống menu phân cấp đa tầng (`/Admin/Menu`).
  - Kiểm duyệt đánh giá / bình luận sản phẩm của khách hàng trước khi hiển thị công khai (`/Admin/Review`).

---

### 2.6. Khách Hàng Mua Sắm & Demo Thuyết Trình (Customer)
Hệ thống tích hợp sẵn hơn 30 khách hàng với đầy đủ hồ sơ da (Skin Profile), lịch sử tích điểm và các hạng thành viên từ Bronze đến Diamond:

| Username | Mật khẩu | Họ và tên | Sinh nhật | Loại da & Tình trạng | Hạng & Điểm | Chi tiêu 12T |
|:---|:---|:---|:---:|:---|:---:|:---:|
| `customer01` | `Admin@123` | Ngọc Khách Hàng 01 | 15/06 | Da hỗn hợp thiên dầu | **Silver** (50 pts) | 500,000 đ |
| `customer02` | `Admin@123` | Mai Khách Hàng 02 | 20/09 | Da dầu mụn | **Gold** (400 pts) | 5,000,000 đ |
| `customer_vip` | `Admin@123` | Linh VIP Diamond | 10/04 | Da lão hóa | **Diamond** (1,800 pts) | 15,000,000 đ |
| `customer03` | `Admin@123` | Lê Thị Mai Anh | **05/11 (Tháng này)** | Da dầu mụn, bít tắc | **Gold** (680 pts) | 6,800,000 đ |
| `customer04` | `Admin@123` | Nguyễn Thu Hà | **12/11 (Tháng này)** | Da nhạy cảm đỏ rát | **Silver** (340 pts) | 3,400,000 đ |
| `customer05` | `Admin@123` | Phạm Hoàng Yến | **18/11 (Tháng này)** | Da khô bong tróc | **Platinum** (1,250 pts) | 12,500,000 đ |
| `customer06` | `Admin@123` | Vũ Mỹ Linh | **22/11 (Tháng này)** | Da hỗn hợp thiên dầu | **Diamond** (2,400 pts) | 24,000,000 đ |
| `customer07` | `Admin@123` | Đỗ Hương Giang | **08/11 (Tháng này)** | Da dầu mụn viêm | **Gold** (590 pts) | 5,900,000 đ |
| `customer08` | `Admin@123` | Hoàng Kim Ngân | **15/11 (Tháng này)** | Da thường, làm sáng | **Bronze** (120 pts) | 1,200,000 đ |
| `customer09` | `Admin@123` | Bùi Bích Phương | **27/11 (Tháng này)** | Da khô thiếu ẩm | **Silver** (280 pts) | 2,800,000 đ |
| `customer10` | `Admin@123` | Đặng Thùy Dung | **03/11 (Tháng này)** | Da nhạy cảm dễ nổi mụn | **Bronze** (95 pts) | 950,000 đ |
| `customer25` | `Admin@123` | Trịnh Kim Chi | **01/11 (Tháng này)** | Da dầu mụn dai dẳng | **Diamond** (2,650 pts) | 26,500,000 đ |
| `customer26` | `Admin@123` | Chu Thúy Nga | **30/11 (Tháng này)** | Da khô nứt nẻ | **Silver** (380 pts) | 3,800,000 đ |
| *(và customer11 -> customer24)* | `Admin@123` | Đa dạng khách hàng | Rải rác các tháng | Đa dạng loại da | Bronze/Silver/Gold | 500k - 15.8M |

> **Ghi chú đặc biệt cho Demo:** Có sẵn **10 khách hàng có ngày sinh nhật rơi vào tháng hiện tại (Tháng 11)** để trình diễn tính năng *Chiến dịch sinh nhật (Birthday Campaign)* và *Tặng điểm nhân đôi/Voucher sinh nhật*.

---

### 2.7. Mã Giảm Giá Khuyến Mãi Demo (Active Demo Coupons)

Hệ thống có sẵn các mã giảm giá không hết hạn để thực hiện demo đặt hàng nhanh:

| Mã Coupon | Loại giảm giá | Giá trị ưu đãi | Điều kiện áp dụng | Mô tả kịch bản demo |
|:---|:---|:---:|:---|:---|
| `DEMO10` | Phần trăm (%) | **10%** (Tối đa 100k) | Đơn bất kỳ (MinOrder = 0đ) | Mã demo phổ thông cho khách mới |
| `FREESHIP` | Số tiền cố định | **30,000 đ** | Miễn phí tiền ship toàn quốc | Test luồng miễn phí vận chuyển O2O |
| `COMBO15` | Phần trăm (%) | **15%** (Tối đa 250k) | Combo Routine 4 bước chăm sóc da | Test mua trọn bộ Skincare Routine |

---

### 2.8. Cơ Chế Khôi Phục Dữ Liệu Demo Chuẩn (Reset State)

Sau mỗi lần diễn tập thuyết trình hoặc thử nghiệm luồng mua sắm/bán hàng POS:
1. Đăng nhập tài khoản `superadmin` / `Admin@123`.
2. Truy cập: `/Admin/Settings`.
3. Nhấp nút **"Khôi Phục Dữ Liệu Demo"** (hoặc thẻ *Demo Ready* ở cuối trang).
4. Hệ thống hiển thị hộp thoại xác nhận SweetAlert2. Khi xác nhận:
   - Tự động xóa sạch toàn bộ các đơn hàng diễn tập tạo mới (`!ORD-SEED-*`).
   - Hoàn nguyên tồn kho vật lý của các lô hàng FEFO về mức chuẩn.
   - Dọn sạch giỏ hàng tạm vãng lai.
   - Bảo toàn nguyên vẹn 150+ đơn hàng chuẩn, biểu đồ doanh thu 90 ngày và 210 đánh giá sản phẩm.

* **Kịch bản kiểm thử khuyến nghị:**
  - Đăng nhập Storefront mua sắm tại trang chủ (`/`).
  - Thêm sản phẩm vào giỏ hàng, chọn chi nhánh nhận hàng hoặc giao tận nơi.
  - Sử dụng điểm Loyalty trừ vào hóa đơn mua hàng.
  - Đặt hàng thanh toán Online qua Cổng giả lập (VNPay Sandbox / MoMo Sandbox).
  - Xem lịch sử đơn hàng cá nhân và theo dõi trạng thái giao vận.

---

## 3. Danh Mục Chi Nhánh & Kho Hàng (Store Locations)

| Mã chi nhánh | Tên chi nhánh | Địa chỉ | Vai trò |
|:---:|:---|:---|:---:|
| `HQ-001` | **Kho Tổng & Showroom Chính** | 123 Nguyễn Huệ, P. Bến Nghé, Quận 1, TP. Hồ Chí Minh | Trụ sở chính / Kho trung tâm |
| `SH-CG` | **Showroom Cầu Giấy** | 45 Xuân Thủy, P. Dịch Vọng Hậu, Q. Cầu Giấy, Hà Nội | Showroom bán lẻ & Kho vệ tinh |
| `SH-TD` | **Showroom Thủ Đức** | 78 Võ Văn Ngân, P. Bình Thọ, TP. Thủ Đức, TP. Hồ Chí Minh | Showroom bán lẻ |

---

## 4. Thông Tin Cổng Thanh Toán & Vận Chuyển Giả Lập (Sandbox)

Khi kiểm thử luồng Checkout đặt hàng, hệ thống đã cấu hình sẵn các thông số Sandbox trong `appsettings.json`:

* **Cổng VNPay Sandbox:**
  - TmnCode: `TESTVNPAY`
  - HashSecret: `TESTHASHSECRET1234567890ABCDEFGH`
  - Cổng URL: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`
  - Thẻ test nội địa: Ngân hàng `NCB`, Số thẻ: `9704198526171436`, Tên: `NGUYEN VAN A`, Ngày phát hành: `07/15`, OTP: `123456`

* **Cổng MoMo Sandbox:**
  - PartnerCode: `MOMOTEST`
  - Ứng dụng: Dùng app MoMo Developer / Quét mã QR môi trường Test

* **Giao Hàng Nhanh (GHN Sandbox):**
  - ShopId: `123456`
  - Token: `TEST_GHN_TOKEN_12345`

---

## 5. Hướng Dẫn Đăng Nhập & Mẹo Kiểm Thử Nhanh

1. **Đường dẫn đăng nhập:**
   - Mở trình duyệt và truy cập: `http://localhost:5116/Account/Login` (hoặc `https://localhost:7142/Account/Login`).
2. **Điền nhanh tài khoản Admin:**
   - Tại trang đăng nhập có sẵn nút **"Điền nhanh"** (Quick fill) tự động gán tài khoản `admin` / `Admin@123456`.
3. **Quy tắc điều hướng tự động (Post-login Redirection):**
   - Tài khoản có `UserType >= 2` (Admin, Manager, Cashier, Warehouse, Editor) sẽ được tự động chuyển hướng đến trang Quản trị: `/Admin/Dashboard`.
   - Tài khoản `Customer` (`UserType == 1`) sẽ được điều hướng về Trang chủ mua sắm: `/`.
4. **Trường hợp lỗi mật khẩu do chưa chạy Seed:**
   - Đảm bảo cơ sở dữ liệu đã được áp dụng migration và chạy seed qua `DbInitializer.cs`.
   - Nếu cần cập nhật lại mật khẩu trực tiếp trong SQL Server, có thể chạy lệnh trong file `update_passwords.sql`:
     ```sql
     -- Cập nhật mật khẩu Admin@123456 cho tài khoản admin
     UPDATE Users SET PasswordHash = 'AQAAAAIAAYagAAAAEGlCLw5GtMCnc3V0yYzrEOTXVWDGJx96YiVytx68Hq0BBgzpW1hMYP6rtwkDUKTkmQ==' WHERE Username = 'admin';
     ```
