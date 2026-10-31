# DANH SÁCH TÀI KHOẢN VÀ MẬT KHẨU KIỂM THỬ HỆ THỐNG
## Omnichannel Beauty Solution - Hệ Thống Bán Lẻ Mỹ Phẩm Đa Kênh

Tài liệu này tổng hợp toàn bộ danh sách tài khoản, mật khẩu, vai trò và phạm vi chi nhánh được khởi tạo sẵn (Seeded Data) trong hệ thống để phục vụ công tác kiểm thử (Manual Test, UAT, Integration Test).

---

## 1. Bảng Tra Cứu Nhanh Tài Khoản (Quick Reference)

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

### 2.6. Khách Hàng Mua Sắm (Customer)
Hệ thống tích hợp sẵn 3 khách hàng với 3 hạng thành viên khác nhau để kiểm thử tích điểm và khuyến mãi:

| Username | Mật khẩu | Hạng thành viên | Điểm tích lũy hiện có | Chi tiêu 12 tháng qua | Mã QR Loyalty |
|:---|:---|:---:|:---:|:---:|:---|
| `customer01` | `Admin@123` | **Silver** (Bạc) | 50 điểm | 500,000 đ | `LOYALTY-X-SILVER` |
| `customer02` | `Admin@123` | **Gold** (Vàng) | 400 điểm | 5,000,000 đ | `LOYALTY-X-GOLD` |
| `customer_vip` | `Admin@123` | **Diamond** (Kim Cương VIP) | 1,800 điểm | 15,000,000 đ | `LOYALTY-X-DIAMOND` |

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
