# MA TRẬN PHÂN QUYỀN & DANH SÁCH TÀI KHOẢN HỆ THỐNG CANMAKE BEAUTY

> **Tài liệu chính thức – Phiên 4:** Tổng hợp toàn bộ cấu trúc Phòng ban (`Department`), Vai trò (`Role`), Phạm vi Ủy quyền (`AdminDelegationScope`), và Ma trận Quyền hiệu lực (`Effective Permissions`) của tất cả tài khoản trong cơ sở dữ liệu `OmnichannelBeautyDb`.
>
> **Mật khẩu mặc định cho các tài khoản quản trị & nhân sự:** `Admin@123` *(riêng tài khoản kế thừa `admin` dùng `Admin@123456`)*.

---

## 1. Quy Tắc Phân Giải Quyền Hiệu Lực (Effective Permissions)

Hệ thống `PermissionService` kết hợp `DelegationService` áp dụng mô hình **Hybrid RBAC + Scoped PBAC (Deny-Overrides)** với 4 nguyên tắc bất biến:

1. **Ranh giới loại tài khoản (`UserType` Boundary):**
   - `UserType = 1` (**Khách hàng**): Chỉ hoạt động trên Storefront mua sắm; bị chặn tuyệt đối ở tầng `AdminAreaBoundaryFilter`, `PermissionAuthorizationHandler` và `PermissionService` (luôn trả về tập quyền quản trị rỗng `∅`, kể cả khi bị gán nhầm Role nội bộ).
   - `UserType = 2` (**Nhân sự nội bộ / Quản trị viên**): Được phép đăng nhập `/Admin/Auth/Login` và truy cập các phân hệ theo đúng quyền hiệu lực.
2. **Công thức tập quyền hiệu lực:**
   $$\text{EffectivePermissions}(u) = \left( \bigcup_{r \in \text{Roles}(u)} \text{Permissions}(r) \cup \text{GrantedOverrides}(u) \right) \setminus \text{DeniedOverrides}(u)$$
   - **Deny luôn thắng (`IsGranted = false`):** Nếu một quyền bị tích **Thu hồi (Deny)** ở cấp cá nhân (`UserPermissions`), nhân viên đó sẽ mất quyền ngay lập tức dù `Role` gốc có quyền đó.
   - **Kế thừa quyền tương đương (`CompositePermissionInheritance`):** `UI_SECTION_MANAGE` $\leftrightarrow$ `THEME_MANAGE`, `ORDER_CONFIRM` $\leftarrow$ `ORDER_EDIT`.
3. **Nguyên tắc Ủy quyền không vượt cấp (`Strict Subset Delegation`):**
   - Admin nhỏ (`ad1`..`ad4`) chỉ được phép cấp/thu hồi các quyền nằm trong giao của **(Quyền hiệu lực của chính mình)** $\cap$ **(`AdminDelegationPermissions` được SuperAdmin giao)**.
   - Admin nhỏ chỉ được quản lý nhân sự thuộc **`AdminDelegationDepartments`** và có **`MaxRoleLevel < ActorMaxRoleLevel`** (Level 50 chỉ quản lý Level $\le 30$).

---

## 2. Sơ Đồ 9 Phòng Ban & Trưởng Phụ Trách (`Departments`)

| ID | Mã Phòng Ban (`Code`) | Tên Phòng Ban | Cấp Cha (`Parent`) | Trưởng Phòng (`HeadUser`) | Vai Trò Chủ Quản |
|:--:|:---|:---|:---|:---|:---|
| 1 | `BAN_GIAM_DOC` | Ban Giám Đốc & Quản Trị Tối Cao | *(Gốc)* | `superadmin` (Nguyễn Quản Trị) | `SuperAdmin` (Lv 100) |
| 2 | `PHONG_NHAN_SU` | Phòng Hành Chính - Nhân Sự | `BAN_GIAM_DOC` | `ad3` (Nguyễn Quốc Bảo) | `HrAdmin` (Lv 50), `HrStaff` (Lv 10) |
| 3 | `PHONG_SALE` | Phòng Kinh Doanh & Chốt Đơn | `BAN_GIAM_DOC` | `ad4` (Phạm Khánh Linh) | `SalesAdmin` (Lv 50), `SalesStaff` (Lv 10) |
| 4 | `PHONG_MARKETING` | Phòng Tiếp Thị, Khuyến Mãi & CRM | `BAN_GIAM_DOC` | `ad4` (Phạm Khánh Linh) | `MarketingStaff` (Lv 10) |
| 5 | `PHONG_GIAO_DIEN_CMS` | Phòng Giao Diện, Menu & Trải Nghiệm Web | `BAN_GIAM_DOC` | `ad1` (Lê Hoàng Minh) | `UiAdmin` (Lv 50), `ContentEditor` (Lv 10) |
| 6 | `PHONG_DU_LIEU_SAN_PHAM` | Phòng Nội Dung & Dữ Liệu Sản Phẩm | `BAN_GIAM_DOC` | `ad2` (Trần Thu Thủy) | `DataAdmin` (Lv 50), `DataStaff` (Lv 10) |
| 7 | `PHONG_KHO_VAN` | Phòng Kho Vận & Kiểm Soát Hạn Dùng FEFO | `BAN_GIAM_DOC` | *(Do Cửa hàng trưởng / SuperAdmin điều phối)* | `WarehouseStaff` (Lv 10) |
| 8 | `PHONG_KE_TOAN` | Phòng Tài Chính - Kế Toán | `BAN_GIAM_DOC` | *(Do Ban Giám Đốc điều phối)* | *(Chuyên trách Tài chính)* |
| 9 | `CUA_HANG_BAN_LE` | Hệ Thống Cửa Hàng & Thu Ngân POS | `BAN_GIAM_DOC` | `manager_hq` (Trần Quản Lý HQ) | `StoreManager` (Lv 30), `Cashier` (Lv 10) |

---

## 3. Danh Mục 14 Vai Trò Hệ Thống (`Roles`) & Quyền Mặc Định (`RolePermissions`)

| # | Mã Vai Trò (`Name`) | Tên Hiển Thị (`DisplayName`) | Level | Phòng Ban | Màu Badge | Số Quyền | Danh Sách Mã Quyền Hạt Nhân (`Permission.Code`) |
|:--:|:---|:---|:--:|:---|:--:|:--:|:---|
| 1 | `SuperAdmin` | Quản Trị Viên Tối Cao (Admin Tổng) | **100** | `BAN_GIAM_DOC` | `purple` | **66 / 66** | Toàn bộ 66 quyền hạt nhân trên 13 phân hệ (bao gồm `PERMISSION_MANAGE`, `ROLE_MANAGE`, `SETTINGS_MANAGE`, `USER_MANAGE`, `FINANCE_*`, `TRANSFER_APPROVE`...) |
| 2 | `UiAdmin` | Admin Giao diện & Menu | **50** | `PHONG_GIAO_DIEN_CMS` | `pink` | **8** | `DASHBOARD_VIEW`, `CMS_MENU_MANAGE`, `CMS_BANNER_MANAGE`, `THEME_MANAGE`, `CMS_STATIC_PAGE_MANAGE`, `UI_SECTION_MANAGE`, `PERMISSION_DELEGATE`, `USER_VIEW` |
| 3 | `DataAdmin` | Admin Nội dung & Dữ liệu | **50** | `PHONG_DU_LIEU_SAN_PHAM` | `indigo` | **11** | `DASHBOARD_VIEW`, `PRODUCT_VIEW`, `PRODUCT_CREATE`, `PRODUCT_EDIT`, `PRODUCT_DELETE`, `PRODUCT_IMPORT`, `CMS_POST_MANAGE`, `CMS_REVIEW_MODERATE`, `INVENTORY_VIEW`, `PERMISSION_DELEGATE`, `USER_VIEW` |
| 4 | `HrAdmin` | Trưởng phòng Nhân sự | **50** | `PHONG_NHAN_SU` | `teal` | **13** | `DASHBOARD_VIEW`, `USER_VIEW`, `USER_CREATE`, `USER_EDIT`, `USER_DELETE`, `USER_RESET_PASSWORD`, `USER_EXPORT`, `USER_ASSIGN_ROLE`, `DEPARTMENT_VIEW`, `DEPARTMENT_MANAGE`, `ROLE_VIEW`, `ROLE_MANAGE_SCOPED`, `PERMISSION_DELEGATE` |
| 5 | `SalesAdmin` | Giám đốc Sale & Marketing | **50** | `PHONG_SALE` | `rose` | **20** | `DASHBOARD_VIEW`, `DASHBOARD_ANALYTICS`, `ORDER_VIEW`, `ORDER_CREATE`, `ORDER_EDIT`, `ORDER_CANCEL`, `ORDER_CONFIRM`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `CUSTOMER_EDIT`, `CUSTOMER_PROFILE_VIEW`, `PROMOTION_MANAGE`, `COUPON_MANAGE`, `VOUCHER_MANAGE`, `LOYALTY_VIEW`, `LOYALTY_ADJUST_POINTS`, `LOYALTY_TIER_MANAGE`, `REPORT_SALES`, `PERMISSION_DELEGATE`, `USER_VIEW` |
| 6 | `StoreManager` | Cửa Hàng Trưởng (Quản Lý Chi Nhánh) | **30** | `CUA_HANG_BAN_LE` | `blue` | **27** | `DASHBOARD_VIEW`, `PRODUCT_VIEW`, `INVENTORY_VIEW`, `INVENTORY_ADJUST`, `INVENTORY_STOCKTAKE`, `POS_ACCESS`, `POS_CHECKOUT`, `POS_REFUND`, `POS_SHIFT_OPEN`, `POS_SHIFT_CLOSE`, `ORDER_VIEW`, `ORDER_CREATE`, `ORDER_EDIT`, `ORDER_CANCEL`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `CUSTOMER_EDIT`, `CUSTOMER_PROFILE_VIEW`, `REPORT_SALES`, `REPORT_INVENTORY`, `FINANCE_LEDGER_VIEW`, `FINANCE_PNL_VIEW`, `VOUCHER_MANAGE`, `LOYALTY_VIEW`, `LOYALTY_ADJUST_POINTS`, `TRANSFER_CREATE`, `TRANSFER_RECEIVE` *(+ kế thừa `ORDER_CONFIRM`)* |
| 7 | `SalesStaff` | Nhân viên Sale / Chốt đơn | **10** | `PHONG_SALE` | `orange` | **8** | `ORDER_VIEW`, `ORDER_CREATE`, `ORDER_EDIT`, `ORDER_CONFIRM`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `CUSTOMER_PROFILE_VIEW`, `PRODUCT_VIEW` *(Không có `ORDER_CANCEL`, không xóa dữ liệu)* |
| 8 | `MarketingStaff` | Nhân viên Marketing | **10** | `PHONG_MARKETING` | `fuchsia` | **8** | `PROMOTION_MANAGE`, `COUPON_MANAGE`, `VOUCHER_MANAGE`, `CMS_BANNER_MANAGE`, `CUSTOMER_VIEW`, `CUSTOMER_PROFILE_VIEW`, `LOYALTY_VIEW`, `PRODUCT_VIEW` |
| 9 | `HrStaff` | Nhân viên Nhân sự | **10** | `PHONG_NHAN_SU` | `cyan` | **5** | `USER_VIEW`, `USER_CREATE`, `USER_EDIT`, `DEPARTMENT_VIEW`, `ROLE_VIEW` *(Không có `USER_DELETE`, `USER_RESET_PASSWORD`, `USER_ASSIGN_ROLE`, `PERMISSION_DELEGATE`)* |
| 10 | `DataStaff` | Nhân viên Nhập liệu sản phẩm | **10** | `PHONG_DU_LIEU_SAN_PHAM` | `violet` | **5** | `PRODUCT_VIEW`, `PRODUCT_CREATE`, `PRODUCT_EDIT`, `CMS_POST_MANAGE`, `INVENTORY_VIEW` *(Không có `PRODUCT_DELETE`, `PRODUCT_IMPORT`, `CMS_REVIEW_MODERATE`)* |
| 11 | `ContentEditor` | Biên Tập Viên Nội Dung | **10** | `PHONG_GIAO_DIEN_CMS` | `emerald` | **7** | `PRODUCT_VIEW`, `CMS_POST_MANAGE`, `CMS_BANNER_MANAGE`, `CMS_MENU_MANAGE`, `CMS_REVIEW_MODERATE`, `CMS_STATIC_PAGE_MANAGE`, `THEME_MANAGE` *(+ kế thừa `UI_SECTION_MANAGE`)* |
| 12 | `Cashier` | Thu Ngân Điểm Bán (POS) | **10** | `CUA_HANG_BAN_LE` | `pink` | **11** | `PRODUCT_VIEW`, `INVENTORY_VIEW`, `POS_ACCESS`, `POS_CHECKOUT`, `POS_SHIFT_OPEN`, `POS_SHIFT_CLOSE`, `ORDER_VIEW`, `ORDER_CREATE`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `LOYALTY_VIEW` |
| 13 | `WarehouseStaff` | Nhân Viên Kho & Kiểm Soát FEFO | **10** | `PHONG_KHO_VAN` | `amber` | **10** | `PRODUCT_VIEW`, `PRODUCT_IMPORT`, `INVENTORY_VIEW`, `INVENTORY_IMPORT`, `INVENTORY_EXPORT`, `INVENTORY_ADJUST`, `INVENTORY_STOCKTAKE`, `REPORT_INVENTORY`, `TRANSFER_CREATE`, `TRANSFER_RECEIVE` |
| 14 | `Customer` | Khách Hàng Thành Viên | **0** | *(Storefront)* | `slate` | **0 (Admin)** | Chỉ dùng cho khách hàng mua sắm (`UserType = 1`), hoàn toàn không có quyền truy cập khu vực `/Admin`. |

---

## 4. Bảng Tổng Hợp 18 Tài Khoản Nhân Sự Nội Bộ (`UserType = 2`) Trong Database

| Mã NV | Username | Họ và Tên | Chức Danh (`JobTitle`) | Phòng Ban | Vai Trò & Level | Quản Lý Trực Tiếp (`Manager`) | Mật Khẩu | Quyền Hiệu Lực (`Effective Permissions`) & Phạm Vi Quản Trị |
|:---|:---|:---|:---|:---|:---|:---|:---|:---|
| `NV0001` | **`superadmin`** | Nguyễn Quản Trị | Tổng Giám Đốc Hệ Thống (SuperAdmin) | `BAN_GIAM_DOC` | `SuperAdmin` (Lv 100) | *(Cao nhất)* | `Admin@123` | **Toàn quyền 66/66 quyền (`*`)**. Quản lý mọi phòng ban, mọi chi nhánh, cấu hình ma trận Role toàn cục và thiết lập `AdminDelegationScope` cho `ad1`..`ad4`. |
| `NV0009` | **`admin`** | Quản Trị Viên Kế Thừa | Quản Trị Viên Hệ Thống | `BAN_GIAM_DOC` | `SuperAdmin` (Lv 100) | `superadmin` | `Admin@123456` | **Toàn quyền 66/66 quyền (`*`)**. Tài khoản quản trị viên hệ thống dự phòng thuộc Ban Giám Đốc. |
| `NV0010` | **`ad1`** | Lê Hoàng Minh (Admin Giao Diện) | Trưởng Phòng Giao Diện & Menu (Admin 1) | `PHONG_GIAO_DIEN_CMS` | `UiAdmin` (Lv 50) | `superadmin` | `Admin@123` | **8 quyền hiệu lực:** `DASHBOARD_VIEW`, `CMS_MENU_MANAGE`, `CMS_BANNER_MANAGE`, `THEME_MANAGE`, `CMS_STATIC_PAGE_MANAGE`, `UI_SECTION_MANAGE`, `PERMISSION_DELEGATE`, `USER_VIEW`.<br>**Scope ủy quyền:** Quản lý `PHONG_GIAO_DIEN_CMS` (nhân viên `editor01`), gán role `ContentEditor`, tích cấp/thu hồi 6 quyền giao diện CMS. |
| `NV0011` | **`ad2`** | Trần Thu Thủy (Admin Dữ Liệu) | Trưởng Phòng Nội Dung & Dữ Liệu (Admin 2) | `PHONG_DU_LIEU_SAN_PHAM` | `DataAdmin` (Lv 50) | `superadmin` | `Admin@123` | **11 quyền hiệu lực:** `DASHBOARD_VIEW`, `PRODUCT_VIEW`, `PRODUCT_CREATE`, `PRODUCT_EDIT`, `PRODUCT_DELETE`, `PRODUCT_IMPORT`, `CMS_POST_MANAGE`, `CMS_REVIEW_MODERATE`, `INVENTORY_VIEW`, `PERMISSION_DELEGATE`, `USER_VIEW`.<br>**Scope ủy quyền:** Quản lý `PHONG_DU_LIEU_SAN_PHAM` (nhân viên `data01`), gán role `DataStaff`, tích cấp/thu hồi 9 quyền sản phẩm/bài viết/đánh giá/xem kho. |
| `NV0012` | **`ad3`** | Nguyễn Quốc Bảo (Trưởng Phòng Nhân Sự) | Trưởng Phòng Hành Chính - Nhân Sự (Admin 3) | `PHONG_NHAN_SU` | `HrAdmin` (Lv 50) | `superadmin` | `Admin@123` | **13 quyền hiệu lực:** `DASHBOARD_VIEW`, `USER_VIEW`, `USER_CREATE`, `USER_EDIT`, `USER_DELETE`, `USER_RESET_PASSWORD`, `USER_EXPORT`, `USER_ASSIGN_ROLE`, `DEPARTMENT_VIEW`, `DEPARTMENT_MANAGE`, `ROLE_VIEW`, `ROLE_MANAGE_SCOPED`, `PERMISSION_DELEGATE`.<br>**Scope ủy quyền:** Quản lý nhân sự **8/9 phòng ban** (trừ `BAN_GIAM_DOC`), được Thêm/Sửa/Xóa mềm/Reset MK/Gán các Role có `Level < 50` (nhân viên & cửa hàng trưởng), không được đụng tới `SuperAdmin` hay các `ad1`..`ad4` (Level 50). |
| `NV0013` | **`ad4`** | Phạm Khánh Linh (Giám Đốc Sale) | Giám Đốc Kinh Doanh & Tiếp Thị (Admin 4) | `PHONG_SALE` | `SalesAdmin` (Lv 50) | `superadmin` | `Admin@123` | **20 quyền hiệu lực:** `DASHBOARD_VIEW`, `DASHBOARD_ANALYTICS`, `ORDER_VIEW`, `ORDER_CREATE`, `ORDER_EDIT`, `ORDER_CANCEL`, `ORDER_CONFIRM`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `CUSTOMER_EDIT`, `CUSTOMER_PROFILE_VIEW`, `PROMOTION_MANAGE`, `COUPON_MANAGE`, `VOUCHER_MANAGE`, `LOYALTY_VIEW`, `LOYALTY_ADJUST_POINTS`, `LOYALTY_TIER_MANAGE`, `REPORT_SALES`, `PERMISSION_DELEGATE`, `USER_VIEW`.<br>**Scope ủy quyền:** Quản lý 2 phòng `PHONG_SALE` & `PHONG_MARKETING` (`sale01`, `sale02`, `mkt01`), gán role `SalesStaff`/`MarketingStaff`, tích chia 18 quyền nghiệp vụ Sale/Marketing/CRM/Loyalty. |
| `NV0002` | **`manager_hq`** | Trần Quản Lý HQ | Cửa Hàng Trưởng Flagship Quận 1 | `CUA_HANG_BAN_LE` | `StoreManager` (Lv 30) | `superadmin` | `Admin@123` | **28 quyền hiệu lực** (27 quyền gốc + `ORDER_CONFIRM` kế thừa), giới hạn tại Chi nhánh 1 (`StoreId = 1` - Flagship Quận 1). Quản lý trực tiếp `cashier01`, `warehouse01`. |
| `NV0003` | **`manager_cg`** | Lê Quản Lý CG | Cửa Hàng Trưởng Chi Nhánh Cầu Giấy | `CUA_HANG_BAN_LE` | `StoreManager` (Lv 30) | `superadmin` | `Admin@123` | **28 quyền hiệu lực** (27 quyền gốc + `ORDER_CONFIRM` kế thừa), giới hạn tại Chi nhánh 2 (`StoreId = 2` - Showroom Ba Đình/Cầu Giấy). Quản lý trực tiếp `cashier02`, `warehouse02`. |
| `NV0014` | **`sale01`** | Vũ Hải Đăng (NV Sale 01) | Chuyên Viên Tư Vấn & Chốt Đơn (Ca Sáng) | `PHONG_SALE` | `SalesStaff` (Lv 10) | `ad4` | `Admin@123` | **8 quyền hiệu lực:** `ORDER_VIEW`, `ORDER_CREATE`, `ORDER_EDIT`, `ORDER_CONFIRM`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `CUSTOMER_PROFILE_VIEW`, `PRODUCT_VIEW`. (Không được hủy đơn `ORDER_CANCEL`, không xem báo cáo doanh số). |
| `NV0015` | **`sale02`** | Đỗ Mai Phương (NV Sale 02) | Chuyên Viên Tư Vấn & Chốt Đơn (Ca Chiều) | `PHONG_SALE` | `SalesStaff` (Lv 10) | `ad4` | `Admin@123` | **8 quyền hiệu lực:** `ORDER_VIEW`, `ORDER_CREATE`, `ORDER_EDIT`, `ORDER_CONFIRM`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `CUSTOMER_PROFILE_VIEW`, `PRODUCT_VIEW`. |
| `NV0016` | **`mkt01`** | Hoàng Ngọc Trâm (NV Marketing) | Chuyên Viên Chiến Dịch Khuyến Mãi & CRM | `PHONG_MARKETING` | `MarketingStaff` (Lv 10) | `ad4` | `Admin@123` | **8 quyền hiệu lực:** `PROMOTION_MANAGE`, `COUPON_MANAGE`, `VOUCHER_MANAGE`, `CMS_BANNER_MANAGE`, `CUSTOMER_VIEW`, `CUSTOMER_PROFILE_VIEW`, `LOYALTY_VIEW`, `PRODUCT_VIEW`. |
| `NV0017` | **`hr01`** | Bùi Thanh Tâm (NV Nhân Sự) | Chuyên Viên Hồ Sơ Nhân Sự & Tuyển Dụng | `PHONG_NHAN_SU` | `HrStaff` (Lv 10) | `ad3` | `Admin@123` | **5 quyền hiệu lực:** `USER_VIEW`, `USER_CREATE`, `USER_EDIT`, `DEPARTMENT_VIEW`, `ROLE_VIEW`. (Chỉ xem/thêm/sửa thông tin cơ bản nhân viên, không được xóa, không gán role, không chia quyền). |
| `NV0018` | **`data01`** | Đặng Gia Huy (NV Nhập Liệu) | Chuyên Viên Dữ Liệu Mỹ Phẩm & Danh Mục | `PHONG_DU_LIEU_SAN_PHAM` | `DataStaff` (Lv 10) | `ad2` | `Admin@123` | **5 quyền hiệu lực:** `PRODUCT_VIEW`, `PRODUCT_CREATE`, `PRODUCT_EDIT`, `CMS_POST_MANAGE`, `INVENTORY_VIEW`. (Chỉ xem/thêm/sửa sản phẩm & bài viết, không có quyền `PRODUCT_DELETE`). |
| `NV0008` | **`editor01`** | Bùi Biên Tập | Biên Tập Viên Nội Dung & Giao Diện CMS | `PHONG_GIAO_DIEN_CMS` | `ContentEditor` (Lv 10) | `ad1` | `Admin@123` | **8 quyền hiệu lực:** `PRODUCT_VIEW`, `CMS_POST_MANAGE`, `CMS_BANNER_MANAGE`, `CMS_MENU_MANAGE`, `CMS_REVIEW_MODERATE`, `CMS_STATIC_PAGE_MANAGE`, `THEME_MANAGE`, `UI_SECTION_MANAGE`. Trực thuộc sự quản lý của `ad1`. |
| `NV0004` | **`cashier01`** | Phạm Thu Ngân 01 | Chuyên Viên Thu Ngân POS (Q1) | `CUA_HANG_BAN_LE` | `Cashier` (Lv 10) | `manager_hq` | `Admin@123` | **11 quyền hiệu lực:** `POS_ACCESS`, `POS_CHECKOUT`, `POS_SHIFT_OPEN`, `POS_SHIFT_CLOSE`, `ORDER_VIEW`, `ORDER_CREATE`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `PRODUCT_VIEW`, `INVENTORY_VIEW`, `LOYALTY_VIEW` (`StoreId = 1`). |
| `NV0005` | **`cashier02`** | Hoàng Thu Ngân 02 | Chuyên Viên Thu Ngân POS (Cầu Giấy) | `CUA_HANG_BAN_LE` | `Cashier` (Lv 10) | `manager_cg` | `Admin@123` | **11 quyền hiệu lực:** `POS_ACCESS`, `POS_CHECKOUT`, `POS_SHIFT_OPEN`, `POS_SHIFT_CLOSE`, `ORDER_VIEW`, `ORDER_CREATE`, `CUSTOMER_VIEW`, `CUSTOMER_CREATE`, `PRODUCT_VIEW`, `INVENTORY_VIEW`, `LOYALTY_VIEW` (`StoreId = 2`). |
| `NV0006` | **`warehouse01`** | Đặng Thủ Kho 01 | Chuyên Viên Kho Trung Tâm & FEFO | `PHONG_KHO_VAN` | `WarehouseStaff` (Lv 10) | `manager_hq` | `Admin@123` | **10 quyền hiệu lực:** `PRODUCT_VIEW`, `PRODUCT_IMPORT`, `INVENTORY_VIEW`, `INVENTORY_IMPORT`, `INVENTORY_EXPORT`, `INVENTORY_ADJUST`, `INVENTORY_STOCKTAKE`, `REPORT_INVENTORY`, `TRANSFER_CREATE`, `TRANSFER_RECEIVE` (`StoreId = 1`). |
| `NV0007` | **`warehouse02`** | Vũ Thủ Kho 02 | Chuyên Viên Kho Chi Nhánh Cầu Giấy | `PHONG_KHO_VAN` | `WarehouseStaff` (Lv 10) | `manager_cg` | `Admin@123` | **10 quyền hiệu lực:** `PRODUCT_VIEW`, `PRODUCT_IMPORT`, `INVENTORY_VIEW`, `INVENTORY_IMPORT`, `INVENTORY_EXPORT`, `INVENTORY_ADJUST`, `INVENTORY_STOCKTAKE`, `REPORT_INVENTORY`, `TRANSFER_CREATE`, `TRANSFER_RECEIVE` (`StoreId = 2`). |

> **Nhóm tài khoản Khách hàng (`UserType = 1`):** `customer01`, `customer02`, `customer_vip` và hơn 30 tài khoản khách hàng demo (`customer.diamond`, `customer.platinum`, `customer.gold`...) có `EmployeeCode = NULL`, `DepartmentId = NULL`, chỉ đăng nhập tại `/Account/Login` để mua hàng và tích điểm Loyalty.

---

## 5. Chi Tiết Phạm Vi Ủy Quyền (`AdminDelegationScope`) Của 4 Admin Nhỏ (`ad1`..`ad4`)

| Thuộc Tính Scope | `ad1` (`UiAdmin`) | `ad2` (`DataAdmin`) | `ad3` (`HrAdmin`) | `ad4` (`SalesAdmin`) |
|:---|:---:|:---:|:---:|:---:|
| **Phòng ban được quản lý (`AllowedDepartments`)** | `PHONG_GIAO_DIEN_CMS` | `PHONG_DU_LIEU_SAN_PHAM` | 8 phòng ban *(Tất cả trừ `BAN_GIAM_DOC`)* | `PHONG_SALE`, `PHONG_MARKETING` |
| **Tạo mới nhân sự (`CanCreateUser`)** | ❌ Không | ❌ Không | ✅ **Có** | ❌ Không *(Do HR `ad3` tạo)* |
| **Sửa / Khóa nhân sự (`CanEditUser`)** | ✅ Có | ✅ Có | ✅ **Có** | ✅ Có |
| **Xóa mềm nhân sự (`CanDeleteUser`)** | ❌ Không | ❌ Không | ✅ **Có** | ❌ Không |
| **Đặt lại mật khẩu (`CanResetPassword`)** | ✅ Có | ✅ Có | ✅ **Có** | ✅ Có |
| **Gán vai trò cấp dưới (`CanAssignRole`)** | ✅ Có (`ContentEditor`) | ✅ Có (`DataStaff`) | ✅ **Có** (Mọi Role `Level < 50` thuộc tập quyền) | ✅ Có (`SalesStaff`, `MarketingStaff`) |
| **Ủy quyền lại (`CanDelegateFurther`)** | ❌ Không | ❌ Không | ❌ Không | ❌ Không |
| **Trần cấp bậc (`MaxGrantLevel`)** | `50` *(Chỉ gán Role `< 50`)* | `50` *(Chỉ gán Role `< 50`)* | `50` *(Chỉ gán Role `< 50`)* | `50` *(Chỉ gán Role `< 50`)* |
| **Số quyền được phép chia (`AllowedPermissions`)** | **6 quyền** (`DASHBOARD_VIEW`, `CMS_MENU_MANAGE`, `CMS_BANNER_MANAGE`, `THEME_MANAGE`, `CMS_STATIC_PAGE_MANAGE`, `UI_SECTION_MANAGE`) | **9 quyền** (`DASHBOARD_VIEW`, `PRODUCT_VIEW/CREATE/EDIT/DELETE/IMPORT`, `CMS_POST_MANAGE`, `CMS_REVIEW_MODERATE`, `INVENTORY_VIEW`) | **12 quyền** (`DASHBOARD_VIEW`, `USER_VIEW/CREATE/EDIT/DELETE/RESET_PASSWORD/EXPORT/ASSIGN_ROLE`, `DEPARTMENT_VIEW/MANAGE`, `ROLE_VIEW/MANAGE_SCOPED`) | **18 quyền** (`DASHBOARD_VIEW/ANALYTICS`, `ORDER_*`, `CUSTOMER_*`, `PROMOTION/COUPON/VOUCHER_MANAGE`, `LOYALTY_*`, `REPORT_SALES`) |
| **Nhân viên dưới quyền trực tiếp hiện tại** | `editor01` | `data01` | `hr01` (+ nhân sự các phòng Level < 50) | `sale01`, `sale02`, `mkt01` |
| **Ví dụ kịch bản tích chia quyền thực tế** | `ad1` có thể thu hồi bớt quyền `CMS_MENU_MANAGE` của `editor01` (chỉ cho sửa bài viết/banner) hoặc cấp lại khi cần. | `ad2` có thể tích cấp tạm thời quyền `CMS_REVIEW_MODERATE` hoặc `PRODUCT_DELETE` cho `data01` trong đợt dọn dữ liệu. | `ad3` tiếp nhận nhân viên mới, tạo tài khoản, phân về phòng ban và gán vai trò khởi đầu (`HrStaff`), hoặc tích cấp thêm `USER_EXPORT` cho `hr01`. | `ad4` có thể tích cấp thêm quyền `ORDER_CANCEL` hoặc `REPORT_SALES` cho trưởng ca `sale01` mà không cần nhờ `superadmin`. |

