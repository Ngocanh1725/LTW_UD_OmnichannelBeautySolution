##### C. Nhóm 3: Phân hệ Quản trị Kho Lô Hạn dùng FEFO & Điều chuyển Liên chi nhánh

Phân hệ này giải quyết bài toán cốt tử nhất của chuỗi bán lẻ mỹ phẩm: kiểm soát dòng chảy hàng hóa vật lý giữa các điểm bán, gắn chặt từng đơn vị sản phẩm với số lô sản xuất, ngày hết hạn và vị trí kệ hàng, đồng thời loại trừ nguy cơ thất thoát hàng hóa khi vận chuyển luân chuyển nội bộ.

###### Bảng 2.23: Đặc tả chi tiết bảng Stores (Mạng lưới Showroom và Tổng kho)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh duy nhất mỗi điểm bán hoặc kho hàng. |
| `Code` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã định danh chi nhánh viết tắt duy nhất (`WH-MN`, `SH-Q1`, `SH-BD`...). |
| `Name` | `string` | `NVARCHAR(150)` | Không | Không | Tên hiển thị của cửa hàng (ví dụ: Tổng kho Miền Nam, Showroom Quận 1, Showroom Ba Đình). |
| `Address` | `string?` | `NVARCHAR(255)` | Có | Không | Địa chỉ thực tế của showroom phục vụ khách hàng tìm kiếm địa điểm nhận hàng BOPIS. |
| `PhoneNumber` | `string?` | `VARCHAR(20)` | Có | Không | Số điện thoại hotline liên hệ trực tiếp của cửa hàng. |
| `Email` | `string?` | `VARCHAR(100)` | Có | Không | Địa chỉ hòm thư điện tử tiếp nhận thông báo nội bộ chi nhánh. |
| `IsActive` | `bool` | `BIT` | Không | `DEFAULT = 1` | Trạng thái hoạt động: 1: Đang mở cửa đón khách; 0: Tạm ngừng kinh doanh. |
| `CreatedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm thành lập và khai báo điểm bán trên hệ thống. |

*Phân tích thiết kế bảng Stores:* Cột `Code` là trường dữ liệu cốt lõi được sử dụng xuyên suốt hệ thống để định tuyến đơn hàng thông minh (Intelligent Order Routing). Điểm bán mang mã `WH` đại diện cho Kho trung tâm (Warehouse), trong khi mã `SH` đại diện cho Cửa hàng bán lẻ trực tiếp (Showroom). Việc phân tách này giúp động cơ định tuyến phân biệt kho nào được phép bán hàng POS và kho nào chuyên trách trung chuyển đóng gói đơn Online.

---

###### Bảng 2.24: Đặc tả chi tiết bảng ProductBatches (Lô sản xuất và Hạn sử dụng FEFO)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh lô hàng mỹ phẩm trong toàn hệ thống. |
| `ProductId` | `int` | `INT` | Không | `FK -> Products(Id), INDEX`| Khóa ngoại xác định dòng sản phẩm Master trực thuộc. |
| `ProductVariantId`|`int?`| `INT` | Có | `FK -> ProductVariants(Id)`| Khóa ngoại xác định biến thể cụ thể (nếu sản phẩm có phân loại biến thể). |
| `BatchNumber` | `string` | `VARCHAR(50)` | Không | `INDEX` | Mã số lô sản xuất in dập nổi trên bao bì do nhà máy ban hành (`LOT-2026-B5`...). |
| `ManufacturingDate`|`DateTime`|`DATETIME2(7)`| Không | Không | Ngày sản xuất chính thức của lô hàng. |
| `ExpDate` | `DateTime`| `DATETIME2(7)` | Không | `INDEX` | Hạn sử dụng của lô mỹ phẩm, là tiêu chí số một để sắp xếp xuất kho theo FEFO. |
| `ImportCost` | `decimal`| `DECIMAL(18,2)` | Không | Không | Đơn giá vốn thực tế khi nhập lô, phục vụ hạch toán giá vốn đích danh (Batch COGS). |
| `InitialQuantity`|`int` | `INT` | Không | Không | Số lượng sản phẩm ban đầu khi hoàn tất thủ tục nhập kho lô hàng. |
| `CurrentQuantity`|`int` | `INT` | Không | `INDEX` | Số lượng tồn kho thực tế còn lại của lô trên toàn bộ hệ thống chuỗi. |
| `Status` | `string` | `VARCHAR(30)` | Không | `DEFAULT = 'ACTIVE'` | Trạng thái lô: `ACTIVE`, `NEAR_EXPIRATION`, `EXPIRED`, `DEPLETED`. |
| `AlertDaysThreshold`|`int`| `INT` | Không | `DEFAULT = 90` | Số ngày cận hạn bắt đầu kích hoạt cảnh báo vàng (mặc định 90 ngày cho dược mỹ phẩm). |
| `ClearanceDiscountPercent`|`decimal?`|`DECIMAL(5,2)`| Có | Không | Tỷ lệ phần trăm giảm giá tự động xả hàng cận date (từ 20% đến 50%). |
| `RowVersion` | `byte[]?`| `ROWVERSION` | Có | `CONCURRENCY CHECK` | Khóa chống xung đột đồng thời khi nhiều giao dịch cùng thao tác trên lô. |
| `CreatedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm tạo bản ghi lô hàng vào cơ sở dữ liệu. |

*Phân tích thiết kế bảng ProductBatches:* Cột `ExpDate` được đánh chỉ mục đơn (Non-clustered Index) kết hợp chỉ mục bao phủ trên `CurrentQuantity` để tối ưu hóa triệt để câu lệnh truy vấn cấp phát lô của `InventoryAllocationEngine`. Khi có đơn hàng phát sinh, hệ thống chỉ mất dưới 2 mili-giây để lọc ra danh sách lô có `CurrentQuantity > 0` và sắp xếp tăng dần theo `ExpDate ASC`.

---

###### Bảng 2.25: Đặc tả chi tiết bảng StoreInventories (Tồn kho hai tầng theo Chi nhánh và Lô)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh bản ghi tồn kho chi nhánh. |
| `StoreId` | `int` | `INT` | Không | `FK -> Stores(Id), INDEX` | Khóa ngoại xác định chi nhánh lưu trữ lô hàng. |
| `ProductBatchId`| `int` | `INT` | Không | `FK -> ProductBatches(Id), INDEX`| Khóa ngoại xác định lô mỹ phẩm cụ thể đang nằm tại kho. |
| `PhysicalQuantity`|`int`| `INT` | Không | `DEFAULT = 0` | Số lượng hàng thực tế hiện hữu vật lý trên giá kệ của chi nhánh. |
| `ReservedQuantity`|`int`| `INT` | Không | `DEFAULT = 0` | Số lượng hàng đang được giữ chỗ tạm thời cho các đơn hàng Online chờ đóng gói. |
| `RowVersion` | `byte[]?`| `ROWVERSION` | Có | `CONCURRENCY CHECK` | Mã băm phiên bản dòng ngăn chặn bán vượt quá tồn kho (Over-selling). |
| `LastStocktakeDate`|`DateTime?`|`DATETIME2(7)`| Có | Không | Thời điểm gần nhất lô hàng tại chi nhánh này được đếm kiểm kê thực tế. |

*Phân tích thiết kế bảng StoreInventories:* Thiết kế tồn kho hai tầng phân biệt rạch ròi giữa Tồn kho vật lý (`PhysicalQuantity`) và Tồn kho giữ chỗ mềm (`ReservedQuantity`). Tồn kho khả dụng thực sự để mở bán được xác định bằng công thức: `AvailableQuantity = PhysicalQuantity - ReservedQuantity`. Khi khách hàng tạo đơn trên website, `ReservedQuantity` tăng lên để ngăn không cho khách hàng tại Showroom mua mất sản phẩm đó. Chỉ khi nhân viên đóng gói quét mã xuất kho, `PhysicalQuantity` mới chính thức bị giảm trừ.

---

###### Bảng 2.26: Đặc tả chi tiết bảng InventoryLogs (Sổ cái kiểm toán biến động kho)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh mỗi dòng nhật ký kho. |
| `StoreId` | `int` | `INT` | Không | `FK -> Stores(Id), INDEX` | Khóa ngoại xác định chi nhánh phát sinh biến động hàng hóa. |
| `ProductBatchId`| `int` | `INT` | Không | `FK -> ProductBatches(Id), INDEX`| Khóa ngoại xác định lô mỹ phẩm chịu tác động biến động. |
| `Action` | `string` | `VARCHAR(50)` | Không | `INDEX` | Mã loại nghiệp vụ kho (`PURCHASE_RECEIPT`, `SALE_POS`, `SALE_ONLINE`, `TRANSFER_OUT`, `TRANSFER_IN`, `STOCKTAKE_ADJUST`, `DISPOSAL_EXPIRED`). |
| `QuantityChange`|`int` | `INT` | Không | Không | Số lượng biến động (giá trị dương nếu tăng tồn, âm nếu giảm tồn). |
| `BalanceBefore` | `int` | `INT` | Không | Không | Số lượng tồn kho vật lý tại chi nhánh ngay trước khi phát sinh giao dịch. |
| `BalanceAfter` | `int` | `INT` | Không | Không | Số lượng tồn kho vật lý tại chi nhánh sau khi cập nhật thành công. |
| `ReferenceType` | `string?`| `VARCHAR(50)` | Có | Không | Tên thực thể tham chiếu sinh ra giao dịch (`Order`, `StockTransfer`, `Stocktake`). |
| `ReferenceCode` | `string?`| `VARCHAR(50)` | Có | `INDEX` | Mã chứng từ cụ thể liên quan (`ORD-20261104-001`, `TRF-2026-0012`...). |
| `Note` | `string?` | `NVARCHAR(500)` | Có | Không | Diễn giải nguyên nhân phát sinh biến động kho từ người thực hiện. |
| `PerformedBy` | `int?` | `INT` | Có | `FK -> Users(Id)` | Mã tài khoản nhân sự trực tiếp xác nhận giao dịch kho. |
| `CreatedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm ghi nhận biến động vào sổ cái kho bất biến. |

*Phân tích thiết kế bảng InventoryLogs:* Bảng này hoạt động như một Sổ cái kiểm toán kho bất biến (Append-Only Immutable Ledger). Không có bất kỳ câu lệnh `UPDATE` hay `DELETE` nào được phép thực thi trên bảng này. Mọi hành động làm thay đổi tồn kho bắt buộc phải sinh ra một bản ghi mới ghi nhận rõ ràng `BalanceBefore` và `BalanceAfter`, bảo đảm khả năng truy vết 100% nguyên nhân thất thoát hàng hóa.

---

###### Bảng 2.27: Đặc tả chi tiết bảng StockTransfers & StockTransferDetails (Điều chuyển kho liên chi nhánh)

*1. Bảng StockTransfers (Phiếu điều chuyển kho liên chi nhánh):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh phiếu điều chuyển kho. |
| `TransferCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã chứng từ điều chuyển duy nhất (`TRF-20261104-0012`). |
| `SourceStoreId` | `int` | `INT` | Không | `FK -> Stores(Id), INDEX` | Chi nhánh xuất kho điều chuyển (Kho nguồn / Tổng kho). |
| `DestinationStoreId`|`int`| `INT` | Không | `FK -> Stores(Id), INDEX` | Chi nhánh tiếp nhận hàng hóa (Showroom đích). |
| `CreatedByUserId`|`int` | `INT` | Không | `FK -> Users(Id)` | Nhân sự lập đề xuất yêu cầu điều chuyển. |
| `ApprovedByUserId`|`int?`| `INT` | Có | `FK -> Users(Id)` | Quản trị viên hoặc Thủ kho xuất ký lệnh phê duyệt và xuất hàng. |
| `ReceivedByUserId`|`int?`| `INT` | Có | `FK -> Users(Id)` | Thủ kho đích xác nhận hoàn tất nhập kho sau kiểm đếm. |
| `Status` | `string` | `VARCHAR(30)` | Không | `DEFAULT = 'PENDING'` | Trạng thái quy trình: `PENDING`, `APPROVED`, `IN_TRANSIT`, `RECEIVED`, `CANCELLED`. |
| `TotalRequestedQuantity`|`int`|`INT` | Không | Không | Tổng số lượng sản phẩm chi nhánh đích đề xuất điều chuyển. |
| `TotalSentQuantity`|`int`| `INT` | Không | Không | Tổng số lượng sản phẩm thực tế kho nguồn đóng gói xuất đi. |
| `TotalReceivedQuantity`|`int`|`INT`| Không | Không | Tổng số lượng sản phẩm thực tế chi nhánh đích nhận được nguyên vẹn. |
| `Note` | `string?` | `NVARCHAR(500)` | Có | Không | Ghi chú điều chuyển (ví dụ: Tiếp ứng khẩn cấp cho sự kiện khai trương). |
| `RequestedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm gửi phiếu đề xuất. |
| `ApprovedAt` | `DateTime?`|`DATETIME2(7)` | Có | Không | Thời điểm kho nguồn bấm xác nhận xuất kho vận chuyển (Dispatch). |
| `ReceivedAt` | `DateTime?`|`DATETIME2(7)` | Có | Không | Thời điểm kho đích bấm xác nhận nhập kho hoàn tất. |

*2. Bảng StockTransferDetails (Chi tiết phân bổ lô điều chuyển):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh dòng chi tiết phiếu chuyển. |
| `StockTransferId`| `int` | `INT` | Không | `FK -> StockTransfers(Id), INDEX`| Khóa ngoại trỏ về phiếu điều chuyển cha. |
| `ProductId` | `int` | `INT` | Không | `FK -> Products(Id)` | Khóa ngoại xác định mặt hàng mỹ phẩm luân chuyển. |
| `ProductBatchId`| `int?` | `INT` | Có | `FK -> ProductBatches(Id)`| Lô hàng cụ thể được cấp phát xuất kho theo nguyên tắc FEFO. |
| `RequestedQuantity`|`int`| `INT` | Không | Không | Số lượng chi nhánh đích yêu cầu cho sản phẩm này. |
| `SentQuantity` | `int` | `INT` | Không | Không | Số lượng kho nguồn thực xuất từ lô tương ứng. |
| `ReceivedQuantity`|`int`| `INT` | Không | Không | Số lượng chi nhánh đích thực nhận nguyên vẹn sau kiểm đếm. |
| `ShortageReason`|`string?`|`NVARCHAR(255)`| Có | Không | Ghi chú lý do sai lệch nếu có hao hụt (vỡ chai thủy tinh, móp hộp...). |

*Phân tích thiết kế bảng StockTransfers & StockTransferDetails:* Việc lưu trữ độc lập `SentQuantity` và `ReceivedQuantity` tại từng dòng chi tiết là mấu chốt để hệ thống xử lý các tình huống rủi ro trong chuỗi cung ứng. Bất kỳ sự chênh lệch nào giữa số lượng gửi và nhận đều tự động sinh ra thuộc tính tính toán `ShortageQuantity = SentQuantity - ReceivedQuantity`, làm cơ sở đẩy dữ liệu sang phân hệ tài chính để ghi nhận chi phí tổn thất vận chuyển.

---

##### D. Nhóm 4: Phân hệ Bán lẻ Đa kênh, Đơn hàng O2O & Ca Thu ngân POS

Phân hệ này đảm nhiệm toàn bộ luồng giao dịch tiền - hàng tại mặt tiền của doanh nghiệp, từ giỏ hàng trực tuyến, thanh toán đa phương thức đến kiểm soát chặt chẽ dòng tiền mặt tại két bán lẻ POS.

###### Bảng 2.28: Đặc tả chi tiết bảng Orders (Đơn hàng đa kênh hợp nhất)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh duy nhất mỗi đơn hàng trong hệ thống. |
| `OrderCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã đơn hàng duy nhất in trên hóa đơn (`ORD-20261104-0089`). |
| `UserId` | `int?` | `INT` | Có | `FK -> Users(Id), INDEX` | Khóa ngoại xác định tài khoản khách hàng (nếu khách đã đăng nhập). |
| `StoreId` | `int` | `INT` | Không | `FK -> Stores(Id), INDEX` | Chi nhánh chịu trách nhiệm thực hiện đơn hàng (Showroom bán POS hoặc chi nhánh đóng gói O2O). |
| `PosShiftId` | `int?` | `INT` | Có | `FK -> PosShifts(Id), INDEX`| Khóa ngoại xác định ca thu ngân tạo đơn (nếu mua trực tiếp tại quầy POS). |
| `OrderSource` | `string` | `VARCHAR(30)` | Không | `DEFAULT = 'POS'` | Kênh phát sinh đơn hàng: `POS`, `WEBSITE`, `MOBILE_APP`. |
| `OrderStatus` | `string` | `VARCHAR(30)` | Không | `INDEX` | Trạng thái đơn hàng: `PENDING`, `CONFIRMED`, `PROCESSING`, `SHIPPED`, `COMPLETED`, `CANCELLED`. |
| `PaymentStatus`| `string` | `VARCHAR(30)` | Không | `DEFAULT = 'UNPAID'` | Trạng thái thanh toán: `UNPAID`, `PAID`, `REFUNDED`, `PARTIALLY_REFUNDED`. |
| `PaymentMethod`| `string` | `VARCHAR(50)` | Không | Không | Phương thức thanh toán: `CASH`, `VIETQR`, `VNPAY`, `MOMO`, `COD`. |
| `SubTotal` | `decimal`| `DECIMAL(18,2)` | Không | Không | Tổng giá trị hàng hóa trước khi giảm giá và tính phí vận chuyển. |
| `DiscountAmount`|`decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Tổng giá trị chiết khấu từ Coupon, Voucher hoặc tiêu điểm Loyalty. |
| `ShippingFee` | `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Cước phí vận chuyển tính toán qua API Giao Hàng Nhanh (GHN). |
| `TotalAmount` | `decimal`| `DECIMAL(18,2)` | Không | `INDEX` | Tổng số tiền thanh toán cuối cùng của đơn hàng sau giảm trừ. |
| `CustomerName` | `string` | `NVARCHAR(100)` | Không | Không | Họ và tên người nhận hàng. |
| `CustomerPhone`| `string` | `VARCHAR(20)` | Không | `INDEX` | Số điện thoại nhận hàng và tra cứu lịch sử mua sắm. |
| `ShippingAddress`|`string?`|`NVARCHAR(255)` | Có | Không | Địa chỉ giao hàng tận nhà (với đơn Online) hoặc để trống (với đơn mua tại quầy). |
| `RowVersion` | `byte[]?`| `ROWVERSION` | Có | `CONCURRENCY CHECK` | Mã kiểm soát xung đột dữ liệu khi nhiều tiến trình cùng cập nhật trạng thái đơn. |
| `CreatedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm tạo lập đơn hàng. |
| `UpdatedAt` | `DateTime?`|`DATETIME2(7)` | Có | Không | Thời điểm cập nhật trạng thái đơn hàng gần nhất. |

*Phân tích thiết kế bảng Orders:* Trường `PosShiftId` liên kết trực tiếp với bảng `PosShifts` là chốt chặn quan trọng bảo đảm tính minh bạch tài chính. Mọi đơn hàng bán ra tại quầy đều phải gắn chặt với một ca làm việc cụ thể của một thu ngân xác định, phục vụ việc đối soát doanh số tiền mặt và thẻ ngân hàng khi kết thúc ca làm việc.

---

###### Bảng 2.29: Đặc tả chi tiết bảng OrderDetails (Chi tiết dòng hàng và Nguồn gốc lô xuất)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh mỗi dòng chi tiết sản phẩm trong đơn. |
| `OrderId` | `int` | `INT` | Không | `FK -> Orders(Id), INDEX` | Khóa ngoại xác định đơn hàng cha. |
| `ProductId` | `int` | `INT` | Không | `FK -> Products(Id), INDEX`| Khóa ngoại xác định sản phẩm mỹ phẩm được mua. |
| `ProductBatchId`|`int?` | `INT` | Có | `FK -> ProductBatches(Id)`| Lô hàng thực tế được xuất kho theo thuật toán FEFO để bán cho khách. |
| `Quantity` | `int` | `INT` | Không | Không | Số lượng sản phẩm khách hàng đặt mua. |
| `UnitPrice` | `decimal`| `DECIMAL(18,2)` | Không | Không | Giá bán niêm yết của một đơn vị sản phẩm tại thời điểm chốt đơn. |
| `DiscountRate` | `decimal`| `DECIMAL(5,2)` | Không | `DEFAULT = 0` | Tỷ lệ chiết khấu riêng cho dòng sản phẩm (từ 0.00% đến 100.00%). |
| `LineTotal` | `decimal`| `DECIMAL(18,2)` | Không | Không | Thành tiền cuối cùng của dòng hàng sau khi trừ chiết khấu. |

*Phân tích thiết kế bảng OrderDetails:* Cột `ProductBatchId` là trái tim của cơ chế kế toán quản trị và an toàn dược mỹ phẩm. Nhờ lưu trữ mã lô cụ thể xuất bán, hệ thống vừa tính toán được giá vốn hàng bán đích danh theo công thức: `Batch COGS = Quantity * ProductBatch.ImportCost`, vừa hỗ trợ truy xuất nguồn gốc ngược trong trường hợp một lô sản phẩm bị nhà sản xuất hoặc Bộ Y tế phát lệnh thu hồi khẩn cấp.

---

###### Bảng 2.30: Đặc tả chi tiết bảng Invoices (Hóa đơn và Lịch sử giao dịch thanh toán)

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh hóa đơn thanh toán. |
| `InvoiceCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã hóa đơn duy nhất in cho khách hàng (`INV-20261104-0089`). |
| `OrderId` | `int` | `INT` | Không | `FK -> Orders(Id), UNIQUE`| Khóa ngoại quan hệ 1-1 với đơn hàng tương ứng. |
| `InvoiceDate` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Ngày giờ chính thức phát hành hóa đơn. |
| `PaymentMethod`| `string` | `VARCHAR(50)` | Không | Không | Phương thức thực hiện giao dịch (`CASH`, `VIETQR`, `VNPAY`). |
| `TotalAmount` | `decimal`| `DECIMAL(18,2)` | Không | Không | Tổng giá trị tiền mặt hoặc tiền điện tử đã thanh toán thành công. |

*Phân tích thiết kế bảng Invoices:* Bản ghi trong bảng `Invoices` là chứng từ hoàn tất nghĩa vụ thanh toán của khách hàng. Khi bản ghi này được khởi tạo thành công, hệ thống lập tức kích hoạt sự kiện miền `OrderPaidEvent` để gọi `LoyaltyService` thực thi tích điểm thưởng cho khách hàng và gọi `FinanceService` ghi nhận doanh thu vào Sổ cái tài chính.

---

###### Bảng 2.31: Đặc tả chi tiết bảng PosShifts & PosDrawerTransactions (Ca bán hàng POS và Quản lý Két tiền mặt)

*1. Bảng PosShifts (Quản lý chu trình ca làm việc của Thu ngân):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh ca làm việc POS. |
| `ShiftCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã ca làm việc duy nhất (`SHIFT-20261104-001`). |
| `StoreId` | `int` | `INT` | Không | `FK -> Stores(Id), INDEX` | Chi nhánh nơi ca làm việc được mở. |
| `CashierId` | `int` | `INT` | Không | `FK -> Users(Id), INDEX` | Tài khoản nhân viên thu ngân chịu trách nhiệm quản lý két trong ca. |
| `OpenedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm thu ngân bấm mở ca làm việc. |
| `ClosedAt` | `DateTime?`|`DATETIME2(7)` | Có | Không | Thời điểm thu ngân bấm đóng ca và nộp tiền két. |
| `OpeningBalance`|`decimal`| `DECIMAL(18,2)` | Không | Không | Số tiền mặt ban đầu bàn giao trong két để thối tiền cho khách. |
| `ExpectedCash` | `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Số tiền mặt lý thuyết hệ thống tính toán phải có trong két cuối ca. |
| `ActualCash` | `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Số tiền mặt thực tế do thu ngân kiểm đếm từng tờ nộp vào khi đóng ca. |
| `Difference` | `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Độ lệch két tiền mặt (`Difference = ActualCash - ExpectedCash`). |
| `CashSalesTotal`|`decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Tổng doanh số bán thu bằng tiền mặt trong suốt ca làm việc. |
| `CardSalesTotal`|`decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Tổng doanh số thanh toán không dùng tiền mặt (VietQR, Cà thẻ). |
| `TransactionCount`|`int` | `INT` | Không | `DEFAULT = 0` | Tổng số lượng hóa đơn đã thanh toán thành công trong ca. |
| `Status` | `string` | `VARCHAR(20)` | Không | `DEFAULT = 'OPEN'` | Trạng thái ca: `OPEN` (Đang mở), `CLOSED` (Đã đóng ca), `VERIFIED` (Quản lý đã duyệt). |
| `RowVersion` | `byte[]?`| `ROWVERSION` | Có | `CONCURRENCY CHECK` | Chống xung đột khi cập nhật đồng thời số dư két. |

*2. Bảng PosDrawerTransactions (Biến động thu/chi tiền mặt tại két bán lẻ):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh giao dịch rút/nạp tiền két. |
| `PosShiftId` | `int` | `INT` | Không | `FK -> PosShifts(Id), INDEX`| Khóa ngoại trỏ về ca làm việc đang phát sinh biến động két. |
| `Type` | `string` | `VARCHAR(30)` | Không | Không | Loại giao dịch két: `CASH_IN` (Nộp thêm tiền thối), `CASH_OUT` (Rút bớt tiền nộp về két sắt), `PETTY_CASH` (Chi tiền lẻ mua đồ dùng cửa hàng). |
| `Amount` | `decimal`| `DECIMAL(18,2)` | Không | Không | Số tiền mặt nạp vào hoặc rút ra khỏi két. |
| `Reason` | `string` | `NVARCHAR(255)` | Không | Không | Lý do cụ thể của hành động nạp hoặc rút tiền mặt. |
| `PerformedBy` | `int` | `INT` | Không | `FK -> Users(Id)` | Nhân sự thực hiện hành động nạp rút tiền két. |
| `CreatedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Ngày giờ thực thi giao dịch két tiền mặt. |

*Phân tích thiết kế bảng PosShifts & PosDrawerTransactions:* Cặp bảng này hiện thực hóa nguyên tắc kiểm toán két mù (Blind Cash Audit). Tại thời điểm đóng ca, hệ thống không bao giờ hiển thị trường `ExpectedCash` cho thu ngân nhìn thấy, buộc thu ngân phải đếm tiền thực tế và nhập con số khách quan. Sự sai lệch `Difference` dù thừa hay thiếu đều được lưu vết vĩnh viễn và tự động đẩy sang Sổ cái tài chính để hạch toán chi phí thất thoát hoặc thu nhập bất thường.

---

##### E. Nhóm 5: Phân hệ Hồ sơ Sắc đẹp CRM, Ví điểm Loyalty, Khuyến mãi, Tài chính & Nhật ký Kiểm toán

Phân hệ này hoàn thiện bức tranh quản trị toàn diện của chuỗi bán lẻ mỹ phẩm OMNI BEAUTY, dung hòa giữa mục tiêu chăm sóc cá nhân hóa khách hàng để tối đa hóa doanh thu trọn đời (LTV) và kiểm soát tài chính minh bạch cho ban giám đốc.

###### Bảng 2.32: Đặc tả chi tiết bảng CustomerProfiles & RepurchaseSchedules (Hồ sơ Sắc đẹp CRM & Dự báo dùng hết)

*1. Bảng CustomerProfiles (Hồ sơ Da liễu & Lịch sử Chăm sóc Sắc đẹp):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh hồ sơ da liễu khách hàng. |
| `UserId` | `int` | `INT` | Không | `FK -> Users(Id), UNIQUE` | Khóa ngoại liên kết 1-1 với tài khoản người dùng hệ thống. |
| `DateOfBirth` | `DateTime?`|`DATETIME2(7)` | Có | Không | Ngày sinh nhật của khách hàng dùng để kích hoạt quà tặng voucher và nhân điểm VIP. |
| `Gender` | `string?` | `VARCHAR(10)` | Có | Không | Giới tính khách hàng (`Male`, `Female`, `Other`). |
| `CurrentSkinType`|`string?` | `NVARCHAR(50)` | Có | Không | Tuýp da hiện tại xác định qua bài trắc nghiệm Skin Quiz (Da dầu, Da khô, Nhạy cảm, Hỗn hợp). |
| `SkinConcerns` | `string?` | `NVARCHAR(500)` | Có | Không | Chuỗi JSON mảng các vấn đề về da cần điều trị (`["Mụn", "Thâm nám", "Lão hóa"]`). |
| `KnownAllergies`| `string?` | `NVARCHAR(500)` | Có | Không | Chuỗi JSON danh mục hoạt chất khách hàng từng bị dị ứng trong quá khứ (`["Paraben", "Cồn khô"]`). |
| `PreferredBrands`|`string?` | `NVARCHAR(500)` | Có | Không | Chuỗi JSON các thương hiệu mỹ phẩm khách hàng tin dùng nhất. |
| `ConsultantUserId`|`int?` | `INT` | Có | `FK -> Users(Id)` | Mã chuyên viên tư vấn làm đẹp (Beauty Advisor) phụ trách chăm sóc riêng khách hàng này. |

*2. Bảng RepurchaseSchedules (Lịch trình Dự báo dùng hết mỹ phẩm để nhắc mua lại):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh lịch nhắc mua hàng. |
| `CustomerProfileId`|`int` | `INT` | Không | `FK -> CustomerProfiles(Id), INDEX`| Khóa ngoại trỏ về hồ sơ khách hàng tương ứng. |
| `ProductId` | `int` | `INT` | Không | `FK -> Products(Id)` | Mặt hàng mỹ phẩm khách hàng đã mua và đang sử dụng. |
| `LastPurchaseDate`|`DateTime`|`DATETIME2(7)` | Không | Không | Ngày gần nhất khách hàng mua sản phẩm này. |
| `EstimatedVolumeMl`|`decimal`|`DECIMAL(10,2)`| Không | Không | Dung tích thực tế của chai lọ mỹ phẩm (ml hoặc gram). |
| `DailyUsageMl` | `decimal`| `DECIMAL(10,2)` | Không | Không | Liều lượng tiêu thụ trung bình hàng ngày theo khuyến cáo da liễu. |
| `EstimatedDepletionDate`|`DateTime`|`DATETIME2(7)`| Không | `INDEX` | Ngày dự báo khách hàng sẽ dùng hết sản phẩm (`EstimatedDepletionDate = LastPurchaseDate + Volume / DailyUsage`). |
| `IsNotified` | `bool` | `BIT` | Không | `DEFAULT = 0` | Trạng thái gửi thông báo đẩy: 1: Đã nhắc nhở mua lại; 0: Chưa gửi. |

*Phân tích thiết kế bảng CustomerProfiles & RepurchaseSchedules:* Cột `KnownAllergies` trong `CustomerProfiles` là chốt chặn an toàn được Động cơ Tương kỵ Hoạt chất `SkincareIngredientInteractionEngine` đọc trực tiếp để phát cảnh báo dị ứng ngay tại giỏ hàng. Trong khi đó, bảng `RepurchaseSchedules` cung cấp dữ liệu cho tiến trình nền tự động quét ngày `EstimatedDepletionDate` để gửi tin nhắn thông báo đẩy trước 5 ngày khi kem chống nắng hoặc serum của khách sắp hết, gia tăng tỷ lệ khách hàng quay lại mua sắm đều đặn.

---

###### Bảng 2.33: Đặc tả chi tiết bảng LoyaltyWallets, MemberTiers & PointTransactions (Ví điểm & Hạng VIP)

*1. Bảng MemberTiers (Chính sách Bậc hạng thành viên):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh bậc hạng thẻ. |
| `TierName` | `string` | `NVARCHAR(50)` | Không | `UNIQUE INDEX` | Tên bậc hạng hội viên (Silver, Gold, Diamond). |
| `MinSpend` | `decimal`| `DECIMAL(18,2)` | Không | Không | Ngưỡng chi tiêu tối thiểu lũy kế trong 12 tháng để đạt và giữ hạng. |
| `PointMultiplier`|`decimal`| `DECIMAL(5,2)` | Không | `DEFAULT = 1.0` | Hệ số nhân điểm thưởng khi mua sắm (Silver: x1.0, Gold: x1.5, Diamond: x2.0). |
| `BirthdayBonusPoints`|`int`| `INT` | Không | `DEFAULT = 0` | Số điểm thưởng tặng sinh nhật tự động nạp vào ví khách hàng. |

*2. Bảng LoyaltyWallets (Ví điểm thưởng thành viên):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh ví điểm thưởng. |
| `UserId` | `int` | `INT` | Không | `FK -> Users(Id), UNIQUE` | Khóa ngoại liên kết 1-1 với tài khoản thành viên. |
| `CurrentTierId` | `int` | `INT` | Không | `FK -> MemberTiers(Id)` | Bậc hạng thẻ VIP hiện tại của khách hàng. |
| `TotalPointsEarned`|`int` | `INT` | Không | `DEFAULT = 0` | Tổng số điểm khách hàng đã từng tích lũy trong toàn bộ lịch sử mua sắm. |
| `CurrentBalance`| `int` | `INT` | Không | `DEFAULT = 0` | Số dư điểm khả dụng hiện tại có thể dùng để khấu trừ tiền khi mua sắm. |
| `TotalSpend12Months`|`decimal`|`DECIMAL(18,2)`| Không | `DEFAULT = 0` | Tổng chi tiêu thuần trong 365 ngày gần nhất dùng để xét thăng/hạ hạng tự động. |
| `QrCodeData` | `string?` | `VARCHAR(200)` | Có | Không | Dữ liệu mã hóa chuỗi định danh mã vạch thẻ thành viên điện tử `BEAUTY-MEM-{UserId}`. |
| `RowVersion` | `byte[]?`| `ROWVERSION` | Có | `CONCURRENCY CHECK` | Ngăn chặn việc tiêu cùng một số dư điểm đồng thời trên hai thiết bị. |

*3. Bảng PointTransactions (Nhật ký giao dịch điểm thưởng):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh mỗi giao dịch điểm. |
| `LoyaltyWalletId`|`int` | `INT` | Không | `FK -> LoyaltyWallets(Id), INDEX`| Khóa ngoại xác định ví điểm thụ hưởng. |
| `OrderId` | `int?` | `INT` | Có | `FK -> Orders(Id)` | Khóa ngoại gắn với đơn hàng phát sinh điểm (nếu có). |
| `Type` | `string` | `VARCHAR(30)` | Không | `INDEX` | Loại biến động: `EARN` (Tích điểm đơn hàng), `REDEEM` (Tiêu điểm trừ tiền), `BIRTHDAY_BONUS` (Thưởng sinh nhật), `ADJUSTMENT` (Điều chỉnh thủ công). |
| `Points` | `int` | `INT` | Không | Không | Số điểm biến động (mang dấu dương nếu cộng, mang dấu âm nếu trừ). |
| `BalanceBefore` | `int` | `INT` | Không | Không | Số dư điểm trong ví trước khi giao dịch diễn ra. |
| `BalanceAfter` | `int` | `INT` | Không | Không | Số dư điểm trong ví sau khi cập nhật giao dịch thành công. |

*Phân tích thiết kế bảng LoyaltyWallets, MemberTiers & PointTransactions:* Cơ chế tích hợp `RowVersion` tại bảng `LoyaltyWallets` bảo vệ tài sản điểm thưởng của người dùng trước các cuộc tấn công tranh chấp tài nguyên đồng thời (Race Condition). Tỷ lệ quy đổi điểm được khóa cứng ở mức 1 điểm = 1.000 VNĐ và được kiểm soát giới hạn tối đa 30% giá trị hóa đơn, bảo đảm doanh nghiệp không bao giờ bị âm biên lợi nhuận gộp khi khách hàng sử dụng điểm.

---

###### Bảng 2.34: Đặc tả chi tiết bảng Promotions & Coupons (Động cơ Khuyến mãi và Mã giảm giá)

*1. Bảng Promotions (Chiến dịch Khuyến mãi tự động):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh chương trình khuyến mãi. |
| `PromotionCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã định danh nội bộ của chiến dịch (`PROMO-SUMMER-2026`). |
| `Name` | `string` | `NVARCHAR(150)` | Không | Không | Tên hiển thị của chương trình ưu đãi trên các banner và giỏ hàng. |
| `PromotionType` | `string` | `VARCHAR(30)` | Không | Không | Hình thức khuyến mãi: `PERCENTAGE`, `FIXED_AMOUNT`, `BUY_X_GET_Y`, `TIERED_GWP`, `FLASH_SALE`. |
| `DiscountValue` | `decimal`| `DECIMAL(18,2)` | Không | Không | Giá trị giảm (% chiết khấu hoặc số tiền giảm cố định). |
| `MinOrderValue` | `decimal?`|`DECIMAL(18,2)` | Có | Không | Giá trị đơn hàng tối thiểu để đủ điều kiện hưởng chương trình. |
| `MaxDiscountAmount`|`decimal?`|`DECIMAL(18,2)`| Có | Không | Giới hạn số tiền giảm tối đa khi áp dụng chính sách giảm theo %. |
| `ValidFrom` | `DateTime`| `DATETIME2(7)` | Không | `INDEX` | Ngày giờ chính thức có hiệu lực của chương trình. |
| `ValidTo` | `DateTime`| `DATETIME2(7)` | Không | `INDEX` | Ngày giờ kết thúc chương trình khuyến mãi. |
| `IsActive` | `bool` | `BIT` | Không | `DEFAULT = 1` | Trạng thái kích hoạt áp dụng của chiến dịch. |

*2. Bảng Coupons (Mã giảm giá nhập tay):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh mã coupon. |
| `CouponCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã ký tự khách hàng nhập vào ô khuyến mãi (`OMNIBEAUTY20`, `WELCOMEVIP`). |
| `DiscountType` | `string` | `VARCHAR(20)` | Không | Không | Loại giảm giá: `PERCENTAGE` hoặc `FIXED_AMOUNT`. |
| `DiscountValue` | `decimal`| `DECIMAL(18,2)` | Không | Không | Mức độ giảm giá tương ứng với loại giảm. |
| `UsageLimit` | `int?` | `INT` | Có | Không | Tổng số lượt cho phép áp dụng trên toàn bộ hệ thống chuỗi. |
| `UsageCount` | `int` | `INT` | Không | `DEFAULT = 0` | Số lượt thực tế khách hàng đã sử dụng mã thành công. |
| `RowVersion` | `byte[]?`| `ROWVERSION` | Có | `CONCURRENCY CHECK` | Khóa chống vượt hạn mức khi hàng ngàn khách cùng nhập mã Flash Sale. |

*Phân tích thiết kế bảng Promotions & Coupons:* Động cơ khuyến mãi `PromotionEngine` đọc đồng thời hai bảng này để áp dụng cơ chế cộng dồn hoặc loại trừ theo quy tắc kinh doanh. Trường `RowVersion` tại bảng `Coupons` đóng vai trò tối quan trọng trong các đợt Flash Sale lớn, bảo đảm khi một mã giảm giá giới hạn 100 lượt sử dụng chạm ngưỡng, hệ thống sẽ từ chối lượt thứ 101 ngay lập tức mà không gặp lỗi tranh chấp dữ liệu.

---

###### Bảng 2.35: Đặc tả chi tiết bảng FinancialLedgers, CodReconciliations & AuditLogs (Kế toán Sổ cái, COD & Kiểm toán)

*1. Bảng FinancialLedgers (Sổ cái Kế toán Tài chính Đích danh):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh bút toán sổ cái. |
| `LedgerCode` | `string` | `VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã bút toán kế toán chuẩn hóa (`LED-20261104-A1B2C3D4`). |
| `EntryDate` | `DateTime`| `DATETIME2(7)` | Không | `INDEX` | Ngày ghi sổ kế toán tài chính. |
| `EntryType` | `string` | `VARCHAR(30)` | Không | `INDEX` | Loại bút toán: `REVENUE` (Doanh thu thuần), `COGS` (Giá vốn đích danh theo lô), `LOYALTY_DISCOUNT` (Chi phí điểm thưởng), `SHIPPING_COST` (Chi phí vận chuyển), `STOCK_LOSS` (Hao hụt kho). |
| `StoreId` | `int` | `INT` | Không | `FK -> Stores(Id), INDEX` | Chi nhánh phát sinh khoản mục tài chính phục vụ báo cáo P&L riêng biệt. |
| `OrderId` | `int?` | `INT` | Có | `FK -> Orders(Id)` | Khóa ngoại gắn với đơn hàng phát sinh dòng tiền. |
| `SupplierId` | `int?` | `INT` | Có | `FK -> Suppliers(Id)` | Khóa ngoại gắn với nhà cung cấp (nếu thanh toán tiền nhập lô). |
| `DebitAmount` | `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Số tiền ghi Nợ (tăng chi phí, tăng tài sản hoặc giảm nợ phải trả). |
| `CreditAmount`| `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Số tiền ghi Có (tăng doanh thu, tăng nguồn vốn hoặc giảm tài sản). |
| `Description` | `string?` | `NVARCHAR(500)` | Có | Không | Diễn giải nội dung chứng từ kế toán chi tiết. |

*2. Bảng CodReconciliations (Đối soát Tiền thu hộ COD Đơn vị Vận chuyển):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh đợt đối soát tiền COD. |
| `ReconciliationCode`|`string`|`VARCHAR(50)` | Không | `UNIQUE INDEX` | Mã phiên đối soát kỳ kế toán (`COD-GHN-20261104-01`). |
| `ShippingPartner`|`string`| `VARCHAR(50)` | Không | Không | Tên đơn vị vận chuyển đối tác (`GHN`, `ViettelPost`). |
| `TotalOrders` | `int` | `INT` | Không | Không | Tổng số lượng đơn hàng nằm trong danh sách đối soát thanh toán. |
| `TotalCodAmount`|`decimal`| `DECIMAL(18,2)` | Không | Không | Tổng số tiền COD lý thuyết đơn vị vận chuyển phải thu của khách hàng. |
| `ReceivedAmount`|`decimal`| `DECIMAL(18,2)` | Không | Không | Số tiền mặt thực tế đơn vị vận chuyển đã chuyển khoản đối soát về ngân hàng. |
| `Difference` | `decimal`| `DECIMAL(18,2)` | Không | `DEFAULT = 0` | Chênh lệch tiền thu hộ (`Difference = ReceivedAmount - TotalCodAmount`). |
| `Status` | `string` | `VARCHAR(20)` | Không | `DEFAULT = 'PENDING'` | Trạng thái đối soát: `PENDING`, `MATCHED` (Khớp lệnh), `DISPUTED` (Tranh chấp khiếu nại). |

*3. Bảng AuditLogs (Nhật ký Kiểm toán An ninh và Thay đổi Dữ liệu nhạy cảm):*

| Tên cột | Kiểu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `Id` | `int` | `INT IDENTITY(1,1)` | Không | `PRIMARY KEY` | Khóa chính tự tăng định danh bản ghi kiểm toán. |
| `UserId` | `int?` | `INT` | Có | `FK -> Users(Id), INDEX` | Mã tài khoản nhân sự thực hiện thao tác can thiệp dữ liệu. |
| `Action` | `string` | `VARCHAR(50)` | Không | `INDEX` | Hành vi thực hiện: `CREATE`, `UPDATE`, `DELETE`, `PERMISSION_OVERRIDE`, `PRICE_CHANGE`. |
| `EntityType` | `string` | `VARCHAR(100)` | Không | `INDEX` | Tên bảng hoặc thực thể bị tác động (`Product`, `Order`, `StoreInventory`). |
| `EntityId` | `string` | `VARCHAR(100)` | Không | Không | Giá trị khóa chính của bản ghi bị tác động. |
| `OldValues` | `string?` | `NVARCHAR(MAX)` | Có | Không | Chuỗi JSON chụp ảnh toàn bộ trạng thái dữ liệu trước khi thay đổi. |
| `NewValues` | `string?` | `NVARCHAR(MAX)` | Có | Không | Chuỗi JSON chụp ảnh toàn bộ trạng thái dữ liệu mới sau khi lưu thành công. |
| `IpAddress` | `string?` | `VARCHAR(50)` | Có | Không | Địa chỉ IP của máy trạm phát sinh yêu cầu can thiệp dữ liệu. |
| `UserAgent` | `string?` | `VARCHAR(500)` | Có | Không | Chuỗi định danh thiết bị và trình duyệt của người thao tác. |
| `CreatedAt` | `DateTime`| `DATETIME2(7)` | Không | `DEFAULT = SYSUTCDATETIME()`| Thời điểm chính xác ghi nhận nhật ký kiểm toán vào hệ thống. |

*Phân tích thiết kế bảng FinancialLedgers, CodReconciliations & AuditLogs:* Bảng `FinancialLedgers` là nền tảng cốt tử cho phép kế toán trưởng kết xuất Báo cáo Lãi/Lỗ (P&L) đa chiều theo từng showroom mà không cần phần mềm ERP phụ trợ bên ngoài. Trong khi đó, bảng `AuditLogs` được nuôi dưỡng tự động bởi `ChangeTracker Interceptor` của Entity Framework Core: Bất kể khi nào có thao tác sửa đổi giá bán hoặc phân quyền nhân sự, hệ thống lập tức tự động chụp ảnh JSON của `OldValues` và `NewValues` kèm địa chỉ IP máy trạm, bảo đảm an ninh hệ thống và ngăn ngừa triệt để các hành vi gian lận nội bộ.

---

### 2.4.4. Thiết kế Kiến trúc Giao diện Người dùng (UI/UX Design Specifications)

Giao diện người dùng trong ngành kinh doanh mỹ phẩm cao cấp đóng vai trò là điểm chạm cảm xúc quyết định tỷ lệ chuyển đổi của khách hàng, đồng thời quyết định tốc độ thao tác và sự chuẩn xác của nhân viên vận hành tại chuỗi cửa hàng. OMNI BEAUTY xây dựng ngôn ngữ thiết kế mỹ phẩm sang trọng (*Luxury Cosmetic Design System*) kết hợp kiến trúc giao diện động dựa trên dữ liệu (*Data-Driven UI*) và bốn bố cục Razor Layouts quản trị chuyên biệt hóa cao theo từng vai trò công việc.

##### 1. Ngôn ngữ Thiết kế Mỹ phẩm Sang trọng (Luxury Cosmetic Design System)

Hệ sinh thái giao diện của OMNI BEAUTY được chuẩn hóa thông qua hệ thống thẻ định danh thiết kế (Design Tokens) lưu trữ tập trung tại `design-tokens.css`, bảo đảm sự nhất quán tuyệt đối giữa kênh bán lẻ trực tuyến Storefront và hệ thống quản trị hậu trường Admin Portal:
- **Bảng màu chủ đạo (Color Palette):**
  + *Rose Gold (`#B76E79` / `#EC4899`):* Màu vàng hồng đại diện cho sự quý phái, thanh lịch và mềm mại của phái đẹp, được sử dụng làm màu nhấn cho các nút kêu gọi hành động (CTA), huy hiệu khuyến mãi và biểu tượng quà tặng.
  + *Deep Emerald (`#1B4D3E`):* Màu xanh ngọc lục bảo đại diện cho tiêu chuẩn Mỹ phẩm Sạch (Clean Beauty), các sản phẩm có nguồn gốc thuần chay, hữu cơ và an toàn dược học, được dùng cho các nhãn chứng nhận thành phần và biểu đồ phân tích da liễu.
  + *Pearl White (`#FBFBFB` / `#FFFDFB`):* Màu trắng ngọc trai và kem sáng tạo cảm giác tinh khiết, sạch sẽ, làm nền chủ đạo cho các trang trưng bày sản phẩm để tôn vinh hình ảnh và màu sắc thực tế của mỹ phẩm.
  + *Dark Slate (`#0F172A` / `#1E293B`):* Màu đá phiến sẫm mang lại sự tương phản cao, được ứng dụng cho hệ thống tiêu đề, văn bản thông số kỹ thuật và nền bảng điều khiển Kiosk POS chống mỏi mắt.
- **Nghệ thuật chữ phân cấp (Typography Hierarchy):**
  + Giao diện ứng dụng hai bộ font chữ tối tân được nhúng trực tiếp qua Google Fonts: Bộ font `Plus Jakarta Sans` với các biến thể độ đậm từ 300 đến 800 được sử dụng cho toàn bộ nhãn giao diện, nút bấm và nội dung bài viết nhờ tỷ lệ khoảng cách chữ hoàn hảo, dễ đọc trên cả màn hình điện thoại di động và máy tính bảng; Song song đó, bộ font chuyên biệt `JetBrains Mono` được sử dụng độc quyền cho các trường dữ liệu mang tính định lượng cao như mã SKU, mã vạch Barcode EAN-13, số lô sản xuất `BatchNumber`, ngày hết hạn FEFO và các con số tiền tệ, bảo đảm các chữ số luôn thẳng hàng hoàn hảo khi hiển thị trên các bảng đối soát và hóa đơn nhiệt POS.
- **Kiến trúc Tùy biến Giao diện Động (Data-Driven Dynamic Theme Engine):**
  + Thay vì gán cứng bố cục trang chủ trong mã nguồn, hệ thống OMNI BEAUTY thiết kế các bảng `ThemeConfigs`, `PageLayouts`, `UiSections` và `SectionItems`. Quản trị viên nội dung có thể tự do thay đổi màu sắc chủ đạo, chuyển đổi chế độ giao diện Mùa Hè sang Chiến dịch Lễ Hội cuối năm, hoặc sắp xếp lại thứ tự các khối hiển thị (Banner Slider -> Sản phẩm Flash Sale cận date -> Danh mục nổi bật -> Trắc nghiệm Phân tích da) trực tiếp trên trang quản trị mà không cần lập trình viên phải can thiệp sửa mã HTML.

##### 2. Thiết kế Công thái học của 4 Razor Layouts Quản trị Chuyên biệt

Nhận thức sâu sắc rằng mỗi chức danh nhân sự trong chuỗi bán lẻ đều có bối cảnh làm việc và áp lực thao tác hoàn toàn khác nhau, OMNI BEAUTY không sử dụng chung một bố cục quản trị đơn điệu mà xây dựng **4 Razor Layouts chuyên biệt hóa tối đa về mặt công thái học (Ergonomics)**:

```
Omnichannel.Web/Areas/Admin/Views/Shared/
├── _AdminLayout.cshtml      -> Bố cục Điều hành Đa năng (SuperAdmin & StoreManager)
├── _PosLayout.cshtml        -> Bố cục Kiosk Toàn màn hình Bán lẻ (Cashier)
├── _WarehouseLayout.cshtml  -> Bố cục Mật độ cao Giám sát Lô FEFO (WarehouseStaff)
└── _CmsLayout.cshtml        -> Bố cục Biên tập & Soát lỗi SEO Chia đôi màn hình (ContentEditor)
```

###### A. Bố cục Điều hành Đa năng (_AdminLayout.cshtml)
Dành riêng cho Quản trị viên tối cao (`SuperAdmin`) và Quản lý chi nhánh (`StoreManager`). Layout này tập trung vào khả năng bao quát toàn diện và chuyển đổi linh hoạt giữa các phân hệ:
- Thanh điều hướng bên trái (Sidebar) có khả năng thu gọn linh hoạt, gom nhóm 17 phân hệ nghiệp vụ thành các khối logic kèm chỉ số đếm thông báo đẩy thời gian thực thông qua kết nối SignalR.
- Thanh đầu trang (Header) tích hợp Huy hiệu Chi nhánh làm việc (`StoreBadge`). Đối với nhân viên chi nhánh, hệ thống hiển thị cố định tên showroom được phân công; đối với SuperAdmin, khu vực này biến thành menu thả chọn nhanh chi nhánh để chuyển đổi tầm nhìn dữ liệu giữa các cửa hàng tức thì.
- Tích hợp sẵn Trung tâm Thông báo Đẩy (Notification Bell) hiển thị cảnh báo tồn kho và chuông reo đơn hàng mới cùng bảng điều khiển tài khoản bảo mật hai lớp.

###### B. Bố cục Kiosk Toàn màn hình Quầy Thu ngân (_PosLayout.cshtml)
Thiết kế chuyên biệt cho Thu ngân (`Cashier`) làm việc liên tục tại quầy bán lẻ Showroom với cường độ cao trong giờ cao điểm:
- **Triệt tiêu thanh cuộn dọc (Zero-Scroll Kiosk):** Bố cục sử dụng thuộc tính `height: 100vh; overflow: hidden; user-select: none;` khóa chặt màn hình trong khung nhìn cố định, bảo đảm nhân viên không bị trượt giao diện khi thao tác trên màn hình cảm ứng POS hoặc quét mã vạch liên tục.
- **Hệ thống phím tắt phần cứng chuyên dụng (Hardware Hotkeys):** Hỗ trợ toàn diện các phím chức năng chuẩn ngành bán lẻ được gắn nhãn trực quan trên màn hình: Phím `F2` để nhảy nhanh vào ô quét mã vạch Barcode, `F4` để tra cứu thông tin khách hàng thành viên theo số điện thoại, `F8` để áp dụng mã giảm giá Coupon, `F9` để chọn phương thức thanh toán chuyển khoản VietQR, và `F10` để chốt đơn in hóa đơn nhiệt tức thì.
- **Cơ chế Đồng bộ Màn hình Phụ Khách hàng (Dual-Screen CFD):** Layout tích hợp ngầm kênh truyền thông liên tiến trình trình duyệt `BroadcastChannel` kết hợp SignalR Hub, tự động đẩy dữ liệu giỏ hàng, hình ảnh chai mỹ phẩm, số tiền chiết khấu và mã VietQR động sang màn hình phụ quay về phía khách hàng (Customer Facing Display) với độ trễ dưới 50 mili-giây.

###### C. Bố cục Mật độ cao Giám sát Lô Hàng FEFO (_WarehouseLayout.cshtml)
Thiết kế tối ưu cho Nhân viên Thủ kho (`WarehouseStaff`) làm việc trong môi trường kho bãi nhiều bụi bặm và cần xử lý hàng ngàn mã sản phẩm:
- **Bảng dữ liệu mật độ siêu cao (High-Density Data Grid):** Loại bỏ hoàn toàn các khoảng trống trang trí thừa, tăng tối đa số lượng dòng hiển thị trên một trang hình ảnh giúp thủ kho có cái nhìn toàn cảnh về các vị trí kệ hàng mà không phải lật trang liên tục.
- **Hệ thống nhãn màu cảnh báo hạn dùng trực quan (Color-Coded Expiry Badges):** Ứng dụng bảng mã màu giao thông quy ước quốc tế để phân loại tức thì tình trạng hạn dùng của từng lô mỹ phẩm:
  + *Màu Đỏ Rực (`.fefo-expired` - Nền đỏ nhạt `#FEE2E2`, chữ đỏ đậm `#991B1B`):* Cảnh báo nguy cấp: Lô hàng đã hết hạn sử dụng hoặc còn hạn dưới 15 ngày, hệ thống tự động khóa chức năng xuất bán và viền đỏ cảnh báo thủ kho đưa vào khu vực cách ly tiêu hủy.
  + *Màu Cam Hổ Phách (`.fefo-near` - Nền cam `#FFEDD5`, chữ cam đậm `#9A3412`):* Cảnh báo cận date: Lô hàng còn hạn từ 16 đến 90 ngày, tự động gắn cờ đề xuất xuất kho ưu tiên theo FEFO hoặc đưa vào danh sách xả hàng thanh lý.
  + *Màu Xanh Ngọc Bích (`.fefo-safe` - Nền xanh nhạt `#ECFDF5`, chữ xanh sẫm `#065F46`):* Trạng thái an toàn: Lô hàng mới nhập, thời hạn sử dụng trên 180 ngày bảo đảm tiêu chuẩn phân phối.
- **Chế độ quét mã vạch liên tục (Continuous Barcode Scanning):** Tự động duy trì con trỏ chuột tại ô nhập liệu máy quét, hỗ trợ thủ kho cầm máy quét mã vạch không dây kiểm đếm hàng trăm thùng hàng mà không cần chạm tay vào bàn phím máy tính.

###### D. Bố cục Biên tập & Soát lỗi SEO Chia đôi màn hình (_CmsLayout.cshtml)
Dành riêng cho Biên tập viên nội dung (`ContentEditor`) sáng tạo các bài viết làm đẹp, hướng dẫn dưỡng da chuẩn y khoa và cấu hình cây menu điều hướng:
- **Giao diện chia đôi màn hình tương tác (Split-View Workspace):** Cửa sổ làm việc được phân đôi hoàn hảo với tỷ lệ 50:50. Phía bên trái là khung soạn thảo văn bản đa dạng định dạng (Rich Text Editor) tích hợp kéo thả hình ảnh từ kho Media; Phía bên phải là khung nhìn giả lập kết quả tìm kiếm thời gian thực (Real-time Google SERP Simulator & Social Share Card Preview).
- Khi biên tập viên gõ tiêu đề, mô tả hoặc thay đổi hình ảnh đại diện, khung nhìn bên phải sử dụng font chữ `Roboto` chuẩn của Google để mô phỏng chính xác mẫu kết quả tìm kiếm sẽ hiển thị trên trang tìm kiếm Google, đồng thời tính toán điểm số tối ưu SEO Onpage (độ dài tiêu đề từ 50-60 ký tự, thẻ mô tả Meta Description từ 150-160 ký tự) giúp nội dung đạt thứ hạng cao nhất trên công cụ tìm kiếm.

Dưới đây là bảng tổng hợp các thông số kỹ thuật và đặc trưng công thái học của 5 môi trường hiển thị trong giải pháp OMNI BEAUTY.

##### Bảng 2.36: Bảng tổng hợp thông số thiết kế công thái học của Storefront và 4 Layout quản trị chuyên biệt

| Môi trường hiển thị / Tệp Layout | Tác nhân sử dụng chính | Tỷ lệ khung nhìn & Cơ chế cuộn | Bộ font chữ chủ đạo | Bảng màu nhận diện công thái học | Tính năng đặc thù & Tối ưu hóa thao tác |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Storefront Khách hàng**<br>(`_Layout.cshtml`) | Khách hàng vãng lai, Thành viên mua sắm Online | Linh hoạt (Responsive), cuộn mượt tự nhiên, tối ưu di động (Mobile-First). | `Plus Jakarta Sans` (Nội dung), `Playfair Display` (Tiêu đề thương hiệu cao cấp). | Nền Pearl White (`#FFFDFB`), điểm nhấn Rose Gold (`#B76E79`), Deep Emerald (`#1B4D3E`). | Tích hợp widget Phòng thí nghiệm Da liễu bỏ túi tại giỏ hàng, chuyển cảnh mượt mà bằng View Transitions API, hỗ trợ giỏ hàng nổi cố định chân trang. |
| **Bố cục Quản trị Đa năng**<br>(`_AdminLayout.cshtml`) | `SuperAdmin`, `StoreManager` | Bố cục Fluid 100% độ rộng màn hình, cuộn độc lập giữa thanh bên và vùng nội dung. | `Plus Jakarta Sans` (Toàn bộ giao diện), `JetBrains Mono` (Mã chứng từ & Doanh thu). | Slate Dark (`#0F172A`), Trắng kem (`#FFFFFF`), Tím hiện đại (`#7C3AED`). | Thanh bên thu gọn thông minh, bộ lọc chi nhánh `StoreBadge`, trung tâm cảnh báo đẩy SignalR Realtime Hub và biểu đồ phân tích trực quan. |
| **Bố cục Kiosk Thu ngân POS**<br>(`_PosLayout.cshtml`) | `Cashier` (Thu ngân tại quầy trực tiếp) | Khung nhìn cố định toàn màn hình (`100vh`), **khóa hoàn toàn cuộn dọc**, giao diện cảm ứng. | `Plus Jakarta Sans` (Nút bấm cảm ứng lớn), `JetBrains Mono` (Mã vạch, Số tiền, Trả lại). | Nền xám nhạt (`#F1F5F9`), phím chức năng tím hồng (`#7C3AED` -> `#EC4899`), viền xám sáng (`#E2E8F0`). | Hệ thống phím tắt `F1-F12`, kiểm toán két mù chống gian lận tiền mặt, đồng bộ kênh truyền thông hai màn hình phụ CFD (`BroadcastChannel`). |
| **Bố cục Giám sát Kho FEFO**<br>(`_WarehouseLayout.cshtml`)| `WarehouseStaff` (Thủ kho điểm bán & Kho tổng) | Mật độ bảng dữ liệu siêu cao (Compact Data Grid), thanh cuộn ngang/dọc tùy biến siêu mảnh. | `Plus Jakarta Sans` (Tên sản phẩm), `JetBrains Mono` (Số lô, Hạn dùng, Số lượng tồn). | Nền xám công nghiệp (`#F8FAFC`), bảng nhãn giao thông (Đỏ hết hạn, Cam cận date, Xanh an toàn). | Luôn tự động giữ con trỏ tại ô quét mã vạch liên tục, in nhanh phiếu xuất kho điều chuyển nội bộ có mã QR định danh, bộ lọc nhanh theo số ngày cận date. |
| **Bố cục Soạn thảo & SEO CMS**<br>(`_CmsLayout.cshtml`) | `ContentEditor` (Biên tập viên truyền thông) | Chia đôi màn hình tỷ lệ 50:50 (Split-View Workspace), cuộn độc lập hai cửa sổ. | `Plus Jakarta Sans` (Soạn thảo), `Roboto` (Giả lập kết quả Google SERP). | Nền ghi sáng (`#F8FAFC`), thanh công cụ hồng cánh sen (`#DB2777`), khung viền xám mềm (`#CBD5E1`). | Giả lập giao diện kết quả tìm kiếm Google và thẻ chia sẻ Facebook theo thời gian thực, đo độ dài tiêu đề chuẩn SEO, trình quản lý tệp media kéo thả. |

---

### TIỂU KẾT CHƯƠNG 2

Chương 2 của khóa luận tốt nghiệp đã hoàn thành xuất sắc toàn bộ khối lượng nghiên cứu phân tích và thiết kế hệ thống cho giải pháp phần mềm Quản lý Bán hàng Chuỗi Cửa hàng Mỹ phẩm OMNI BEAUTY theo phương pháp luận hướng đối tượng (OOAD) kết hợp kiến trúc phần mềm doanh nghiệp hiện đại:

1. **Khảo sát hiện trạng và Xác lập bài toán:** Khóa luận đã bóc tách tường tận quy trình vận hành thực tế tại chuỗi bán lẻ mỹ phẩm, chỉ rõ 5 điểm nghẽn nghiêm trọng của mô hình bán hàng rời rạc truyền thống (độ trễ tồn kho O2O, rủi ro tiêu hủy mỹ phẩm cận date, thất thoát hàng hóa điều chuyển, gian lận két tiền mặt thu ngân và nguy cơ kích ứng da do tương kỵ hoạt chất). Trên cơ sở đó, quy trình vận hành mục tiêu (To-Be) đã được thiết lập chặt chẽ thông qua Sơ đồ luồng vận hành hàng hóa và tài chính khép kín.
2. **Xác định Yêu cầu và Phân rã chức năng:** Hệ thống đã định danh chính xác 11 tác nhân người dùng và dịch vụ nền, xây dựng Biểu đồ Phân rã Chức năng (BFD) bao quát 17 phân hệ nghiệp vụ, và thiết lập Ma trận 52 Điểm chạm thực tế ánh xạ chi tiết giữa màn hình giao diện, API Endpoint, Controller, Service và Bảng cơ sở dữ liệu.
3. **Mô hình hóa Hành vi bằng UML:** Khóa luận đã mô hình hóa toàn diện hệ thống Use Case tổng quát và 5 biểu đồ Use Case phân rã chuyên sâu. Tám nghiệp vụ cốt lõi mang tính sống còn của chuỗi mỹ phẩm đã được đặc tả chi tiết qua Bảng đặc tả 10 mục chuẩn mực, Biểu đồ Tuần tự (Sequence Diagram) chuẩn UML và các đoạn phân tích kỹ thuật tầng xử lý Backend sâu sắc.
4. **Thiết kế Kiến trúc và Cơ sở dữ liệu Chuẩn hóa 3NF:** Khóa luận đã hiện thực hóa kiến trúc Clean Architecture 4 tầng bảo đảm nguyên lý Đảo ngược Phụ thuộc (DIP), chi tiết hóa chuỗi đường ống dẫn HTTP Request Pipeline 9 bước thực tế trong `Program.cs`, và mô hình hóa thành công Biểu đồ Lớp thực thi BCE. Hệ thống Phân quyền Động PBAC 51 quyền nguyên tử kết hợp kỹ thuật cô lập dữ liệu chi nhánh (`StoreId Isolation`) đã giải quyết triệt để bài toán bảo mật đa điểm bán. Đặc biệt, Cơ sở dữ liệu `OmnichannelBeautyDb` quy mô 65 bảng dữ liệu được chứng minh đạt chuẩn 3NF tuyệt đối, đi kèm Từ điển dữ liệu chi tiết của 22 bảng thực thể cốt lõi và hệ thống 4 Razor Layouts quản trị chuyên biệt hóa cao về mặt công thái học.

Những kết quả phân tích và thiết kế vững chắc trong Chương 2 là kim chỉ nam và nền tảng kỹ thuật tiên quyết để bước sang **CHƯƠNG 3: CÀI ĐẶT, TRIỂN KHAI VÀ THỰC NGHIỆM HỆ THỐNG WEBSITE QUẢN LÝ BÁN HÀNG OMNI BEAUTY**.
