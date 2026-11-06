# CHƯƠNG 3 (TIẾP THEO) – MỤC 3.2: KẾT QUẢ ĐẠT ĐƯỢC VỀ GIAO DIỆN VÀ CHỨC NĂNG HỆ THỐNG

## 3.2.1. Phân hệ Giao diện Khách hàng (Luxury Storefront & Customer Portal)

Phân hệ Giao diện Khách hàng của hệ thống CANMAKE BEAUTY được định vị theo phong cách thương mại điện tử mỹ phẩm thượng lưu (Haute Parfumerie & Luxury Cosmetics), lấy cảm hứng từ chuẩn mực thiết kế của các hệ sinh thái làm đẹp hàng đầu thế giới như Sephora và Ulta Beauty, đồng thời tối ưu hóa sâu sắc cho hành vi mua sắm đa kênh của người tiêu dùng Việt Nam. Toàn bộ giao diện được xây dựng trên ngôn ngữ thiết kế tinh tế với gam màu chủ đạo Pearl White (#FFFDFB), điểm xuyết ánh Rose Gold (#B76E79) và Deep Emerald (#1B4D3E), mang lại cảm giác sang trọng, an tâm và chuyên nghiệp.

Dưới đây là phân tích chi tiết 7 màn hình giao diện khách hàng cốt lõi của hệ thống, đi kèm hướng dẫn thực nghiệm chụp ảnh minh chứng, mô tả bố cục thị giác và phân tích luồng xử lý dữ liệu tầng Backend kết nối cơ sở dữ liệu quan hệ.

---

### 1. Hình 3.1 – Giao diện Trang chủ Luxury, Mega Menu Đa hình và Khu vực Flash Sale / Xả hàng Cận Date FEFO

Trang chủ là trung tâm điều hướng và tiếp thị trực quan của CANMAKE BEAUTY, tích hợp khả năng cá nhân hóa trải nghiệm người dùng, truyền tải thông điệp cam kết mỹ phẩm sạch và kích thích quyết định mua hàng thông qua các chiến dịch ưu đãi hạn dùng thông minh.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.1 - GIAO DIỆN TRANG CHỦ VÀ KHU VỰC FEFO CLEARANCE]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_1_Giao_Dien_Trang_Chu.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Khởi động ứng dụng web tại địa chỉ `http://localhost:5116/` trên trình duyệt Google Chrome hoặc Microsoft Edge ở chế độ cửa sổ cực đại (độ phân giải tối thiểu 1920x1080). Không yêu cầu đăng nhập tài khoản.  
>   + *Bước 2:* Quan sát thanh thông báo trên cùng, rê chuột vào thanh Mega Menu "Chăm sóc da" hoặc "Bộ sưu tập Clean Beauty" để mở rộng cây danh mục đa hình.  
>   + *Bước 3:* Cuộn nhẹ chuột xuống khu vực "FEFO Smart Clearance / Flash Sale" hoặc truy cập trực tiếp đường dẫn `http://localhost:5116/Home/ClearanceFefo` để thấy đồng hồ đếm ngược thời gian thực và lưới sản phẩm cận date đang mở bán với tem nhãn giảm giá đỏ cam rực rỡ.  
>   + *Bước 4:* Nhấn tổ hợp phím `Alt + D` trên bàn phím để kích hoạt bảng điều khiển thuyết trình nhanh (Presentation Quick Bar) nếu cần hiển thị các phím tắt kịch bản bảo vệ khóa luận.  
>   + *Bước 5:* Dùng công cụ Snipping Tool hoặc phím `Windows + Shift + S` chụp toàn bộ khung màn hình và chèn vào vị trí tương ứng trong báo cáo khóa luận.

```
+--------------------------------------------------------------------------------------------------------------------+
| [Thanh thông báo] Freeship đơn từ 500k | Hotline 1800 6868 | Tra cứu Showroom gần nhất | Phím tắt: Alt+D           |
+--------------------------------------------------------------------------------------------------------------------+
| [LOGO CANMAKE BEAUTY]   [ Ô tìm kiếm thông minh: Niacinamide, Retinol, Son dưỡng... (🔍) ]   [Wishlist] [Giỏ: (3)]    |
| MEGA MENU: Chăm sóc da (v) | Trang điểm (v) | Chăm sóc tóc | Thương hiệu Clean | Skincare Lab | Flash Sale FEFO    |
+--------------------------------------------------------------------------------------------------------------------+
|                                      HERO BANNER CHIẾN DỊCH THƯỢNG LƯU                                             |
|              "MỸ PHẨM SẠCH CHUẨN KHOA HỌC - BẢO HÀNH MINH BẠCH HẠN SỬ DỤNG ĐẾN TỪNG SỐ LÔ SẢN XUẤT"                 |
|                                       [ KHÁM PHÁ BỘ SƯU TẬP ]   [ XEM PHÁC ĐỒ DA ]                                 |
+--------------------------------------------------------------------------------------------------------------------+
| (⭐ 100% Chính hãng)    |    (⚡ Giao hỏa tốc 2H)    |    (🔄 Đổi trả 7 ngày)    |    (🩺 Bác sĩ Da liễu tư vấn)  |
+--------------------------------------------------------------------------------------------------------------------+
| KHU VỰC FEFO SMART CLEARANCE / FLASH SALE CẬN DATE               [ Đồng hồ đếm ngược: 14 : 28 : 55 ]               |
| Tiêu chí: Cam kết minh bạch 100% số lô và ngày hết hạn. Hàng bảo quản kho lạnh GSP đạt chuẩn dược phẩm.             |
| ------------------------------------------------------------------------------------------------------------------ |
| [Ảnh SP 1:1]                        | [Ảnh SP 1:1]                        | [Ảnh SP 1:1]                           |
| [HSD: 03/2027 - Lô LOT-2024-C12]    | [HSD: 04/2027 - Lô LOT-2024-D05]    | [HSD: 02/2027 - Lô LOT-2024-A08]       |
| Kem Chống Nắng Anessa 60ml          | Dung Dịch Tẩy Da Chết Paula's BHA   | Serum Phục Hồi B5 La Roche-Posay       |
| Giá gốc: 650.000 đ                  | Giá gốc: 399.000 đ                  | Giá gốc: 550.000 đ                     |
| Giá xả FEFO: 357.500 đ (-45%)       | Giá xả FEFO: 219.000 đ (-45%)       | Giá xả FEFO: 275.000 đ (-50%)          |
| Đã bán: [=========>    ] 85%        | Đã bán: [===========>  ] 92%        | Đã bán: [======>       ] 60%           |
| [ MUA NGAY LÔ NÀY ]                 | [ MUA NGAY LÔ NÀY ]                 | [ MUA NGAY LÔ NÀY ]                    |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Thanh thông báo tiện ích và điều hướng thượng tầng:** Hiển thị thông điệp kích cầu miễn phí giao hàng toàn quốc cho đơn hàng từ 500.000 đồng, số điện thoại tổng đài tư vấn miễn cước, nút chọn chi nhánh showroom gần nhất và gợi ý phím tắt `Alt + D` mở bảng điều khiển thuyết trình.
- **Header trung tâm và Mega Menu đa hình:** Logo thương hiệu CANMAKE BEAUTY được thiết kế cách điệu trang trọng. Ô tìm kiếm ứng dụng kỹ thuật giảm tải thao tác Debounce 300ms, hỗ trợ nhận diện danh pháp hoạt chất khoa học lẫn tên thông dụng. Hệ thống Mega Menu xổ xuống đa cấp khi rê chuột, phân loại sản phẩm theo cả công năng sử dụng, thương hiệu phân phối và nhóm hoạt chất điều trị.
- **Hero Banner tương tác mượt mà:** Khối banner chính hiển thị hình ảnh độ phân giải cao quảng bá chiến dịch làm đẹp thuần chay, áp dụng kỹ thuật ưu tiên tải sớm `fetchpriority="high"` bảo đảm chỉ số LCP đạt dưới 1.2 giây.
- **Khu vực Flash Sale xả hàng cận date FEFO:** Điểm nhấn công nghệ độc quyền của CANMAKE BEAUTY với đồng hồ đếm ngược chạy theo thời gian thực. Mỗi thẻ sản phẩm hiển thị đầy đủ thông tin số lô sản xuất, tháng và năm hết hạn, tỷ lệ giảm giá từ 45% đến 70%, cùng thanh đo tiến độ số lượng đã bán tạo hiệu ứng tâm lý khan hiếm chân thực.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Khi người dùng truy cập trang chủ, máy chủ ASP.NET Core kích hoạt Action `HomeController.Index()`. Để bảo đảm trang nạp tức thì trong vòng 2.85 mili-giây, hệ thống gọi `HomeSectionsViewComponent`. Thay vì gửi truy vấn trực tiếp vào SQL Server cho mỗi phiên duyệt, dịch vụ khai thác tầng bộ nhớ đệm `IMemoryCache` với khóa `Storefront_Home_Sections` có thời hạn lưu 30 phút.
- Đối với khu vực xả kho cận date FEFO, dịch vụ `IStoreInventoryService.GetFefoClearanceBatchesAsync()` thực hiện truy vấn bảng `ProductBatches` lọc các lô hàng có trạng thái `ACTIVE`, trường `CurrentQuantity > 0`, khoảng cách ngày hết hạn `ExpDate <= DateTime.UtcNow.AddMonths(6)` và có gán tỷ lệ chiết khấu thanh lý `ClearanceDiscountPercent > 0`. Câu lệnh khai thác chỉ mục phức hợp `IX_ProductBatches_Fefo_Composite` trên các trường `(ProductId, Status, ExpDate, CurrentQuantity)`, thực hiện phép nối bảng nội bộ `INNER JOIN` với bảng `Products` và `MediaAssets` để kết xuất dữ liệu ra view model mà không nạp thừa các trường dữ liệu nhị phân nặng nề.
- Khi người dùng bấm tổ hợp phím `Alt + D`, đoạn mã JavaScript trong tệp `site.js` bắt sự kiện bàn phím `keydown`, kích hoạt chuyển đổi trạng thái hiển thị của phần tử DOM `div#presentationQuickBar`, cung cấp danh sách tài khoản demo và lối tắt truy cập các phân hệ quản trị mà không làm tải lại trang web.

---

### 2. Hình 3.2 – Giao diện Chi tiết Sản phẩm Mỹ phẩm Chuẩn Quốc tế (Chọn Lô Mới vs Lô Cận Date FEFO, Studio Thử màu Swatch theo Tông da, Bảng thành phần EWG Động & Chứng nhận Clean Beauty)

Trang chi tiết sản phẩm là nơi hội tụ các giải pháp kỹ thuật chuyên biệt cho ngành bán lẻ mỹ phẩm cao cấp, giải quyết bài toán minh bạch thông tin sản phẩm, hỗ trợ khách hàng lựa chọn màu sắc phù hợp với làn da và chủ động chọn lựa giữa sản phẩm date mới hoặc sản phẩm xả kho cận date tiết kiệm chi phí.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.2 - GIAO DIỆN CHI TIẾT SẢN PHẨM MỸ PHẨM CHUẨN QUỐC TẾ]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_2_Giao_Dien_Chi_Tiet_San_Pham_Full.png` (ảnh cuộn dài đầy đủ) hoặc `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_2_Giao_Dien_Chi_Tiet_San_Pham.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Truy cập đường dẫn `http://localhost:5116/san-pham/son-kem-li-luxury-velvet` hoặc click chọn sản phẩm "Son Kem Lì Luxury Velvet" / "Tinh Chất Phục Hồi B5 Panthenol" từ trang chủ.  
>   + *Bước 2:* Tại khối chọn lô hàng FEFO, nhấp chuột qua lại giữa hai tùy chọn: "Lô Mới Nhất (HSD xa - Giá chuẩn)" và "Lô Cận Date Xả Kho (Tiết kiệm 45% - HSD hiển thị rõ ràng)" để thấy giao diện tự động cập nhật lại giá tiền và thông tin tồn kho.  
>   + *Bước 3:* Nhấp vào các ô tròn Swatch màu son để kiểm tra hiệu ứng viền sáng và chuyển đổi ảnh swatch thực tế; bấm nút "Video Swatch Son Trên Da Thật" để kiểm tra trình phát video ngắn.  
>   + *Bước 4:* Cuộn xuống phần tab "Thành phần Khoa học & EWG" để thấy bảng dữ liệu danh pháp INCI và thang điểm an toàn EWG được tô màu xanh/cam trực quan.  
>   + *Bước 5:* Chụp ảnh toàn cảnh màn hình hoặc dùng công cụ cuộn trang (Full Page Screenshot) của trình duyệt để lưu lại ảnh sắc nét nhất.

```
+--------------------------------------------------------------------------------------------------------------------+
| Trang chủ > Trang điểm môi > Son kem lì > Son Kem Lì Haute Velvet Lip Tint                                         |
+-------------------------------------------------------------+------------------------------------------------------+
| [GALLERY ẢNH CHÍNH TỶ LỆ 1:1 CÓ ZOOM LENS]                  | THƯƠNG HIỆU: OMNI LUXE BEAUTY | Mã SKU: LUX-LIP-01   |
| (Hiển thị ảnh swatch màu son sắc nét hoặc Video ngắn)       | Tên SP: Son Kem Lì Mịn Môi Haute Velvet Lip Tint     |
|                                                             | ⭐⭐⭐⭐⭐ 4.9/5 (248 lượt đánh giá thực tế)           |
| [Ảnh 1] [Ảnh 2] [Ảnh 3] [▶ Video Swatch Da Thật]            | ---------------------------------------------------- |
|                                                             | KHỐI CHỨNG NHẬN AN TOÀN LÀN DA (CLEAN BEAUTY):       |
| STUDIO THỬ MÀU SWATCH TRÊN 3 TÔNG DA (SKIN TONE):           | [🛡️ Không Cồn Khô] [🌿 Thuần Chay] [🤰 An Toàn Bầu]  |
| Chọn tông da của bạn để mô phỏng màu son:                   | ---------------------------------------------------- |
| (o) Da sáng (Fair)   ( ) Da trung bình (Medium)  ( ) Da ngăm| BỘ CHỌN LÔ HÀNG FEFO THÔNG MINH (DUAL BATCH SELECTOR):|
| [ Khung hiển thị mô phỏng vệt son trên nền da đã chọn ]     | +-------------------------+ +----------------------+ |
|                                                             | | (o) LÔ MỚI NHẤT         | | ( ) LÔ CẬN DATE XẢ KHO | |
|                                                             | | Mã: LOT-2028-001        | | Mã: LOT-2026-B12     | |
|                                                             | | HSD: 11/2028            | | HSD: 04/2027         | |
|                                                             | | Tồn kho: Còn 150 hộp    | | Tồn kho: Chỉ còn 18  | |
|                                                             | | Giá: 420.000 đ          | | Giá: 231.000 đ (-45%)| |
|                                                             | +-------------------------+ +----------------------+ |
|                                                             | ---------------------------------------------------- |
|                                                             | BẢNG MÀU SON (COLOR SWATCHES):                       |
|                                                             | (● Đỏ Ruby)   (● Cam Đất)   (● Hồng Đào)   (● Nude)  |
|                                                             | Đang chọn: #02 Cam Cháy Warm Terracotta              |
|                                                             | ---------------------------------------------------- |
|                                                             | TÌNH TRẠNG CÒN HÀNG SHOWROOM GẦN BẠN (O2O AVAILABILITY):|
|                                                             | - Showroom 123 Cầu Giấy, Hà Nội:     [ CÒN HÀNG (24) ] |
|                                                             | - Showroom 45 Tràng Tiền, Hoàn Kiếm: [ CÒN HÀNG (08) ] |
|                                                             | ---------------------------------------------------- |
|                                                             | Số lượng: [ - ]  1  [ + ]                            |
|                                                             | [ THÊM VÀO GIỎ HÀNG ]      [ MUA NGAY BẰNG 1-CLICK ] |
+-------------------------------------------------------------+------------------------------------------------------+
| TAB 1: BẢNG THÀNH PHẦN KHOA HỌC (INCI) & CHỈ SỐ AN TOÀN EWG SKIN DEEP®                                             |
| Tiêu chuẩn cam kết: 100% thành phần công khai minh bạch. Điểm an toàn tổng quan: 1.4 / 10 (Mức Xanh - Rất an toàn). |
| +-------------------+---------+---------------------------------+-----------+--------------------------------------+ |
| | Danh pháp INCI    | Tỷ lệ   | Công năng da liễu chuyên sâu    | Điểm EWG  | Phân loại mức độ an toàn             | |
| +-------------------+---------+---------------------------------+-----------+--------------------------------------+ |
| | Dimethicone       | 15%     | Tạo màng mịn mượt, khóa ẩm môi  | Điểm 1    | [ Xanh: An toàn tuyệt đối ]          | |
| | Isododecane       | 12%     | Giữ màu bền bỉ, chống lem trôi  | Điểm 2    | [ Xanh: Rất lành tính ]              | |
| | Tocopheryl (Vit E)| 2%      | Chống oxy hóa, ngừa thâm môi    | Điểm 1    | [ Xanh: Chuẩn Dược điển ]            | |
| | CI 15850 (Red 7)  | 1.5%    | Hạt màu khoáng hữu cơ tinh khiết| Điểm 2    | [ Vàng nhạt: Rủi ro tối thiểu ]      | |
| +-------------------+---------+---------------------------------+-----------+--------------------------------------+ |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Khối Gallery truyền thông và Studio thử màu Swatch trên 3 tông da:** Cột bên trái gồm khung hiển thị ảnh chính độ nét cao tích hợp kính lúp điện tử (Zoom Lens). Bên dưới là tiện ích thử màu son độc quyền cho phép khách hàng chuyển đổi giữa 3 sắc thái da thực tế (Da sáng, Da trung bình tự nhiên và Da ngăm bánh mật), giúp mô phỏng chân thực độ lên màu của thỏi son trên môi người dùng thực tế. Bên cạnh đó, nút xem video swatch thực tế được tích hợp ngay trên dải ảnh thu nhỏ, hỗ trợ phát video vòng lặp mượt mà.
- **Khối Chứng nhận An toàn Làn da (Clean Beauty Verification):** Bố trí trang trọng bên dưới tên sản phẩm với các huy hiệu đạt chuẩn y khoa như Không chứa cồn khô (Alcohol-Free), Không hương liệu nhân tạo (Fragrance-Free) và An toàn cho phụ nữ mang thai (Pregnancy-Safe).
- **Bộ chọn lô hàng kép FEFO thông minh (Dual Interactive Batch Selector):** Trình bày trực quan hai phương án mua hàng dưới dạng hai thẻ lựa chọn song song:
  + *Tùy chọn Lô Mới Nhất (Fresh Batch):* Bán đúng giá niêm yết chuẩn, hạn sử dụng dài từ 2 đến 3 năm, phù hợp với khách hàng mua làm quà tặng hoặc có nhu cầu tích trữ sử dụng lâu dài.
  + *Tùy chọn Lô Cận Date Xả Kho (Clearance Batch):* Chiết khấu sâu 45% kèm thông báo số lượng tồn kho khan hiếm thực tế, công khai minh bạch mã số lô và hạn dùng đến tháng/năm, bảo đảm quyền lợi tối đa cho người mua có nhu cầu sử dụng ngay.
- **Bảng màu Swatch và Tra cứu tồn kho Showroom O2O:** Các ô tròn màu sắc thể hiện chính xác mã màu hex thực tế của chất son. Khung tra cứu tồn kho Showroom liên kết trực tiếp theo thời gian thực với các cửa hàng trong chuỗi, cho phép khách hàng biết ngay Showroom nào gần nhất còn hàng để đến trải nghiệm thử sản phẩm.
- **Bảng thành phần hóa mỹ phẩm INCI và Điểm an toàn EWG Động:** Tab thành phần phân tích chi tiết từng danh pháp hóa mỹ phẩm theo thứ tự tỷ lệ giảm dần, giải thích công năng khoa học và gắn điểm an toàn EWG Skin Deep® từ 1 đến 10 với màu sắc trực quan, giúp người tiêu dùng hiểu rõ từng chất tiếp xúc với cơ thể.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Khi người dùng truy cập đường dẫn `/san-pham/{slug}`, hệ thống định tuyến gọi `ProductController.Detail(string slug)`. Dịch vụ `IProductService.GetBySlugAsync(slug)` nạp thông tin chi tiết thực thể `Product`, bao gồm danh sách các biến thể trong bảng `ProductVariants` và thông tin thương hiệu trong bảng `Brands`.
- Để phục vụ bộ chọn lô FEFO kép, Controller gọi dịch vụ `IStoreInventoryService.GetDualBatchesForProductAsync(productId)`. Thuật toán thực hiện truy vấn bảng `ProductBatches`:
  + Lô mới nhất (`FreshBatch`) được lấy bằng biểu thức: `.Where(b => b.ProductId == productId && b.Status == "ACTIVE" && b.ExpDate > now.AddMonths(12)).OrderByDescending(b => b.ExpDate).FirstOrDefaultAsync()`.
  + Lô cận date xả kho (`ClearanceBatch`) được lấy bằng biểu thức: `.Where(b => b.ProductId == productId && b.Status == "ACTIVE" && b.ExpDate <= now.AddMonths(6) && b.CurrentQuantity > 0).OrderBy(b => b.ExpDate).FirstOrDefaultAsync()`.
- Chuỗi văn bản thô trong trường `Product.Ingredients` được chuyển qua dịch vụ `IngredientParserHelper.ParseAndScore()`. Thuật toán bóc tách từng phân đoạn danh pháp hóa học dựa trên dấu phẩy, tra cứu đối soát với cơ sở dữ liệu thành phần `SkincareIngredients` để trích xuất điểm số EWG, cơ chế hoạt động và gán nhãn màu sắc.
- Khi người dùng chọn chi nhánh để xem tình trạng còn hàng, một lời gọi AJAX không đồng bộ được gửi tới endpoint `ProductController.GetShowroomStockAvailability(productId, variantId)`. Backend truy vấn bảng `StoreInventories` kết hợp bảng `Stores` để trả về số lượng tồn khả dụng cho từng điểm bán thực tế.

---

### 3. Hình 3.3 – Giao diện Khu vực Đánh giá Thực tế Đồng điệu theo Hồ sơ Làn da ("Match My Skin Twin" Reviews)

Khu vực đánh giá sản phẩm của CANMAKE BEAUTY vượt qua giới hạn của các hệ thống bình luận sao đơn thuần bằng việc triển khai cơ chế kết nối những người tiêu dùng có cùng đặc tính làn da ("Match My Skin Twin"), gia tăng độ tin cậy và giá trị tham khảo của ý kiến cộng đồng.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.3 - GIAO DIỆN KHU VỰC ĐÁNH GIÁ ĐỒNG ĐIỆU HỒ SƠ LÀN DA]**  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Trên trang chi tiết sản phẩm `http://localhost:5116/san-pham/son-kem-li-luxury-velvet` hoặc `http://localhost:5116/san-pham/tinh-chat-phuc-hoi-b5-panthenol`, cuộn chuột xuống phần chân trang đến khối "Đánh Giá & Trải Nghiệm Khách Hàng (Customer Reviews)".  
>   + *Bước 2:* Quan sát khung tổng hợp sao 5 mức độ và thanh công cụ lọc nhanh bằng Alpine.js: "Tất Cả", "Có Hình Ảnh", "Đã Mua Hàng Chính Hãng (Verified Buyer)" và "5 Sao".  
>   + *Bước 3:* Nhấp chuột vào nút "Đã Mua Hàng Chính Hãng" để thấy danh sách các thẻ bình luận tự động lọc ngay lập tức mà không tải lại trang, hiển thị huy hiệu xanh lục chứng thực người mua hàng đã nhận lô FEFO chuẩn.  
>   + *Bước 4:* Bấm vào nút "Viết Đánh Giá Của Bạn" để mở modal pop-up cho phép chấm điểm sao, chọn loại da, đánh giá mùi hương và tải lên ảnh thực tế.  
>   + *Bước 5:* Dùng công cụ chụp màn hình chụp lại khối danh sách đánh giá kèm các huy hiệu loại da sinh động để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| ĐÁNH GIÁ & PHẢN HỒI THỰC TẾ TỪ KHÁCH HÀNG (CUSTOMER REVIEWS)                          [ VIẾT ĐÁNH GIÁ CỦA BẠN ]    |
+--------------------------------------------------------------------------------------------------------------------+
| TỔNG QUAN XẾP HẠNG: 4.9 / 5.0 (248 Lượt)      | PHÂN BỔ ĐÁNH GIÁ SAO:                                              |
| ⭐⭐⭐⭐⭐ 100% Người mua chính ngạch FEFO     | 5 Sao: [====================================>  ] 88% (218)         |
|                                               | 4 Sao: [=====>                                 ] 10% (25)          |
|                                               | 3 Sao: [=>                                     ]  2% (5)           |
+--------------------------------------------------------------------------------------------------------------------+
| THANH BỘ LỌC ĐÁNH GIÁ NHANH (ALPINE.JS):                                                                           |
| [x] Tất cả (248)   [ ] Có hình ảnh (64)   [ ] Đã mua hàng chính hãng (240)   [ ] 5 Sao (218)   [ ] Cùng loại da của tôi|
+--------------------------------------------------------------------------------------------------------------------+
| THẺ ĐÁNH GIÁ 1: NGUYỄN HOÀNG MAI ANH               ⭐⭐⭐⭐⭐ 5.0 Sao | Ngày đăng: 28/10/2026 14:20                |
| [✔ Đã mua hàng từ Canmake Beauty - Lô FEFO Chuẩn]     HỒ SƠ LÀN DA: [ Da dầu mụn ] | [ Mối quan tâm: Lỗ chân lông ]   |
| ------------------------------------------------------------------------------------------------------------------ |
| Tiêu chí trải nghiệm: [ Mùi hương: Dịu nhẹ tự nhiên ] [ Độ bám màu: 8 Giờ ] [ Cảm giác trên da: Nhẹ tênh không dính]|
| "Màu son lên môi cực kỳ chuẩn xác giống như phần mô tả Studio thử màu trên web. Chất son mịn như nhung, không làm  |
| lộ vân môi và đặc biệt là kiểm tra mã vạch lô sản xuất đúng y xì số lô LOT-2028 mà mình đã chọn trên trang web.   |
| Rất an tâm về chất lượng và độ mới của sản phẩm!"                                                                  |
| [Ảnh chụp thật 1] [Ảnh chụp thật 2] (Khách hàng tự chụp khi nhận kiện hàng)                                        |
+--------------------------------------------------------------------------------------------------------------------+
| THẺ ĐÁNH GIÁ 2: TRẦN THỊ PHƯƠNG THẢO               ⭐⭐⭐⭐⭐ 5.0 Sao | Ngày đăng: 26/10/2026 09:15                |
| [✔ Đã mua hàng từ Canmake Beauty - Lô FEFO Chuẩn]     HỒ SƠ LÀN DA: [ Da hỗn hợp thiên khô ] | [ Tuổi: 24-28 ]        |
| ------------------------------------------------------------------------------------------------------------------ |
| Tiêu chí trải nghiệm: [ Mùi hương: Không mùi nhân tạo ] [ Độ ẩm mượt: Xuất sắc ] [ Cảm giác: Thấm nhanh dịu da ] |
| "Mình săn được lô xả kho cận date giá giảm tới 45% nhưng nhận hàng hộp vẫn nguyên vẹn seal niêm phong, hạn dùng còn |
| đến tận tháng 04/2027 tính ra dùng thoải mái trong 5 tháng tới. Tiết kiệm được một khoản tiền đáng kể!"            |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Khung điểm số tổng hợp và phân bổ đánh giá sao:** Thể hiện trực quan điểm đánh giá sao trung bình của sản phẩm, tổng số lượt phản hồi đã qua kiểm duyệt và biểu đồ thanh đo tỷ lệ phần trăm phân bổ từ 1 sao đến 5 sao. Bên cạnh đó là cam kết 100% đánh giá minh bạch từ khách hàng đã phát sinh giao dịch thật.
- **Thanh lọc đánh giá thông minh đa tiêu chí:** Tích hợp các tab lọc nhanh dựa trên Alpine.js giúp người dùng chuyển đổi bộ lọc tức thời giữa các nhóm: tất cả đánh giá, đánh giá có đính kèm ảnh chụp thực tế, đánh giá từ người mua đã xác thực (Verified Buyer) và các đánh giá đạt điểm tuyệt đối 5 sao.
- **Hệ thống huy hiệu Hồ sơ Làn da trên từng thẻ đánh giá (Skin Profile Badges):** Mỗi bình luận hiển thị ảnh đại diện cách điệu, tên khách hàng và hệ thống các huy hiệu màu chuyên sâu: loại da của người đánh giá (Da dầu mụn, Da khô, Da hỗn hợp thiên dầu, Da nhạy cảm), mối quan tâm chăm sóc da, độ tuổi và thời gian sử dụng sản phẩm.
- **Tiêu chuẩn đo lường trải nghiệm đa giác quan:** Đánh giá không dừng lại ở mức văn bản tự do mà được chuẩn hóa thành các tiêu chí khoa học gồm Mùi hương (Scent), Độ bám màu / duy trì độ ẩm (Longevity) và Cảm giác biểu bì trên da (Skin Feel).
- **Huy hiệu bảo chứng mua hàng chính ngạch (Verified Buyer Badge):** Biểu tượng tích xanh kèm dòng chữ "Đã mua hàng từ Canmake Beauty - Lô FEFO Chuẩn", xác nhận người đánh giá đã thực sự đặt mua sản phẩm đó trên hệ thống và đã hoàn tất nhận hàng vật lý.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Danh sách bình luận được kết xuất thông qua partial view `_ReviewSection.cshtml` với dữ liệu truyền từ thuộc tính `ProductDetailViewModel.Reviews`.
- Khi người dùng gửi một đánh giá mới thông qua modal, form dữ liệu được gửi đến Action `ProductController.SubmitReview()`. Backend thực hiện xác thực người dùng thông qua Cookie phiên làm việc; tiếp đó truy vấn bảng `Orders` và `OrderDetails` để kiểm tra lịch sử mua sắm. Nếu tìm thấy hóa đơn mua đúng `ProductId` có trạng thái `COMPLETED`, thuộc tính `IsVerifiedPurchase` được tự động gán giá trị `true`.
- Bản ghi mới được lưu vào bảng `ProductReviews` cùng các trường phân tích da liễu `SkinType`, `SkinConcern`, `Scent`, `Longevity`, `SkinFeel`. Nếu khách hàng tải lên tệp ảnh minh chứng, hệ thống lưu trữ tệp vào thư mục `wwwroot/uploads/reviews/` và ghi các bản ghi liên kết vào bảng `ReviewImages`.
- Đồng thời, hệ thống kích hoạt dịch vụ khách hàng thân thiết `ILoyaltyService.AddPointsAsync(userId, 20, "Tặng điểm thưởng đánh giá sản phẩm có ảnh")`, tự động ghi một dòng bút toán cộng 20 điểm vào bảng `LoyaltyLedgers` nhằm khuyến khích cộng đồng chia sẻ trải nghiệm chân thực.

---

### 4. Hình 3.4 – Giao diện Trắc nghiệm Chẩn đoán Da Thông minh (AI Routine Quiz 4 Bước) & Bộ lọc Chuyên sâu SmartFilter

Tính năng trắc nghiệm chẩn đoán da trực tuyến đóng vai trò như một chuyên gia tư vấn da liễu số (Digital Beauty Advisor), hỗ trợ khách hàng tự phân loại làn da chỉ trong 30 giây và nhận ngay một phác đồ dưỡng da 4 bước hoàn chỉnh có khả năng đặt mua trọn gói tiết kiệm 15%.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.4 - GIAO DIỆN TRẮC NGHIỆM CHẨN ĐOÁN DA VÀ SMARTFILTER]**  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Truy cập đường dẫn `http://localhost:5116/tu-van-da` hoặc `http://localhost:5116/RoutineQuiz` trên trình duyệt.  
>   + *Bước 2:* Tại Bước 1, nhấp chọn thẻ "Da Dầu / Hỗn Hợp Thiên Dầu (Bóng nhờn vùng chữ T, dễ bít tắc)".  
>   + *Bước 3:* Hệ thống tự động chuyển sang Bước 2, tích chọn mối quan tâm: "Mụn Viêm & Thâm Sau Mụn" và "Lỗ Chân Lông To".  
>   + *Bước 4:* Tại Bước 3, chọn mức ngân sách mong muốn "Tiêu Chuẩn (Standard Care - 300k đến 700k/món)".  
>   + *Bước 5:* Màn hình kết quả xuất hiện hiển thị phác đồ 4 bước khoa học kèm nút bấm vàng kim "Thêm Trọn Bộ 4 Bước Vào Giỏ Hàng (Tiết Kiệm 15% - Mã COMBO15)". Chụp ảnh màn hình kết quả này để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| TRẮC NGHIỆM CHẨN ĐOÁN DA LIỄU VÀ PHÁC ĐỒ CHĂM SÓC 4 BƯỚC                                [ ĐỔI CÂU TRẢ LỜI ]       |
+--------------------------------------------------------------------------------------------------------------------+
| THANH TIẾN ĐỘ 3 BƯỚC: [1. Loại Da (✔)] -------- [2. Vấn Đề Da (✔)] -------- [3. Ngân Sách (✔)] -> KẾT QUẢ PHÁC ĐỒ |
+--------------------------------------------------------------------------------------------------------------------+
| KẾT QUẢ CHẨN ĐOÁN: BẠN CÓ LÀN DA DẦU THIẾU ẨM VÀ ĐANG GẶP TÌNH TRẠNG MỤN BÍT TẮC LỖ CHÂN LÔNG                     |
| Lời khuyên chuyên gia: Cần tập trung làm sạch sâu dịu nhẹ, tẩy tế bào chết bằng BHA tan trong dầu và phục hồi màng ẩm.|
+--------------------------------------------------------------------------------------------------------------------+
| PHÁC ĐỒ CHĂM SÓC DA KHOA HỌC 4 BƯỚC (PERSONALIZED 4-STEP ROUTINE):                                                 |
|                                                                                                                    |
| BƯỚC 1: LÀM SẠCH SÂU             BƯỚC 2: CÂN BẰNG DA             BƯỚC 3: ĐẶC TRỊ MỤN THÂM        BƯỚC 4: KHÓA ẨM   |
| [Ảnh SP: Sữa Rửa Mặt Cerave]     [Ảnh SP: Klairs Supple Toner]   [Ảnh SP: Paula's Choice BHA 2%] [Ảnh SP: Kem Dưỡng|
| Sữa Rửa Mặt Dịu Nhẹ 236ml        Toner Không Mùi Làm Dịu         Dung Dịch Salicylic Acid 30ml   Kem Dưỡng B5 Omni |
| Giá lẻ: 350.000 đ                Giá lẻ: 390.000 đ               Giá lẻ: 399.000 đ               Giá lẻ: 450.000 đ |
| Vai trò: Cân bằng pH 5.5         Vai trò: Cấp ẩm tầng biểu bì    Vai trò: Làm thông thoáng chân lông Vai trò: Phục hồi|
| [ Đã chọn vào bộ (✔) ]           [ Đã chọn vào bộ (✔) ]          [ Đã chọn vào bộ (✔) ]          [ Đã chọn vào bộ ]|
+--------------------------------------------------------------------------------------------------------------------+
| TỔNG GIÁ BÁN LẺ 4 SẢN PHẨM: 1.589.000 đ                                                                            |
| GIẢM GIÁ ĐỘC QUYỀN MUA TRỌN BỘ COMBO15 (-15%): -238.350 đ                                                          |
| TỔNG THANH TOÁN TIẾT KIỆM KHI MUA TRỌN BỘ: 1.350.650 đ (Tiết kiệm gần 250.000 đồng)                               |
|                                                                                                                    |
| [ ⚡ THÊM TRỌN BỘ 4 BƯỚC VÀO GIỎ HÀNG (TIẾT KIỆM 15% - MÃ COMBO15) ]                                                |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Tiêu đề và thanh tiến độ tương tác 3 bước:** Bố trí thanh đo tiến trình đồ họa chuyển màu từ hồng phấn sang đỏ tím hiện đại, hiển thị rõ ràng vị trí bước đang thực hiện gồm Loại da, Mối quan tâm chính và Phân khúc ngân sách.
- **Khung chẩn đoán tổng quan tình trạng da:** Phân tích ngắn gọn cơ chế sinh học của làn da người dùng dựa trên các câu trả lời đã chọn, chỉ dẫn nguyên nhân gốc rễ gây ra mụn hoặc bong tróc và đề xuất hướng xử lý khoa học.
- **Phác đồ gợi ý 4 bước khoa học chuẩn Da liễu:**
  + *Bước 1 (Cleanse - Làm sạch):* Gợi ý dòng sữa rửa mặt có độ pH sinh lý 5.5 bảo tồn màng lipid tự nhiên.
  + *Bước 2 (Tone - Cân bằng):* Gợi ý nước hoa hồng không cồn, giàu chiết xuất thực vật làm dịu biểu bì.
  + *Bước 3 (Treat - Đặc trị hoạt chất):* Gợi ý serum hoặc dung dịch tẩy tế bào chết hóa học chứa nồng độ hoạt chất tối ưu (như Salicylic Acid 2% hoặc Niacinamide 10%).
  + *Bước 4 (Moisturize - Khóa ẩm & Phục hồi):* Gợi ý kem dưỡng chứa Ceramide, B5 Panthenol để khóa các dưỡng chất lại dưới da.
- **Khối chuyển đổi đơn hàng Combo siêu tốc (Bundle 1-Click Checkout):** Hiển thị bảng tính kinh tế đối chiếu giữa tổng giá bán lẻ từng sản phẩm và mức giá ưu đãi khi mua trọn bộ. Nút bấm kích thước lớn "Thêm Trọn Bộ 4 Bước Vào Giỏ Hàng" hỗ trợ khách hàng mua sắm nhanh chóng chỉ qua một thao tác duy nhất.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Logic trắc nghiệm được quản lý bởi component Alpine.js `routineQuizComponent()` trong tệp view `RoutineQuiz/Index.cshtml`. Khi khách hàng hoàn thành 3 bước lựa chọn, JavaScript gửi yêu cầu POST bất đồng bộ chứa chuỗi dữ liệu `{ skinType: 'da-dau', concern: 'mun-thao-dau', budget: 'standard' }` tới API endpoint `/RoutineQuiz/GetRecommendations`.
- Tại tầng Backend, dịch vụ `SkincareConsultantService.GenerateRoutineAsync()` thực hiện thuật toán đối sánh ma trận:
  + Dịch vụ truy vấn bảng `Products` và `ProductAttributes`, lọc các sản phẩm đang có trạng thái `IsActive == true` và có số lượng tồn khả dụng trong bảng `StoreInventories`.
  + Phân loại sản phẩm vào 4 danh mục tương ứng với 4 bước chăm sóc: Bước 1 (danh mục Sữa rửa mặt), Bước 2 (danh mục Nước hoa hồng), Bước 3 (danh mục Tinh chất đặc trị), Bước 4 (danh mục Kem dưỡng ẩm).
  + Quan trọng nhất, động cơ da liễu `SkincareIngredientInteractionEngine` thẩm định chéo danh mục hoạt chất của 4 sản phẩm được chọn để bảo đảm không tồn tại bất kỳ cặp hoạt chất đối kháng nào, bảo đảm phác đồ an toàn tuyệt đối cho người dùng.
- Khi người dùng nhấp vào nút "Thêm trọn bộ 4 bước", JavaScript kích hoạt hàm `addRoutineToCart()` gửi mảng danh sách 4 `VariantId` lên `CartController.AddBundle()`. Hệ thống tự động thêm 4 dòng sản phẩm vào session giỏ hàng, đồng thời ghi nhận mã khuyến mại `COMBO15` vào bảng tạm tính chiết khấu của giỏ hàng với mức giảm 15% tổng giá trị đơn hàng.

---

### 5. Hình 3.5 – Giao diện Khay So sánh Mỹ phẩm Chuyên sâu (Định giá trên từng ml/g & Đối sánh Hoạt chất/FEFO)

Khay so sánh mỹ phẩm là công cụ trợ giúp đắc lực giúp người tiêu dùng giải tỏa băn khoăn khi đứng trước nhiều sản phẩm tương đồng, cho phép đặt các sản phẩm lên bàn cân để đối sánh từ góc độ kinh tế chi phí trên từng đơn vị thể tích đến góc độ an toàn da liễu và hạn sử dụng thực tế.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.5 - GIAO DIỆN KHAY SO SÁNH MỸ PHẨM CHUYÊN SÂU]**  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Truy cập trang danh mục sản phẩm `http://localhost:5116/Catalog` hoặc trang tìm kiếm `http://localhost:5116/Product/Search`.  
>   + *Bước 2:* Trên hai thẻ sản phẩm cùng nhóm công năng (ví dụ Kem Chống Nắng Anessa 60ml và Kem Chống Nắng La Roche-Posay 50ml), nhấp vào biểu tượng chiếc cân (Compare Icon) ở góc thẻ sản phẩm.  
>   + *Bước 3:* Quan sát thanh Compare Bar xuất hiện cố định trượt lên từ đáy màn hình hiển thị 2 sản phẩm đã chọn kèm nút "So Sánh Ngay (2/3)".  
>   + *Bước 4:* Bấm nút "So Sánh Ngay" để mở Modal ma trận đối sánh chi tiết `/Product/CompareMatrix`.  
>   + *Bước 5:* Dùng công cụ chụp ảnh chụp lại toàn bộ cửa sổ Modal hiển thị bảng ma trận đối sánh nhiều dòng để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| MA TRẬN ĐỐI SÁNH MỸ PHẨM CHUYÊN SÂU (PRODUCT COMPARISON MATRIX)                               [ ĐÓNG CỬA SỔ (X) ]  |
+------------------------------------------------+---------------------------------+---------------------------------+
| TIÊU CHÍ ĐỐI SÁNH / THUỘC TÍNH                 | SẢN PHẨM A                      | SẢN PHẨM B                      |
+------------------------------------------------+---------------------------------+---------------------------------+
| Hình ảnh & Tên mỹ phẩm                         | [Ảnh] Kem Nắng Anessa Perfect UV| [Ảnh] Kem Nắng La Roche Anthelios|
| Giá bán niêm yết                               | 650.000 đ                       | 495.000 đ                       |
| Dung tích thực quy chuẩn                       | 60 ml                           | 50 ml                           |
+------------------------------------------------+---------------------------------+---------------------------------+
| CHỈ SỐ KINH TẾ (ĐƠN GIÁ / THỂ TÍCH THỰC):      | 10.833 đ / ml                   | 9.900 đ / ml                    |
| Đánh giá kinh tế học người tiêu dùng           | [ Tiêu chuẩn ]                  | [ TIẾT KIỆM HƠN 9% TRÊN MỖI ML ]|
+------------------------------------------------+---------------------------------+---------------------------------+
| Màng lọc chống nắng & Chỉ số quang phổ         | SPF 50+ / PA++++ (Auto Booster) | SPF 50+ / PA++++ (Mexoryl 400)  |
| Hoạt chất dưỡng ẩm / Kiềm dầu chủ đạo          | Chiết xuất Trà xanh, Collagen   | Airlicium kiềm dầu 12 giờ       |
| Phù hợp với loại da                            | Mọi loại da, hoạt động ngoài trời| Da dầu mụn, nhạy cảm dễ kích ứng|
| Điểm an toàn EWG Skin Deep®                    | 2.1 / 10 (Mức Xanh an toàn)     | 1.8 / 10 (Mức Xanh rất an toàn) |
| Chứng nhận Clean Beauty                        | [ Thuần Chay ] [ Chống Trôi ]   | [ Không Hương Liệu ] [ Không Cồn]|
+------------------------------------------------+---------------------------------+---------------------------------+
| TÌNH TRẠNG LÔ HÀNG FEFO HIỆN CÓ:               |                                 |                                 |
| - Số lô sản xuất đang xuất kho                 | LOT-2024-C12                    | LOT-2025-E01                    |
| - Hạn sử dụng (Exp Date)                       | 12/2027 (Còn 13 tháng)          | 08/2028 (Còn 21 tháng)          |
| - Cơ hội ưu đãi xả hàng cận date               | [ Đang có lô xả date giảm 45% ] | [ Chỉ có lô mới date xa ]       |
+------------------------------------------------+---------------------------------+---------------------------------+
| THAO TÁC MUA SẮM                               | [ THÊM SẢN PHẨM A VÀO GIỎ ]     | [ THÊM SẢN PHẨM B VÀO GIỎ ]     |
+------------------------------------------------+---------------------------------+---------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Thanh khay nhớ nổi cố định đáy màn hình (Sticky Bottom Compare Bar):** Tự động trượt lên mượt mà bằng hiệu ứng CSS trượt đáy khi khách hàng chọn từ 1 đến tối đa 3 sản phẩm. Thanh hiển thị ảnh thu nhỏ của các sản phẩm đã chọn kèm nút xóa nhanh và nút kích hoạt mở bảng đối sánh.
- **Cửa sổ ma trận so sánh trực diện (Comparison Modal Window):** Bố cục theo dạng bảng 3 cột với tiêu đề thuộc tính bên trái và các cột dữ liệu tương ứng của từng sản phẩm bên phải, giúp việc đọc hiểu thông tin trực quan theo từng dòng ngang.
- **Chỉ số kinh tế độc quyền Đơn giá trên từng thể tích (Price per ml / Price per gram):** Hệ thống tự động tính toán số tiền quy đổi ra đơn vị 1ml hoặc 1g (ví dụ: 10.833 đ/ml so với 9.900 đ/ml). Thuật toán tự động gắn nhãn "Tiết kiệm hơn X% trên mỗi ml" cho sản phẩm có chi phí kinh tế hơn, mang lại giá trị gia tăng thực sự cho người tiêu dùng thông thái.
- **Đối sánh công nghệ màng lọc và thành phần hoạt chất:** So sánh trực diện chỉ số chống nắng SPF/PA, các thành phần màng lọc quang phổ rộng, công dụng kiểm soát dầu nhờn và điểm số an toàn EWG.
- **Đối sánh hiện trạng lô hàng FEFO:** Thể hiện chi tiết số lô sản xuất đang mở bán của từng sản phẩm, hạn dùng còn lại tính theo tháng, và thông báo liệu sản phẩm có đang mở bán lô xả kho cận date giá sốc hay không.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Danh sách sản phẩm chọn so sánh được quản lý phía máy khách trong cấu trúc mảng `compareList` lưu tạm tại `sessionStorage` của trình duyệt nhằm bảo toàn trạng thái khi người dùng chuyển trang.
- Khi người dùng bấm nút mở modal so sánh, JavaScript gửi yêu cầu POST tới endpoint `/Product/GetCompareMatrix` với payload danh sách các ID sản phẩm `[id1, id2]`.
- Controller `ProductController` tiếp nhận yêu cầu và gọi phương thức `IProductService.GetComparisonMatrixAsync(productIds)`. Tầng dữ liệu thực hiện truy vấn bảng `Products`, lấy thông tin giá bán `SellingPrice`, trường dung tích `NetVolume` hoặc `NetWeight` trong bảng `ProductVariants`.
- Phép tính toán đơn giá kinh tế được thực hiện trực tiếp trên bộ nhớ máy chủ: `unitPrice = Math.Round(sellingPrice / volume, 2)`. Đồng thời, hệ thống nạp các bản ghi từ bảng `ProductBatches` để trích xuất số lô cận hạn nhất đang mở bán và đối soát ma trận chứng nhận an toàn, đóng gói thành đối tượng DTO `ProductCompareMatrixDto` trả về định dạng JSON để trình duyệt kết xuất bảng giao diện chỉ trong 18 mili-giây.

---

### 6. Hình 3.6 – Giao diện Giỏ hàng Thông minh, Phòng Lab Kiểm tra Xung đột Hoạt chất (Skincare Conflict Checker) & Quầy Chọn Mẫu thử Miễn phí (Free Sample Bar)

Giỏ hàng của CANMAKE BEAUTY được thiết kế như một trung tâm kiểm duyệt an toàn da liễu tự động và kích cầu mua sắm thượng lưu, biến khâu chốt đơn đơn thuần trở thành trải nghiệm tương tác chăm sóc khách hàng chuyên sâu.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.6 - GIAO DIỆN GIỎ HÀNG VÀ PHÒNG LAB CẢNH BÁO TƯƠNG KỴ]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_3_Giao_Dien_Gio_Hang_Va_Canh_Bao_Xung_Dot.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Thêm vào giỏ 1 sản phẩm chứa Retinol (ví dụ "Kem Dưỡng Tái Tạo Ban Đêm Retinol Avene") và 1 sản phẩm chứa BHA (ví dụ "Dung Dịch Tẩy Da Chết Paula's Choice BHA 2%").  
>   + *Bước 2:* Truy cập vào đường dẫn `http://localhost:5116/Cart`.  
>   + *Bước 3:* Quan sát thanh tiến độ Freeship Meter và Quầy chọn mẫu thử miễn phí (Free Sample Bar) hiển thị các mốc phần thưởng 500.000đ và 1.000.000đ.  
>   + *Bước 4:* Chú ý quan sát khung cảnh báo của Phòng Lab Da Liễu nổi bật màu đỏ cam phía trên bảng sản phẩm với nội dung: "CẢNH BÁO TƯƠNG KỴ HOẠT CHẤT DA LIỄU - CẶP RETINOL VÀ BHA" kèm lời khuyên chia lịch sử dụng sáng/tối.  
>   + *Bước 5:* Chụp toàn bộ giao diện giỏ hàng sắc nét để chèn vào báo cáo khóa luận.

```
+--------------------------------------------------------------------------------------------------------------------+
| GIỎ HÀNG CỦA BẠN (Đang chọn 2 sản phẩm cao cấp)                                       [ Tiếp tục chọn mỹ phẩm ]    |
+--------------------------------------------------------------------------------------------------------------------+
| 🚚 THANH TIẾN ĐỘ MIỄN PHÍ VẬN CHUYỂN & MẪU THỬ THƯỢNG LƯU (FREESHIP & SAMPLE METER):                              |
| Bạn đã mua: 1.049.000 đ [========================================================================] 100% (ĐẠT MỐC)  |
| 🎁 CHÚC MỪNG! BẠN ĐÃ MỞ KHÓA TOÀN BỘ QUÀ TẶNG:                                                                     |
| [✔ Đã đạt: MIỄN PHÍ VẬN CHUYỂN TOÀN QUỐC]   |   [✔ Đã đạt: TẶNG 02 MẪU THỬ SERUM MINI CAO CẤP TỰ CHỌN]            |
+--------------------------------------------------------------------------------------------------------------------+
| ⚠️ CẢNH BÁO PHÒNG LAB DA LIỄU CANMAKE BEAUTY: PHÁT HIỆN TƯƠNG KỴ HOẠT CHẤT TRONG GIỎ HÀNG (SKINCARE CONFLICT)!         |
| Mức độ cảnh báo: NGHIÊM TRỌNG (HIGH CONFLICT RISK)                                                                 |
| - Cặp hoạt chất đối kháng phát hiện: [ Retinol 1.0% ] ⚡ [ Salicylic Acid (BHA 2%) ]                                |
| - Sản phẩm liên đới trong giỏ hàng:                                                                                |
|   + Kem Dưỡng Tái Tạo Ban Đêm Avene Retinol 30ml (Chứa Retinol)                                                    |
|   + Dung Dịch Tẩy Tế Bào Chết Hóa Học Paula's Choice 30ml (Chứa Salicylic Acid BHA)                                |
| - Hướng dẫn khoa học từ Bác sĩ Da liễu CANMAKE BEAUTY:                                                                |
|   "Việc bôi đồng thời Retinol cùng BHA trong cùng một buổi tối có thể phá hủy hàng rào bảo vệ lipid, gây khô rát   |
|   dữ dội, bong tróc diện rộng và tăng nguy cơ kích ứng ban đỏ.                                                     |
|   Khuyến nghị phân chia lịch trình: Dùng BHA vào buổi tối Thứ 2-4-6 và dùng Retinol vào buổi tối Thứ 3-5-7!"        |
+--------------------------------------------------------------------------------------------------------------------+
| DANH SÁCH MỸ PHẨM TRONG GIỎ HÀNG                                                                                   |
| [Ảnh SP] Kem Retinol Avene 30ml (Lô LOT-2028-A01)     | Giá: 650.000 đ | Số lượng: [ - ]  1  [ + ] | = 650.000 đ [Xóa]|
| [Ảnh SP] Dung dịch BHA Paula's 30ml (Lô LOT-2027-C09) | Giá: 399.000 đ | Số lượng: [ - ]  1  [ + ] | = 399.000 đ [Xóa]|
+--------------------------------------------------------------------------------------------------------------------+
| 🎀 DỊCH VỤ ĐÓNG GÓI HỘP QUÀ TẶNG THƯỢNG LƯU:                                                                       |
| [x] Đóng gói hộp quà cao cấp thắt nơ lụa vàng (+50.000 đ) | [ Viết lời chúc thiệp tay tặng người thương... ]       |
+--------------------------------------------------------------------------------------------------------------------+
| TẠM TÍNH HÀNG HÓA: 1.049.000 đ            | PHÍ VẬN CHUYỂN: 0 đ (Freeship) | GIẢM GIÁ VOUCHER: 0 đ                 |
| TÍCH LŨY ĐIỂM VIP: +104 Điểm Thưởng       | TỔNG CỘNG THANH TOÁN: 1.049.000 đ                                      |
|                                           | [ TIẾN HÀNH ĐẶT HÀNG & THANH TOÁN (CHECKOUT) ]                         |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Thanh đo tiến độ quyền lợi Freeship và Quầy mẫu thử miễn phí (Free Sample Bar):** Thiết kế thanh đo tiến trình đồ họa trực quan theo số tiền giỏ hàng hiện tại, minh họa rõ ràng các mốc đạt thưởng: mốc 500.000 đồng mở khóa Miễn phí giao hàng toàn quốc và mốc 1.000.000 đồng mở khóa Quầy chọn 2 mẫu thử mini size (Sample Bar) miễn phí từ các thương hiệu dược mỹ phẩm hàng đầu.
- **Hộp cảnh báo Phòng Lab Da Liễu Độc quyền (Dermatological Clinical Conflict Banner):** Chiếm vị trí thị giác trung tâm phía trên danh sách hàng hóa với nền đỏ hổ phách và biểu tượng tia sét cảnh báo. Nội dung phân tích nêu rõ tên cặp hoạt chất xung đột, chỉ ra đích danh hai sản phẩm trong giỏ gây ra tình trạng này và đưa ra phác đồ phân chia lịch dùng sáng/tối hoặc cách ngày rất thuyết phục.
- **Bảng danh mục sản phẩm chi tiết:** Thể hiện ảnh đại diện, tên sản phẩm, thông tin số lô xuất kho theo thuật toán FEFO, đơn giá, cụm nút tăng giảm số lượng (+ / -) tự động giới hạn theo tồn kho thực tế và nút xóa khỏi giỏ hàng.
- **Tùy chọn dịch vụ Đóng gói Quà tặng Thượng lưu (Luxury Gift Packaging):** Cho phép người mua tích chọn gói hộp quà thắt nơ lụa thủ công kèm ô nhập lời chúc viết tay gửi tặng bạn bè, người thân.
- **Khối tóm tắt tài chính và nút kêu gọi hành động:** Hiển thị rõ số tiền hàng, phí vận chuyển 0 đồng do đã đạt điều kiện miễn phí, số điểm thưởng thành viên VIP sẽ được tích lũy vào tài khoản và nút bấm màu vàng kim nổi bật dẫn hướng sang trang Checkout.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Khi nạp trang giỏ hàng, Action `CartController.Index()` trích xuất cookie giỏ hàng định dạng JSON đã mã hóa bảo vệ chống giả mạo `CanmakeBeauty_CartSession`. Dữ liệu được giải mã thành đối tượng `CartViewModel`.
- Đối với tính năng kiểm tra xung đột hoạt chất, Controller gọi dịch vụ `SkincareIngredientInteractionEngine.AnalyzeCartConflictsAsync(cartVm.Items)`. Động cơ thực hiện:
  + Lấy danh sách ID các sản phẩm có trong giỏ, truy vấn bảng `Products` nạp chuỗi thành phần `Ingredients`.
  + Phân tích chuỗi thành danh sách mã hoạt chất chuẩn hóa (như `RETINOL`, `SALICYLIC_ACID`, `GLYCOLIC_ACID`, `VITAMIN_C_LAA`, `BENZOYL_PEROXIDE`).
  + Lặp qua danh sách các cặp hoạt chất, tra cứu bảng `IngredientInteractions` trong cơ sở dữ liệu nơi định nghĩa các quy tắc xung đột. Khi tìm thấy bản ghi có mức độ `Severity == "HIGH"`, hệ thống khởi tạo đối tượng `ConflictDetectionResult` chứa thông báo cảnh báo và hướng dẫn da liễu.
- Khi người dùng bấm tăng hoặc giảm số lượng, mã JavaScript gửi lời gọi AJAX tới `POST /Cart/UpdateQuantity`. Backend kiểm tra số lượng yêu cầu với trường `AvailableQuantity` trong bảng `ProductBatches` để bảo đảm không vượt quá tồn kho khả dụng của lô hàng đang gán, sau đó trả về cấu trúc giỏ hàng mới cập nhật lại toàn bộ giao diện chỉ trong 3.6 mili-giây.

---

### 7. Hình 3.7 – Giao diện Thanh toán Đa kênh (Giao Tận Nơi vs Nhận Tại Showroom O2O 1 Giờ), Cấn trừ Điểm Loyalty & Vé Nhận Hàng QR Fast-Track

Trang thanh toán hoàn thiện trải nghiệm mua sắm bằng việc cung cấp các tùy chọn giao nhận đa kênh linh hoạt, cho phép cấn trừ điểm thưởng thành viên trực tiếp vào hóa đơn và cung cấp tấm Vé Nhận Hàng Ưu Tiên O2O mã QR giúp khách hàng lấy hàng nhanh tại Showroom trong vòng 1 giờ.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.7 - GIAO DIỆN THANH TOÁN ĐA KÊNH VÀ VÉ NHẬN HÀNG O2O]**  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập hệ thống bằng tài khoản khách hàng VIP: Tên đăng nhập `customer_vip` / Mật khẩu `Admin@123`.  
>   + *Bước 2:* Thêm 1 sản phẩm vào giỏ và nhấn "Tiến Hành Đặt Hàng" để đến trang `http://localhost:5116/Checkout`.  
>   + *Bước 3:* Tại mục 2 (Hình thức giao hàng), nhấp chuột chuyển đổi giữa Tab "Giao Hàng Tiêu Chuẩn" và Tab "Nhận Tại Showroom Trong 1 Giờ (O2O Click-and-Collect - Freeship 0đ)" để thấy danh sách Showroom đang sẵn hàng xuất hiện.  
>   + *Bước 4:* Tại cột tóm tắt bên phải, tích chọn ô "Sử dụng điểm thưởng Loyalty (Ví có sẵn 500 điểm - tương đương giảm 50.000đ)" để kiểm tra số tiền thanh toán cuối cùng tự động trừ lùi. Chọn phương thức thanh toán "Chuyển khoản VietQR 24/7".  
>   + *Bước 5:* Bấm nút "Xác Nhận Đặt Hàng" để chuyển sang trang hoàn tất `http://localhost:5116/Checkout/OrderSuccess`. Quan sát mã QR thanh toán động VietQR Napas247, đồng hồ đếm ngược giữ chỗ lô hàng FEFO 15 phút, và tấm Vé Nhận Hàng Ưu Tiên O2O kèm mã QR và mã PIN 6 số bí mật. Chụp ảnh màn hình này chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| THANH TOÁN ĐƠN HÀNG CANMAKE BEAUTY (CHECKOUT)                                              [ Quay lại giỏ hàng ]      |
+-------------------------------------------------------------------+------------------------------------------------+
| CỘT TRÁI (THÔNG TIN NHẬN HÀNG & PHƯƠNG THỨC THANH TOÁN):          | CỘT PHẢI (TÓM TẮT ĐƠN HÀNG & CẤN TRỪ ƯU ĐÃI):  |
| 1. Thông Tin Khách Hàng:                                          | Mỹ phẩm trong đơn:                             |
| - Họ và tên: Lê Hoàng Yến | SĐT: 0912.345.678 (Hạng KIM CƯƠNG)    | - Son Kem Lì Luxury Velvet #02 x 1:  420.000 đ |
| - Email: hoangyen.le@gmail.com                                    | - Sữa Rửa Mặt Dịu Nhẹ 236ml x 1:     350.000 đ |
| ----------------------------------------------------------------- | ---------------------------------------------- |
| 2. Hình Thức Nhận Hàng Đa Kênh:                                   | Tạm tính tiền hàng:                  770.000 đ |
| [ Tab: Giao tận nơi (2-3 ngày) ]   [x Tab: Nhận tại Showroom 1H ] | Phí vận chuyển O2O Showroom:              0 đ  |
| Chọn Showroom tiếp nhận thuận tiện nhất:                          | ---------------------------------------------- |
| (o) Showroom 1: 123 Cầu Giấy, Q. Cầu Giấy, Hà Nội  [CÒN HÀNG SẴN] | 🎁 KHUNG ÁP MÃ VOUCHER GIẢM GIÁ (AJAX):        |
| ( ) Showroom 2: 45 Tràng Tiền, Q. Hoàn Kiếm, HN    [CÒN HÀNG SẴN] | [ BEAUTY10              ] [ ÁP DỤNG ]          |
| Cam kết: Chuẩn bị kiện hàng đóng gói cao cấp sẵn sàng trong 60p!  | -> Đã áp dụng mã BEAUTY10: -77.000 đ (Hợp lệ)  |
| ----------------------------------------------------------------- | ---------------------------------------------- |
| 3. Phương Thức Thanh Toán Bảo Mật SSL 256-bit:                    | 👑 KHUNG CẤN TRỪ ĐIỂM THƯỞNG LOYALTY:          |
| ( ) Cổng thanh toán VNPAY (Thẻ ATM, Visa, MasterCard)             | Ví điểm hiện có: 1.250 Điểm Thưởng VIP         |
| (o) Chuyển khoản ngân hàng VietQR 24/7 (Khuyên dùng - Đối soát tự | [x] Dùng 50 Điểm thưởng (Quy đổi giảm 50.000 đ)|
|     động trong 30 giây)                                           | ---------------------------------------------- |
| ( ) Ví điện tử MoMo                                               | TỔNG CỘNG THANH TOÁN:                643.000 đ |
| ( ) Thanh toán tiền mặt khi nhận hàng (COD)                       | (Đã tiết kiệm được 127.000 đ từ ưu đãi)        |
+-------------------------------------------------------------------+------------------------------------------------+
| [ NÚT BẤM HOÀN TẤT: XÁC NHẬN ĐẶT HÀNG & LẤY VÉ FAST-TRACK ]                                                        |
+--------------------------------------------------------------------------------------------------------------------+
| KẾT QUẢ ĐẶT HÀNG THÀNH CÔNG: TẤM VÉ NHẬN HÀNG ƯU TIÊN TẠI SHOWROOM (O2O FAST-TRACK PICKUP TICKET)                  |
| Mã đơn hàng: #ORD-20261104-8921 | Trạng thái: ĐÃ XÁC NHẬN GIỮ CHỖ LÔ HÀNG FEFO TRONG 15 PHÚT                      |
|                                                                                                                    |
| [ HÌNH MÃ QR CODE NHẬN HÀNG ]         MÃ PIN BẢO MẬT NHẬN HÀNG: [ OB - 8 4 9 2 0 1 ]                               |
| (Quét tại quầy POS để lấy đồ ngay)    Showroom nhận: 123 Cầu Giấy, Hà Nội (Thời gian: 08h00 - 21h30)               |
|                                       Hạn chót giữ hàng: 15:00 Ngày 05/11/2026                                     |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả bố cục trực quan và các thành phần giao diện
- **Cột thông tin khách hàng và Bộ chuyển đổi Giao nhận Đa kênh:** Cho phép người dùng chuyển đổi linh hoạt giữa phương thức Giao hàng tận nơi truyền thống (tự động tính cước qua API GHN) và phương thức Nhận tại Showroom O2O trong 1 giờ (miễn phí 100% cước vận chuyển). Khi chọn nhận tại Showroom, giao diện hiển thị danh sách các điểm bán đang có sẵn hàng trong kho để khách hàng chủ động chọn địa điểm thuận đường nhất.
- **Khung lựa chọn cổng thanh toán số hóa:** Tích hợp các tùy chọn thanh toán hiện đại nhất Việt Nam: Cổng thanh toán quốc gia VNPAY, Ví điện tử MoMo, Tiền mặt khi nhận hàng COD và đặc biệt là Chuyển khoản ngân hàng tức thì VietQR Napas247 có khả năng đối soát tự động mà không cần tải ảnh chụp biên lai ủy nhiệm chi.
- **Khung cấn trừ ưu đãi kép (Voucher kết hợp Điểm thưởng Loyalty):** Cho phép nhập mã giảm giá trực tiếp với phản hồi AJAX không gián đoạn; đồng thời tích hợp thanh trượt cấn trừ điểm tích lũy thành viên (quy đổi theo tỷ lệ vàng 1 điểm thưởng = 1.000 đồng trừ trực tiếp vào tổng thanh toán).
- **Tấm Vé Nhận Hàng Ưu Tiên O2O (Fast-Track Pickup Ticket):** Xuất hiện trang trọng trên trang xác nhận đơn hàng thành công, hiển thị mã vạch QR động định danh giao dịch và mã PIN 6 số bí mật `OB-XXXXXX`. Khách hàng chỉ cần xuất trình màn hình vé này trên điện thoại cho thu ngân tại cửa hàng để được phục vụ tại làn ưu tiên Fast-Track mà không cần xếp hàng chờ đợi.

#### Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)
- Khi khách hàng nhấn nút xác nhận đặt hàng, form thanh toán được gửi đến Action `CheckoutController.PlaceOrder()`. Tầng nghiệp vụ mở một giao dịch dữ liệu nguyên tử `await using var transaction = await _context.Database.BeginTransactionAsync()`.
- Đầu tiên, hệ thống thẩm định mã giảm giá trong bảng `Coupons`. Nếu hợp lệ, hệ thống ghi nhận giá trị giảm giá vào trường `Order.DiscountAmount` và tăng số lượt sử dụng trong bảng khuyến mại.
- Nếu khách hàng tích chọn cấn trừ điểm Loyalty, dịch vụ `ILoyaltyService.RedeemPointsAsync()` kiểm tra số dư điểm khả dụng trong bảng `CustomerProfiles`. Nếu đủ điều kiện, hệ thống trừ số điểm tương ứng và tạo một bản ghi bút toán xuất điểm trong bảng `LoyaltyLedgers` với mã loại giao dịch `REDEEM_ORDER`.
- Tiếp theo, hệ thống kích hoạt thuật toán xuất kho `InventoryAllocationEngine.AllocateBatchFefoAsync()` cho từng sản phẩm. Động cơ tìm kiếm các lô hàng cận hạn nhất trong bảng `ProductBatches` của showroom tiếp nhận, tăng trường `ReservedQuantity` và trừ trường `AvailableQuantity` để đóng băng hàng hóa, ngăn chặn nguy cơ bán đè tại quầy POS.
- Đơn hàng được tạo trong bảng `Orders` với mã định danh duy nhất (ví dụ: `ORD-20261104-8921`) và trạng thái `PENDING_PICKUP`. Một chuỗi ngẫu nhiên 6 chữ số được sinh ra, băm an toàn và lưu vào trường `Orders.PickupPinHash`.
- Ngay sau khi lệnh commit giao dịch thành công, hệ thống gửi thông điệp thời gian thực qua SignalR Hub tới máy tính tiền POS của Showroom tiếp nhận để nhân viên soạn hàng lập tức đóng gói kiện hàng đưa vào kệ lưu trữ Fast-Track.

---

### TIỂU KẾT MỤC 3.2.1

Mục 3.2.1 đã mô tả và phân tích tường tận 7 màn hình giao diện cốt lõi của Phân hệ Giao diện Khách hàng (Luxury Storefront & Customer Portal) trong hệ thống CANMAKE BEAUTY. Sự kết hợp nhuần nhuyễn giữa thẩm mỹ giao diện thượng lưu, tính công thái học tối ưu cho hành vi người tiêu dùng và các giải pháp xử lý dữ liệu chuẩn xác ở tầng Backend (từ bóc tách hoạt chất INCI, kiểm tra tương kỵ da liễu tự động, đến cấp phát kho FEFO và cấn trừ điểm tích lũy) đã tạo nên một nền tảng thương mại điện tử mỹ phẩm toàn diện, tin cậy và đạt chuẩn mực kỹ thuật quốc tế.
