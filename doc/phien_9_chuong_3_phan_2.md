## 3.2. Hiện thực hóa và Minh họa giao diện các phân hệ chức năng chính

Phần này trình bày chi tiết kết quả hiện thực hóa giao diện người dùng thực tế của hệ thống website quản lý bán hàng cho chuỗi cửa hàng mỹ phẩm OMNI BEAUTY. Toàn bộ các màn hình dưới đây được chụp trực tiếp từ hệ thống phần mềm đang vận hành thực tế trên môi trường máy chủ cục bộ và thử nghiệm, thể hiện sự kết hợp hài hòa giữa ngôn ngữ thiết kế Clean Beauty hiện đại (Modern Clean Beauty Aesthetics), tính công thái học trong vận hành chuỗi bán lẻ đa kênh (Omnichannel Ergonomics) và khả năng xử lý nghiệp vụ thông minh chuyên sâu cho ngành mỹ phẩm.

---

### 3.2.1. Phân hệ Kênh bán lẻ trực tuyến (Storefront Khách hàng)

Kênh bán lẻ trực tuyến của OMNI BEAUTY được xây dựng nhằm mang đến cho khách hàng trải nghiệm mua sắm trực quan, mượt mà và an toàn tối đa cho làn da. Giao diện Storefront tuân thủ nghiêm ngặt chuẩn mực thiết kế Mobile-First, sử dụng bảng màu thương hiệu sang trọng với nền Pearl White kết hợp điểm nhấn Rose Gold và Deep Emerald, đồng thời tích hợp các tính năng công nghệ đặc thù của ngành làm đẹp.

#### 1. Màn hình Trang chủ Omni Beauty (Storefront Home)

Trang chủ là điểm chạm đầu tiên của khách hàng khi tiếp cận thương hiệu OMNI BEAUTY trên môi trường số, đóng vai trò định vị đẳng cấp thương hiệu, truyền tải cam kết mỹ phẩm sạch và kích thích chuyển đổi hành vi mua sắm.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.1 - GIAO DIỆN TRANG CHỦ STOREFRONT]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_1_Giao_Dien_Trang_Chu.png`  
> *Mô tả hình:* Ảnh chụp thực tế toàn màn hình Trang chủ Storefront OMNI BEAUTY hiển thị thanh thông báo chiến dịch, Header đa hình, Hero Banner Clean Beauty cao cấp, Thanh bảo chứng giá trị USP, Bộ lọc đa diện Facet thông minh và Lưới sản phẩm nổi bật gắn nhãn chứng chỉ an toàn.

```
+--------------------------------------------------------------------------------------------------------+
| [Thanh thông báo] Freeship đơn từ 500k | Hotline tư vấn da liễu: 1900-xxxx | Cửa hàng gần nhất         |
+--------------------------------------------------------------------------------------------------------+
| [LOGO] OMNI BEAUTY    [Ô tìm kiếm thông minh Niacinamide, Retinol...] (🔍)    [Wishlist] [Giỏ hàng: (3)] |
| Danh mục: Chăm sóc da | Trang điểm | Chăm sóc cơ thể | Thương hiệu Clean | Cẩm nang Skincare Lab       |
+--------------------------------------------------------------------------------------------------------+
|                                HERO BANNER SLIDER CHIẾN DỊCH                                           |
|       "THUẦN CHAY & LÀNH TÍNH - KHÁM PHÁ BỘ SƯU TẬP MỸ PHẨM CLEAN BEAUTY CHUẨN KHOA HỌC"               |
|                                [Khám phá ngay]                                                         |
+--------------------------------------------------------------------------------------------------------+
| (⭐ 100% Chính hãng)  |  (⚡ Giao hỏa tốc 2H)  |  (🔄 Đổi trả 7 ngày)  |  (🩺 Bác sĩ Da liễu tư vấn)  |
+--------------------------------------------------------------------------------------------------------+
| BỘ LỌC ĐA DIỆN (FACETS)       | LƯỚI SẢN PHẨM NỔI BẬT (CLEAN BEAUTY SHOWCASE)                          |
| Loại da:                      | [Ảnh tỷ lệ 1:1]         [Ảnh tỷ lệ 1:1]         [Ảnh tỷ lệ 1:1]         |
| [x] Da nhạy cảm               | [EWG 1] [Vegan]         [EWG 2] [Cruelty-Free]  [EWG 1] [Paraben-Free]  |
| [ ] Da dầu mụn                | Paula's Choice BHA 2%   The Ordinary Niacinamide Klairs Supple Toner    |
| Hoạt chất yêu thích:          | 850.000 đ               280.000 đ               390.000 đ               |
| [x] Niacinamide               | [Thêm vào giỏ]          [Thêm vào giỏ]          [Thêm vào giỏ]          |
| [ ] BHA / Salicylic Acid      |                         (Hiệu ứng Fly-to-Cart)                          |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Thanh thông báo trên cùng (Top Notification Bar):** Nổi bật với chính sách miễn phí giao hàng toàn quốc cho đơn từ 500.000 đồng, tích hợp nút chọn chi nhánh gần nhất giúp khách hàng xác định được kho hàng phục vụ ngay từ khi bắt đầu phiên duyệt web.
- **Thanh điều hướng Header đa hình (Polymorphic Navigation Header):** Logo OMNI BEAUTY được đặt trang trọng tại góc trái; ô tìm kiếm toàn trang ứng dụng kỹ thuật Debounce 300ms kết hợp phân tích từ khóa hoạt chất thông minh; góc phải là nút truy cập nhanh vào Danh sách yêu thích (Wishlist) và Giỏ hàng kèm huy hiệu số lượng cập nhật theo thời gian thực. Hệ thống Mega Menu đa cấp phân loại sản phẩm theo cả hai chiều: nhóm công năng (Dưỡng ẩm, Trị mụn, Chống lão hóa) và nhóm hoạt chất (AHA, Retinol, Vitamin C).
- **Hero Slider Banner chuẩn Clean Beauty:** Trình bày hình ảnh quảng bá chiến dịch mỹ phẩm thuần chay với tỷ lệ vàng, sử dụng kỹ thuật tải ưu tiên `fetchpriority="high"` để tối ưu hóa chỉ số LCP. Nút kêu gọi hành động (Call to Action - CTA) "Khám phá ngay" đưa người dùng đến trang đích chiến dịch được cá nhân hóa.
- **Thanh bảo chứng niềm tin (USP Bar - Unique Selling Proposition):** 4 khối biểu tượng cam kết mạnh mẽ về nguồn gốc sản phẩm: 100% hàng nhập khẩu chính ngạch có tem phụ tiếng Việt, Giao hỏa tốc trong 2 giờ tại nội thành Hà Nội và TP. Hồ Chí Minh, Đổi trả miễn phí 7 ngày và Đội ngũ chuyên gia da liễu hỗ trợ tư vấn 24/7.
- **Bộ lọc thông minh đa diện (Smart Facet Filters) kết hợp Lưới sản phẩm:** Cho phép lọc đồng thời theo Loại da (Da khô, Da nhạy cảm, Da dầu mụn), Tình trạng da, Hoạt chất chủ đạo và Chứng chỉ an toàn (EWG 1-2, Vegan, Cruelty-Free). Thẻ sản phẩm hiển thị ảnh tỷ lệ chuẩn 1:1 chống giật layout (Zero CLS), gắn nhãn màu chứng chỉ xanh, hiển thị giá niêm yết/giá khuyến mại rõ ràng và nút "Thêm vào giỏ" hỗ trợ hiệu ứng hoạt họa Fly-to-Cart mượt mà.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi người dùng truy cập trang chủ, máy chủ ASP.NET Core kích hoạt `HomeSectionsViewComponent`. Thay vì truy vấn trực tiếp cơ sở dữ liệu cho mỗi lượt truy cập, hệ thống khai thác bộ nhớ đệm đa tầng `IMemoryCache` để lấy danh sách banner, chuyên mục nổi bật và sản phẩm bán chạy với thời hạn đệm 30 phút. Khi người dùng thao tác chọn các tiêu chí trên bộ lọc bên trái (ví dụ: bấm chọn loại da "Da nhạy cảm" và hoạt chất "Niacinamide"), mã JavaScript Alpine.js chặn sự kiện gửi form truyền thống, kích hoạt một lệnh gọi AJAX không đồng bộ tới endpoint `/Catalog/FilterProducts`. Backend xử lý lọc dữ liệu thông qua truy vấn tổng hợp SQL duy nhất được tối ưu hóa bởi chỉ mục tổng hợp `IX_Products_Search_Composite`, trả về danh sách sản phẩm và cập nhật lại lưới hiển thị chỉ trong 33 mili-giây mà không cần tải lại toàn bộ trang web.

---

#### 2. Màn hình Chi tiết Sản phẩm chuyên sâu (Product Detail Page)

Giao diện chi tiết sản phẩm của OMNI BEAUTY là nơi phô diễn thế mạnh công nghệ chuyên biệt cho ngành dược mỹ phẩm, cung cấp đầy đủ thông tin khoa học giúp người tiêu dùng đưa ra quyết định mua hàng an tâm và có trách nhiệm.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.2 - GIAO DIỆN CHI TIẾT SẢN PHẨM CHUYÊN SÂU]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_2_Giao_Dien_Chi_Tiet_San_Pham.png`  
> *Mô tả hình:* Ảnh chụp chi tiết trang thông tin sản phẩm OMNI BEAUTY gồm khối Gallery ảnh độ nét cao kèm tính năng Zoom, bảng lựa chọn biến thể màu sắc/dung tích đồng bộ tồn kho chi nhánh, và widget độc quyền "Phòng thí nghiệm Thành phần Skincare Lab" hiển thị danh sách hoạt chất INCI, chỉ số an toàn EWG và thời hạn sử dụng sau khi mở nắp PAO.

```
+--------------------------------------------------------------------------------------------------------+
| Trang chủ > Chăm sóc da > Serum > Tinh chất Phục hồi & Làm dịu da B5 Omni Serum                         |
+-------------------------------------------------------------------+------------------------------------+
| [GALLERY ẢNH CHÍNH TỶ LỆ 1:1]                                     | THƯƠNG HIỆU: OMNI DERMA LAB        |
|                                                                   | Tinh chất Phục hồi B5 Panthenol    |
| (Ảnh zoom cực đại khi rê chuột)                                   | ⭐⭐⭐⭐⭐ 4.9/5 (128 đánh giá)      |
|                                                                   | ---------------------------------- |
| [Ảnh nhỏ 1] [Ảnh nhỏ 2] [Ảnh nhỏ 3] [Ảnh nhỏ 4]                   | Giá bán: 450.000 đ                 |
|                                                                   | Chọn Dung tích:                    |
| CHỨNG NHẬN AN TOÀN:                                               | [ 30ml ]  [ (50ml) ]  [ 100ml ]    |
| [Huy hiệu Vegan] [Không Cồn] [Không Hương liệu]                   | Tồn kho Showroom Cầu Giấy: Còn 15  |
|                                                                   | Số lượng: [ - ]  1  [ + ]          |
|                                                                   | [ THÊM VÀO GIỎ HÀNG ] [ MUA NGAY ] |
+-------------------------------------------------------------------+------------------------------------+
| WIDGET ĐỘC QUYỀN: PHÒNG THÍ NGHIỆM THÀNH PHẦN (SKINCARE LAB & INCI BREAKDOWN)                          |
| - Điểm an toàn EWG Skin Deep®: 1.2 / 10 (Mức Xanh - Rất an toàn cho da nhạy cảm)                      |
| - Thời hạn sau khi mở nắp (PAO - Period After Opening): [Biểu tượng Hộp mở nắp 12M] (12 Tháng)         |
| - Danh sách hoạt chất chủ đạo (Key Actives):                                                           |
|   + Panthenol (Vitamin B5) 5%: Phục hồi màng ẩm, làm dịu kích ứng tức thì.                            |
|   + Madecassoside (Rau má): Kháng viêm, tái tạo mô da sau tổn thương mụn.                             |
|   + Hyaluronic Acid (HA đa phân tử): Cấp ẩm sâu đa tầng biểu bì.                                       |
| - Bảng thành phần chuẩn quốc tế INCI:                                                                  |
|   Aqua/Water, Panthenol, Glycerin, Butylene Glycol, Madecassoside, Sodium Hyaluronate, Phenoxyethanol...|
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Khối hiển thị hình ảnh đa phương tiện (Media Gallery):** Tích hợp khung hiển thị ảnh chính độ nét cao hỗ trợ thấu kính phóng to chi tiết (Zoom Lens) khi khách hàng rê chuột, cho phép người dùng kiểm tra kỹ lưỡng bao bì, tem phụ chống hàng giả và chất kem. Bên dưới là dải hình thu nhỏ (Thumbnails) chuyển cảnh mượt mà bằng CSS View Transitions.
- **Khối thông tin giao dịch và chọn biến thể (Variant & Inventory Selection):** Hiển thị rõ ràng tên thương hiệu gốc, tiêu đề sản phẩm chuẩn hóa SEO, điểm đánh giá sao trung bình kèm số lượt bình luận. Khung chọn dung tích (30ml, 50ml, 100ml) hoặc bảng màu (Shade Swatches) được liên kết trực tiếp với dữ liệu tồn kho theo từng chi nhánh cụ thể thông qua dropdown chọn địa điểm. Nút bấm "Thêm vào giỏ hàng" và "Mua ngay" được bố trí với kích thước ngón tay cái tiêu chuẩn (độ cao 48px) tạo sự thuận tiện tối đa khi thao tác trên điện thoại thông minh.
- **Widget độc quyền "Phòng thí nghiệm Thành phần" (Skincare Lab Widget):**
  + *Chỉ số an toàn EWG Skin Deep®:* Hiển thị thang đo đồ họa trực quan từ mức 1 đến 10 với dải màu giao thông tiêu chuẩn (Màu xanh lá 1-2: Rủi ro thấp, Màu vàng 3-6: Rủi ro trung bình, Màu đỏ 7-10: Rủi ro cao). Sản phẩm được tự động tính điểm trung bình dựa trên cơ sở dữ liệu thành phần bóc tách.
  + *Biểu tượng Hạn dùng sau khi mở nắp (PAO Icon):* Mô phỏng chuẩn mực quốc tế biểu tượng hũ mở nắp ghi rõ số tháng sử dụng an toàn (ví dụ: `12M` tương ứng với 12 tháng kể từ ngày bóc niêm phong), nhắc nhở người dùng tuân thủ hạn sử dụng sau khi sản phẩm tiếp xúc với không khí.
  + *Bảng thành phần hóa mỹ phẩm quốc tế INCI:* Liệt kê toàn văn các thành phần hóa học theo thứ tự nồng độ giảm dần, làm nổi bật các hoạt chất chủ đạo (Key Actives) kèm công dụng da liễu chuyên biệt được hệ thống `IngredientParserHelper` tự động phân tích và gán nhãn giải thích.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi trang sản phẩm được mở, Controller `ProductController.Detail(string slug)` thực thi câu lệnh truy vấn nạp thông tin sản phẩm và các biến thể con thông qua `AsNoTracking()`. Dữ liệu chuỗi thành phần thô trong thuộc tính `Product.Ingredients` được chuyển qua dịch vụ `IngredientParserHelper.ParseAndScore()`. Thuật toán bóc tách từng phân đoạn hoạt chất dựa trên dấu phẩy ngăn cách, đối soát từng danh pháp hóa học với từ điển da liễu nội bộ để trích xuất điểm số EWG và kiểm tra nồng độ các chất thuộc diện kiểm soát. Khi khách hàng nhấp chuột chọn thay đổi dung tích sản phẩm (ví dụ từ 30ml sang 50ml), một sự kiện JavaScript được kích hoạt gửi yêu cầu kiểm tra số lượng tồn kho khả dụng tới endpoint `/Store/CheckStockByVariant`. Backend truy vấn bảng `StoreInventories` theo đúng chi nhánh đang được chọn và trả về số lượng còn lại, cho phép vô hiệu hóa nút đặt hàng nếu biến thể đã tạm thời hết hàng tại điểm bán đó.

---

#### 3. Màn hình Giỏ hàng thông minh và Cảnh báo Tương kỵ Hoạt chất (Cart & Skincare Conflict Alert)

Đây là tính năng mang tính đột phá và duy nhất chỉ có tại OMNI BEAUTY, biến một giỏ hàng thương mại điện tử thông thường trở thành một trợ lý da liễu cá nhân thông minh, bảo vệ sức khỏe người tiêu dùng trước nguy cơ tự ý phối hợp các hoạt chất xung đột.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.3 - GIAO DIỆN GIỎ HÀNG VÀ CẢNH BÁO XUNG ĐỘT HOẠT CHẤT]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_3_Giao_Dien_Gio_Hang_Va_Canh_Bao_Xung_Dot.png`  
> *Mô tả hình:* Ảnh chụp thực tế giao diện Giỏ hàng OMNI BEAUTY với danh sách mỹ phẩm đã chọn, thanh công cụ tính điểm thưởng thành viên, và hộp cảnh báo da liễu nổi bật màu đỏ/cam thông báo phát hiện cặp hoạt chất tương kỵ Retinol và AHA/BHA giữa hai sản phẩm trong giỏ kèm khuyến nghị phân chia chu trình sáng/tối hoặc dùng cách ngày.

```
+--------------------------------------------------------------------------------------------------------+
| GIỎ HÀNG CỦA BẠN (2 sản phẩm)                                                 [Tiếp tục mua sắm]       |
+--------------------------------------------------------------------------------------------------------+
| ⚠️ CẢNH BÁO TƯƠNG KỴ HOẠT CHẤT DA LIỄU PHÁT HIỆN TRONG GIỎ HÀNG (SKINCARE CONFLICT DETECTED)          |
| Mức độ cảnh báo: NGHIÊM TRỌNG (SEVERE CONFLICT)                                                        |
| - Cặp hoạt chất xung đột: [ Retinol 1.0% ] ⚡ [ Salicylic Acid (BHA 2%) ]                               |
| - Sản phẩm liên quan:                                                                                  |
|   + Kem dưỡng tái tạo ban đêm Retinol Avene (Chứa Retinol)                                             |
|   + Dung dịch tẩy tế bào chết Paula's Choice (Chứa Salicylic Acid)                                      |
| - Khuyến cáo của Chuyên gia Da liễu OMNI BEAUTY:                                                       |
|   "Việc sử dụng đồng thời Retinol cùng BHA trong cùng một buổi tối có thể làm suy yếu nghiêm trọng     |
|   hàng rào bảo vệ tự nhiên của da, gây khô căng rát, bong tróc diện rộng và tăng nguy cơ kích ứng đỏ.  |
|   Khuyến nghị: Hãy dùng BHA vào các buổi tối Thứ 2-4-6 và dùng Retinol vào các buổi tối Thứ 3-5-7!"    |
+--------------------------------------------------------------------------------------------------------+
| SẢN PHẨM TRONG GIỎ               | ĐƠN GIÁ    | SỐ LƯỢNG      | THÀNH TIỀN   | THAO TÁC                |
| [Ảnh] Kem Retinol Avene 30ml     | 650.000 đ  | [ - ]  1  [ + ]| 650.000 đ    | [ Xóa ]                 |
| [Ảnh] Dung dịch BHA Paula's 30ml | 399.000 đ  | [ - ]  1  [ + ]| 399.000 đ    | [ Xóa ]                 |
+--------------------------------------------------------------------------------------------------------+
| TẠM TÍNH: 1.049.000 đ            | GIẢM GIÁ VOUCHER: -50.000 đ | TỔNG CỘNG: 999.000 đ                  |
| Điểm thưởng tích lũy: +99 điểm   | [ NHẬP MÃ GIẢM GIÁ ]        | [ TIẾN HÀNH THANH TOÁN (CHECKOUT) ]   |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Hộp Cảnh báo Tương kỵ Hoạt chất Da liễu (Skincare Conflict Banner):** Được đặt ở vị trí thị giác ưu tiên cao nhất, ngay phía trên bảng kê chi tiết sản phẩm với viền màu đỏ cam nổi bật và biểu tượng tia sét xung đột. Nội dung hộp chỉ rõ tên hai thành phần đối nghịch đang cùng hiện diện trong giỏ (ví dụ: Retinol và BHA), gắn nhãn mức độ rủi ro (Nghiêm trọng / Trung bình), và đưa ra giải pháp khắc phục chi tiết mang tính xây dựng (chia lịch xen kẽ cách ngày) thay vì cấm đoán người dùng.
- **Bảng kê giỏ hàng tương tác cao:** Hiển thị rõ ảnh đại diện, tên biến thể sản phẩm, đơn giá niêm yết, công cụ điều chỉnh số lượng với các nút cộng trừ kích thước lớn hạn chế ấn nhầm. Trường số lượng tự động giới hạn ở mức tồn kho khả dụng tối đa của chi nhánh đang chọn.
- **Khối tính toán tài chính và chuyển đổi:** Tự động tính toán tổng số tiền hàng, hiển thị số tiền được khấu trừ từ mã giảm giá đã nhập, tính toán số điểm thưởng thành viên (Loyalty Points) sẽ nhận được sau đơn hàng, và nút bấm màu vàng kim sang trọng "Tiến hành thanh toán" dẫn hướng sang trang Checkout.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Mỗi khi khách hàng thêm một sản phẩm vào giỏ hoặc thay đổi số lượng, JavaScript gọi API `POST /Cart/UpdateQuantity`. Sau khi cập nhật lại session giỏ hàng trong Cookie bảo mật, hệ thống tự động kích hoạt động cơ da liễu `SkincareIngredientInteractionEngine.AnalyzeCartConflictsAsync(cartItems)`. Động cơ này trích xuất toàn bộ danh mục hoạt chất của các sản phẩm có trong giỏ, lặp qua ma trận tương kỵ da liễu đã được định nghĩa trong cơ sở dữ liệu (ví dụ: bảng quy tắc cấm phối hợp giữa `AHA/BHA` với `Retinoids`, `Vitamin C tinh khiết (L-AA)` với `Niacinamide ở độ pH không phù hợp`, `Benzoyl Peroxide` với `Retinol`). Nếu phát hiện sự kết hợp có nguy cơ gây hại, hàm trả về đối tượng `ConflictDetectionResult` chứa thông báo cảnh báo chi tiết. Giao diện giỏ hàng lắng nghe phản hồi JSON và dùng Alpine.js để kết xuất khung cảnh báo động ngay lập tức mà không làm gián đoạn trải nghiệm mua sắm của khách.

---

### 3.2.2. Phân hệ Quản trị Trung tâm (Admin Executive Dashboard & Phân quyền)

Phân hệ Quản trị Trung tâm là cơ quan đầu não điều hành toàn bộ chuỗi bán lẻ OMNI BEAUTY, cung cấp cho Ban Giám đốc và các Trưởng bộ phận góc nhìn 360 độ về hiệu quả kinh doanh, dòng tiền, an toàn kho vận và mức độ tuân thủ quy chế phân quyền trong tổ chức.

#### 4. Màn hình Bảng điều khiển Quản trị Trung tâm (Admin Executive Dashboard)

Bảng điều khiển Executive Dashboard đóng vai trò là một tháp điều khiển số (Digital Control Tower), tổng hợp hàng ngàn giao dịch đa kênh diễn ra mỗi ngày thành các chỉ số tài chính và cảnh báo vận hành cô đọng, hỗ trợ ra quyết định kịp thời.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.4 - GIAO DIỆN BẢNG ĐIỀU KHIỂN QUẢN TRỊ ADMIN DASHBOARD]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_4_Giao_Dien_Bang_Dieu_Khien_Admin_Dashboard.png`  
> *Mô tả hình:* Ảnh chụp thực tế Bảng điều khiển Admin Dashboard gồm 4 thẻ chỉ số tài chính then chốt (Doanh thu ròng, Tỷ lệ đơn O2O, Tỷ suất GMROI, Cảnh báo cận date), Biểu đồ đường cong so sánh doanh thu Online vs POS trực tiếp tại quầy theo thời gian thực, và danh sách cảnh báo tồn kho lô hạn khẩn cấp.

```
+--------------------------------------------------------------------------------------------------------+
| BẢNG ĐIỀU KHIỂN QUẢN TRỊ OMNI BEAUTY | Chi nhánh: [ Toàn chuỗi v ] | Thời gian: [ Tháng này v ]        |
+--------------------------------------------------------------------------------------------------------+
| [ THẺ DOANH THU RÒNG ]     | [ THẺ TỶ LỆ ĐƠN O2O ]      | [ TỶ SUẤT GMROI ]        | [ LÔ CẬN DATE ]   |
| 1.845.250.000 đ            | 38.6 %                     | 3.42 x                   | 12 Lô cần xả      |
| (▲ +14.8% so với tháng trước)| (▲ Tăng 8.2% sau kích hoạt)| (Hiệu quả vòng quay tốt) | (Hạn dùng <60 ngày)|
+--------------------------------------------------------------------------------------------------------+
| BIỂU ĐỒ DIỄN BIẾN DOANH THU ĐA KÊNH THEO NGÀY (LINE CHART)                                            |
| [VNĐ]                                                                                                  |
| 100M |               /\                                   -- Đơn hàng Online Storefront                |
|  80M |      /\      /  \        /\                       - - Bán trực tiếp tại quầy POS Showroom       |
|  60M |     /  \    /    \      /  \       /\                                                           |
|  40M |    /    \--/      \----/    \-----/  \                                                          |
|   0M +----+----+----+----+----+----+----+----+----+----> [Thời gian từ Ngày 01 đến Ngày 30]             |
+-------------------------------------------------------------------+------------------------------------+
| CẢNH BÁO LÔ HÀNG CẬN HẠN KHẨN CẤP (FEFO WATCHLIST)                | GIAO DỊCH GẦN NHẤT (REALTIME FEED) |
| - Lô LOT-2024-C12: Kem chống nắng Anessa (Còn 24 ngày) -> [Xả]    | 14:32 - Đơn ORD-8921: 850k (POS CG)|
| - Lô LOT-2024-A08: Serum B5 La Roche-Posay (Còn 45 ngày) -> [Xả]  | 14:30 - Đơn ORD-8920: 320k (Web)   |
| - Lô LOT-2024-B15: Son kem 3CE Velvet Tint (Còn 52 ngày) -> [Chuyển]| 14:28 - Đơn ORD-8919: 1.2M (O2O) |
+-------------------------------------------------------------------+------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Thanh lọc ngữ cảnh hoạt động (Contextual Filter Toolbar):** Bố trí góc trên cùng bên phải, cho phép người quản lý chuyển đổi linh hoạt phạm vi quan sát giữa toàn bộ hệ thống chuỗi và từng Showroom đơn lẻ (Showroom Cầu Giấy, Showroom Ba Đình, Kho Tổng Hà Nội) cùng khoảng thời gian phân tích (Hôm nay, 7 ngày qua, Tháng này, Năm nay).
- **Hệ thống 4 Thẻ chỉ số hiệu năng then chốt (Executive KPI Cards):**
  + *Doanh thu ròng (Net Revenue):* Phản ánh tổng giá trị bán lẻ thực tế sau khi đã trừ giảm giá và hàng trả về, hiển thị tỷ lệ tăng trưởng phần trăm màu xanh lục so với chu kỳ kế toán trước.
  + *Tỷ lệ đơn hàng O2O (Click-and-Collect Adoption Rate):* Đo lường tỷ trọng đơn hàng khách đặt trước trên website và tới nhận tại cửa hàng, minh chứng cho sức mạnh hợp nhất đa kênh của giải pháp.
  + *Tỷ suất sinh lời gộp trên vốn tồn kho (GMROI - Gross Margin Return on Investment):* Đo lường khả năng sinh lời trên mỗi đồng vốn đang ứ đọng trong kho, là chỉ số tài chính sống còn đối với ngành bán lẻ mỹ phẩm có hạn dùng.
  + *Thẻ giám sát Lô hàng cận Date:* Đếm số lượng lô mỹ phẩm đang có ngày hết hạn dưới 60 ngày cần sự can thiệp lập tức của phòng kinh doanh.
- **Biểu đồ so sánh doanh thu đa kênh (Interactive Multi-Channel Chart):** Sử dụng biểu đồ đường cong mượt mà phân tách rõ ràng giữa dòng tiền thu được từ website trực tuyến và dòng tiền mặt/thẻ quẹt trực tiếp tại các điểm bán POS, giúp nhận diện quy luật mua sắm của khách hàng theo các ngày trong tuần.
- **Bảng cấp báo Lô hàng cận hạn và Dòng giao dịch trực tiếp (Realtime Transaction Stream):** Kết hợp luồng thông báo cảnh báo kho hàng theo nguyên tắc FEFO với bảng ghi nhận đơn hàng phát sinh tức thời được đẩy tự động từ các máy tính tiền thông qua công nghệ WebSocket của thư viện SignalR.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi tải trang Dashboard, phương thức `AdminController.Dashboard()` trích xuất `StoreId` từ danh tính người dùng đang đăng nhập. Nếu người dùng là `SuperAdmin`, bộ lọc chi nhánh được mở khóa cho phép chọn bất kỳ cơ sở nào; nếu là `StoreManager`, hệ thống tự động khóa chặt phạm vi dữ liệu theo cửa hàng được phân công. Controller kích hoạt phương thức bất đồng bộ `IDashboardService.GetExecutiveMetricsAsync()` thực hiện tổng hợp song song các chỉ số tài chính thông qua LINQ to Entities kết hợp với hàm tổng hợp `SumAsync()` và `CountAsync()` đã được tối ưu hóa chỉ mục SQL, đảm bảo thời gian tải toàn bộ dữ liệu biểu đồ và thẻ số liệu chỉ mất chưa đầy 40 mili-giây. Kết nối SignalR Hub `NotificationHub` duy trì kênh liên lạc hai chiều giữa máy chủ với màn hình quản trị, tự động cập nhật số liệu ngay khi có một giao dịch mới hoàn tất tại bất kỳ cửa hàng nào.

---

#### 5. Màn hình Quản trị Menu Đa hình & Cây Danh mục SEO (Polymorphic Navigation & SEO Engine)

Giao diện quản trị menu và danh mục cung cấp cho chuyên viên quản trị nội dung khả năng tùy biến toàn bộ kiến trúc cây điều hướng trên website mà không cần chỉnh sửa bất kỳ dòng mã HTML nào, đồng thời chuẩn hóa tối đa các tiêu chuẩn kỹ thuật SEO của Google.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.5 - GIAO DIỆN QUẢN TRỊ MENU ĐA HÌNH VÀ SEO ENGINE]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_5_Giao_Dien_Quan_Tri_Menu_Da_Hinh_SEO.png`  
> *Mô tả hình:* Ảnh chụp thực tế giao diện Quản trị Cây Menu Đa hình hiển thị cấu trúc phân cấp danh mục kéo thả, bảng thiết lập liên kết đa hình đến Danh mục, Thương hiệu, Trang tĩnh CMS, và Hộp công cụ xem trước mô phỏng giao diện tìm kiếm Google SERP Preview chuẩn SEO.

```
+--------------------------------------------------------------------------------------------------------+
| QUẢN TRỊ MENU ĐA HÌNH & KIẾN TRÚC DANH MỤC SEO                                    [+ Thêm mục mới]    |
+-------------------------------------------------------------------+------------------------------------+
| CÂY CẤU TRÚC PHÂN CẤP ĐIỀU HƯỚNG (DRAG & DROP TREE)               | THIẾT LẬP THUỘC TÍNH MỤC ĐIỀU HƯỚNG|
|                                                                   | Tiêu đề hiển thị: [ Chăm sóc da  ] |
| [-] 1. CHĂM SÓC DA (SKINCARE)                        (Thứ tự: 10) | Kiểu liên kết đa hình (Target):    |
|     |-- [+] 1.1. Làm sạch da                         (Thứ tự: 1)  | (o) Danh mục sản phẩm (Category)   |
|     |-- [-] 1.2. Serum & Tinh chất đặc trị           (Thứ tự: 2)  | ( ) Thương hiệu mỹ phẩm (Brand)    |
|     |       |-- 1.2.1. Serum Trị Mụn (BHA/AHA)       (Thứ tự: 1)  | ( ) Chiến dịch Clean Beauty Badge  |
|     |       `-- 1.2.2. Serum Chống Lão Hóa (Retinol) (Thứ tự: 2)  | ( ) Trang nội dung tĩnh (CMS Page) |
|     `-- [+] 1.3. Kem dưỡng ẩm phục hồi               (Thứ tự: 3)  | Liên kết chọn: [ Serum Đặc Trị v ] |
| [+] 2. TRANG ĐIỂM (MAKEUP)                           (Thứ tự: 20) | Huy hiệu nổi bật: [ HOT ] (Màu đỏ) |
| [+] 3. BỘ SƯU TẬP CLEAN BEAUTY                       (Thứ tự: 30) | Icon biểu tượng: [ icon-droplet ]  |
+-------------------------------------------------------------------+------------------------------------+
| HỘP CÔNG CỤ TỐI ƯU HÓA TÌM KIẾM GOOGLE (GOOGLE SERP PREVIEW BOX)                                       |
| - Tiêu đề SEO (Title): [ Serum & Tinh Chất Dưỡng Da Chính Hãng Giá Tốt | OMNI BEAUTY ] (56/60 ký tự)  |
| - Đường dẫn URL Thân thiện (Slug): https://omnibeauty.vn/catalog/serum-tinh-chat-dac-tri                |
| - Thẻ mô tả (Meta Description): (152/160 ký tự)                                                        |
|   [ Mua serum & tinh chất dưỡng da phục hồi B5, chống lão hóa retinol, trị mụn BHA chính hãng 100%    |
|     tại chuỗi cửa hàng OMNI BEAUTY. Tư vấn phác đồ da liễu miễn phí, giao hỏa tốc 2H. ]               |
|                                                                                                        |
| GIAO DIỆN HIỂN THỊ MÔ PHỎNG TRÊN GOOGLE TRÊN MÁY TÍNH:                                                 |
|   https://omnibeauty.vn > catalog > serum-tinh-chat-dac-tri                                            |
|   Serum & Tinh Chất Dưỡng Da Chính Hãng Giá Tốt | OMNI BEAUTY                                          |
|   Mua serum & tinh chất dưỡng da phục hồi B5, chống lão hóa retinol, trị mụn BHA chính hãng 100% tại...|
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Cây cấu trúc phân cấp danh mục kéo thả (Hierarchical Tree View):** Trình bày trực quan toàn bộ hệ thống menu của trang web theo mô hình thư mục lồng nhau không giới hạn độ sâu. Người quản trị có thể trực tiếp kéo thả các nút menu để thay đổi vị trí thứ tự hiển thị hoặc chuyển đổi cấp độ cha - con giữa các chuyên mục.
- **Bảng cấu hình liên kết đa hình (Polymorphic Target Configurator):** Cho phép một mục menu ánh xạ linh hoạt tới nhiều thực thể nghiệp vụ khác nhau trong hệ thống thông qua khóa đa hình `TargetType` và `TargetId`: liên kết đến Danh mục sản phẩm (Category), Thương hiệu phân phối độc quyền (Brand), Bộ sưu tập theo nhãn Clean Beauty, hoặc Trang bài viết cẩm nang làm đẹp CMS. Giao diện cung cấp bảng chọn màu huy hiệu đính kèm (như nhãn "NEW", "HOT", "SALE") và thư viện biểu tượng SVG sống động.
- **Hộp mô phỏng kết quả tìm kiếm Google (Google SERP Simulator Box):** Bao gồm thanh công cụ đo độ dài ký tự của Thẻ tiêu đề trang (chuẩn từ 50-60 ký tự) và Thẻ mô tả tóm tắt (chuẩn từ 150-160 ký tự) với màu sắc cảnh báo theo thời gian thực (chuyển từ xanh lá sang đỏ khi vượt quá giới hạn). Khung xem trước mô phỏng chính xác giao diện hiển thị của đường link trên trang kết quả tìm kiếm Google Desktop và Mobile, hỗ trợ đội ngũ tiếp thị kiểm soát chất lượng hiển thị trước khi lưu lại.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi người quản lý thực hiện chỉnh sửa cây điều hướng hoặc thay đổi đường dẫn thân thiện (Slug) của một danh mục, phương thức `MenuEngineController.SaveMenuStructure()` được gọi. Dữ liệu cây được duyệt đệ quy để cập nhật lại các trường `ParentId` và `DisplayOrder` trong bảng `NavigationMenus`. Đặc biệt, nếu thuộc tính `Slug` bị thay đổi, hệ thống kích hoạt cơ chế tự động ghi nhận đường dẫn cũ vào bảng `SlugRedirects` và tạo mã chuyển hướng vĩnh viễn HTTP 301. Ngay sau khi lệnh lưu thành công, hệ thống gọi phương thức xóa bộ nhớ đệm `_memoryCache.Remove("GlobalNavigationMenuTree")`. Nhờ đó, hàng triệu khách hàng đang lướt web lập tức nhìn thấy cấu trúc menu mới ở lượt tải trang tiếp theo mà máy chủ hoàn toàn không phải khởi động lại.

---

#### 6. Màn hình Quản trị Ma trận Phân quyền Động PBAC (Role-Based Dynamic Permission Matrix)

Giao diện quản trị phân quyền PBAC cho phép quản lý chặt chẽ ranh giới bảo mật của 11 tác nhân người dùng trong hệ thống, thực thi nguyên tắc đặc quyền tối thiểu (Principle of Least Privilege) trên toàn chuỗi cửa hàng.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.6 - GIAO DIỆN MA TRẬN PHÂN QUYỀN ĐỘNG PBAC]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_6_Giao_Dien_Ma_Tran_Phan_Quyen_PBAC.png`  
> *Mô tả hình:* Ảnh chụp thực tế Giao diện Quản trị Ma trận Phân quyền PBAC dạng lưới 2 chiều thể hiện danh sách 51 quyền hạn nguyên tử được phân nhóm theo phân hệ nghiệp vụ, các công tắc bật/tắt quyền Toggle Switch cho từng vai trò và cấu hình phạm vi dữ liệu chi nhánh Store Scope.

```
+--------------------------------------------------------------------------------------------------------+
| THIẾT LẬP MA TRẬN PHÂN QUYỀN ĐỘNG PBAC | Vai trò: [ Quản lý Cửa hàng (StoreManager) v ] [LƯU THAY ĐỔI] |
+--------------------------------------------------------------------------------------------------------+
| CƠ CHẾ PHẠM VI DỮ LIỆU: [x] Giới hạn phạm vi dữ liệu trong Chi nhánh được phân công (Store Scope)      |
+--------------------------------------------------------------------------------------------------------+
| NHÓM PHÂN HỆ NGHIỆP VỤ    | MÃ QUYỀN HẠN NGUYÊN TỬ       | MÔ TẢ NGHIỆP VỤ               | TRẠNG THÁI  |
|---------------------------+------------------------------+-------------------------------+-------------|
| PHÂN HỆ QUẢN TRỊ KHO VẬN  | INVENTORY_VIEW               | Xem danh sách tồn kho lô hạn  | [ ON  (v) ] |
| & ĐIỀU CHUYỂN NỘI BỘ      | INVENTORY_ADJUST             | Điều chỉnh số lượng kiểm kê   | [ OFF ( ) ] |
|                           | STOCK_TRANSFER_REQUEST       | Tạo yêu cầu điều chuyển kho   | [ ON  (v) ] |
|                           | STOCK_TRANSFER_APPROVE       | Phê duyệt xuất điều chuyển    | [ ON  (v) ] |
|                           | BATCH_EXPIRY_OVERRIDE        | Mở khóa xuất lô cận date      | [ OFF ( ) ] |
|---------------------------+------------------------------+-------------------------------+-------------|
| PHÂN HỆ BÁN HÀNG TẠI QUẦY | POS_ORDER_CREATE             | Tạo đơn hàng bán trực tiếp    | [ ON  (v) ] |
| (SMART POS & CA THU NGÂN) | POS_DISCOUNT_APPLY           | Áp dụng chiết khấu thủ công   | [ ON  (v) ] |
|                           | POS_SHIFT_OPEN_CLOSE         | Mở và đóng ca bán hàng        | [ ON  (v) ] |
|                           | POS_BLIND_AUDIT_VIEW         | Xem số dư thực tế kiểm toán   | [ OFF ( ) ] |
|---------------------------+------------------------------+-------------------------------+-------------|
| PHÂN HỆ TÀI CHÍNH KẾ TOÁN | FINANCE_VIEW_REVENUE         | Xem báo cáo doanh thu điểm bán| [ ON  (v) ] |
|                           | FINANCE_VIEW_COGS            | Xem giá vốn lô hàng (COGS)    | [ OFF ( ) ] |
|                           | FINANCE_LEDGER_MANUAL_ENTRY  | Ghi nhận bút toán điều chỉnh  | [ OFF ( ) ] |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Thanh chọn vai trò và bộ điều khiển phạm vi (Role & Scope Selector):** Dropdown cho phép chọn bất kỳ vai trò người dùng nào trong 11 nhóm tác nhân đã phân tích ở Chương 2. Kế bên là ô tích chọn "Giới hạn phạm vi dữ liệu chi nhánh (Store Scope)" - một tính năng sống còn bảo đảm Quản lý cửa hàng hoặc Thu ngân chỉ được nhìn thấy hóa đơn, kho hàng và báo cáo thuộc Showroom mà họ làm việc, hoàn toàn cô lập dữ liệu với các Showroom khác trong chuỗi.
- **Bảng ma trận phân quyền dạng lưới 2 chiều (Dynamic Permission Grid):** Toàn bộ 51 quyền hạt nhân được tổ chức mạch lạc thành từng nhóm phân hệ nghiệp vụ có tiêu đề nổi bật: Nhóm Sản phẩm & Danh mục, Nhóm Kho vận & Lô hạn, Nhóm Bán hàng POS, Nhóm Khách hàng CRM & Loyalty, Nhóm Tài chính & Giá vốn. Mỗi dòng thể hiện mã quyền kỹ thuật viết hoa chuẩn hóa (như `STOCK_TRANSFER_APPROVE`), tên tiếng Việt diễn giải rõ ràng và công tắc gạt trạng thái (Toggle Switch) chuyển màu xanh khi được kích hoạt.
- **Cơ chế phân tách quyền lợi nhạy cảm:** Giao diện chủ động tách biệt các quyền nhạy cảm có nguy cơ gian lận cao (ví dụ: quyền xem kết quả kiểm toán két tiền mù `POS_BLIND_AUDIT_VIEW` hay quyền xem giá vốn nhập khẩu `FINANCE_VIEW_COGS`) thành các mục riêng có cảnh báo màu cam, ngăn chặn việc vô tình cấp thừa quyền cho cấp nhân viên vận hành.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi quản trị viên tối cao thay đổi một công tắc quyền và nhấn "Lưu thay đổi", form dữ liệu được gửi đến `RolePermissionController.UpdateRoleMatrix()`. Backend mở một giao dịch cơ sở dữ liệu `BeginTransactionAsync()`, xóa toàn bộ các quyền cũ của vai trò đó trong bảng `RolePermissions` và nạp danh sách các quyền mới được tích chọn. Đồng thời, hệ thống kích hoạt phương thức làm sạch bộ nhớ đệm `_permissionService.InvalidatePermissionCacheForRole(roleId)`. Toàn bộ các đối tượng phân quyền đang lưu đệm trong `HashSet<string>` của các phiên làm việc của nhân viên thuộc vai trò đó bị xóa bỏ, buộc hệ thống phải đánh giá lại quyền trong lượt gọi HTTP kế tiếp, bảo đảm chính sách bảo mật mới có hiệu lực tức thời trên toàn chuỗi mà không cần người dùng phải đăng xuất.

---

### 3.2.3. Phân hệ Quản trị Kho Lô Hạn và Điều chuyển Nội bộ

Phân hệ Kho vận là trái tim kỹ thuật giải quyết bài toán chống suy giảm giá trị mỹ phẩm do hết hạn sử dụng, bảo đảm luân chuyển hàng hóa nhịp nhàng giữa Kho tổng với hệ thống các Showroom bán lẻ.

#### 7. Màn hình Giám sát Tồn kho Lô Hạn & Đèn Giao Thông Date (FEFO Warehouse Monitor)

Màn hình này được thiết kế dành riêng cho Thủ kho tổng và Thủ kho chi nhánh, cung cấp cái nhìn chi tiết đến từng số lô sản xuất, ngày hết hạn và tình trạng tồn kho khả dụng của từng sản phẩm.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.7 - GIAO DIỆN GIÁM SÁT TỒN KHO LÔ HẠN FEFO]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_7_Giao_Dien_Giam_Sat_Ton_Kho_FEFO.png`  
> *Mô tả hình:* Ảnh chụp thực tế Màn hình Giám sát Lô Hạn FEFO mật độ cao hiển thị danh sách tồn kho theo số lô sản xuất, ngày hết hạn đi kèm hệ thống Huy hiệu Đèn giao thông Date (Đỏ: Hết hạn/Khóa xuất; Cam: Cận date <90 ngày/Kích hoạt xả hàng; Xanh: An toàn) và thanh tác vụ tạo lệnh điều chuyển kho nội bộ.

```
+--------------------------------------------------------------------------------------------------------+
| GIÁM SÁT TỒN KHO LÔ HẠN & ĐIỀU CHUYỂN FEFO | Kho: [ Showroom Cầu Giấy v ] | Lọc Date: [ Cận hạn <90 ngày v] |
+--------------------------------------------------------------------------------------------------------+
| TÌM NHANH MÃ VẠCH / SỐ LÔ: [ Quét Barcode...                   (🔍) ]  [+ TẠO PHIẾU ĐIỀU CHUYỂN KHO]   |
+--------------------------------------------------------------------------------------------------------+
| MÃ SKU   | TÊN MỸ PHẨM              | SỐ LÔ SẢN XUẤT | HẠN SỬ DỤNG  | TRẠNG THÁI DATE | TỒN KHẢ DỤNG | THAO TÁC |
|----------+--------------------------+----------------+--------------+-----------------+--------------+----------|
| SKU-0129 | Kem Chống Nắng Anessa 60ml| LOT-2023-A09   | 15/12/2026   | [ ĐỎ: HẾT HẠN ] | 0 hộp (Khóa) | [Tiêu hủy|
| SKU-0482 | Serum B5 La Roche-Posay  | LOT-2024-C12   | 28/02/2027   | [ CAM: CÒN 42N ]| 24 hộp       | [Xả Date]|
| SKU-0751 | Tẩy Trang Bioderma 500ml | LOT-2024-D05   | 10/04/2027   | [ CAM: CÒN 83N ]| 45 hộp       | [Xả Date]|
| SKU-0933 | Son Kem 3CE Velvet Tint  | LOT-2025-E01   | 20/11/2027   | [ XANH: AN TOÀN]| 150 cây      | [Chi tiết|
| SKU-1044 | Mặt Nạ Giấy Banobagi     | LOT-2025-F18   | 15/05/2028   | [ XANH: AN TOÀN]| 300 miếng    | [Chi tiết|
+--------------------------------------------------------------------------------------------------------+
| TỔNG KẾT: 5 Lô hàng | Tồn khả dụng: 519 đơn vị | 1 Lô hết hạn cần tiêu hủy | 2 Lô cận date cần xả hàng  |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Bố cục bảng dữ liệu mật độ cao (Compact High-Density Grid):** Sử dụng phông chữ đơn không gian (Monospace) `JetBrains Mono` cho các cột Mã SKU, Số Lô Sản Xuất, Ngày tháng và Số lượng tồn, bảo đảm các chữ số luôn thẳng hàng tuyệt đối theo phương thẳng đứng giúp thủ kho dễ dàng rà soát bằng mắt khi kiểm đếm hàng nghìn mã hàng.
- **Hệ thống Huy hiệu Trạng thái Đèn giao thông Date (Traffic Light Badges):**
  + *Huy hiệu Đỏ (Hết hạn):* Tự động kích hoạt khi `ExpDate <= DateTime.UtcNow`. Hàng hóa thuộc lô này bị hệ thống đóng băng tự động trên toàn bộ website và các máy tính tiền POS, khóa hoàn toàn quyền bán và chỉ cho phép lập biên bản xuất hủy hàng hỏng.
  + *Huy hiệu Màu cam (Cận hạn < 90 ngày):* Hiển thị số ngày sử dụng còn lại chính xác đến từng ngày (ví dụ: "CÒN 42 NGÀY"), kích hoạt gợi ý chính sách giá xả hàng `ClearanceDiscount` để kích thích tiêu thụ trước khi hàng mất giá trị sử dụng.
  + *Huy hiệu Màu xanh lục (An toàn > 90 ngày):* Thể hiện hàng hóa có hạn sử dụng dồi dào, đủ điều kiện lưu kho bình thường.
- **Thanh tác vụ quét mã vạch và điều chuyển:** Hộp tìm kiếm luôn tự động duy trì con trỏ chuột (Auto-focus) để nhận tín hiệu tức thời từ máy quét mã vạch cầm tay, bên cạnh nút bấm tạo nhanh phiếu xuất điều chuyển kho hàng nội bộ sang các chi nhánh có tốc độ bán nhanh hơn.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi thủ kho mở màn hình giám sát, `WarehouseController.FefoMonitor()` gọi dịch vụ `IStoreInventoryService.GetFefoWatchlistAsync(storeId)`. Truy vấn khai thác chỉ mục `IX_ProductBatches_Fefo_Composite` trên SQL Server để lọc nhanh các bản ghi có `Status == "ACTIVE"` và sắp xếp tăng dần theo `ExpDate ASC`. Khi thủ kho nhấp vào nút "Xả Date" trên một lô hàng cận hạn, hệ thống mở modal cho phép thiết lập tỷ lệ chiết khấu thanh lý. Lệnh xác nhận sẽ cập nhật giá trị `ClearanceDiscountPercent` trong bảng `ProductBatches`, đồng thời phát sự kiện qua SignalR để hạ giá bán của lô hàng đó trên toàn bộ các kênh bán lẻ trong vòng chưa đầy 1 giây.

---

#### 8. Màn hình Quản lý Đơn hàng Đa kênh & Cấp phát Lô Đích danh (Omnichannel Order Fulfillment)

Giao diện quản lý đơn hàng giúp nhân viên xử lý đơn kết nối hoàn hảo giữa các giao dịch trực tuyến từ website với khâu lấy hàng vật lý tại kho, bảo đảm xuất đúng số lô theo thuật toán FEFO và theo dõi lộ trình giao nhận chuyên nghiệp.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.8 - GIAO DIỆN QUẢN LÝ ĐƠN HÀNG VÀ CẤP PHÁT LÔ FEFO]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_8_Giao_Dien_Quan_Ly_Don_Hang_Cap_Phat_Lo.png`  
> *Mô tả hình:* Ảnh chụp thực tế Màn hình Chi tiết Đơn hàng Đa kênh OMNI BEAUTY hiển thị luồng thông tin đơn hàng O2O Click-and-Collect, bảng phân bổ số lô đích danh tự động gán cho từng sản phẩm theo thuật toán FEFO, mã vận đơn Giao Hàng Nhanh GHN và ô nhập mã PIN 6 số xác thực khi khách nhận hàng tại showroom.

```
+--------------------------------------------------------------------------------------------------------+
| CHI TIẾT ĐƠN HÀNG: #ORD-20261104-7892 | Kênh: [ O2O CLICK-AND-COLLECT ] | Trạng thái: [ ĐÃ XÁC NHẬN ]  |
+--------------------------------------------------------------------------------------------------------+
| THÔNG TIN KHÁCH HÀNG:                   | ĐỊA ĐIỂM NHẬN HÀNG TỰ CHỌN:                                  |
| - Khách hàng: Nguyễn Thu Hà (Hạng VÀNG) | - Showroom tiếp nhận: Showroom Cầu Giấy (123 Cầu Giấy, HN)   |
| - Số điện thoại: 0987.xxx.321           | - Hình thức nhận: Khách tự đến lấy hàng trong 24 giờ         |
| - Trạng thái thanh toán: ĐÃ THANH TOÁN  | - Mã PIN bảo mật nhận hàng: [ 8 4 9 2 0 1 ]                  |
|   (Chuyển khoản VietQR Napas247 - Khớp) |   (Khách trình mã PIN này cho thu ngân khi lấy hàng)         |
+--------------------------------------------------------------------------------------------------------+
| BẢNG CẤP PHÁT SỐ LÔ ĐÍCH DANH THEO THUẬT TOÁN FEFO (BATCH ALLOCATION BREAKDOWN)                        |
| SẢN PHẨM ĐẶT HÀNG        | SL | MÃ SỐ LÔ ĐƯỢC GÁN | HẠN SỬ DỤNG  | GIÁ BÁN   | THÀNH TIỀN | VỊ TRÍ KHO |
|--------------------------+----+-------------------+--------------+-----------+------------+------------|
| Tinh chất B5 La Roche    | 01 | LOT-2024-C12      | 28/02/2027   | 650.000 đ | 650.000 đ  | Kệ A2 - T3 |
| Dung dịch BHA Paula 30ml | 01 | LOT-2024-D05      | 10/04/2027   | 399.000 đ | 399.000 đ  | Kệ B1 - T2 |
+--------------------------------------------------------------------------------------------------------+
| TỔNG GIÁ TRỊ ĐƠN HÀNG: 1.049.000 đ | VẬN ĐƠN ĐỐI TÁC: GHN-VN-8839201 | [ IN PHIẾU ĐÓNG GÓI CÓ BARCODE ]|
| [ XÁC NHẬN ĐÓNG GÓI XONG ]  [ HOÀN TẤT GIAO CHO KHÁCH (NHẬP PIN) ]   [ HỦY ĐƠN HOÀN TRẢ TỒN KHO ]       |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Khối thông tin hành trình đơn hàng đa kênh (Omnichannel Order Context):** Hiển thị rõ ràng mã đơn hàng kèm huy hiệu phân loại kênh tiếp nhận: Kênh Online giao tận nhà, Kênh O2O Click-and-Collect (khách đặt online nhận tại showroom) hoặc Kênh mua trực tiếp. Đối với đơn O2O, màn hình làm nổi bật địa chỉ showroom khách đăng ký đến lấy và ô hiển thị mã PIN 6 số bí mật được hệ thống sinh ngẫu nhiên khi khách đặt đơn thành công.
- **Bảng cấp phát lô đích danh FEFO (Batch Allocation Table):** Chi tiết hóa từng dòng sản phẩm với số lượng đặt, đơn giá, và đặc biệt là cột Mã số lô sản xuất `BatchCode` cùng Hạn sử dụng mà động cơ `InventoryAllocationEngine` đã tự động gán đích danh khi đơn hàng được tạo. Cột "Vị trí kho" (ví dụ: Kệ A2 - Tầng 3) chỉ dẫn chính xác vị trí ngăn kệ vật lý trong kho giúp nhân viên nhặt hàng (picker) lấy đúng lô cận hạn nhất mà không tốn thời gian tìm kiếm.
- **Thanh tác vụ đóng gói và in ấn:** Nút bấm "In phiếu đóng gói có Barcode" cho phép xuất ra mẫu phiếu nhặt hàng chuẩn khổ A5 có in kèm mã vạch số lô; nút xác nhận hoàn tất giao hàng yêu cầu nhân viên đối chiếu mã PIN của khách trước khi đóng đơn hàng.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi khách hoàn tất thanh toán trên website, `OrderProcessingService` mở một giao dịch cơ sở dữ liệu và gọi động cơ `InventoryAllocationEngine.AllocateBatchFefoAsync()`. Thuật toán rà soát bảng `ProductBatches` của chi nhánh xuất hàng, tìm các lô còn hạn dùng xa hơn ngưỡng an toàn của đơn online (tối thiểu 60 ngày) và sắp xếp tăng dần theo `ExpDate`. Hệ thống ghi nhận số lượng giữ hàng vào trường `OrderDetail.AllocatedBatchId` và trừ lùi `AvailableQuantity` của lô tương ứng để chống bán đè (Race Condition). Khi khách hàng đến showroom đọc mã PIN, thu ngân nhập mã vào ô xác thực trên màn hình. Backend gọi phương thức kiểm tra mã PIN băm; nếu khớp, hệ thống chuyển trạng thái đơn hàng sang `Completed`, kích hoạt dịch vụ tài chính `FinanceService` tự động ghi nhận bút toán doanh thu và xuất kho giá vốn hàng bán (COGS) vào sổ cái.

---

### 3.2.4. Phân hệ Kiosk Bán lẻ tại Quầy (POS & Quản lý Ca bán hàng)

Phân hệ POS là công cụ làm việc liên tục của đội ngũ Thu ngân tại các Showroom, đòi hỏi tốc độ xử lý tức thì, tính tiện dụng công thái học cao và sự kiểm soát tài chính tuyệt đối chống thất thoát tiền mặt.

#### 9. Màn hình Bán hàng Kiosk POS Đa Năng (Smart POS Kiosk & Dual CFD)

Màn hình bán hàng POS được thiết kế dành riêng cho môi trường làm việc cường độ cao tại quầy thu ngân, hỗ trợ thao tác hoàn toàn bằng phím tắt hoặc màn hình cảm ứng khổ lớn mà không cần sử dụng chuột.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.9 - GIAO DIỆN MÀN HÌNH BÁN HÀNG TẠI QUẦY POS]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_9_Giao_Dien_Man_Hinh_Ban_Hang_POS.png`  
> *Mô tả hình:* Ảnh chụp thực tế Màn hình Bán hàng Kiosk POS tại quầy Showroom OMNI BEAUTY gồm bố cục cố định 100vh không cuộn trang, danh sách giỏ hàng bán lẻ tự động nhận diện số lô FEFO, bảng phím tắt F1-F12, khung thanh toán quét mã VietQR Napas247 động và cơ chế đồng bộ màn hình phụ khách hàng CFD.

```
+--------------------------------------------------------------------------------------------------------+
| SMART POS - OMNI BEAUTY | Ca: SÁNG (Thu ngân: Trần Thu Trang) | Showroom: CẦU GIẤY | [ĐÓNG CA (F10)]   |
+-------------------------------------------------------------------+------------------------------------+
| [F1] QUÉT MÃ VẠCH / TÌM SẢN PHẨM: [ 8935028401923             ]  | [F2] KHÁCH HÀNG: [ 0912.345.678  ] |
+-------------------------------------------------------------------+ Tên: Lê Hoàng Yến - Hạng: KIM CƯƠNG|
| GIỎ HÀNG BÁN LẺ (HÓA ĐƠN HIỆN TẠI)                                | Điểm tích lũy: 1.250 điểm          |
| STT | TÊN MỸ PHẨM            | LÔ XUẤT (FEFO) | SL | ĐƠN GIÁ | T.TIỀN   | ---------------------------------- |
| 01  | Kem Nắng Anessa 60ml   | LOT-2024-B02   | 01 | 580.000 | 580.000  | TỔNG TIỀN HÀNG:        930.000 đ   |
| 02  | Sữa Rửa Mặt Cerave 236ml| LOT-2024-F11  | 01 | 350.000 | 350.000  | Giảm giá thành viên:   -46.500 đ   |
|     |                        |                |    |         |          | Tiêu điểm (200 điểm):  -20.000 đ   |
|     |                        |                |    |         |          | KHÁCH PHẢI TRẢ:        863.500 đ   |
|     |                        |                |    |         |          | ---------------------------------- |
|     |                        |                |    |         |          | HÌNH THỨC THANH TOÁN NHANH:       |
|     |                        |                |    |         |          | [ TIỀN MẶT ]  [ THẺ NGÂN HÀNG ]    |
|     |                        |                |    |         |          | [ VIETQR ĐỘNG (NAPAS247) (F4) ]    |
|     |                        |                |    |         |          | [ F12: THANH TOÁN & IN HÓA ĐƠN ]   |
+-------------------------------------------------------------------+------------------------------------+
| THANH PHÍM TẮT CHỨC NĂNG: [F1: Tìm hàng] [F2: Khách] [F3: Chiết khấu] [F4: VietQR] [F8: Giữ đơn] [F12: Thu]   |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Bố cục khung nhìn cố định toàn màn hình (Fixed 100vh Layout):** Giao diện khóa hoàn toàn thanh cuộn trình duyệt (`overflow: hidden`), loại bỏ mọi hiện tượng nhảy trang gây mất tập trung. Khung màn hình được chia tỷ lệ vàng: 60% bên trái dành cho danh sách hàng hóa trong hóa đơn và 40% bên phải dành cho khối nhận diện khách hàng và bảng điều khiển thanh toán.
- **Hệ thống phím chức năng tiêu chuẩn (Standard Hotkeys Bar):** Toàn bộ các thao tác nghiệp vụ đều được gắn phím tắt phần cứng chuyên biệt từ `F1` đến `F12`: `F1` (Tập trung vào ô quét mã vạch), `F2` (Tìm kiếm khách hàng theo số điện thoại), `F3` (Nhập chiết khấu hóa đơn), `F4` (Bật cổng thanh toán VietQR động), `F8` (Tạm lưu hóa đơn để phục vụ khách kế tiếp trong giờ cao điểm), `F12` (Xác nhận hoàn tất thanh toán và kích hoạt máy in bill nhiệt).
- **Khung chọn phương thức thanh toán đa dạng:** Cung cấp 3 nút bấm kích thước siêu lớn cho các phương thức thanh toán phổ biến nhất hiện nay: Tiền mặt, Thẻ ngân hàng cà qua máy POS và Mã QR Động VietQR chuẩn Napas247.
- **Tích hợp kênh truyền thông màn hình phụ khách hàng (Dual Customer-Facing Display - CFD):** Hệ thống sử dụng API trình duyệt chuẩn `BroadcastChannel("pos_customer_display")`. Ngay khi thu ngân quét một sản phẩm trên màn hình chính, màn hình phụ quay về phía khách hàng lập tức hiển thị tên sản phẩm, số tiền và mã QR thanh toán động với đúng số tiền lẻ đến từng đồng, mang lại trải nghiệm minh bạch và chuyên nghiệp.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi máy quét mã vạch đọc được mã Barcode sản phẩm, JavaScript POS bắt sự kiện bàn phím và tự động phát lệnh gọi API `POST /Pos/ScanItem`. Backend thực hiện hai nhiệm vụ song song: tra cứu thông tin sản phẩm và ngay lập tức gọi `InventoryAllocationEngine.AllocateBatchFefoAsync(productId, storeId, 1)` để gán số lô có hạn sử dụng gần nhất hiện có trong quầy hàng vào dòng đơn. Dữ liệu dòng đơn được phản hồi về trình duyệt và kết xuất vào bảng hóa đơn chỉ trong 12 mili-giây. Khi thu ngân bấm `F4` để chọn thanh toán VietQR, JavaScript gửi yêu cầu tới `PaymentController.GenerateVietQr()`. Máy chủ tính toán chuỗi dữ liệu theo định dạng EMVCo bao gồm mã số ngân hàng thụ hưởng, số tài khoản đích danh của cửa hàng và số tiền chính xác `863.500 đ`, tạo chuỗi ảnh Base64 của mã QR và đồng thời phát thông điệp sang màn hình phụ CFD để khách hàng dùng ứng dụng ngân hàng quét thanh toán ngay lập tức.

---

#### 10. Màn hình Quản lý Ca Bán Hàng & Kiểm Toán Két Mù POS (Shift Management & Blind Cash Audit)

Giao diện quản lý ca bán hàng hiện thực hóa quy trình kiểm toán két mù (Blind Cash Drawer Audit) nghiêm ngặt nhất trong ngành bán lẻ chuỗi, loại bỏ hoàn toàn khả năng thu ngân gian lận hoặc bù trừ tiền mặt chênh lệch.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.10 - GIAO DIỆN ĐÓNG CA VÀ KIỂM TOÁN KẾT MÙ POS]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_10_Giao_Dien_Dong_Mo_Ca_Va_Doi_Soat_Ket.png`  
> *Mô tả hình:* Ảnh chụp thực tế Màn hình Đóng ca và Kiểm toán Két tiền Mù POS tại OMNI BEAUTY gồm bảng kê kiểm đếm chi tiết 9 mệnh giá tiền mặt Việt Nam (từ 1.000đ đến 500.000đ), cơ chế ẩn doanh thu hệ thống trước khi nộp tiền, và bảng đối soát tài chính sau khóa ca thể hiện chênh lệch thừa/thiếu tiền mặt kèm yêu cầu giải trình bắt buộc.

```
+--------------------------------------------------------------------------------------------------------+
| ĐÓNG CA BÁN HÀNG & KIỂM ĐẾM TIỀN MẶT KÉT THU NGÂN (BLIND AUDIT) | Ca: SÁNG (08:00 - 15:30)             |
+--------------------------------------------------------------------------------------------------------+
| ⚠️ NGUYÊN TẮC KỂM TOÁN KÉT MÙ: Hệ thống KHÔNG hiển thị doanh thu phần mềm ghi nhận trong ca.            |
| Thu ngân bắt buộc phải kiểm đếm thực tế toàn bộ tiền mặt trong két và nhập đúng số tờ theo từng mệnh giá!|
+--------------------------------------------------------------------------------------------------------+
| BẢNG KÊ KIỂM ĐẾM TỪNG MỆNH GIÁ TIỀN MẶT VIỆT NAM (CASH DENOMINATION BREAKDOWN)                         |
| MỆNH GIÁ (VNĐ) | SỐ TỜ THỰC TẾ | THÀNH TIỀN (VNĐ) || MỆNH GIÁ (VNĐ) | SỐ TỜ THỰC TẾ | THÀNH TIỀN (VNĐ) |
| 500.000 đ      | [    12     ] |   6.000.000 đ    || 20.000 đ       | [    15     ] |     300.000 đ    |
| 200.000 đ      | [    25     ] |   5.000.000 đ    || 10.000 đ       | [    20     ] |     200.000 đ    |
| 100.000 đ      | [    30     ] |   3.000.000 đ    || 5.000 đ        | [    10     ] |      50.000 đ    |
| 50.000 đ       | [    18     ] |     900.000 đ    || 1.000 đ / 2.000 | [    15     ] |      25.000 đ    |
+--------------------------------------------------------------------------------------------------------+
| TỔNG TIỀN MẶT THỰC TẾ TRONG KÉT (ACTUAL CASH AT CLOSE): 15.475.000 đ                                   |
| [ XÁC NHẬN NỘP TIỀN & KHÓA SỔ CA BÁN HÀNG ]                                                           |
+--------------------------------------------------------------------------------------------------------+
| KẾT QUẢ ĐỐI SOÁT TỰ ĐỘNG CỦA HỆ THỐNG (HIỂN THỊ SAU KHI ĐÃ KHÓA SỔ VĨNH VIỄN):                         |
| - Tiền mặt đầu ca (Bàn giao):                 2.000.000 đ                                              |
| - Doanh thu tiền mặt phần mềm ghi nhận:      13.500.000 đ                                              |
| - Tổng tiền mặt hệ thống kỳ vọng (Expected): 15.500.000 đ                                              |
| - Tổng tiền mặt thực tế kiểm đếm (Actual):   15.475.000 đ                                              |
| - CHÊNH LỆCH KÉT TIỀN (DISCREPANCY):           -25.000 đ (THIẾU HỤT - ĐÁNH DẤU CẢNH BÁO MÀU ĐỎ)        |
| Ghi chú giải trình của Thu ngân: [ Bù 25.000 đ do thối nhầm tiền lẻ cho khách đơn #8921 ]            |
+--------------------------------------------------------------------------------------------------------+
```

##### (1) Bố cục công thái học và thành phần giao diện
- **Thông điệp thực thi Quy chế Két Mù (Blind Audit Mandate):** Được trình bày trang trọng trong hộp cảnh báo màu vàng phía trên cùng. Giao diện kiên quyết ẩn hoàn toàn con số tổng doanh thu phần mềm tính toán được trong ca trực, buộc nhân viên thu ngân phải tự giác kiểm đếm số tiền vật lý thực tế trong ngăn kéo và nhập vào bảng số tờ.
- **Bảng kê mệnh giá tiền mặt Việt Nam hai cột đối xứng (Two-Column Denomination Grid):** Phân bổ khoa học 9 mệnh giá tiền cotton và polymer đang lưu hành tại Việt Nam (từ tờ 1.000 đồng đến tờ 500.000 đồng). Mỗi ô nhập liệu đều có khả năng tự động tính toán thành tiền tức thì bằng JavaScript khi người dùng thay đổi số lượng tờ, hiển thị tổng tiền mặt thực tế kiểm đếm (`ActualCashAtClose`) bằng phông chữ nổi bật ở chân bảng.
- **Khung đối soát tự động sau khóa sổ (Post-Closing Reconciliation Panel):** Chỉ xuất hiện duy nhất sau khi thu ngân đã nhấn nút xác nhận khóa ca. Khung thể hiện rõ ràng phép tính đối soát chuẩn mực kế toán: `ExpectedCashAtClose = InitialCashBalance + CashSalesAmount - CashRefundAmount`. Số tiền chênh lệch `DiscrepancyAmount` được tô màu trực quan: màu xanh nếu số liệu khớp hoàn toàn (bằng 0), màu vàng nếu thừa tiền (> 0) và màu đỏ rực rỡ nếu thiếu hụt (< 0) đi kèm ô bắt buộc nhập lý do giải trình trước khi nộp báo cáo cho Cửa hàng trưởng.

##### (2) Luồng tương tác người dùng và cơ chế Backend
Khi thu ngân hoàn thành nhập bảng kê và bấm nút xác nhận khóa sổ, phương thức `PosShiftController.CloseShift()` được triệu gọi với dữ liệu phân rã mệnh giá. Dịch vụ `PosShiftService` kích hoạt một giao dịch cơ sở dữ liệu `BeginTransactionAsync()`. Máy chủ tính toán tổng doanh thu tiền mặt thực tế từ các đơn hàng hoàn tất trong ca trực, cộng với số dư bàn giao đầu ca để xác định `ExpectedCashAtClose`. Sau đó, hệ thống trừ đi con số `ActualCashAtClose` do thu ngân vừa khai báo để tính toán `DiscrepancyAmount`. Bản ghi ca bán hàng trong bảng `PosShifts` được chuyển trạng thái sang `CLOSED`, lưu vết thời gian kết thúc chính xác đến từng giây và ghi nhận người ký duyệt. Bút toán chênh lệch két tiền nếu có sẽ được chuyển sang dịch vụ `FinanceService` để tự động hạch toán vào tài khoản tổn thất hoặc thu nhập khác, bảo đảm tính minh bạch tài chính tuyệt đối của toàn chuỗi cửa hàng OMNI BEAUTY.

---

### TIỂU KẾT MỤC 3.2

Mục 3.2 đã tái hiện một cách sống động, chân thực và toàn diện kết quả hiện thực hóa 10 màn hình giao diện cốt lõi của hệ thống website bán hàng OMNI BEAUTY. Từ trải nghiệm mua sắm trực tuyến tinh tế, an toàn cho làn da trên Storefront khách hàng đến các công cụ quản trị sắc bén, chuẩn mực công thái học tại tháp điều hành Admin, kho vận FEFO và kiosk bán lẻ POS, hệ thống chứng minh sự trưởng thành vượt bậc về mặt năng lực thiết kế phần mềm doanh nghiệp, đáp ứng trọn vẹn mọi yêu cầu chức năng và nghiệp vụ đã đề ra trong bài toán tổng thể.
