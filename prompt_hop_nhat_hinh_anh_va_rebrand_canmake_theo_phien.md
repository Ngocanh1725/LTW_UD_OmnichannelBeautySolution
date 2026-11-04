# BỘ PROMPT THEO PHIÊN: HỢP NHẤT HÌNH ẢNH & ĐỔI THƯƠNG HIỆU "OMNI BEAUTY" → "CANMAKE BEAUTY"

> **Cách dùng:** Gửi lần lượt từng phiên cho AI. Mỗi phiên có **ĐIỂM DỪNG XÁC NHẬN** – AI phải dừng, báo cáo và chờ bạn trả lời "Đồng ý / Proceed" rồi mới làm tiếp. Không gộp nhiều phiên vào một lần gửi.
>
> **Hiện trạng đã khảo sát sơ bộ (để AI đối chiếu, không tin tuyệt đối):**
> - `Omnichannel.Web/wwwroot/images/products/` – 90 ảnh, chia 4 thư mục con: `skincare` (38), `makeup` (27), `perfume` (15), `hair-body` (10). **Đây là thư mục chuẩn duy nhất được giữ lại.**
> - `Omnichannel.Web/wwwroot/images/real_products/` – 84 ảnh (bản sao, cần xóa).
> - `Real_Products_Images/` (gốc solution) – 84 ảnh (bản tải gốc, cần xóa).
> - Code còn tham chiếu `real_products`: `Controllers/CartController.cs` (dòng ~135, ~149), `Views/Cart/Index.cshtml` (dòng ~271–274), `scripts/download_and_rename_products.ps1` (dòng 7, 140–141).
> - Code còn tham chiếu ảnh **đường dẫn phẳng cũ có thể không tồn tại**: `/images/products/serum-b5.jpg`, `perfume-jazz-club.jpg`, `clinique-moisture.jpg`, `toner-bha.jpg`, `cleanser-cerave.jpg`, `skinceuticals-b5.jpg`, `mac-chili.jpg`, `sunscreen-anessa.jpg` (trong `CustomerController.cs` ~1051–1112, `DbInitializer.cs` ~2957, ~5376).
> - Nguồn dữ liệu ảnh: `Omnichannel.Infrastructure/Data/product_image_registry.json` + `ProductImageSourceRegistry.cs` + bảng `MediaAssets` / cột ảnh của `Products` trong SQL Server (DbInitializer log: 123 MediaAssets, 97 sản phẩm đã gắn ảnh).
> - **Lỗi nội dung ảnh đã phát hiện:** file `xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg` thực tế là ảnh **nước hoa Chanel Coco Mademoiselle** → tên file/sản phẩm và nội dung ảnh không khớp.
> - Tên thương hiệu cũ xuất hiện ở ~55 file ngoài `doc/` (các biến thể: `Omni Beauty`, `OMNI BEAUTY`, `OmniBeauty`, `omnibeauty`), gồm Views, Layout, CSS, ThemeService, DbInitializer, PaymentService, GhnShippingService, Tests, cookie `OmniBeauty.Admin.Session` / `OmniBeauty.Customer.Session`.
> - Logo hiện tại: `wwwroot/images/branding/` (`logo-full.jpg`, `logo-icon.jpg`, `logo-circle.png`, `favicon-32x32.png`, `favicon-192x192.png`, `favicon-512x512.png`, `apple-touch-icon.png`, …).

---

## PHIÊN 0 – BỐI CẢNH, VAI TRÒ & NGUYÊN TẮC BẮT BUỘC

```text
Bạn là Senior Full-stack Engineer (.NET 8, ASP.NET Core MVC, EF Core, SQL Server, Tailwind/Alpine/HTMX)
kiêm Brand/UI Designer, đang làm việc trên solution Clean Architecture:
D:\Lập trình web\OmnichannelBeautySolution (Domain / Application / Infrastructure / Web / Tests).

NHIỆM VỤ TỔNG: (1) hợp nhất toàn bộ ảnh sản phẩm về DUY NHẤT thư mục
Omnichannel.Web\wwwroot\images\products, xóa 2 thư mục trùng lặp; (2) đồng bộ ảnh – dữ liệu trên toàn
hệ thống; (3) đổi tên thương hiệu hiển thị từ "OMNI BEAUTY" sang "CANMAKE BEAUTY"; (4) thiết kế logo
mới tông hồng và áp dụng đầy đủ.

NGUYÊN TẮC BẮT BUỘC (áp dụng cho mọi phiên):
1. An toàn trước: tạo nhánh git riêng `feature/canmake-rebrand-images` và commit trước mỗi phiên có sửa
   code. KHÔNG xóa file/thư mục, KHÔNG sửa dữ liệu DB khi chưa được tôi xác nhận.
2. Thay đổi ảnh/brand chỉ ở tầng HIỂN THỊ & DỮ LIỆU. GIỮ NGUYÊN định danh kỹ thuật: namespace
   `Omnichannel.*`, tên project/.csproj/.sln, tên database `OmnichannelBeautyDb`, tên bảng/cột,
   route API. (Lý do: đổi các định danh này gây vỡ build, migration và không mang lại giá trị cho
   người dùng.)
3. Không tạo Migration mới nếu không thật cần; nếu cần thay dữ liệu DB → viết script/seed cập nhật
   idempotent (chạy nhiều lần không sai) và xin xác nhận trước.
4. Không hardcode đường dẫn ảnh rải rác: mọi đường dẫn ảnh sản phẩm phải đi qua 1 nguồn chuẩn
   (DB/registry + 1 helper/resolver duy nhất có ảnh fallback).
5. Giữ nguyên comment/docstring không liên quan. Tiếng Việt UTF-8, không lỗi font.
6. Sau mỗi phiên có sửa code: `dotnet build OmnichannelBeautySolution.sln -c Release` (0 Error) và
   `dotnet test` (100% pass, hiện tại 317 tests). Nếu test assert chuỗi brand cũ → cập nhật test
   cho đúng brand mới, KHÔNG xóa test.
7. Mỗi phiên kết thúc bằng: báo cáo ngắn (đã làm / file đã sửa / kết quả build-test / rủi ro còn lại)
   và cập nhật `doc/nang-cap/00_tien_do.md` (thêm nhóm phiên "F – Hình ảnh & Rebrand").

Xác nhận đã hiểu bằng cách tóm tắt lại 7 nguyên tắc trong 7 dòng, rồi chờ tôi gửi Phiên 1.
```

---

## PHIÊN 1 – KIỂM KÊ & ĐỐI CHIẾU HÌNH ẢNH (CHỈ ĐỌC, KHÔNG SỬA)

```text
PHIÊN 1: KIỂM KÊ HÌNH ẢNH TOÀN HỆ THỐNG (READ-ONLY)

1. Quét 3 thư mục và lập bảng đối chiếu theo HASH NỘI DUNG (SHA-256), không chỉ theo tên file:
   - A = Omnichannel.Web\wwwroot\images\products\** (chuẩn)
   - B = Omnichannel.Web\wwwroot\images\real_products\
   - C = Real_Products_Images\
   Mỗi ảnh ghi: tên file, thư mục, kích thước (KB), độ phân giải (px), định dạng, hash.
   Phân loại:
   (a) Có trong B/C và đã có bản giống hệt trong A → an toàn để xóa.
   (b) Có trong B/C nhưng KHÔNG có trong A (khác hash) → phải đưa vào A trước khi xóa
       (đề xuất thư mục con skincare/makeup/perfume/hair-body và tên slug chuẩn).
   (c) Trùng nội dung nhưng khác tên (cùng hash, nhiều tên) → trùng lặp.
   (d) Cùng tên nhưng khác nội dung giữa A/B/C → xung đột, cần tôi chọn bản giữ.

2. Quét MỌI tham chiếu ảnh trong code & dữ liệu:
   - `git grep` các chuỗi: `real_products`, `Real_Products_Images`, `/images/`, `.jpg`, `.png`,
     `.webp` trong *.cs, *.cshtml, *.js, *.css, *.json, *.ps1.
   - `product_image_registry.json`, `ProductImageSourceRegistry.cs`, `DbInitializer.cs`.
   - Truy vấn SQL (chỉ SELECT) bảng `Products`, `ProductVariants`, `MediaAssets`, `Banners`,
     `Categories`, `ProductReviews.EvidenceUrlsJson` … để lấy toàn bộ URL ảnh đang lưu trong DB.
   Lập bảng: URL tham chiếu → file có tồn tại không → nơi tham chiếu (file#dòng hoặc bảng/Id).

3. Phát hiện lỗi đồng bộ:
   - Tham chiếu gãy (404): URL trỏ tới file không tồn tại (ví dụ `/images/products/serum-b5.jpg`).
   - Ảnh mồ côi: file trong A không được sản phẩm/nơi nào dùng.
   - 1 ảnh dùng cho nhiều sản phẩm khác nhau (ảnh dùng chung sai).
   - Sản phẩm không có ảnh / dùng ảnh placeholder.
   - ẢNH SAI NỘI DUNG: mở xem trực quan từng ảnh, so với tên sản phẩm/brand (đã biết:
     `xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg` thực chất là Chanel Coco
     Mademoiselle). Liệt kê toàn bộ ca tương tự.
   - Lệch giữa registry JSON ↔ DB ↔ file vật lý (slug, SKU, fileName, categorySubfolder).
   - Ảnh quá nặng (> 300 KB) hoặc quá nhỏ (< 600px cạnh ngắn), sai tỷ lệ so với khung product card.

4. Đầu ra: tạo `doc/nang-cap/06_kiem_ke_hinh_anh.md` gồm: số liệu tổng, 4 bảng phân loại (a–d),
   bảng tham chiếu gãy, bảng ảnh sai nội dung, bảng ảnh mồ côi, và KẾ HOẠCH HỢP NHẤT đề xuất
   (file nào copy/đổi tên vào A, tham chiếu nào sửa thành gì).

ĐIỂM DỪNG: Trình bày tóm tắt + các ca (b) và (d) cần tôi quyết định. KHÔNG sửa/xóa gì.
```

---

## PHIÊN 2 – HỢP NHẤT VỀ `images/products` & XÓA 2 THƯ MỤC TRÙNG

```text
PHIÊN 2: HỢP NHẤT ẢNH VỀ wwwroot\images\products VÀ XÓA THƯ MỤC TRÙNG LẶP
(Thực hiện theo kế hoạch đã duyệt ở Phiên 1.)

1. Đưa ảnh còn thiếu vào thư mục chuẩn:
   - Copy các ảnh loại (b) vào đúng `images/products/{skincare|makeup|perfume|hair-body}/`.
   - Quy tắc tên file: slug tiếng Việt không dấu, chữ thường, gạch nối, khớp `Product.Slug`,
     ví dụ `serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg`. Không khoảng trắng, không ký tự đặc biệt.
   - Xử lý ca (d) theo lựa chọn của tôi.

2. Cập nhật toàn bộ tham chiếu sang đường dẫn chuẩn `/images/products/{sub}/{file}`:
   - `CartController.cs`, `Views/Cart/Index.cshtml` (bỏ hardcode `real_products`; dùng ảnh thật của
     item, fallback qua helper chung).
   - `CustomerController.cs`, `DbInitializer.cs` (các đường dẫn phẳng cũ gãy).
   - `product_image_registry.json` (fileName + categorySubfolder đúng thực tế).
   - `scripts/download_and_rename_products.ps1`: đổi thư mục đích về `images/products/{sub}`,
     XÓA bước "đồng bộ bản sao vào real_products" để không sinh lại thư mục trùng.

3. Cập nhật dữ liệu DB (sau khi tôi duyệt script):
   - Viết script SQL hoặc hàm seed idempotent cập nhật URL ảnh trong `Products`, `ProductVariants`,
     `MediaAssets`, `ProductReviews.EvidenceUrlsJson`… từ `/images/real_products/...` hoặc đường dẫn
     gãy → đường dẫn chuẩn. Báo trước số dòng bị ảnh hưởng (SELECT COUNT) rồi mới UPDATE trong transaction.
   - Đảm bảo `DbInitializer` khi chạy lại KHÔNG ghi đè ngược về đường dẫn cũ.

4. Chỉ khi bước 1–3 xong và `git grep -i "real_products\|Real_Products_Images"` trả về 0 kết quả
   trong code (ngoài tài liệu lịch sử), mới XÓA:
   - D:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Web\wwwroot\images\real_products
   - D:\Lập trình web\OmnichannelBeautySolution\Real_Products_Images
   Kiểm tra `.csproj`/`.gitignore` không còn include/exclude 2 thư mục này.

5. Kiểm thử:
   - Build Release + dotnet test.
   - Chạy web, viết script PowerShell quét tất cả URL ảnh (từ DB + HTML render của trang chủ,
     /san-pham, 5 PDP, /cart, /checkout, /clearance-fefo, /so-sanh, /Admin/Product, /Admin/Pos)
     → yêu cầu 0 ảnh trả 404.

ĐIỂM DỪNG 1 (trước bước 3): trình script SQL + số dòng ảnh hưởng.
ĐIỂM DỪNG 2 (trước bước 4): xác nhận xóa 2 thư mục.
```

---

## PHIÊN 3 – ĐỒNG BỘ ẢNH ↔ DỮ LIỆU, SỬA TRÙNG LẶP & TỐI ƯU

```text
PHIÊN 3: ĐỒNG BỘ HÌNH ẢNH – DỮ LIỆU VÀ TỐI ƯU HIỂN THỊ

1. Một nguồn sự thật duy nhất cho ảnh sản phẩm:
   - Tạo/chuẩn hóa 1 service hoặc helper (ví dụ `IProductImageResolver` ở Application, triển khai ở
     Infrastructure/Web) trả về URL ảnh chính, gallery, thumbnail và ảnh fallback
     `/images/products/_placeholder.webp` (tạo mới, tông hồng nhạt có logo mờ).
   - Thay thế mọi đoạn tự ghép chuỗi đường dẫn ảnh trong Views/Controllers/ViewComponents
     (product card, PDP, mini cart, cart, checkout, order detail, POS, wishlist, compare, quick view,
     search suggest, recently viewed, email/print) bằng helper này.

2. Sửa lỗi nội dung & trùng lặp (theo bảng Phiên 1):
   - Ảnh sai nội dung (vd Bath & Body Works ↔ Chanel): đổi tên file cho đúng sản phẩm thực tế trong
     ảnh, hoặc gán lại đúng sản phẩm; sản phẩm nào không còn ảnh đúng → dùng placeholder và liệt kê
     danh sách "cần bổ sung ảnh thật" cho tôi (KHÔNG tự tải ảnh từ Internet nếu chưa được phép).
   - Một ảnh bị dùng cho nhiều sản phẩm khác nhau → tách hoặc dùng placeholder.
   - Xóa ảnh mồ côi trong `images/products` (sau khi liệt kê và tôi duyệt).
   - Đồng bộ registry JSON ↔ DB ↔ file vật lý: slug, SKU, brand, category, alt text.

3. Tối ưu hiệu năng & SEO ảnh:
   - Chuẩn hóa ảnh sản phẩm: nền sáng, tỷ lệ 1:1, ~1000×1000px; xuất thêm bản WebP (giữ JPG/PNG gốc
     làm fallback qua <picture>), mục tiêu ≤ 150 KB/ảnh. Dùng công cụ cục bộ (ImageMagick / sharp /
     SkiaSharp) – ghi rõ lệnh đã dùng vào scripts/optimize_images.ps1.
   - `loading="lazy"` + `decoding="async"` cho ảnh dưới màn hình đầu, `fetchpriority="high"` cho ảnh
     LCP (hero, ảnh chính PDP); luôn có `width`/`height` để tránh CLS.
   - Alt text tiếng Việt có tên sản phẩm + thương hiệu. Schema.org Product `image` trỏ URL tuyệt đối đúng.
   - Rà soát luôn `images/banners` và `images/categories`: tham chiếu gãy, ảnh nặng, trùng lặp.

4. Đầu ra: cập nhật `doc/nang-cap/06_kiem_ke_hinh_anh.md` phần "Kết quả sau hợp nhất" (trước/sau:
   số file, dung lượng tổng, số tham chiếu gãy, số ảnh sai nội dung còn lại).
   Build + test 100% + quét 0 ảnh 404.

ĐIỂM DỪNG: trình danh sách ảnh mồ côi định xóa và danh sách sản phẩm cần bổ sung ảnh thật.
```

---

## PHIÊN 4 – PHÂN TÍCH ĐỔI THƯƠNG HIỆU SANG "CANMAKE BEAUTY" (CHỈ ĐỌC)

```text
PHIÊN 4: RÀ SOÁT & LẬP KẾ HOẠCH REBRAND "OMNI BEAUTY" → "CANMAKE BEAUTY" (READ-ONLY)

1. Quét toàn bộ biến thể tên cũ (không phân biệt hoa thường): "Omni Beauty", "OMNI BEAUTY",
   "OmniBeauty", "omnibeauty", "Omni-Beauty", "OMNI", "Omnichannel Beauty", "omnichannelbeauty.vn",
   cả trong: *.cs, *.cshtml, *.js, *.css, *.json, appsettings*.json, manifest/site.webmanifest,
   robots/sitemap, email template, PDF/hóa đơn in (PrintReceipt, PrintShippingLabel), prompt của
   Chatbot, SignalR message, Audit log text, seed DbInitializer, Tests, README, TEST_ACCOUNTS.
   Và trong DB: ThemeConfig/SiteSettings (SiteName, Slogan, Logo URL, Footer), Banner, Post, Menu,
   email người dùng seed (`@omnichannelbeauty.vn`), mã voucher/đơn có tiền tố OMNI (`OMNI-PICKUP|`…).

2. Phân loại từng vị trí vào 4 nhóm và đề xuất xử lý:
   - NHÓM 1 – Hiển thị cho người dùng (title, header, footer, meta, OG, schema.org Organization,
     email, hóa đơn, chatbot, thông báo): ĐỔI sang "CANMAKE BEAUTY" (dạng viết thường hợp lý:
     "Canmake Beauty" trong câu văn; "CANMAKE BEAUTY" trong logo/tiêu đề lớn).
   - NHÓM 2 – Định danh kỹ thuật (namespace Omnichannel.*, project, sln, DB name, tên bảng):
     GIỮ NGUYÊN, giải thích lý do.
   - NHÓM 3 – Định danh có tác động vận hành, cần cân nhắc: tên cookie
     (`OmniBeauty.*.Session` → `CanmakeBeauty.*.Session`: mọi phiên đăng nhập hiện tại bị đăng xuất),
     tiền tố mã QR nhận hàng `OMNI-PICKUP|` (phải giữ tương thích đọc mã cũ), localStorage key,
     email domain tài khoản seed (ảnh hưởng đăng nhập & TEST_ACCOUNTS), CSS class/biến có chữ omni.
     → đề xuất phương án + mức rủi ro, chờ tôi chọn.
   - NHÓM 4 – Tài liệu lịch sử trong doc/ (báo cáo khóa luận, ảnh chụp màn hình): chỉ đổi trong tài
     liệu đang dùng (README, TEST_ACCOUNTS, kịch bản demo); tài liệu lưu trữ ghi chú "tên cũ".

3. Lưu ý pháp lý cần nêu trong báo cáo: "CANMAKE" (CANMAKE TOKYO – IDA Laboratories, Nhật Bản) là
   thương hiệu mỹ phẩm đã đăng ký. Với đồ án học thuật có thể dùng nhưng logo mới PHẢI là thiết kế
   độc lập, không sao chép logo/font/bao bì CANMAKE TOKYO; ghi chú miễn trừ trong footer/README nếu
   triển khai công khai.

4. Đầu ra: `doc/nang-cap/07_ke_hoach_rebrand_canmake.md` – bảng (file#dòng | chuỗi cũ | chuỗi mới |
   nhóm | rủi ro), tổng số vị trí theo nhóm, danh sách test sẽ bị ảnh hưởng.

ĐIỂM DỪNG: chờ tôi chốt phương án Nhóm 3.
```

---

## PHIÊN 5 – THIẾT KẾ LOGO "CANMAKE BEAUTY" TÔNG HỒNG

```text
PHIÊN 5: THIẾT KẾ BỘ NHẬN DIỆN LOGO CANMAKE BEAUTY

1. Ý nghĩa (concept) – trình bày 3 phương án phác thảo, mỗi phương án 3–4 câu ý nghĩa:
   - "CAN MAKE" = "Bạn có thể tạo nên vẻ đẹp của riêng mình": tự tin, chủ động, trẻ trung.
   - Gợi ý hình tượng: monogram chữ "C" ôm chữ "M" tạo thành hình trái tim/cánh hoa; hoặc chữ "C"
     là một nét cọ trang điểm/nét son vẽ vòng cung kèm điểm lấp lánh (sparkle) 4 cánh; hoặc giọt
     son/giọt serum cách điệu. Phải đơn giản, nhận diện tốt ở 16×16px.
   - Chữ: sans-serif bo tròn hiện đại cho "CANMAKE" (đậm), "BEAUTY" mảnh + giãn chữ (letter-spacing).
     Chỉ dùng font có giấy phép mở (Google Fonts: ví dụ Quicksand, Nunito, Poppins, Montserrat) hoặc
     chuyển chữ thành path trong SVG.

2. Bảng màu tông hồng (đề xuất, kiểm tra tương phản WCAG AA với chữ trắng/đen):
   - Primary Rose  #E8618C   - Deep Berry  #B8336A (chữ/nhấn, dark mode)
   - Blush         #F7C6D4   - Petal Cream #FFF4F7 (nền)
   - Ink           #2B1B24   (chữ chính)
   Tham chiếu và hài hòa với design token hiện có trong wwwroot/css/theme.css, design-system.css,
   tailwind.config.js.

3. Sau khi tôi chọn 1 phương án, sản xuất bộ file vào `wwwroot/images/branding/`:
   - `canmake-logo-horizontal.svg` (icon + chữ ngang, dùng cho header),
   - `canmake-logo-stacked.svg` (icon trên, chữ dưới – trang đăng nhập, hóa đơn),
   - `canmake-logo-icon.svg` (chỉ biểu tượng),
   - `canmake-logo-white.svg` (bản trắng cho nền tối/dark mode/footer),
   - `canmake-logo-mono.svg` (1 màu, cho in nhiệt hóa đơn POS 80mm và nhãn vận chuyển),
   - favicon: `favicon.ico` (16/32/48), `favicon-32x32.png`, `favicon-192x192.png`,
     `favicon-512x512.png`, `apple-touch-icon.png` (180×180), `maskable-512.png` (vùng an toàn 80%),
   - `og-image-1200x630.png` (logo + slogan trên nền hồng, dùng cho Open Graph/Zalo/Facebook share).
   SVG viết tay sạch (viewBox chuẩn, không ảnh nhúng base64, có <title> để hỗ trợ a11y);
   PNG xuất từ SVG bằng script cục bộ (sharp/ImageMagick/Inkscape) – lưu `scripts/export_brand_assets.ps1`.

4. Tạo `doc/nang-cap/08_bo_nhan_dien_canmake.md`: ý nghĩa logo, bảng màu (HEX/RGB), font, kích thước
   tối thiểu, vùng an toàn, cách dùng đúng/sai, preview tất cả biến thể (nhúng ảnh).

ĐIỂM DỪNG 1: trình 3 phương án (ảnh preview PNG) để tôi chọn.
ĐIỂM DỪNG 2: trình bộ file hoàn chỉnh để tôi duyệt trước khi áp vào hệ thống.
```

---

## PHIÊN 6 – ÁP DỤNG THƯƠNG HIỆU & LOGO MỚI TOÀN HỆ THỐNG

```text
PHIÊN 6: TRIỂN KHAI REBRAND CANMAKE BEAUTY (theo kế hoạch Phiên 4 + logo Phiên 5 đã duyệt)

1. Văn bản thương hiệu (Nhóm 1 + phương án Nhóm 3 đã chốt):
   - Tập trung tên thương hiệu về 1 chỗ cấu hình: `appsettings.json` → `Brand: { Name, ShortName,
     Slogan, LogoUrl, LogoWhiteUrl, IconUrl, Domain, Hotline, Email }` + DTO/Options, inject vào
     Layout/ViewComponent; DB ThemeConfig (SiteName/Logo) cập nhật qua seed idempotent. Tránh
     lặp chuỗi "CANMAKE BEAUTY" hardcode trong hàng chục view.
   - Cập nhật: `_Layout`, `_Header`, `_Footer`, `_AdminLayout`, `_PosLayout`, `_CmsLayout`,
     `_WarehouseLayout`, trang Account (Login/Register/Forgot/Reset), Checkout/OrderSuccess/MomoSandbox,
     Customer portal, PDP, Category, ClearanceFefo, SmartFilter, Chatbot widget + system prompt,
     PrintReceipt, PrintShippingLabel, PaymentService (nội dung chuyển khoản/VietQR), GhnShippingService
     (tên shop gửi), email template, SignalR/Audit message, `<title>`, meta description, OG tags,
     schema.org Organization/WebSite, `site.webmanifest` (name, short_name, theme_color #E8618C),
     robots/sitemap nếu có tên.
   - DbInitializer: văn bản seed (banner, bài viết, menu, review phản hồi CSKH) dùng tên mới;
     dữ liệu DB hiện có → script cập nhật idempotent.
   - Tests: cập nhật assert chuỗi brand (ThemeServiceTests, ThemeConfigControllerTests…).

2. Logo & favicon:
   - Thay toàn bộ tham chiếu `logo-full.jpg`, `logo-icon.jpg`, `logo-circle.png` sang bộ SVG mới
     (header dùng horizontal, dark mode/footer dùng white, POS & in ấn dùng mono, login dùng stacked).
   - Cập nhật <link rel="icon">, apple-touch-icon, manifest icons, og:image ở mọi layout.
   - Giữ file logo cũ trong `branding/_archive/` đến khi nghiệm thu xong rồi mới xóa (xin xác nhận).

3. Màu sắc giao diện: đồng bộ tông hồng vào design token (theme.css / design-system.css /
   tailwind.config.js: primary, accent, ring, gradient), ThemeConfig mặc định; kiểm tra dark mode
   và tương phản nút/chữ. Rebuild Tailwind CSS nếu dự án dùng bước build.

4. Không đổi namespace/sln/DB name. Sau khi xong:
   `git grep -i -I "omni beauty\|omnibeauty"` ngoài doc lưu trữ → chỉ còn các vị trí được phép
   giữ (liệt kê và giải thích từng vị trí).

5. Build Release + dotnet test 100%.

ĐIỂM DỪNG: báo cáo danh sách file đã sửa + ảnh chụp header/footer/login/POS trước–sau.
```

---

## PHIÊN 7 – NGHIỆM THU TỔNG & CẬP NHẬT TÀI LIỆU

```text
PHIÊN 7: NGHIỆM THU HỢP NHẤT ẢNH & REBRAND CANMAKE BEAUTY

1. Kỹ thuật: `dotnet build -c Release` (0 Error, thống kê Warning), `dotnet test` 100%,
   chạy lại `scripts/test_extended_smoke_e2.ps1` → 0 lỗi 500; script quét ảnh → 0 ảnh 404.
2. Trực quan (375 / 768 / 1366 / 1920px, light + dark): trang chủ, danh mục, PDP, giỏ hàng,
   checkout, order success (vé Click & Collect), customer dashboard, login, Admin dashboard, POS,
   hóa đơn in, nhãn vận chuyển: logo sắc nét, không méo/vỡ, không còn chữ "Omni Beauty",
   ảnh sản phẩm đúng nội dung, không ảnh trùng sai.
3. Kiểm tra favicon trên tab trình duyệt, ảnh chia sẻ OG (dùng meta tag), manifest hợp lệ.
4. Chạy kịch bản demo 10 phút (Demo HUD Alt + D) 1 vòng đầy đủ + Reset Demo Data → ổn định.
5. Tài liệu: cập nhật README.md (tên, logo, mô tả), TEST_ACCOUNTS.md (email nếu đổi domain),
   doc/KICH_BAN_DEMO_10_PHUT_VA_BO_HOI_DAP_PHAN_BIEN.md (tên thương hiệu, lời thuyết trình
   giới thiệu ý nghĩa logo CANMAKE), doc/nang-cap/00_tien_do.md (nhóm F: F1–F7 ✅).
6. Báo cáo cuối: bảng trước/sau (số thư mục ảnh 3 → 1, số file ảnh, dung lượng, số tham chiếu
   gãy, số vị trí brand cũ, số test), danh sách việc còn tồn (ảnh thật cần bổ sung, cookie đã đổi
   → người dùng phải đăng nhập lại…).

Tiêu chí hoàn thành: chỉ còn 1 thư mục ảnh sản phẩm `wwwroot/images/products`; 0 ảnh 404;
0 chữ "Omni Beauty" hiển thị cho người dùng; logo CANMAKE BEAUTY tông hồng hiển thị đúng ở mọi
kênh (Web, Admin, POS, in ấn, favicon, OG); build & test xanh.
```

---

### Gợi ý thứ tự gửi
`Phiên 0 → 1 → (duyệt) → 2 → (duyệt SQL, duyệt xóa) → 3 → 4 → (chốt Nhóm 3) → 5 → (chọn logo) → 6 → 7`

> Phiên 1–3 (hình ảnh) và Phiên 4–6 (thương hiệu) độc lập tương đối; nếu cần gấp logo có thể chạy 4–6 trước, nhưng **nên hợp nhất ảnh trước** để Phiên 6 không phải sửa lại các view chứa đường dẫn ảnh hai lần.

