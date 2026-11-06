# BÁO CÁO PHÂN TÍCH, ĐỐI SÁNH CHUYÊN SÂU 4 KHÓA LUẬN MẪU & ĐỀ CƯƠNG MỤC LỤC TỐI ƯU (CHUẨN ĐIỂM A+)

**Đề tài mục tiêu:** *Xây dựng website quản lý bán hàng cho chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY*  
**Ngành:** Hệ thống thông tin (Định hướng Hệ thống thông tin Quản lý & Thương mại điện tử Đa kênh)  
**Cơ sở đào tạo:** Học viện Hành chính và Quản trị công (APAG)

---

## PHẦN I: NHẬN XÉT, ĐÁNH GIÁ VÀ SO SÁNH CHI TIẾT 4 BÀI KHÓA LUẬN MẪU

Qua quá trình rà soát độc lập và đối chiếu từng trang của 4 cuốn khóa luận tốt nghiệp khóa 2022–2026 tại Học viện Hành chính và Quản trị công, với tư cách là giảng viên chấm phản biện hội đồng, tôi đưa ra đánh giá toàn diện về **cấu trúc học thuật, phương pháp luận phân tích thiết kế, chất lượng cơ sở dữ liệu và kỹ năng trình bày** của từng bài như sau:

### 1\. Nhận xét chi tiết từng bài khóa luận

#### 1.1. Khóa luận 1: Vũ Thị Minh Huyền (MSV: 2205HTTC027)

* **Đề tài:** *Xây dựng website quản lý bán hàng Công ty TNHH Thời trang Star* (168 trang)  
* **Nền tảng công nghệ:** PHP 8.x, MySQL, HTML/CSS/Tailwind CSS, JavaScript, XAMPP, Postman, Apache JMeter.  
* **Ưu điểm nổi bật (Đáng học hỏi nhất về kỹ thuật trình bày & kiểm thử):**  
  1. **Phần Mở đầu chuẩn mực tuyệt đối (8 mục):** Đầy đủ từ Lý do chọn đề tài, Tổng quan tình hình nghiên cứu (chia rõ Trong nước & Nước ngoài cập nhật đến 2025–2026 kèm số liệu báo cáo VECOM, Gartner), Mục tiêu & Nhiệm vụ, Đối tượng & Phạm vi (tách rõ Nội dung, Không gian, Thời gian, Khách thể), Phương pháp & Công cụ, **Giả thuyết nghiên cứu**, Đóng góp và Bố cục.  
  2. **Bảng Danh mục từ viết tắt & thuật ngữ chuyên ngành xuất sắc (Trang 12–13):** Thiết kế thành 3 cột (*Từ viết tắt \| Tên đầy đủ tiếng Anh \| Ý nghĩa/Giải thích gắn liền với nghiệp vụ đề tài*), thể hiện sự am hiểu sâu sắc thay vì chỉ dịch nghĩa từ điển.  
  3. **Cách trình bày Thiết kế Giải pháp (Mục 2.3.2 & 2.3.3, trang 81–100):** Không chỉ liệt kê giao diện đơn thuần mà trình bày theo cấu trúc 4 lớp rất thuyết phục: *(1) Giải pháp kỹ thuật (Fuzzy Search, Hybrid Cart & Reserved Stock giữ chỗ tồn kho, HMAC-SHA512 VNPay) \-\> (2) Trích đoạn code xử lý cốt lõi (10–15 dòng) \-\> (3) Hình ảnh giao diện thực tế \-\> (4) Diễn giải luồng hoạt động*.  
  4. **Phần Kiểm thử hệ thống (Mục 2.5, trang 113–140) đạt chuẩn xuất sắc:** Có kịch bản kiểm thử tổng quát, 11 bảng Test Case chi tiết (`TS001` – `TS011`), kết hợp kiểm thử API bằng **Postman** và kiểm thử hiệu năng chịu tải bằng **Apache JMeter**.  
* **Hạn chế & Lỗi kỹ thuật cần tuyệt đối tránh:**  
  1. **Lỗi biên tập CSDL nghiêm trọng (Trang 71–72):** *Bảng 2.11: Bảng đơn hàng (Orders)* nhưng toàn bộ danh sách cột bên trong (`customer_code`, `full_name`, `email`, `phone`, `status`, `total_spent`) và phần giải thích bên dưới lại bị copy-paste nhầm từ *Bảng 2.10: Bảng khách hàng (Customers)*. Ra hội đồng phản biện, lỗi này sẽ bị trừ điểm rất nặng.  
  2. **Dư thừa thuộc tính trong cùng một bảng (Vi phạm chuẩn 3NF):** Bảng `Customers` (trang 69\) vừa có cột `date_of_birth` vừa có `birth_date`; Bảng `Inventory_log` (trang 79\) vừa có `note` vừa có `notes`, vừa có `created_by` vừa có `user_id`.  
  3. **Mất cân đối bố cục giữa các chương:** Chương 2 ôm đồm cả Khảo sát, Phân tích, Thiết kế, Cài đặt lẫn Kiểm thử nên dài tới **120 trang** (trang 22–141), trong khi Chương 3 chỉ dài vẻn vẹn **5 trang** (trang 142–146).  
  4. **Lỗi khái niệm công nghệ (Trang 64):** Viết *"thiết kế CSDL trên phần mềm Microsoft SQL Server và phpMyAdmin tích hợp trong XAMPP"* (XAMPP tích hợp MySQL/MariaDB, không phải MS SQL Server).

---

#### 1.2. Khóa luận 2: Trương Quốc Việt (MSV: 2205HTTC055)

* **Đề tài:** *Xây dựng website quản lý mua bán bất động sản Quốc Việt* (89 trang)  
* **Nền tảng công nghệ:** C\#, ASP.NET MVC, Entity Framework, Microsoft SQL Server, Bootstrap.  
* **Ưu điểm nổi bật (Đáng học hỏi nhất về chiều sâu lý luận, pháp lý & kiến trúc .NET):**  
  1. **Gắn kết Cơ sở Pháp lý vào Logic Phần mềm (Mục 1.1 & 1.4, trang 13–24):** Đây là điểm sáng đắt giá nhất trong cả 4 bài. Sinh viên đối chiếu trực tiếp các quy định pháp luật hiện hành (Nghị định 52/2013/NĐ-CP về TMĐT, Nghị định 13/2023/NĐ-CP về Bảo vệ dữ liệu cá nhân, Luật chuyên ngành) để giải thích *"Sự đáp ứng của hệ thống phần mềm"* (ẩn thông tin nhạy cảm ở Backend, cơ chế khóa bản ghi bằng Transaction/Trigger chống bán trùng 1 sản phẩm cho 2 khách).  
  2. **Lý luận về C\#, ASP.NET MVC, Entity Framework và SQL Server (Mục 1.2, 1.3 & 2.3.2):** Viết rất sâu sắc, chuẩn thuật ngữ kỹ thuật phần mềm (Type-safe, Garbage Collection, LINQ, ORM chống SQL Injection, tính chất ACID & Rollback của Transaction, cơ chế Routing và Separation of Concerns trong MVC).  
  3. **Bố cục 3 chương hợp lý:** Tách rõ Chương 1 (Cơ sở lý luận), Chương 2 (Phân tích & Thiết kế) và Chương 3 (Triển khai, Thử nghiệm & Hướng phát triển).  
* **Hạn chế & Lỗi kỹ thuật cần tuyệt đối tránh:**  
  1. **Lỗi thiếu mục và nhảy số thứ tự ở Phần Mở đầu (Trang 4–5):** Mục lục nhảy từ *"1. Lý do chọn khóa luận"* sang *"3. Nhiệm vụ nghiên cứu"*, thiếu hẳn mục *"Tổng quan tình hình nghiên cứu"* và *"Giả thuyết nghiên cứu"*.  
  2. **Bất nhất về phiên bản công nghệ:** Mở đầu và Chương 1 khẳng định dùng **ASP.NET Core MVC & EF Core**, nhưng đến Mục 2.4.1 (trang 65–66) lại mô tả **ASP.NET MVC 5 trên .NET Framework 4.8 và Entity Framework 5.0 với file `.edmx`**.  
  3. **Lệch pha giữa Công nghệ Hướng đối tượng và Phương pháp Phân tích Hướng cấu trúc:** Hệ thống lập trình bằng C\# OOP, ASP.NET MVC và Entity Framework (ORM ánh xạ đối tượng), nhưng toàn bộ phần phân tích lại dùng phương pháp hướng cấu trúc cổ điển (**SSADM: BFD, DFD mức 0, mức 1**) mà không có bất kỳ sơ đồ UML hướng đối tượng nào (Use Case, Sequence Diagram, Class Diagram).  
  4. **Lỗi cú pháp kiểu dữ liệu SQL/C\#:** Viết kiểu dữ liệu trong bảng SQL Server là `Int(50)` (trang 47–61) và trong hàm C\# là `public ActionResult Index(int(50) page = 1)` (trang 67\) – đây là cú pháp không tồn tại trong SQL Server và C\#.  
  5. **Phần Kiểm thử quá sơ sài (Trang 80–81):** Chỉ viết tóm tắt dạng gạch đầu dòng trong 1,5 trang, không có bảng Test Case chi tiết.

---

#### 1.3. Khóa luận 3: Nguyễn Thị Ngọc Thương (MSV: 2205HTTC045)

* **Đề tài:** *Xây dựng website quản lý bán hàng cho cửa hàng NT Flowers* (146 trang)  
* **Nền tảng công nghệ:** C\#, ASP.NET MVC (Code First), Entity Framework, LINQ, SQL Server.  
* **Ưu điểm nổi bật (Đáng học hỏi về mô hình hóa quy trình & UML OOAD):**  
  1. **Đối sánh Quy trình Nghiệp vụ Hiện tại vs. Đề xuất bằng Sơ đồ Swimlane (Mục 2.1.1 – 2.1.3, trang 21–25):** Vẽ sơ đồ luồng công việc chia 3 làn (*Khách hàng – Hệ thống – Admin*) rất trực quan, giúp Hội đồng nhìn thấy ngay hệ thống tự động hóa được những khâu nào so với quy trình sổ sách cũ.  
  2. **Hệ thống Biểu đồ Use Case & Biểu đồ Tuần tự (Sequence Diagrams) chuẩn MVC (Trang 30–70):** Các biểu đồ tuần tự được vẽ rất đẹp, phân tách đúng các lớp đối tượng trong MVC (`Actor` \-\> `Giao diện View` \-\> `Controller` \-\> `CSDL`), có sử dụng khung điều kiện rẽ nhánh `alt [Hợp lệ / Không hợp lệ]` và có đoạn văn phân tích cơ chế kiểm soát dữ liệu (`Validate`, `Constraint`) rất kỹ bên dưới từng biểu đồ.  
  3. **Trình bày rõ ràng cách hiện thực hóa MVC ở đầu Chương 3 (Mục 3.1, trang 90–94):** Giải thích cách tổ chức thư mục Models, Views, Controllers và truy vấn LINQ trước khi đi vào giao diện kết quả.  
* **Hạn chế & Lỗi kỹ thuật cần tuyệt đối tránh:**  
  1. **Đánh số đề mục Chương 2 sai quy tắc phân cấp (Trang 6 & nội dung Chương 2):** Dưới mục `2.2` lại đánh số con là `2.1.1`; dưới mục `2.3` lại đánh số con là `2.2.1`; dưới mục `2.4` lại đánh số con là `2.3.1`.  
  2. **Thiết kế bảng CSDL thiếu toàn bộ cột Khóa ngoại (FK) và sai kiểu dữ liệu (Trang 71–86):**  
     - Bảng `Users` (Bảng 2.1): Lặp 2 cột `Fullname` và `Full_name`; cột `Avatar` để kiểu `Datetime`.  
     - Bảng `Orders` (Bảng 2.4): Cột `RecipientPhone` (Số điện thoại) để kiểu `Bit`\!  
     - Các bảng `ProductImages`, `Order_items`, `Reviews`, `CartItems` hoàn toàn **không có cột Khóa ngoại (`FK`)** nào trong bảng đặc tả cấu trúc cột, dù trong sơ đồ quan hệ (Sơ đồ 2.17) có nối dây liên kết.  
  3. **Lỗi vẽ trùng nút trong Sơ đồ Use Case 2.15 (Trang 49):** Chức năng "Quản lý Hot Deal" vẽ 2 nút `<include>` có cùng tên là *"Sửa Hot Deal"* (bị thiếu *"Xóa Hot Deal"*).

---

#### 1.4. Khóa luận 4: Đặng Văn Vượng (MSV: 2205HDTA036)

* **Đề tài:** *Xây dựng website quản lý bán điện thoại Hoàng Minh* (131 trang)  
* **Nền tảng công nghệ:** PHP, MySQL, XAMPP, Bootstrap, MVC.  
* **Ưu điểm nổi bật (Đáng tham khảo về cách viết Tổng quan nghiên cứu):**  
  1. **Cách viết Tổng quan tình hình nghiên cứu (Mục 2 Phần Mở đầu, trang 11–13):** Trích dẫn cụ thể tên tác giả, tên đề tài/bài báo khoa học trong nước và quốc tế (ví dụ: nghiên cứu của Nguyễn Hoàng Nam \- ĐH Công nghiệp HN, nghiên cứu của Li Wei, bài báo trên *IOP Conference Series*), chỉ rõ *nội dung họ đã làm được* và *hạn chế/khoảng trống chưa giải quyết* để dẫn dắt vào đề tài của mình. Đây là chuẩn mực viết Literature Review trong nghiên cứu khoa học.  
  2. **Phân nhóm tác nhân rõ ràng (Hình 2.1, trang 32):** Chia rõ Khách hàng (Khách vãng lai vs. Thành viên), Admin và Nhân viên.  
* **Hạn chế & Lỗi kỹ thuật cần tuyệt đối tránh:**  
  1. **Lỗi "Râu ông nọ cắm cằm bà kia" trong Sơ đồ ERD (Hình 2.27, trang 69):** Đề tài làm về **Cửa hàng Điện thoại Hoàng Minh**, nhưng sơ đồ ERD chụp màn hình lại để nguyên tên tất cả các bảng là `duan1_laptop_products`, `duan1_laptop_orders`, `duan1_laptop_users`... Lỗi này tạo ấn tượng rất xấu với Hội đồng chấm về tính trung thực của sản phẩm.  
  2. **Nhầm lẫn khái niệm Khóa chính (PK) và Khóa ngoại (FK) hàng loạt (Trang 70–74):** Trong các bảng `Products`, `Carts`, `Comments`, `Orders`, cột ID định danh chính của bảng đều bị ghi ràng buộc là `FK, NOT NULL, Tự tăng` thay vì `PK`.  
  3. **Đánh số thứ tự bảng bị trùng lặp (Trang 8 & 74–75):** Có 3 bảng liên tiếp đều mang số hiệu `Bảng 2.25`.  
  4. **Trình bày kịch bản kiểm thử bằng văn xuôi (Trang 78–81):** Không kẻ bảng Test Case mà viết thành từng đoạn văn dài dòng, khó theo dõi và thiếu tính chuyên nghiệp của ngành HTTT.

---

### 2\. Bảng Ma trận Đối sánh Tổng hợp 4 Khóa luận Mẫu & Định vị Đề tài CANMAKE BEAUTY

| Tiêu chí đánh giá | KL1: Vũ Thị Minh Huyền (*Thời trang Star*) | KL2: Trương Quốc Việt (*BĐS Quốc Việt*) | KL3: Nguyễn Thị Ngọc Thương (*NT Flowers*) | KL4: Đặng Văn Vượng (*ĐTDĐ Hoàng Minh*) | Định chuẩn Tối ưu cho CANMAKE BEAUTY |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **1\. Quy mô bài toán nghiệp vụ** | 1 công ty thời trang (B2C \+ Kho đơn giản, 10 bảng CSDL) | 1 văn phòng môi giới BĐS (Ký gửi \+ Hợp đồng, 25 bảng CSDL) | 1 cửa hàng hoa nhỏ lẻ (B2C cơ bản, 12 bảng CSDL) | 1 cửa hàng điện thoại nhỏ (B2C cơ bản, 10 bảng CSDL) | **Chuỗi cửa hàng mỹ phẩm Đa kênh (Omnichannel: E-Commerce \+ Smart POS \+ Kho Lô FEFO \+ CRM/AI \+ Tài chính, 65 bảng 3NF)** |
| **2\. Nền tảng & Kiến trúc Công nghệ** | PHP thuần \+ MySQL \+ Tailwind CSS | C\# ASP.NET MVC \+ EF \+ SQL Server | C\# ASP.NET MVC \+ EF \+ SQL Server | PHP MVC \+ MySQL \+ Bootstrap | **.NET 8 (C\# 12\) Clean Architecture 4 tầng \+ EF Core 8 \+ SQL Server (3NF) \+ SignalR \+ HybridCache** |
| **3\. Cấu trúc Phần Mở đầu** | **Xuất sắc (8 mục đầy đủ, có Giả thuyết nghiên cứu)** | Thiếu Tổng quan NC, thiếu Giả thuyết, đánh nhảy số mục | Đầy đủ 8 mục (nhưng thiếu Giả thuyết nghiên cứu) | **Đầy đủ 8 mục (Tổng quan nghiên cứu viết rất chuẩn học thuật)** | **Kết hợp khung 8 mục của KL1 \+ Cách viết Tổng quan nghiên cứu học thuật của KL4** |
| **4\. Chiều sâu Cơ sở Lý luận (Chương 1\)** | Tốt về MIS & TMĐT B2C, nhưng lặp lại nội dung công nghệ với Mở đầu | **Xuất sắc (Kết hợp Lý luận MIS \+ Kiến trúc MVC/.NET \+ Cơ sở Pháp lý)** | Khá tốt về Quy trình 6 bước xây dựng Web & Kiến trúc 3 lớp / MVC | Khá cơ bản, có lỗi mâu thuẫn tên hệ quản trị CSDL | **Kế thừa KL2 & KL3: Lý luận MIS & Omnichannel O2O \+ Đặc thù Pháp lý Mỹ phẩm/Dược \+ Clean Architecture .NET 8** |
| **5\. Phương pháp Phân tích & Thiết kế** | Kết hợp BPC \+ Use Case \+ Activity \+ Sequence \+ DFD mức ngữ cảnh | Chỉ dùng hướng cấu trúc (BFD \+ DFD mức 0, 1, 2), thiếu UML | **Chuẩn OOAD (Swimlane As-Is/To-Be \+ BPC \+ Use Case \+ Sequence MVC)** | Dùng Use Case \+ Sequence (nhưng thiếu Activity & phân tích luồng) | **Chuẩn OOAD hiện đại: Swimlane As-Is/To-Be \+ BFD Phân hệ \+ Use Case \+ Activity \+ Sequence BCE \+ Class Diagram** |
| **6\. Chất lượng Thiết kế CSDL (ERD & Từ điển)** | Phân tích tốt (có `reserved_stock`), nhưng **lỗi copy nhầm bảng Orders** | Chuẩn hóa tốt (25 bảng), nhưng **lỗi ghi kiểu `Int(50)`** | **Lỗi thiếu cột FK trong bảng mô tả & sai kiểu dữ liệu (`Bit` cho SĐT)** | **Lỗi để tên bảng `duan1_laptop_` & ghi nhầm PK thành FK** | **Chuẩn hóa 3NF tuyệt đối (`INT IDENTITY(1,1)`), đồng bộ 100% giữa ERD, Bảng mô tả PK/FK và Code EF Core 8** |
| **7\. Trình bày Cài đặt & Kiểm thử** | **Xuất sắc (Kịch bản \+ 11 Bảng Test Case \+ Postman \+ JMeter)**, nhưng đặt nhầm vào Chương 2 | Đặt đúng ở Chương 3 (có hướng dẫn Deploy IIS), nhưng Test Case quá sơ sài | Đặt đúng ở Chương 3 (có giải thích cấu trúc MVC), có bảng Test Case cơ bản | Đặt nhầm Kiểm thử vào Chương 2, viết Test Case dạng văn xuôi | **Tách riêng Chương 3 cân đối: Kiến trúc Clean Architecture & Code cốt lõi \+ Giao diện Đa kênh \+ Ma trận Kiểm thử Đa tầng (xUnit, PBAC, Postman, Concurrency)** |

---

### 3\. Đúc kết "10 Nguyên tắc Vàng" giúp Khóa luận CANMAKE BEAUTY đạt điểm Tuyệt đối

Từ những ưu điểm và sai sót thực tế của 4 bài khóa luận trên, khi viết báo cáo cho **Chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY**, bạn cần tuân thủ 10 nguyên tắc sau:

2. **Cân đối dung lượng 3 Chương theo "Tỷ lệ Vàng" (20% – 50% – 30%):**  
   - **Phần Mở đầu (\~10–12 trang):** Đầy đủ 8 mục chuẩn APAG.  
   - **Chương 1 (\~22–25 trang):** Cơ sở lý luận về Hệ thống thông tin quản lý bán hàng đa kênh (Omnichannel), cơ sở pháp lý ngành mỹ phẩm, nền tảng công nghệ `.NET 8 Clean Architecture` và tổng quan chuỗi cửa hàng CANMAKE BEAUTY.  
   - **Chương 2 (\~60–70 trang):** Khảo sát hiện trạng và Phân tích – Thiết kế hệ thống (tập trung hoàn toàn vào *Analysis & Design*: BFD, Use Case, Activity, Sequence, Class BCE, CSDL 3NF và Thiết kế UI/UX). **Tuyệt đối không nhồi nhét phần Kiểm thử vào Chương 2** như bài của Minh Huyền hay Văn Vượng.  
   - **Chương 3 (\~35–40 trang):** Triển khai cài đặt hệ thống, Kết quả đạt được (Trình diễn giải pháp kỹ thuật kèm code \+ Giao diện Storefront/POS/Warehouse/Admin), Kiểm thử toàn diện (Unit Test, Integration Test 12 tài khoản PBAC, Concurrency Test) và Đánh giá – Hướng phát triển.  
2. **Nâng tầm Bài toán từ "Cửa hàng đơn lẻ" lên "Chuỗi cửa hàng Bán lẻ Đa kênh (Omnichannel Retail Chain)":**  
   - 3/4 bài mẫu chỉ dừng ở một cửa hàng nhỏ lẻ (bán hoa, bán điện thoại). Với **CANMAKE BEAUTY**, bạn sở hữu lợi thế cạnh tranh tuyệt đối khi giải quyết bài toán **Chuỗi cửa hàng & Kho tổng** (đồng bộ tức thời giữa Website Thương mại điện tử, Quầy thu ngân Smart POS tại từng Showroom, Kho tổng và Điều chuyển hàng hóa liên chi nhánh).  
3. **Khắc họa rõ nét "Đặc thù Ngành Dược – Mỹ phẩm" trong từng trang viết:**  
   - Khác với quần áo hay điện thoại, mỹ phẩm gắn liền với **an toàn sức khỏe làn da và hạn sử dụng nghiêm ngặt**. Hãy làm nổi bật:  
     - Thuật toán quản lý lô sản xuất và xuất kho theo hạn dùng **FEFO (First-Expired, First-Out)** kèm cơ chế cảnh báo cận date & định giá xả hàng tự động.  
     - Quản lý biến thể chuyên sâu (**Swatch màu son mã Hex/Texture** & **Dung tích ml/g**) và cơ sở dữ liệu khoa học (**Hoạt chất Active Ingredients**, **Loại da Skin Types**, **Vấn đề da Skin Concerns**, **Số công bố mỹ phẩm Bộ Y tế**).  
     - Công nghệ **BeautyTech AI & CRM**: Hồ sơ sắc đẹp cá nhân (`CustomerProfiles`), Lịch sử soi da (`SkinHistories`), Kiểm tra tương kỵ hoạt chất (`SkincareIngredientInteractionEngine`), Dự báo ngày dùng hết mỹ phẩm để nhắc mua lại (`RepurchaseSchedules`) và Chatbot AI tư vấn da liễu.  
4. **Chiến lược "Phân tầng CSDL Thông minh" (Core Tables vs. Full Schema):**  
   - Dự án thực tế của bạn có tới **65 bảng chuẩn 3NF** chia làm **17 phân hệ**. Nếu đưa bảng đặc tả chi tiết của cả 65 bảng vào Chương 2 thì riêng phần bảng biểu sẽ chiếm mất 80 trang giấy gây loãng báo cáo.  
   - **Giải pháp tối ưu của chuyên gia:**  
     - Tại **Mục 2.4 (Thiết kế CSDL)** trong Chương 2: Trình bày **Sơ đồ Kiến trúc Tổng thể 17 Phân hệ (65 bảng)** để khẳng định quy mô đồ sộ của dự án, sau đó đi sâu phân tích **Sơ đồ ERD và Đặc tả chi tiết 20–22 Bảng Cốt lõi nhất (Core Tables)** đại diện cho các luồng nghiệp vụ trọng tâm (Định danh & Phân quyền PBAC, Sản phẩm & Biến thể Swatch/Hoạt chất, Kho Lô FEFO & Điều chuyển liên kho, Đơn hàng Đa kênh & Ca POS, Hồ sơ sắc đẹp CRM & Ví điểm Loyalty, Sổ cái Tài chính & Đối soát COD).  
     - Các bảng phụ trợ còn lại được tổng hợp trong một **Bảng Danh mục Tổng quan Hệ thống Bảng** hoặc đưa vào **Phụ lục**, vừa giúp báo cáo súc tích, vừa phô diễn được 100% công sức lập trình của bạn\!  
5. **Tuyệt đối đồng nhất Kiểu dữ liệu và Ràng buộc CSDL:**  
   - Kiểm tra kỹ từng bảng: Khóa chính đồng bộ `INT IDENTITY(1,1)` (`PK, Not Null`), Khóa ngoại ghi rõ `INT` (`FK -> Tên_Bảng(Id)`), Tiền tệ dùng `DECIMAL(18,2)`, Chuỗi tiếng Việt dùng `NVARCHAR(...)`, Mã định danh/Slug dùng `VARCHAR(...)`, Trạng thái logic dùng `BIT`. Không để xảy ra lỗi copy-paste nhầm bảng hoặc thiếu cột FK.

---

## PHẦN II: ĐỀ CƯƠNG MỤC LỤC HOÀN CHỈNH, TỐI ƯU CHO BÁO CÁO KHÓA LUẬN "CANMAKE BEAUTY"

Dưới đây là bản **Đề cương Mục lục Chi tiết (Master Outline)** được thiết kế chuẩn hóa theo đúng quy định trình bày Khóa luận tốt nghiệp của Học viện Hành chính và Quản trị công (APAG), kết hợp hoàn hảo với kiến trúc kỹ thuật của dự án **CANMAKE BEAUTY**.

---

### KHUNG TRÌNH BÀY ĐẦU BÁO CÁO (FRONT MATTER)

* **TRANG BÌA CHÍNH & TRANG BÌA PHỤ** *(Tên đề tài: "XÂY DỰNG WEBSITE QUẢN LÝ BÁN HÀNG CHO CHUỖI CỬA HÀNG MỸ PHẨM CANMAKE BEAUTY" – Ngành: Hệ thống thông tin)*  
* **LỜI CẢM ƠN**  
* **LỜI CAM ĐOAN**  
* **MỤC LỤC**  
* **DANH MỤC CÁC TỪ VIẾT TẮT VÀ THUẬT NGỮ CHUYÊN NGÀNH** *(Thiết kế bảng 3 cột: Từ viết tắt/Thuật ngữ \| Tiếng Anh đầy đủ \| Ý nghĩa & Ứng dụng tại hệ thống CANMAKE BEAUTY: FEFO, PBAC, O2O, BOPIS, POS, SKU, PAO, COGS, COD, ORM, CSRF, XSS...)*  
* **DANH MỤC BẢNG BIỂU**  
* **DANH MỤC SƠ ĐỒ & BIỂU ĐỒ**  
* **DANH MỤC HÌNH ẢNH GIAO DIỆN**

---

### PHẦN MỞ ĐẦU (Dự kiến: 10 – 12 trang)

**1\. Lý do chọn đề tài**

* *Bối cảnh vĩ mô:* Sự bùng nổ của chuyển đổi số ngành bán lẻ và xu hướng dịch chuyển từ thương mại điện tử đơn kênh sang mô hình **Bán lẻ hợp nhất đa kênh (Omnichannel Retail / O2O)** trong ngành mỹ phẩm và chăm sóc sắc đẹp tại Việt Nam.  
* *Đặc thù khắt khe của ngành kinh doanh mỹ phẩm chuỗi:* Hàng hóa có hàng nghìn biến thể (màu sắc Swatch, dung tích, hoạt chất), nhạy cảm cao với **hạn sử dụng (Date/Lot)** và điều kiện bảo quản; khách hàng đòi hỏi tư vấn cá nhân hóa theo đúng loại da/tình trạng da và kiểm tra tương kỵ hoạt chất.  
* *Thực trạng cấp thiết tại chuỗi cửa hàng CANMAKE BEAUTY:* Sự đứt gãy dữ liệu giữa kênh bán hàng trực tuyến (Website/Mạng xã hội) và các Showroom vật lý (Quận 1, Ba Đình) cùng Tổng kho phân phối; rủi ro bán lố tồn kho (Overselling), thất thoát hàng cận date do xuất kho cảm tính, đối soát tiền mặt ca thu ngân POS và tiền thu hộ COD thủ công.  
* *Ý nghĩa của việc thực hiện đề tài:* Xây dựng nền tảng website quản lý bán hàng đa kênh tập trung, chuẩn hóa toàn diện quy trình từ Storefront, Smart POS, Kho lô FEFO đến CRM và Tài chính.

**2\. Tổng quan tình hình nghiên cứu**

* **2.1. Tình hình nghiên cứu và ứng dụng trong nước:**  
  * Phân tích các báo cáo chuyển đổi số bán lẻ (VECOM, mô hình chuỗi Hasaki, Guardian, Beauty Box), các nền tảng SaaS đóng gói (KiotViet, Sapo) và nhận xét các công trình khóa luận/nghiên cứu trước đó về hệ thống quản lý bán hàng.  
  * *Chỉ ra khoảng trống nghiên cứu (Research Gap):* Các phần mềm đóng gói đại trà hoặc các đề tài bán hàng thông thường chỉ quản lý tồn kho theo phương pháp bình quân/FIFO đơn giản, thiếu cơ chế **phân bổ lô tự động theo hạn dùng FEFO 2 giai đoạn**, thiếu công cụ **phân tích tương tác hoạt chất da liễu AI** và thiếu **ma trận phân quyền động PBAC cô lập dữ liệu đa chi nhánh**.  
* **2.2. Tình hình nghiên cứu và ứng dụng trên thế giới:**  
  * Tổng quan các nghiên cứu quốc tế (Gartner, Forrester) về kiến trúc Thương mại hợp nhất (*Unified Commerce*), quản trị chuỗi cung ứng dược \- mỹ phẩm theo chuẩn mã vạch lô hạn dùng và ứng dụng AI cá nhân hóa chu trình chăm sóc da (*BeautyTech RAG & Skin Diagnostics*).

**3\. Mục tiêu và nhiệm vụ nghiên cứu**

* **3.1. Mục tiêu nghiên cứu:**  
  * *Mục tiêu tổng quát:* Thiết kế và xây dựng thành công Hệ thống website quản lý bán hàng đa kênh cho chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY trên nền tảng .NET 8 Clean Architecture.  
  * *6 Mục tiêu cụ thể (Chiến lược):*  
    1. Quản trị bảo mật đa chi nhánh với cơ chế phân quyền động 3 tầng (**Granular Dynamic PBAC**).  
    2. Tối ưu hóa trải nghiệm mua sắm mỹ phẩm trực tuyến và tư vấn da liễu thông minh (**SmartFilter, Hoạt chất & AI Beauty Advisor**).  
    3. Tự động hóa vận hành quầy thu ngân **Smart POS** (quản lý ca làm việc, kiểm két tiền mặt, thanh toán QR động VietQR/VNPay/MoMo).  
    4. Quản trị kho vận thông minh theo lô và hạn sử dụng (**Thuật toán FEFO**, giữ chỗ tồn kho 2 giai đoạn và điều chuyển kho liên chi nhánh 3 bước).  
    5. Quản trị quan hệ khách hàng (**Hồ sơ sắc đẹp CRM 360°**, lịch nhắc mua lại tự động và Ví điểm thưởng/Thăng hạng thành viên **Loyalty**).  
    6. Số hóa Sổ cái tài chính, hạch toán giá vốn đích danh theo lô xuất (**Batch-Specific COGS**), đối soát công nợ NCC/COD và hệ thống cảnh báo vận hành thời gian thực (**SignalR & AI Alerts**).  
* **3.2. Nhiệm vụ nghiên cứu:**  
  * Nhiệm vụ về mặt lý thuyết (Hệ thống hóa lý luận MIS, Omnichannel, pháp lý mỹ phẩm, kiến trúc .NET 8).  
  * Nhiệm vụ về mặt thực tiễn (Khảo sát chuỗi CANMAKE BEAUTY, phân tích thiết kế OOAD, chuẩn hóa CSDL 3NF, lập trình, kiểm thử đa tầng).

**4\. Đối tượng và phạm vi nghiên cứu**

* **4.1. Đối tượng nghiên cứu:** Các quy trình nghiệp vụ quản lý bán lẻ đa kênh, điều phối tồn kho theo lô hạn dùng FEFO, chăm sóc khách hàng và kiến trúc phần mềm web phục vụ chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY.  
* **4.2. Phạm vi nghiên cứu:**  
  * *Phạm vi về nội dung:* Tập trung giải quyết trọn vẹn 2 phân vùng: Phân hệ Khách hàng (Storefront E-Commerce & Cổng thành viên) và Phân hệ Quản trị Đa kênh (Admin Dashboard, POS Terminal, Quản lý Kho & Điều chuyển, CRM & Loyalty, CMS, Tài chính & Bảo mật PBAC).  
  * *Phạm vi về không gian:* Chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY (bao gồm Tổng kho phân phối trung tâm và các Showroom chi nhánh như Flagship Quận 1, Showroom Ba Đình).  
  * *Phạm vi về thời gian:* Khảo sát, phân tích, thiết kế và triển khai thực nghiệm từ tháng 11/2025 đến tháng 04/2026.  
  * *Phạm vi về khách thể (Actors):* 6 nhóm tác nhân tương tác trực tiếp gồm Quản trị viên tối cao (`SuperAdmin`), Quản lý chi nhánh (`StoreManager`), Thu ngân tại quầy (`Cashier`), Nhân viên thủ kho (`WarehouseStaff`), Biên tập viên nội dung (`ContentEditor`) và Khách hàng (`Customer`), cùng các hệ thống đối tác ngoài (`VNPay`, `MoMo`, `VietQR Napas247`, `Giao Hàng Nhanh - GHN`).

**5\. Phương pháp và công cụ thực hiện đề tài**

* **5.1. Phương pháp nghiên cứu:**  
  * Phương pháp thu thập, nghiên cứu tài liệu và văn bản pháp quy ngành mỹ phẩm/TMĐT.  
  * Phương pháp khảo sát thực tiễn và phỏng vấn nghiệp vụ tại các điểm bán/kho của chuỗi CANMAKE BEAUTY.  
  * Phương pháp phân tích và thiết kế hệ thống hướng đối tượng (**OOAD** với ngôn ngữ mô hình hóa thống nhất **UML 2.5**: Use Case, Activity, Sequence, BCE Class Diagram) kết hợp chuẩn hóa cơ sở dữ liệu quan hệ dạng chuẩn 3 (**3NF**).  
  * Phương pháp thực nghiệm lập trình theo kiến trúc sạch (**Clean Architecture 4 tầng**).  
  * Phương pháp kiểm thử tự động và đánh giá thực nghiệm (Unit Test & Integration Test với `xUnit`, kiểm thử API với `Postman`, kiểm thử chịu tải đồng thời).  
* **5.2. Công cụ và công nghệ sử dụng:**  
  * *Backend & Kiến trúc:* C\# 12, ASP.NET Core 8.0, Entity Framework Core 8, SignalR WebSockets, MemoryCache/DistributedCache.  
  * *Frontend & Giao diện:* Razor Views, HTML5, Tailwind CSS / CSS3 Design Tokens, JavaScript ES6+, HTMX, Alpine.js, Chart.js.  
  * *Hệ quản trị CSDL & Công cụ:* Microsoft SQL Server (SSMS), Visual Studio 2022, Git, xUnit, Postman.

**6\. Giả thuyết nghiên cứu**

* Đặt ra 6 giả thuyết khoa học kỳ vọng hệ thống CANMAKE BEAUTY giải quyết triệt để bài toán bán lố tồn kho, triệt tiêu hàng hết hạn nhờ FEFO, rút ngắn thời gian thanh toán POS và nâng cao tỷ lệ khách hàng quay lại nhờ hồ sơ da CRM.

**7\. Đóng góp của khóa luận**

* Đóng góp về mặt lý luận & học thuật; Đóng góp về mặt thực tiễn vận hành cho chuỗi CANMAKE BEAUTY.

**8\. Bố cục của khóa luận**

* Tóm tắt nội dung 3 chương chính của khóa luận.

---

### PHẦN NỘI DUNG

#### CHƯƠNG 1: CƠ SỞ LÝ LUẬN VỀ HỆ THỐNG THÔNG TIN QUẢN LÝ BÁN HÀNG ĐA KÊNH VÀ TỔNG QUAN CHUỖI CỬA HÀNG MỸ PHẨM CANMAKE BEAUTY (Dự kiến: 22 – 25 trang)

**1.1. Cơ sở lý luận về Hệ thống thông tin quản lý và Thương mại điện tử đa kênh (Omnichannel)**

* **1.1.1. Hệ thống thông tin (IS) và Hệ thống thông tin quản lý (MIS) trong doanh nghiệp bán lẻ chuỗi**  
* **1.1.2. Mô hình Thương mại điện tử B2C kết hợp Bán lẻ hợp nhất đa kênh (Omnichannel O2O \- Online to Offline):**  
  * Sự khác biệt giữa Bán hàng đơn kênh (Single-channel), Đa kênh rời rạc (Multi-channel) và Đa kênh hợp nhất (Omnichannel) trong việc đồng bộ Giỏ hàng, Tồn kho, Đơn hàng và Điểm thưởng thành viên theo thời gian thực.  
* **1.1.3. Đặc thù quản trị hàng hóa và chuỗi cung ứng trong ngành Dược – Mỹ phẩm:**  
  * Quản lý danh mục đa biến thể (Master SKU vs. Sub-SKU theo Swatch màu sắc, chất liệu, dung tích) và hệ thống phân loại khoa học theo Hoạt chất (`Active Ingredients`) – Loại da (`Skin Types`).  
  * Nguyên tắc quản trị kho theo Lô sản xuất (`Batch/Lot`) và Hạn sử dụng **FEFO (First-Expired, First-Out – Hết hạn trước, Xuất trước)** kết hợp hạn dùng sau mở nắp (**PAO – Period After Opening**).  
  * Phương pháp hạch toán giá vốn đích danh theo lô xuất (**Batch-Specific COGS**) và kiểm soát ca thu ngân POS.

**1.2. Cơ sở pháp lý điều chỉnh hoạt động kinh doanh mỹ phẩm trực tuyến và sự đáp ứng của hệ thống** *(Kế thừa điểm sáng của KL2 \- Trương Quốc Việt)*

* **1.2.1. Quy định về quản lý và công bố sản phẩm mỹ phẩm (Thông tư 06/2011/TT-BYT & Hiệp định Mỹ phẩm ASEAN):**  
  * *Sự đáp ứng trên hệ thống CANMAKE BEAUTY:* Thiết kế các trường lưu trữ Số phiếu tiếp nhận công bố mỹ phẩm của Cục Quản lý Dược (`CosmeticNotificationNumber`), nước xuất xứ, đơn vị nhập khẩu và cơ chế khóa tự động các lô hàng chạm ngưỡng an toàn hạn dùng (\< 30 ngày).  
* **1.2.2. Quy định về giao dịch thương mại điện tử và bảo vệ dữ liệu cá nhân (Nghị định 52/2013/NĐ-CP, Nghị định 85/2021/NĐ-CP & Nghị định 13/2023/NĐ-CP):**  
  * *Sự đáp ứng trên hệ thống CANMAKE BEAUTY:* Bảo mật dữ liệu hồ sơ làn da và tiền sử dị ứng của khách hàng (`CustomerProfiles`), mã hóa mật khẩu một chiều chuẩn PBKDF2-SHA256, kiểm soát truy cập đa tầng PBAC và lưu vết nhật ký kiểm toán bất biến (`AuditLogs`).

**1.3. Cơ sở lý luận về nền tảng công nghệ phát triển hệ thống**

* **1.3.1. Nền tảng ASP.NET Core 8.0, ngôn ngữ lập trình C\# 12 và kiến trúc Clean Architecture 4 tầng:**  
  * Phân tích 4 tầng độc lập: `Omnichannel.Domain` (Lõi thực thể & quy tắc nghiệp vụ), `Omnichannel.Application` (Interfaces & DTOs), `Omnichannel.Infrastructure` (EF Core 8, Security PBAC, Services), `Omnichannel.Web` (Controllers, Razor Layouts, SignalR Hubs, Middlewares) và nguyên lý Đảo ngược phụ thuộc (DIP).  
* **1.3.2. Hệ quản trị cơ sở dữ liệu Microsoft SQL Server và công nghệ ánh xạ ORM Entity Framework Core 8:**  
  * Đảm bảo nguyên tắc ACID trong giao dịch tài chính/kho vận, cơ chế chống xung đột đồng thời (`RowVersion` Optimistic Concurrency) và tối ưu hóa truy vấn.  
* **1.3.3. Công nghệ giao tiếp thời gian thực (SignalR WebSockets), giao diện hiện đại (Tailwind CSS, HTMX, Alpine.js) và tích hợp API đối tác (VietQR Napas247, VNPay, MoMo, GHN).**  
* **1.3.4. Phương pháp phân tích và thiết kế hệ thống hướng đối tượng (OOAD với UML và mô hình BCE: Boundary – Control – Entity).**

**1.4. Tổng quan về Chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY**

* **1.4.1. Giới thiệu chung, tầm nhìn, sứ mệnh và mô hình chuỗi điểm bán:**  
  * Hệ sinh thái CANMAKE BEAUTY gồm Tổng kho phân phối trung tâm (`WH-MN`) và hệ thống các Showroom bán lẻ (`SH-Q1` Flagship Quận 1, `SH-BD` Showroom Ba Đình...).  
* **1.4.2. Cơ cấu tổ chức nhân sự và chức năng các bộ phận:**  
  * Ban Giám đốc (SuperAdmin), Quản lý Chi nhánh (StoreManager), Bộ phận Thu ngân & Tư vấn viên Soi da tại quầy (Cashier & Beauty Consultant), Bộ phận Kho vận & Điều phối (WarehouseStaff), Bộ phận Marketing & Nội dung (ContentEditor), Bộ phận Tài chính – Kế toán. *(Kèm Sơ đồ 1.1: Sơ đồ cơ cấu tổ chức chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY)*.

**Tiểu kết Chương 1**

---

#### CHƯƠNG 2: KHẢO SÁT, PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG WEBSITE QUẢN LÝ BÁN HÀNG CHO CHUỖI CỬA HÀNG MỸ PHẨM CANMAKE BEAUTY (Dự kiến: 60 – 70 trang)

**2.1. Khảo sát hiện trạng và xác lập bài toán nghiệp vụ**

* **2.1.1. Khảo sát quy trình hoạt động nghiệp vụ hiện tại (As-Is Process):**  
  * Quy trình bán hàng trực tiếp tại các Showroom, quy trình nhận đơn qua mạng xã hội/tin nhắn, quy trình kiểm tra tồn kho – hạn dùng trên Excel và quy trình đối soát tiền mặt cuối ca/cuối ngày. *(Kèm Sơ đồ 2.1: Sơ đồ quy trình bán hàng và quản lý kho thủ công hiện tại)*.  
* **2.1.2. Đánh giá các hạn chế, "điểm nghẽn" của mô hình hiện tại:**  
  * (1) Xung đột tồn kho giữa kênh Online và tại quầy dẫn đến bán lố (Overselling); (2) Không kiểm soát được hạn sử dụng theo lô dẫn đến nhân viên lấy hàng lô mới bán trước, lô cũ hết hạn phải tiêu hủy; (3) Thiếu công cụ tra cứu nhanh thành phần hoạt chất và tiền sử dị ứng da của khách; (4) Rủi ro thất thoát tiền mặt tại két thu ngân POS và sai lệch đối soát COD; (5) Phân quyền lỏng lẻo, nhân viên chi nhánh này có thể xem hoặc sửa nhầm dữ liệu của chi nhánh khác.  
* **2.1.3. Đề xuất quy trình nghiệp vụ bán lẻ đa kênh số hóa mới (To-Be Omnichannel Process):**  
  * *(Kèm Sơ đồ 2.2: Sơ đồ Swimlane quy trình bán hàng đa kênh và điều phối kho FEFO đề xuất tại CANMAKE BEAUTY)*.

**2.2. Phân tích yêu cầu hệ thống và Mô hình hóa chức năng tổng quát**

* **2.2.1. Xác định các tác nhân hệ thống (Actors) và phạm vi truy cập:**  
  * Phân tích chi tiết 6 nhóm tác nhân con người (`SuperAdmin`, `StoreManager`, `Cashier`, `WarehouseStaff`, `ContentEditor`, `Customer`), 1 tác nhân tiến trình nền tự động (`AlertBackgroundWorker`) và 4 tác nhân hệ thống ngoài (`VNPay`, `MoMo`, `VietQR Napas247`, `GHN Express`). *(Kèm Bảng 2.1: Bảng đặc tả các tác nhân tham gia hệ thống)*.  
* **2.2.2. Xây dựng Biểu đồ phân rã chức năng (BFD / BPC):**  
  * Phân rã hệ thống thành các phân hệ nghiệp vụ cốt lõi từ tổng quát đến chi tiết. *(Kèm Sơ đồ 2.3: Biểu đồ phân rã chức năng tổng quát của hệ thống CANMAKE BEAUTY và các sơ đồ phân rã nhánh con)*.  
* **2.2.3. Phân tích chi tiết yêu cầu chức năng theo phân hệ:**  
  * *(Kèm Bảng 2.2: Bảng danh mục yêu cầu chức năng chi tiết theo từng nhóm phân hệ Storefront & Admin Back-office)*.  
* **2.2.4. Phân tích yêu cầu phi chức năng (Non-Functional Requirements):**  
  * Yêu cầu về Bảo mật đa tầng (PBKDF2-SHA256, Dynamic PBAC, Multi-Store Isolation, Anti-CSRF/XSS/SQLi, Rate Limiting, Audit Logging); Yêu cầu về Hiệu năng & Chịu tải đồng thời (HybridCache L1/L2, Optimistic Concurrency `RowVersion`, thời gian phản hồi \< 1.5s); Yêu cầu về Tính khả dụng & Giao diện công thái học (4 Layout chuyên biệt, Responsive).

**2.3. Mô hình hóa nghiệp vụ hướng đối tượng bằng UML (Use Case, Activity & Sequence Diagrams)**

* **2.3.1. Mô hình hóa Use Case tổng quát và phân rã theo tác nhân:**  
  * *Sơ đồ 2.4:* Biểu đồ Use Case tổng quát hệ thống CANMAKE BEAUTY.  
  * *Sơ đồ 2.5 – 2.9:* Các biểu đồ Use Case phân rã theo tác nhân (`SuperAdmin & Bảo mật PBAC`, `StoreManager & Tài chính`, `Cashier & Smart POS`, `WarehouseStaff & Kho FEFO`, `Customer & AI BeautyTech`).  
* **2.3.2. Đặc tả chi tiết Use Case, Biểu đồ Hoạt động (Activity) và Biểu đồ Tuần tự (Sequence) cho 8 nghiệp vụ cốt lõi nhất:** *(Trình bày theo cặp: Bảng Đặc tả Use Case chuẩn \-\> Biểu đồ Hoạt động / Biểu đồ Tuần tự UML \-\> Phân tích luồng thông điệp kỹ thuật)*  
  1. **Nghiệp vụ 1:** Quản trị Ma trận Phân quyền Động 3 tầng (Dynamic PBAC) và Cô lập dữ liệu đa chi nhánh *(Bảng đặc tả \+ Biểu đồ hoạt động \+ Biểu đồ tuần tự SD-01)*.  
  2. **Nghiệp vụ 2:** Đặt hàng trực tuyến và Cấp phát – Trừ tồn kho 2 giai đoạn theo thuật toán **FEFO** (Soft Reserve & Hard Deduct chống bán khống) *(Bảng đặc tả \+ Biểu đồ hoạt động \+ Biểu đồ tuần tự SD-02)*.  
  3. **Nghiệp vụ 3:** Bán hàng tại quầy **Smart POS**, đồng bộ màn hình phụ khách hàng (Dual-Screen CFD) và Thanh toán **VietQR / VNPay** tự động in hóa đơn *(Bảng đặc tả \+ Biểu đồ hoạt động \+ Biểu đồ tuần tự SD-03)*.  
  4. **Nghiệp vụ 4:** Quản lý Vòng đời Ca thu ngân POS (`PosShifts`) và Kiểm toán – Đối soát Két tiền mặt (`PosDrawerTransactions`) *(Bảng đặc tả \+ Biểu đồ hoạt động \+ Biểu đồ tuần tự SD-04)*.  
  5. **Nghiệp vụ 5:** Nhập lô mỹ phẩm mới, Giám sát hạn sử dụng **FEFO** và Định giá xả hàng cận date tự động *(Bảng đặc tả \+ Biểu đồ hoạt động \+ Biểu đồ tuần tự SD-05)*.  
  6. **Nghiệp vụ 6:** Điều chuyển hàng hóa liên chi nhánh 3 bước (`Draft/Pending` \-\> `Approved/InTransit` \-\> `Received/Completed`) *(Bảng đặc tả \+ Biểu đồ hoạt động \+ Biểu đồ tuần tự SD-06)*.  
  7. **Nghiệp vụ 7:** Chẩn đoán da thông minh (`SmartFilter`), Kiểm tra tương kỵ hoạt chất (`SkincareIngredientInteractionEngine`), Tư vấn AI Chatbot và Nhắc lịch tái mua (`RepurchaseSchedules`) *(Bảng đặc tả \+ Biểu đồ tuần tự SD-07)*.  
  8. **Nghiệp vụ 8:** Tích/Tiêu điểm thưởng thành viên (`LoyaltyWallets`), Tự động xét thăng hạng VIP (`MemberTiers`) và Hạch toán giá vốn đích danh theo lô (`Batch-Specific COGS` & Đối soát COD) *(Bảng đặc tả \+ Biểu đồ tuần tự SD-08)*.

**2.4. Thiết kế Kiến trúc Hệ thống và Cơ sở dữ liệu Chuẩn hóa 3NF**

* **2.4.1. Thiết kế Kiến trúc tổng thể Clean Architecture 4 tầng và Biểu đồ lớp thực thi (BCE Class Diagram):**  
  * *Sơ đồ 2.18:* Kiến trúc tổng thể 4 tầng (`Omnichannel.Domain`, `Omnichannel.Application`, `Omnichannel.Infrastructure`, `Omnichannel.Web`) và luồng HTTP Pipeline.  
  * *Sơ đồ 2.19:* Biểu đồ Lớp kiến trúc BCE (Boundary – Control – Entity) cho các phân hệ cốt lõi.  
* **2.4.2. Thiết kế Ma trận Phân quyền Động (Granular Dynamic PBAC Matrix):**  
  * *Bảng 2.11:* Ma trận phân rã 51 Quyền nguyên tử (`Permission Code`) thuộc 17 Phân hệ cho 6 Vai trò (`SuperAdmin`, `StoreManager`, `Cashier`, `WarehouseStaff`, `ContentEditor`, `Customer`) và cơ chế Ghi đè quyền cá nhân (`UserPermissions Override`).  
* **2.4.3. Thiết kế Cơ sở dữ liệu quan hệ chuẩn 3NF và Sơ đồ Thực thể – Liên kết (ERD):**  
  * Tổng quan kiến trúc CSDL gồm 17 phân nhóm nghiệp vụ (65 bảng chuẩn 3NF trên Microsoft SQL Server).  
  * *Sơ đồ 2.20:* Sơ đồ quan hệ thực thể (ERD) tổng thể và các cụm phân hệ chính.  
  * **Đặc tả chi tiết cấu trúc 22 Bảng dữ liệu cốt lõi (Core Data Dictionary):** *(Mỗi bảng trình bày chuẩn 6 cột: Tên cột \| Kiểu dữ liệu C\# \| Kiểu SQL Server \| Nullable \| Ràng buộc PK/FK/Check/Index \| Ý nghĩa nghiệp vụ & Phân tích vai trò)*:  
    - *Nhóm 1 – Định danh & Phân quyền PBAC (4 bảng):* `Users`, `Roles`, `Permissions`, `RolePermissions` (& `UserPermissions`).  
    - *Nhóm 2 – Danh mục, Biến thể Mỹ phẩm & Khoa học Da liễu (5 bảng):* `Categories`, `Brands`, `Products` (Master SKU \+ Số công bố BYT), `ProductVariants` (& `ProductSwatches`), `ActiveIngredients` (& `SkinTypes`).  
    - *Nhóm 3 – Quản trị Kho Lô Hạn dùng FEFO & Điều chuyển Liên chi nhánh (5 bảng):* `Stores`, `ProductBatches` (Lõi FEFO `ManufacturingDate`, `ExpDate`), `StoreInventories` (`PhysicalQuantity`, `ReservedQuantity`, `RowVersion`), `InventoryLogs`, `StockTransfers` (& `StockTransferDetails`).  
    - *Nhóm 4 – Bán lẻ Đa kênh, Đơn hàng & Ca Thu ngân POS (4 bảng):* `Orders`, `OrderDetails` (Gắn `ProductBatchId` xuất theo FEFO), `Invoices`, `PosShifts` (& `PosDrawerTransactions`).  
    - *Nhóm 5 – Hồ sơ Sắc đẹp CRM, Ví điểm Loyalty, Khuyến mãi & Tài chính (4 bảng):* `CustomerProfiles` (& `RepurchaseSchedules`), `LoyaltyWallets` (& `MemberTiers`), `Promotions` (& `Coupons`), `FinancialLedgers` (& `CodReconciliations`).  
* **2.4.4. Thiết kế Kiến trúc Giao diện Người dùng (UI/UX Design Specifications):**  
  * Thiết kế hệ thống Token màu sắc mỹ phẩm cao cấp (*Luxury Cosmetic Palette*: Rose Gold `#B76E79`, Deep Emerald `#1B4D3E`, Pearl White `#FBFBFB`) cho Storefront.  
  * Thiết kế 4 Razor Layouts quản trị chuyên biệt theo công thái học nhân sự: `_AdminLayout.cshtml` (Điều hành chung), `_PosLayout.cshtml` (Fullscreen Kiosk \+ Phím tắt F1–F12 cho Thu ngân), `_WarehouseLayout.cshtml` (Mật độ cao \+ Thẻ màu cảnh báo hạn dùng Đỏ/Vàng/Xanh cho Thủ kho), `_CmsLayout.cshtml` (Trình dựng cây Menu đa hình & Blog cho Biên tập viên).

**Tiểu kết Chương 2**

---

#### CHƯƠNG 3: TRIỂN KHAI CÀI ĐẶT, KIỂM THỬ HỆ THỐNG VÀ ĐỊNH HƯỚNG PHÁT TRIỂN (Dự kiến: 35 – 40 trang)

**3.1. Môi trường triển khai, Tổ chức mã nguồn và Hiện thực hóa các giải pháp kỹ thuật cốt lõi**

* **3.1.1. Môi trường phát triển, công cụ cài đặt và cấu trúc tổ chức Solution (.NET 8):**  
  * Cấu trúc thư mục chi tiết của 5 Projects trong Solution (`Omnichannel.Domain`, `Omnichannel.Application`, `Omnichannel.Infrastructure`, `Omnichannel.Web`, `Omnichannel.Tests`). *(Kèm Bảng 3.1: Mô tả cấu trúc thư mục mã nguồn của hệ thống)*.  
* **3.1.2. Hiện thực hóa các giải pháp kỹ thuật và thuật toán cốt lõi (Code & Algorithm Walkthrough):** *(Học tập phong cách trình bày đỉnh cao của KL1 \- Minh Huyền kết hợp KL2 \- Quốc Việt)*  
  * *Giải pháp 1:* Động cơ Phân bổ Lô Hạn dùng **FEFO** và Khóa tồn kho 2 giai đoạn (`InventoryAllocationEngine.cs` & `OrderWorkflowService.cs`).  
  * *Giải pháp 2:* Động cơ Phân quyền Động **PBAC 3 tầng** kết hợp Bộ nhớ đệm Cache-Aside và Tự động ghi Nhật ký kiểm toán (`DynamicPermissionPolicyProvider.cs`, `PermissionAuthorizationHandler.cs`, `PermissionService.cs`).  
  * *Giải pháp 3:* Động cơ Kiểm tra Tương kỵ & Hiệp đồng Hoạt chất Da liễu và Tư vấn AI (`SkincareIngredientInteractionEngine.cs` & `ChatbotService.cs`).  
  * *Giải pháp 4:* Xử lý Thanh toán Đa phương thức (`VietQR Napas247`, Chữ ký số `HMAC-SHA512 VNPay`, `MoMo`) và Hạch toán Giá vốn Đích danh theo Lô (`PaymentService.cs` & `FinanceService.cs`).

**3.2. Kết quả đạt được về Giao diện và Chức năng hệ thống**

* **3.2.1. Phân hệ Giao diện Khách hàng (Luxury Storefront & Customer Portal):**  
  * Trang chủ Luxury, Mega Menu đa hình và Khu vực Xả hàng Cận Date (`FEFO Clearance`).  
  * Trang Chi tiết sản phẩm mỹ phẩm (Chọn biến thể Swatch màu son/dung tích, Bảng phân tích Hoạt chất & Loại da, Kiểm tra hạn dùng lô xuất dự kiến, Đánh giá thực tế).  
  * Bộ lọc Chẩn đoán Da thông minh (`SmartFilter`), Kiểm tra tương kỵ Routine Sáng/Tối và Trợ lý ảo AI Beauty Advisor.  
  * Ngăn kéo Giỏ hàng (`Mini-Cart Drawer`), Trang Thanh toán (`Checkout` tích hợp áp mã Coupon, đổi điểm Loyalty, tính phí ship GHN, quét mã VietQR/VNPay/MoMo) và Cổng Hồ sơ Sắc đẹp Cá nhân.  
* **3.2.2. Phân hệ Giao diện Quản trị Điều hành & Bán lẻ Đa kênh (Admin Back-Office & Smart POS):**  
  * Trung tâm Điều hành Quản trị (`Admin Dashboard` với 6 thẻ KPI thời gian thực, Biểu đồ doanh thu đa kênh & Chuông thông báo đẩy `SignalR`).  
  * Giao diện Quầy Thu ngân **Smart POS** (`_PosLayout`, Mở/Đóng ca làm việc, Kiểm két tiền mặt, Quét Barcode, Màn hình phụ khách hàng & In hóa đơn nhiệt K80).  
  * Giao diện Quản trị Kho Dược – Mỹ phẩm theo Lô **FEFO** (`_WarehouseLayout`, Cảnh báo màu hạn dùng Đỏ/Vàng/Xanh, Sổ cái biến động kho `InventoryLogs` và Quy trình Điều chuyển kho liên chi nhánh 3 bước).  
  * Giao diện Quản trị Hồ sơ Khách hàng CRM 360°, Ví điểm thưởng Loyalty, Chiến dịch Khuyến mãi & Trình dựng Cây Menu Đa hình CMS.  
  * Giao diện Quản trị Tài chính (Sổ cái thu chi `FinancialLedgers`, Báo cáo Lãi/Lỗ `P&L`, Công nợ nhà cung cấp `SupplierDebts`, Đối soát tiền thu hộ `COD`) và Ma trận Phân quyền Động PBAC (`51 Permissions x 6 Roles`).

**3.3. Triển khai Kiểm thử và Đánh giá chất lượng hệ thống**

* **3.3.1. Chiến lược kiểm thử đa tầng (4-Tier Testing Pyramid) và Dữ liệu khởi tạo chuẩn (Seed Data 12 tài khoản mẫu):**  
  * *(Kèm Bảng 3.2: Danh sách 12 Tài khoản kiểm thử tiêu chuẩn theo 6 Vai trò và các Chi nhánh cửa hàng trong `DbInitializer.cs`)*.  
* **3.3.2. Kết quả Kiểm thử Đơn vị và Kiểm thử Tích hợp Tự động (Automated Unit & Integration Tests với `xUnit`):**  
  * Tổng hợp kết quả thực thi 21 bộ Test tự động trong dự án `Omnichannel.Tests` (Kiểm thử Domain FEFO & Tồn kho, Kiểm thử Ca POS, Kiểm thử Điều chuyển kho, Kiểm thử Ma trận 51 quyền PBAC & Ghi đè quyền cá nhân, Kiểm thử Cô lập dữ liệu giữa Chi nhánh Quận 1 và Chi nhánh Ba Đình). *(Kèm Bảng 3.3: Bảng tổng hợp kết quả chạy 21 bộ Unit & Integration Tests)*.  
* **3.3.3. Kịch bản và Kết quả Kiểm thử Chức năng Hộp đen (Black-box Test Cases):**  
  * Thiết lập các bảng Test Case chi tiết (`TC01` đến `TC10`) kiểm tra các luồng nghiệp vụ trọng tâm và các trường hợp biên (Boundary/Exception cases). *(Kèm các Bảng 3.4 – 3.13: Chi tiết Test Case từng phân hệ)*.  
* **3.3.4. Kiểm thử Chịu tải Đồng thời & Chống Bán lố Tồn kho Flash Sale (Concurrency & Load Testing):**  
  * Kiểm chứng cơ chế khóa tồn kho 2 giai đoạn và `RowVersion` khi nhiều người dùng cùng đặt mua lô hàng cận date cuối cùng tại một thời điểm (Đảm bảo 0% lỗi âm kho hay Overselling).

**3.4. Đánh giá kết quả đạt được, Hạn chế và Đề xuất hướng phát triển (Roadmap v3.0+)**

* **3.4.1. Đánh giá mức độ hoàn thành so với Mục tiêu và Giả thuyết nghiên cứu ban đầu.**  
* **3.4.2. Một số hạn chế còn tồn tại trong phạm vi thời gian khóa luận.**  
* **3.4.3. Đề xuất lộ trình nâng cấp và phát triển hệ thống trong tương lai:**  
  1. Mở rộng cụm bộ nhớ đệm phân tán **Redis Cluster & RedLock** cho các chiến dịch Flash Sale quy mô lớn.  
  2. Tích hợp đồng bộ tồn kho và đơn hàng thời gian thực với các Sàn TMĐT (**Shopee Mall, TikTok Shop Mall, Lazada LazMall**).  
  3. Kết nối phát hành **Hóa đơn điện tử có mã của Cơ quan Thuế khởi tạo từ máy tính tiền POS** (theo Nghị định 123/2020/NĐ-CP & Thông tư 78/2021/TT-BTC).  
  4. Tích hợp công nghệ **Thử trang điểm ảo AR (Virtual Lipstick/Makeup Try-On)** và **Camera AI Soi da trực tiếp trên trình duyệt (WebAssembly \+ MediaPipe FaceMesh)**.

**Tiểu kết Chương 3**

---

### PHẦN KẾT LUẬN, TÀI LIỆU THAM KHẢO & PHỤ LỤC

* **KẾT LUẬN CHUNG** *(Dự kiến: 2 trang)*  
* **DANH MỤC TÀI LIỆU THAM KHẢO** *(Phân nhóm rõ ràng theo chuẩn IEEE / Bộ GD&ĐT: I. Văn bản pháp quy; II. Giáo trình và Sách chuyên khảo tiếng Việt; III. Tài liệu học thuật và Kỹ thuật tiếng Anh)*  
* **PHỤ LỤC (APPENDICES):**  
  * *Phụ lục A:* Toàn văn Lược đồ Cơ sở dữ liệu tổng thể 65 bảng 3NF và danh mục các bảng phụ trợ.  
  * *Phụ lục B:* Hướng dẫn cài đặt, cấu hình chuỗi kết nối SQL Server, chạy Migration và vận hành hệ thống (`Runbook`).  
  * *Phụ lục C:* Một số biểu mẫu chứng từ in ra từ hệ thống (Hóa đơn nhiệt bán lẻ POS K80, Phiếu xuất điều chuyển kho liên chi nhánh, Nhãn vận đơn GHN, Báo cáo kết quả kinh doanh P\&L xuất Excel/PDF).