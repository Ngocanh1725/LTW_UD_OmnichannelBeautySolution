# BỘ PROMPT THEO PHIÊN CHI TIẾT: VIẾT BÁO CÁO KHÓA LUẬN TỐT NGHIỆP HOÀN CHỈNH "XÂY DỰNG WEBSITE QUẢN LÝ BÁN HÀNG CHO CHUỖI CỬA HÀNG MỸ PHẨM OMNI BEAUTY"

> **Định hướng chuyên môn:** Ngành Hệ thống thông tin – Học viện Hành chính và Quản trị công (APAG)  
> **Thư mục Workspace mã nguồn & tài liệu cục bộ:** `d:\Lập trình web\OmnichannelBeautySolution`  
> **Cơ sở đối chiếu & tra cứu trực tiếp:**  
> 1. Toàn bộ mã nguồn & tài liệu nội bộ: `d:\Lập trình web\OmnichannelBeautySolution` (5 projects: Domain, Application, Infrastructure, Web, Tests)  
> 2. Đề cương chuẩn A+: [Đề Cương Tối Ưu - OMNI BEAUTY](https://docs.google.com/document/d/1eqL1HssmxmY9I26KwVlikmK_1K0Rwx4nsVBEhGH2n_Q/edit)  
> 3. Tài liệu phân tích hệ thống: [OOAD Blueprint v2.0](https://docs.google.com/document/d/1BWFXsgY0n5a-fKEUwGr7OAkCqK8WR0Id0OAMnDPLOnc/edit) (lưu tại `d:\Lập trình web\OmnichannelBeautySolution\doc\OOAD BLUEPRINT v2.0 - OMNICHANNEL BEAUTY SOLUTION.md`)  
> 4. Tài liệu báo cáo nền tảng: [Báo cáo Khóa luận Omni Beauty](https://docs.google.com/document/d/1Tne7JXEYLY5dltjgdK24up1JDotFKaonJxfIdMnHT7Y/edit) (lưu tại `d:\Lập trình web\OmnichannelBeautySolution\doc\BAO_CAO_KHOA_LUAN_OMNI_BEAUTY.md`)  
> 5. Kho mã nguồn GitHub: [ngocanh1725/ltw_ud_omnichannelbeautysolution](https://github.com/ngocanh1725/ltw_ud_omnichannelbeautysolution)

---

## HƯỚNG DẪN SỬ DỤNG ĐỂ ĐẠT CHẤT LƯỢNG CAO NHẤT (KHÔNG GIỚI HẠN SỐ TRANG)

Vì một cuốn khóa luận tốt nghiệp hoàn chỉnh có độ dài từ **130 đến 160 trang** (tương đương 45.000 – 60.000 từ), nếu yêu cầu AI viết toàn bộ trong 1–2 lần gửi thì AI chắc chắn sẽ tự động tóm tắt, cắt bớt bảng biểu và viết kiểu liệt kê sơ sài.

Do đó, bộ prompt này được chia thành **1 Prompt Thiết lập Quy tắc Gốc (Prompt 0)** và **12 Phiên viết chuyên sâu (Phiên 1 đến Phiên 12)** bám sát từng chương mục của **Đề Cương Tối Ưu - OMNI BEAUTY**:
1. **Bước 1:** Mở cuộc hội thoại mới trong Antigravity (hoặc công cụ lập trình AI có mở thư mục `d:\Lập trình web\OmnichannelBeautySolution`), gửi **PROMPT 0** để khóa chặt vai trò, văn phong sinh viên năm cuối, quy tắc chống giọng văn AI và chỉ định thư mục workspace `d:\Lập trình web\OmnichannelBeautySolution` làm căn cứ sự thật duy nhất (Single Source of Truth).
2. **Bước 2:** Gửi lần lượt từ **PHIÊN 1** đến **PHIÊN 12**. AI sẽ đọc trực tiếp mã nguồn, bảng CSDL, service, controller, view và các file ảnh trong thư mục `d:\Lập trình web\OmnichannelBeautySolution\doc` để viết chi tiết từng trang, kèm bảng biểu, sơ đồ và hướng dẫn chèn ảnh thực tế.

---

## PROMPT 0: THIẾT LẬP VAI TRÒ, WORKSPACE CỤC BỘ & QUY TẮC BẢO ĐẢM CHẤT LƯỢNG (GỬI ĐẦU TIÊN)

```text
ĐÓNG VAI TRÒ:
Bạn là Giảng viên hướng dẫn kiêm Chuyên gia cao cấp ngành Hệ thống thông tin Quản lý (MIS) và Kỹ thuật Phần mềm, đồng thời hóa thân vào vị trí một Sinh viên năm cuối ngành Hệ thống thông tin (Học viện Hành chính và Quản trị công) thực sự tâm huyết, hiểu sâu sắc từng dòng code và từng quy trình nghiệp vụ của hệ thống mình tự tay xây dựng để viết báo cáo Khóa luận tốt nghiệp:
"XÂY DỰNG WEBSITE QUẢN LÝ BÁN HÀNG CHO CHUỖI CỬA HÀNG MỸ PHẨM OMNI BEAUTY"

THƯ MỤC LÀM VIỆC (WORKSPACE CỤC BỘ) & TÀI LIỆU CĂN CỨ:
- Thư mục gốc dự án: `d:\Lập trình web\OmnichannelBeautySolution`
- Khi viết báo cáo, bạn ĐƯỢC PHÉP VÀ BẮT BUỘC tra cứu trực tiếp các file trong thư mục `d:\Lập trình web\OmnichannelBeautySolution`:
  1. `d:\Lập trình web\OmnichannelBeautySolution\doc\OOAD BLUEPRINT v2.0 - OMNICHANNEL BEAUTY SOLUTION.md`: Toàn văn thiết kế 65 bảng 3NF, 17 phân hệ, 52 điểm chạm, 9 sơ đồ tuần tự, kiến trúc BCE và thiết kế bảo mật PBAC.
  2. `d:\Lập trình web\OmnichannelBeautySolution\doc\BAO_CAO_KHOA_LUAN_OMNI_BEAUTY.md`: Toàn văn báo cáo nền tảng, thuật ngữ, số liệu đối sánh.
  3. `d:\Lập trình web\OmnichannelBeautySolution\doc\KICH_BAN_DEMO_10_PHUT_VA_BO_HOI_DAP_PHAN_BIEN.md`: Kịch bản demo 10 phút, bộ câu hỏi phản biện hội đồng và câu trả lời chuẩn kiến trúc sư.
  4. `d:\Lập trình web\OmnichannelBeautySolution\TEST_ACCOUNTS.md`: Danh sách 12 tài khoản kiểm thử tiêu chuẩn theo 6 vai trò và chi nhánh.
  5. `d:\Lập trình web\OmnichannelBeautySolution\rule.md`: Bộ quy tắc kiến trúc Clean Architecture 4 tầng.
  6. Thư mục mã nguồn thực tế:
     - `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Domain`: 65 thực thể POCO (Catalog, Inventory, Sales, POS, CRM, Loyalty, Finance, Identity...).
     - `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Application`: 25 Interfaces, DTOs, ViewModels.
     - `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Infrastructure`: ApplicationDbContext, DbInitializer, 27 Services, Dynamic PBAC Engine.
     - `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Web`: 40 Controllers (Storefront + Areas/Admin), 4 Layouts chuyên biệt, Views, SignalR Hubs.
     - `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Tests`: 231 Unit & Integration Tests vượt qua 100%.
  7. Thư mục hình ảnh & sơ đồ có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\` (Hinh_2_1 đến Hinh_2_7, Hinh_3_1 đến Hinh_3_4, Hinh_4_1 đến Hinh_4_10, Hinh_5_1, Kien_Truc_Clean_Architecture_4_Tang.png).

7 QUY TẮC BẤT DI BẤT DỊCH VỀ VĂN PHONG & CHẤT LƯỢNG (CHỐNG VĂN PHONG AI RẬP KHUÔN):
1. BỎ HOÀN TOÀN GIỚI HẠN 20 TRANG:
   - Đây là báo cáo Khóa luận tốt nghiệp hoàn chỉnh, không giới hạn số trang. Phải viết chi tiết, đầy đủ, phân tích sâu từng luận điểm, từng bảng biểu, từng sơ đồ và từng màn hình chức năng. Tuyệt đối không viết tóm tắt, không dùng dấu "..." hay ghi "(tương tự các bảng trên)".
2. VĂN PHONG SINH VIÊN NĂM CUỐI CHỮNG CHẠC, TỰ NHIÊN – KHÔNG GIẬP KHUÔN AI:
   - Viết bằng giọng văn khoa học nhưng gần gũi, rõ ràng, mạch lạc của một sinh viên năm cuối ngành Hệ thống thông tin trực tiếp đi khảo sát cửa hàng, tự thiết kế cơ sở dữ liệu và lập trình hệ thống.
   - TUYỆT ĐỐI CẤM dùng các từ ngữ sáo rỗng, khoa trương hoặc dịch máy đặc trưng của AI như: "bức tranh toàn cảnh", "kỷ nguyên số hóa bùng nổ", "kiến trúc siêu việt", "tối ưu hóa trải nghiệm đỉnh cao", "bảo chứng vàng", "hạt nhân cốt lõi không thể tách rời", "một cách toàn diện và triệt để nhất".
   - Không dùng từ ngữ quá trừu tượng, đánh đố hoặc lạm dụng thuật ngữ tiếng Anh khi đã có từ tiếng Việt tương đương dễ hiểu.
3. HẠN CHẾ TỐI ĐA ĐÓNG MỞ NGOẶC ĐƠN `(...)` VÀ LIỆT KÊ GẠCH ĐẦU DÒNG:
   - Nghiêm cấm thói quen của AI cứ viết vài chữ tiếng Việt lại mở ngoặc tiếng Anh hoặc mở ngoặc giải thích vụn vặt trong mọi câu văn. Chỉ sử dụng ngoặc đơn khi thật sự cần thiết để định danh tên bảng/tên lớp lần đầu tiên, còn lại phải diễn đạt tự nhiên thành câu văn hoàn chỉnh.
   - Không lạm dụng gạch đầu dòng liên tiếp. Trước và sau mỗi bảng biểu hoặc sơ đồ đều phải có các đoạn văn xuôi hoàn chỉnh (từ 8 đến 15 dòng) để dẫn nhập bối cảnh và phân tích ý nghĩa thực tế của bảng/sơ đồ đó.
4. TRUNG THỰC TUYỆT ĐỐI – KHÔNG BỊA ĐẶT, KHÔNG XÀO NẤU THÔNG TIN:
   - Mọi tên bảng CSDL, tên cột, kiểu dữ liệu (`INT IDENTITY(1,1)`, `DECIMAL(18,2)`, `NVARCHAR`, `ROWVERSION`), tên Controller, Service, Middleware, phân quyền PBAC (51 quyền trên 17 phân hệ, 6 vai trò, 12 tài khoản kiểm thử) phải lấy chính xác 100% từ codebase trong thư mục `d:\Lập trình web\OmnichannelBeautySolution`.
   - Các số liệu thị trường, văn bản pháp lý (Thông tư 06/2011/TT-BYT, Nghị định 52/2013/NĐ-CP, Nghị định 85/2021/NĐ-CP, Nghị định 13/2023/NĐ-CP) và tài liệu tham khảo phải trích dẫn từ nguồn có giá trị thực tế.
5. ĐẦY ĐỦ QUY TRÌNH, PHÂN QUYỀN, BẢNG BIỂU & SƠ ĐỒ:
   - Mỗi sơ đồ (Cơ cấu tổ chức, Quy trình As-Is/To-Be Swimlane, BFD, Use Case, Activity, Sequence, Class BCE, ERD) đều phải có: (1) Mã nguồn vẽ sơ đồ chuẩn (PlantUML/Mermaid) hoặc chỉ dẫn file ảnh sẵn có trong thư mục `d:\Lập trình web\OmnichannelBeautySolution\doc\`, (2) Hướng dẫn cách xuất và chèn ảnh vào Word, và (3) Đoạn văn thuyết minh chi tiết luồng đi từ đầu đến cuối.
6. BẮT BUỘC CÓ HƯỚNG DẪN CHỤP & CHÈN HÌNH ẢNH TRỰC QUAN CHO TỪNG GIAO DIỆN/SƠ ĐỒ:
   - Tại mọi mục phân tích sơ đồ hoặc giao diện hệ thống, bắt buộc phải tạo một hộp chỉ dẫn đóng khung rõ ràng theo mẫu:
     > **[HƯỚNG DẪN CHỤP & CHÈN HÌNH X.Y - TÊN HÌNH]:**
     > - **File ảnh có sẵn trong dự án (nếu có):** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_...png`
     > - **Cách tự chụp mới từ hệ thống thực tế:** Truy cập URL `http://localhost:5116/...` bằng tài khoản `...` (Mật khẩu: `Admin@123`). Thực hiện thao tác `...` để màn hình hiển thị rõ vùng `...`.
     > - **Yêu cầu bố cục ảnh:** Chụp ở độ phân giải 1920x1080, khoanh đỏ/đánh số thứ tự các vùng chức năng chính trên ảnh.
     > - **Tên chú thích hình đặt dưới ảnh (Canh giữa, in nghiêng):** *Hình X.Y: Tên hình ảnh minh họa*
7. QUY TẮC ĐÁNH SỐ ĐỀ MỤC, BẢNG BIỂU, HÌNH ẢNH ĐỒNG BỘ:
   - Đánh số bảng và hình theo số chương (ví dụ Chương 1: `Bảng 1.1`, `Hình 1.1`; Chương 2: `Bảng 2.1`, `Sơ đồ 2.1`; Chương 3: `Bảng 3.1`, `Hình 3.1`), tuyệt đối không nhảy cóc hay trùng số hiệu.

Hãy xác nhận bạn đã nắm vững 7 quy tắc trên và sẵn sàng nhận Phiên 1.
```

---

## PHIÊN 1: KHUNG TRÌNH BÀY ĐẦU BÁO CÁO (FRONT MATTER) & PHẦN MỞ ĐẦU (8 MỤC CHUẨN KHÓA LUẬN)

```text
Thực hiện viết toàn bộ KHUNG TRÌNH BÀY ĐẦU BÁO CÁO và PHẦN MỞ ĐẦU của khóa luận "Xây dựng website quản lý bán hàng cho chuỗi cửa hàng mỹ phẩm OMNI BEAUTY" theo đúng quy tắc đã thiết lập ở Prompt 0.
Tham chiếu trực tiếp các file:
- `d:\Lập trình web\OmnichannelBeautySolution\doc\BAO_CAO_KHOA_LUAN_OMNI_BEAUTY.md` (Phần Lời mở đầu, Danh mục từ viết tắt)
- `d:\Lập trình web\OmnichannelBeautySolution\README.md` (Giới thiệu dự án, bối cảnh)

YÊU CẦU NỘI DUNG CHI TIẾT CẦN VIẾT ĐẦY ĐỦ:

I. KHUNG TRÌNH BÀY ĐẦU BÁO CÁO (FRONT MATTER):
1. Trang bìa chính & Trang bìa phụ (Chuẩn Học viện Hành chính và Quản trị công - Ngành Hệ thống thông tin).
2. Lời cảm ơn & Lời cam đoan (Viết chân thành, trang trọng, đúng văn phong sinh viên năm cuối).
3. Mục lục tổng thể toàn khóa luận.
4. Danh mục các từ viết tắt và thuật ngữ chuyên ngành:
   - Kẻ bảng đầy đủ 3 cột: `Từ viết tắt / Thuật ngữ | Tên tiếng Anh đầy đủ | Giải thích ý nghĩa và cách ứng dụng thực tế tại hệ thống OMNI BEAUTY`.
   - Bao gồm tối thiểu 25 thuật ngữ thực tế có trong dự án: 3NF, ACID, API, BCE, BOPIS (Click & Collect), CFD, COD, COGS, DIP, DTO, EAN-13, EF Core, EWG, FEFO, FIFO, HTMX, O2O, ORM, P&L, PAO, PBAC, POS, SEO, SKU, UI/UX.
5. Danh mục Bảng biểu, Danh mục Sơ đồ & Biểu đồ, Danh mục Hình ảnh giao diện (Liệt kê đầy đủ số hiệu từ Chương 1 đến Chương 3 để đối chiếu).

II. PHẦN MỞ ĐẦU (Viết sâu sắc, mạch lạc thành từng đoạn văn hoàn chỉnh cho đủ 8 mục):
1. Lý do chọn đề tài:
   - Phân tích bối cảnh chuyển đổi số ngành bán lẻ mỹ phẩm tại Việt Nam và sự dịch chuyển từ bán hàng đơn kênh sang mô hình bán lẻ đa kênh hợp nhất (Omnichannel O2O).
   - Đi sâu vào đặc thù khắt khe của mặt hàng mỹ phẩm: một sản phẩm có nhiều biến thể màu sắc (Swatch son, tông phấn) và dung tích (ml/g), nhạy cảm với hạn sử dụng theo lô sản xuất và hạn dùng sau mở nắp, đồng thời khách hàng cần được tư vấn hoạt chất phù hợp với từng loại da để tránh kích ứng.
   - Phân tích thực trạng quản lý rời rạc tại chuỗi cửa hàng OMNI BEAUTY (gồm Tổng kho trung tâm HQ-001 và các Showroom chi nhánh Cầu Giấy, Quận 1): tình trạng lệch tồn kho giữa kênh Online và quầy thu ngân dẫn đến bán quá số lượng thực có, nhân viên xuất hàng theo cảm tính làm lô cũ hết hạn nằm tồn trong kho, sai sót khi kiểm két tiền mặt cuối ca POS và đối soát tiền thu hộ COD. Từ đó khẳng định tính cấp thiết của đề tài.
2. Tổng quan tình hình nghiên cứu (Literature Review):
   - 2.1. Tình hình nghiên cứu và ứng dụng trong nước: Phân tích số liệu báo cáo VECOM, cách vận hành của các chuỗi lớn như Hasaki, Guardian, các phần mềm đóng gói KiotViet, Sapo và nhận xét ưu/nhược điểm của các đề tài khóa luận quản lý bán hàng trước đó. Chỉ rõ khoảng trống nghiên cứu mà các hệ thống thông thường chưa giải quyết sâu: cơ chế cấp phát lô tự động theo hạn dùng FEFO hai giai đoạn, công cụ kiểm tra xung đột hoạt chất mỹ phẩm và cơ chế phân quyền động PBAC cô lập dữ liệu từng chi nhánh.
   - 2.2. Tình hình nghiên cứu và ứng dụng trên thế giới: Phân tích các nghiên cứu về kiến trúc thương mại hợp nhất (Unified Commerce theo Gartner/Forrester), mô hình O2O của Sephora, Olive Young và xu hướng ứng dụng trí tuệ nhân tạo hỗ trợ chẩn đoán da cá nhân hóa.
3. Mục tiêu và nhiệm vụ nghiên cứu:
   - Mục tiêu tổng quát và 6 mục tiêu cụ thể: (1) Quản trị bảo mật đa chi nhánh với phân quyền động PBAC 3 tầng và cách ly phiên đăng nhập; (2) Tối ưu trải nghiệm mua sắm mỹ phẩm trực tuyến với tư vấn chu trình dưỡng da, bảng thành phần EWG động và kiểm tra tương kỵ hoạt chất; (3) Tự động hóa quầy thu ngân Smart POS, quản lý ca làm việc, kiểm két tiền mặt và nhận đơn O2O tại quầy; (4) Quản trị kho lô theo hạn dùng FEFO, giữ chỗ tồn kho 2 giai đoạn và điều chuyển liên chi nhánh 3 bước; (5) Quản trị hồ sơ làn da khách hàng CRM, đánh giá đồng điệu theo loại da và ví điểm thưởng thành viên Loyalty; (6) Số hóa sổ cái tài chính, tính giá vốn đích danh theo lô xuất và cảnh báo vận hành thời gian thực qua SignalR.
   - Nhiệm vụ nghiên cứu về lý thuyết và thực tiễn.
4. Đối tượng và phạm vi nghiên cứu:
   - Đối tượng nghiên cứu.
   - Phạm vi nghiên cứu tách rõ 4 khía cạnh: Phạm vi nội dung (Phân hệ Storefront và Phân hệ Quản trị đa kênh), Phạm vi không gian (Chuỗi OMNI BEAUTY: Kho Tổng HQ-001 và các Showroom chi nhánh), Phạm vi thời gian, và Phạm vi khách thể (6 nhóm tác nhân người dùng cùng các cổng đối tác VNPay, MoMo, VietQR, GHN).
5. Phương pháp và công cụ thực hiện đề tài:
   - Phương pháp nghiên cứu: Khảo sát nghiệp vụ, phân tích thiết kế hướng đối tượng OOAD với UML 2.5 và mô hình BCE, chuẩn hóa CSDL dạng chuẩn 3 (3NF), lập trình Clean Architecture 4 tầng và kiểm thử tự động đa tầng.
   - Công cụ và công nghệ sử dụng: C# 12, ASP.NET Core 8.0, EF Core 8, SQL Server, SignalR, Tailwind CSS, Alpine.js, HTMX, xUnit.
6. Giả thuyết nghiên cứu: Trình bày rõ các giả thuyết khoa học về việc triệt tiêu lỗi bán lố tồn kho, giảm tỷ lệ hủy hàng cận date nhờ FEFO và rút ngắn thời gian thanh toán POS.
7. Đóng góp của khóa luận (Về mặt lý luận học thuật và về mặt thực tiễn vận hành).
8. Bố cục của khóa luận (Giới thiệu mạch lạc nội dung 3 chương).

LƯU Ý: Viết văn xuôi mạch lạc, hạn chế tối đa đóng mở ngoặc đơn và không viết liệt kê khô khan.
```

---

## PHIÊN 2: CHƯƠNG 1 – CƠ SỞ LÝ LUẬN, PHÁP LÝ NGÀNH MỸ PHẨM, CÔNG NGHỆ .NET 8 & TỔNG QUAN CHUỖI OMNI BEAUTY

```text
Tiếp tục viết trọn vẹn CHƯƠNG 1: CƠ SỞ LÝ LUẬN VỀ HỆ THỐNG THÔNG TIN QUẢN LÝ BÁN HÀNG ĐA KÊNH VÀ TỔNG QUAN CHUỖI CỬA HÀNG MỸ PHẨM OMNI BEAUTY.
Tham chiếu trực tiếp:
- `d:\Lập trình web\OmnichannelBeautySolution\rule.md` (Kiến trúc Clean Architecture 4 tầng)
- `d:\Lập trình web\OmnichannelBeautySolution\doc\BAO_CAO_KHOA_LUAN_OMNI_BEAUTY.md` (Chương 1)
- `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Infrastructure\Data\ApplicationDbContext.cs` (Cơ sở dữ liệu & EF Core 8)

YÊU CẦU NỘI DUNG CHI TIẾT TỪNG MỤC TRONG CHƯƠNG 1:

1.1. Cơ sở lý luận về Hệ thống thông tin quản lý và Thương mại điện tử đa kênh (Omnichannel)
- 1.1.1. Hệ thống thông tin và Hệ thống thông tin quản lý trong doanh nghiệp bán lẻ chuỗi: Phân tích vai trò của hệ thống thông tin trong việc kết nối luồng hàng hóa vật lý, luồng giao dịch tài chính và luồng thông tin điều hành giữa trụ sở chính với các điểm bán.
- 1.1.2. Mô hình Thương mại điện tử B2C kết hợp Bán lẻ hợp nhất đa kênh (Omnichannel O2O):
  + Phân tích sự khác biệt bản chất giữa bán hàng đơn kênh, bán hàng đa kênh rời rạc và bán hàng đa kênh hợp nhất.
  + Lập **Bảng 1.1: Bảng đối sánh đặc trưng kỹ thuật giữa mô hình Đa kênh rời rạc (Multi-channel) và Đa kênh hợp nhất (Omnichannel) tại chuỗi OMNI BEAUTY** theo các tiêu chí: Kiến trúc cơ sở dữ liệu, Đồng bộ tồn kho thời gian thực, Liên thông đơn hàng O2O (Mua trên web - Nhận tại Showroom trong 1 giờ), Đồng bộ hồ sơ da và ví điểm thưởng, Hạch toán tài chính. Viết đoạn văn phân tích kỹ bảng này.
- 1.1.3. Đặc thù quản trị hàng hóa và chuỗi cung ứng trong ngành Dược – Mỹ phẩm:
  + Quản lý danh mục đa biến thể (màu sắc Swatch mã Hex, kết cấu chất son/kem, dung tích ml/g) và phân loại theo hoạt chất khoa học, điểm an toàn EWG, chứng nhận Clean Beauty (Không cồn khô, Không hương liệu, An toàn cho mẹ bầu).
  + Nguyên tắc quản trị kho theo lô sản xuất và hạn sử dụng FEFO (Hết hạn trước - Xuất trước) kết hợp hạn dùng sau mở nắp (PAO). Giải thích tại sao phương pháp FIFO hay bình quân gia quyền truyền thống đều thất bại khi áp dụng cho mỹ phẩm.
  + Phương pháp hạch toán giá vốn đích danh theo từng lô xuất thực tế và kiểm soát ca làm việc tại quầy thu ngân POS.

1.2. Cơ sở pháp lý điều chỉnh hoạt động kinh doanh mỹ phẩm trực tuyến và sự đáp ứng của hệ thống OMNI BEAUTY
- 1.2.1. Quy định về quản lý và công bố sản phẩm mỹ phẩm (Thông tư số 06/2011/TT-BYT của Bộ Y tế và Hiệp định Mỹ phẩm ASEAN):
  + Phân tích các quy định bắt buộc về Số phiếu tiếp nhận công bố sản phẩm mỹ phẩm, nguồn gốc xuất xứ, bảng thành phần INCI, số lô sản xuất và hạn sử dụng.
  + Phân tích cụ thể cách hệ thống OMNI BEAUTY thiết kế cơ sở dữ liệu và nghiệp vụ để tuân thủ: lưu trữ trường `CosmeticNotificationNumber`, bảng phân tích thành phần hoạt chất tự động, và quy tắc tự động khóa không cho xuất bán các lô hàng có hạn sử dụng dưới ngưỡng an toàn.
- 1.2.2. Quy định về giao dịch thương mại điện tử và bảo vệ dữ liệu cá nhân (Nghị định số 52/2013/NĐ-CP, Nghị định số 85/2021/NĐ-CP và Nghị định số 13/2023/NĐ-CP):
  + Phân tích trách nhiệm minh bạch thông tin hàng hóa, giá bán, điều kiện giao dịch và bảo vệ dữ liệu nhạy cảm của người tiêu dùng (hồ sơ tình trạng da, lịch sử dị ứng mỹ phẩm, số điện thoại, địa chỉ).
  + Liên hệ trực tiếp với cơ chế mã hóa mật khẩu một chiều PBKDF2-SHA256, cơ chế tách biệt phiên đăng nhập Multi-Scheme Cookie (`OmniBeauty.Admin.Session` và `OmniBeauty.Customer.Session`), kiểm soát truy cập đa tầng PBAC và nhật ký kiểm toán tự động `AuditLogs` trong dự án.

1.3. Cơ sở lý luận về nền tảng công nghệ phát triển hệ thống
- 1.3.1. Nền tảng ASP.NET Core 8.0, ngôn ngữ C# 12 và kiến trúc Clean Architecture 4 tầng:
  + Phân tích lý thuyết về kiến trúc sạch và nguyên lý Đảo ngược phụ thuộc, giải thích rõ vai trò của 4 tầng trong dự án (`Omnichannel.Domain`, `Omnichannel.Application`, `Omnichannel.Infrastructure`, `Omnichannel.Web`).
- 1.3.2. Hệ quản trị cơ sở dữ liệu Microsoft SQL Server và công nghệ ánh xạ đối tượng Entity Framework Core 8:
  + Phân tích tính chất ACID trong giao dịch trừ kho/thanh toán, cơ chế khóa lạc quan bằng dấu thời gian `RowVersion` chống xung đột khi nhiều người mua cùng lúc, và cơ chế tự động thử lại kết nối `EnableRetryOnFailure`.
- 1.3.3. Công nghệ giao tiếp thời gian thực SignalR, giao diện hiện đại (Tailwind CSS, HTMX, Alpine.js) và tích hợp cổng đối tác (VietQR Napas247, VNPay, MoMo, Giao Hàng Nhanh):
  + Lập **Bảng 1.2: Bảng so sánh và luận chứng lựa chọn giải pháp công nghệ phát triển hệ thống OMNI BEAUTY**.
- 1.3.4. Phương pháp phân tích thiết kế hệ thống hướng đối tượng (OOAD với ngôn ngữ UML 2.5 và mô hình Boundary – Control – Entity).

1.4. Tổng quan về Chuỗi cửa hàng mỹ phẩm OMNI BEAUTY
- 1.4.1. Giới thiệu chung, tầm nhìn, sứ mệnh và mô hình chuỗi điểm bán (Tổng kho phân phối trung tâm HQ-001 và hệ thống các Showroom bán lẻ tại Cầu Giấy - Hà Nội, Quận 1 - TP.HCM).
- 1.4.2. Cơ cấu tổ chức nhân sự và chức năng nhiệm vụ của các bộ phận:
  + Phân tích chi tiết chức năng của Ban Giám đốc, Quản lý chi nhánh, Bộ phận Thu ngân và Tư vấn viên tại quầy, Bộ phận Kho vận, Bộ phận Nội dung & Marketing, Bộ phận Tài chính – Kế toán.
  + Cung cấp mã vẽ **Sơ đồ 1.1: Sơ đồ cơ cấu tổ chức quản lý của chuỗi cửa hàng mỹ phẩm OMNI BEAUTY** kèm hộp **[HƯỚNG DẪN CHÈN SƠ ĐỒ 1.1]** và đoạn văn phân tích mối liên hệ giữa cơ cấu tổ chức này với việc thiết kế 6 vai trò phân quyền trên phần mềm.

Kết thúc bằng mục **Tiểu kết Chương 1**.
LƯU Ý: Trình bày sâu sắc, văn phong tự nhiên như sinh viên năm cuối viết khóa luận, hạn chế tối đa đóng mở ngoặc đơn và liệt kê vụn vặt.
```

---

## PHIÊN 3: CHƯƠNG 2 (PHẦN 2.1 & 2.2) – KHẢO SÁT HIỆN TRẠNG, QUY TRÌNH AS-IS / TO-BE, TÁC NHÂN, BIỂU ĐỒ BFD & PHÂN TÍCH YÊU CẦU 17 PHÂN HỆ

```text
Tiếp tục viết CHƯƠNG 2: KHẢO SÁT, PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG WEBSITE QUẢN LÝ BÁN HÀNG CHO CHUỖI CỬA HÀNG MỸ PHẨM OMNI BEAUTY – Tập trung hoàn thiện trọn vẹn Mục 2.1 và Mục 2.2.
Tham chiếu trực tiếp:
- `d:\Lập trình web\OmnichannelBeautySolution\doc\OOAD BLUEPRINT v2.0 - OMNICHANNEL BEAUTY SOLUTION.md` (Mục 1.2 Bảng 52 điểm chạm thực tế, Phần 2 Phân quyền PBAC, Phần 3 Phân hệ nghiệp vụ)
- File ảnh sơ đồ luồng hàng: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_1_Luong_Van_Hanh_Hang_Hoa.png`

YÊU CẦU NỘI DUNG CHI TIẾT CẦN ĐẠT ĐỘ SÂU TỐI ĐA:

2.1. Khảo sát hiện trạng và xác lập bài toán nghiệp vụ tại chuỗi cửa hàng OMNI BEAUTY
- 2.1.1. Khảo sát quy trình hoạt động nghiệp vụ hiện tại (Quy trình thủ công / bán tự động trước khi có hệ thống):
  + Mô tả chi tiết quy trình khách mua hàng trực tiếp tại Showroom, quy trình nhân viên chốt đơn qua tin nhắn mạng xã hội, quy trình thủ kho theo dõi hạn sử dụng mỹ phẩm trên bảng tính Excel rời rạc và quy trình thu ngân bàn giao tiền mặt cuối ca.
  + Cung cấp mã vẽ **Sơ đồ 2.1: Sơ đồ quy trình bán hàng và quản lý kho hiện tại (As-Is)** kèm hộp **[HƯỚNG DẪN CHÈN SƠ ĐỒ 2.1]**.
- 2.1.2. Đánh giá các hạn chế và điểm nghẽn của mô hình hiện tại:
  + Phân tích sâu 5 vấn đề thực tế: (1) Độ trễ đồng bộ tồn kho giữa kênh Online và Showroom gây ra tình trạng khách đặt xong mới báo hết hàng; (2) Xuất kho không theo thứ tự hạn dùng khiến lô mới bán hết trước trong khi lô cũ bị cận date nằm sâu trong kho; (3) Thiếu công cụ tra cứu nhanh thành phần hoạt chất và lịch sử kích ứng da của khách hàng; (4) Khó kiểm soát chênh lệch tiền mặt tại két thu ngân POS khi giao ca và đối soát tiền thu hộ COD; (5) Chưa có cơ chế cô lập dữ liệu giữa các chi nhánh và thiếu công cụ chỉnh sửa giao diện chiến dịch linh hoạt cho bộ phận Marketing.
- 2.1.3. Đề xuất quy trình nghiệp vụ bán lẻ đa kênh số hóa mới (Quy trình To-Be):
  + Thuyết minh luồng vận hành khép kín từ lúc nhập lô hàng từ nhà cung cấp vào Tổng kho, điều chuyển về các Showroom, phân bổ tự động theo thuật toán FEFO cho cả kênh Website và quầy POS, cho đến khi hạch toán vào sổ cái tài chính.
  + Cung cấp mã PlantUML/Mermaid và hộp **[HƯỚNG DẪN CHÈN HÌNH 2.1 / SƠ ĐỒ 2.2]** sử dụng trực tiếp file ảnh có sẵn trong dự án: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_1_Luong_Van_Hanh_Hang_Hoa.png` (*Sơ đồ luồng vận hành hàng hóa và dữ liệu đa kênh đề xuất tại OMNI BEAUTY*). Phân tích kỹ từng bước trên sơ đồ này.

2.2. Phân tích yêu cầu hệ thống và Mô hình hóa chức năng tổng quát
- 2.2.1. Xác định các tác nhân hệ thống (Actors) và phạm vi truy cập dữ liệu:
  + Phân tích chi tiết 6 tác nhân con người (`SuperAdmin`, `StoreManager`, `Cashier`, `WarehouseStaff`, `ContentEditor`, `Customer`), 1 tác nhân tiến trình ngầm (`AlertBackgroundWorker`) và 4 tác nhân hệ thống đối tác (`VNPay`, `MoMo`, `VietQR Napas247`, `Giao Hàng Nhanh - GHN`).
  + Lập **Bảng 2.1: Bảng đặc tả chi tiết các tác nhân tham gia hệ thống OMNI BEAUTY** (Gồm: Mã tác nhân, Tên vai trò, Phạm vi chi nhánh quản lý, Cookie/Cơ chế xác thực và Trách nhiệm nghiệp vụ chính).
- 2.2.2. Xây dựng Biểu đồ phân rã chức năng (BFD - Business Function Decomposition):
  + Cung cấp sơ đồ phân cấp chức năng tổng quát **Sơ đồ 2.3: Biểu đồ phân rã chức năng hệ thống quản lý bán hàng chuỗi mỹ phẩm OMNI BEAUTY** chia thành 2 khối lớn (Khối Khách hàng Storefront & Khối Quản trị Điều hành Back-Office) và phân rã xuống các nhánh chức năng chi tiết. Kèm hộp **[HƯỚNG DẪN CHÈN SƠ ĐỒ 2.3]** và đoạn văn giải thích.
- 2.2.3. Phân tích chi tiết yêu cầu chức năng theo 17 phân hệ nghiệp vụ thực tế của hệ thống:
  + Phân tích đầy đủ, rõ ràng từng phân hệ trong tổng số 17 phân hệ thực tế của `OmnichannelBeautySolution`:
    1. Phân hệ Quản lý Điểm bán lẻ trực tiếp (Smart POS), Ca làm việc và Giao nhận đơn O2O Click & Collect.
    2. Phân hệ Quản trị Kho Dược – Mỹ phẩm và Giám sát Hạn dùng theo nguyên tắc FEFO.
    3. Phân hệ Quản lý Đặt hàng Đa kênh và Cấp phát Tồn kho 2 giai đoạn.
    4. Phân hệ Điều chuyển Hàng hóa Liên chi nhánh 3 bước.
    5. Phân hệ Quản trị Quan hệ Khách hàng (CRM), Hồ sơ Làn da và Đánh giá đồng điệu Skin Twin.
    6. Phân hệ Chương trình Thành viên VIP và Ví điểm thưởng Loyalty.
    7. Phân hệ Động cơ Khuyến mãi Chiến dịch, Flash Sale và Mã giảm giá Coupon.
    8. Phân hệ Kế toán Sổ cái, Hạch toán Giá vốn đích danh theo lô (Batch-Specific COGS) và Đối soát COD/Công nợ.
    9. Phân hệ Bảo mật Động, Cách ly Phiên Multi-Scheme Cookie và Ma trận Phân quyền Hạt nhân PBAC.
    10. Phân hệ Tư vấn Da thông minh (Routine Quiz AI), Phòng Lab Kiểm tra Tương kỵ Hoạt chất và Chatbot 24/7.
    11. Phân hệ Quản trị Danh mục, Sản phẩm Đa biến thể (Swatch màu/Dung tích), Bảng thành phần EWG động và Khay So sánh Giá/ml.
    12. Phân hệ Quản trị Cây Menu Đa hình và Chuyển hướng SEO 301 tự động.
    13. Phân hệ Quản trị Nội dung Blog, Cẩm nang Làm đẹp và Trang Tĩnh.
    14. Phân hệ Quản trị Banner, Chiến dịch Truyền thông và Trình Tùy biến Giao diện Trực quan (No-Code Visual Site Customizer).
    15. Phân hệ Kiểm duyệt Đánh giá Thực tế và Chương trình Tặng Mẫu thử (Free Sample Bar tại Giỏ hàng).
    16. Phân hệ Giám sát Tự động và Cảnh báo Vận hành Thông minh (Background Worker).
    17. Phân hệ Thông báo Đẩy Thời gian thực (SignalR) và Hộp thư Điều hành Nội bộ.
  + Tổng hợp lại trong **Bảng 2.2: Bảng ma trận ánh xạ các điểm chạm chức năng với Controller, Service và Bảng cơ sở dữ liệu tương ứng** (lấy từ bảng 52 điểm chạm trong `OOAD BLUEPRINT v2.0`).
- 2.2.4. Phân tích yêu cầu phi chức năng:
  + Phân tích sâu về: (1) Hiệu năng và bộ nhớ đệm đa tầng `IMemoryCache`; (2) Tính toàn vẹn giao dịch ACID và chống xung đột đồng thời bằng `RowVersion`; (3) Bảo mật đa lớp (PBKDF2-SHA256, chống CSRF/XSS/IDOR, chặn Admin mua hàng bằng `BlockAdminShoppingAttribute`, ghi vết `AuditLogs` tự động qua EF Core Interceptor); (4) Tính công thái học của 4 giao diện Layout chuyên biệt.

LƯU Ý: Viết diễn giải bằng văn xuôi mạch lạc, tự nhiên, hạn chế tối đa đóng mở ngoặc đơn.
```

---

## PHIÊN 4: CHƯƠNG 2 (PHẦN 2.3 - TẬP 1) – MÔ HÌNH HÓA USE CASE TỔNG QUÁT, PHÂN RÃ THEO TÁC NHÂN & ĐẶC TẢ CHI TIẾT NGHIỆP VỤ 1 ĐẾN 4

```text
Tiếp tục viết CHƯƠNG 2 – Mục 2.3: Mô hình hóa nghiệp vụ hướng đối tượng bằng UML (Phần 1: Hệ thống Biểu đồ Use Case tổng quát, các Biểu đồ Use Case phân rã và Đặc tả chi tiết + Activity/Sequence Diagram cho 4 nghiệp vụ cốt lõi đầu tiên).
Tham chiếu trực tiếp:
- Thư mục ảnh Use Case: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_2_So_Do_Use_Case_Tong_Quat.png`, `Hinh_2_3_...png` đến `Hinh_2_7_...png`
- Thư mục ảnh Sequence Diagram: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_3_1_So_Do_Tuan_Tu_SD01.png` đến `Hinh_3_3_So_Do_Tuan_Tu_SD03.png`
- Mã PlantUML nguồn: `d:\Lập trình web\OmnichannelBeautySolution\doc\generate_plantuml_hinh2_*.js` và `generate_plantuml_sd*.js`

YÊU CẦU NỘI DUNG CHI TIẾT:

2.3.1. Mô hình hóa Use Case tổng quát và phân rã theo nhóm phân hệ / tác nhân:
- Trình bày và phân tích đầy đủ 6 sơ đồ Use Case chuẩn UML (kèm mã PlantUML và hộp **[HƯỚNG DẪN CHÈN HÌNH]** trỏ trực tiếp tới các file ảnh đã có sẵn trong thư mục `d:\Lập trình web\OmnichannelBeautySolution\doc\`):
  1. **Sơ đồ 2.4 (Tương ứng file `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_2_So_Do_Use_Case_Tong_Quat.png`):** Biểu đồ Use Case tổng quát của hệ thống OMNI BEAUTY.
  2. **Sơ đồ 2.5 (Tương ứng file `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_3_So_Do_Use_Case_Chi_Tiet_UC01.png`):** Biểu đồ Use Case phân rã nhóm chức năng Quản trị Bảo mật và Phân quyền Động PBAC.
  3. **Sơ đồ 2.6 (Tương ứng file `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_4_So_Do_Use_Case_Chi_Tiet_UC02.png`):** Biểu đồ Use Case phân rã nhóm chức năng Quản trị Kho Lô Hạn dùng FEFO và Điều chuyển Liên chi nhánh.
  4. **Sơ đồ 2.7 (Tương ứng file `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_5_So_Do_Use_Case_Chi_Tiet_UC03.png`):** Biểu đồ Use Case phân rã nhóm chức năng Bán hàng tại quầy Smart POS và Quản lý Ca thu ngân.
  5. **Sơ đồ 2.8 (Tương ứng file `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_6_So_Do_Use_Case_Chi_Tiet_UC04.png`):** Biểu đồ Use Case phân rã nhóm chức năng Mua sắm Trực tuyến, Đặt hàng Đa kênh và Thanh toán.
  6. **Sơ đồ 2.9 (Tương ứng file `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_2_7_So_Do_Use_Case_Chi_Tiet_UC05.png`):** Biểu đồ Use Case phân rã nhóm chức năng Tư vấn Da AI, Hồ sơ Sắc đẹp CRM và Ví điểm Loyalty.
- Dưới mỗi sơ đồ, viết đoạn văn thuyết minh rõ các quan hệ `<<include>>`, `<<extend>>` và ý nghĩa nghiệp vụ của từng ca sử dụng.

2.3.2. Đặc tả chi tiết Use Case, Biểu đồ Hoạt động (Activity) và Biểu đồ Tuần tự (Sequence) cho các nghiệp vụ cốt lõi (Từ Nghiệp vụ 1 đến Nghiệp vụ 4):
Với MỖI nghiệp vụ trong 4 nghiệp vụ dưới đây, bắt buộc trình bày đủ 3 thành phần: (A) Bảng đặc tả Use Case chuẩn 10 mục (Mã UC, Tên UC, Tác nhân, Mô tả, Tiền điều kiện, Hậu điều kiện, Luồng sự kiện chính từng bước, Luồng rẽ nhánh/Ngoại lệ, Quy tắc nghiệp vụ ràng buộc, Yêu cầu đặc biệt); (B) Biểu đồ Hoạt động hoặc Biểu đồ Tuần tự chuẩn UML kèm hướng dẫn chèn ảnh; (C) Đoạn văn phân tích sâu cơ chế kiểm soát dữ liệu ở tầng Backend:
- **Nghiệp vụ 1 (UC-01 & SD-01):** Quản trị Ma trận Phân quyền Động 3 tầng (Dynamic PBAC) và Phân giải quyền hạn khi xử lý yêu cầu HTTP.
  + Hướng dẫn chèn trực tiếp file ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_3_1_So_Do_Tuan_Tu_SD01.png`.
  + Phân tích rõ quy trình đối chiếu ưu tiên 3 bước: Quyền tối cao `SuperAdmin` -> Quyền ghi đè cá nhân trong `UserPermissions` (`IsGranted = true/false`) -> Quyền kế thừa từ vai trò trong `RolePermissions`, kết hợp cơ chế lưu bộ nhớ đệm `IMemoryCache` và thu hồi quyền tức thời qua `SecurityStamp`.
- **Nghiệp vụ 2 (UC-02 & SD-02):** Đặt hàng trực tuyến (Giao tận nơi hoặc Nhận tại Showroom O2O 1H) và Cấp phát – Trừ tồn kho 2 giai đoạn theo thuật toán FEFO.
  + Hướng dẫn chèn trực tiếp file ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_3_2_So_Do_Tuan_Tu_SD02.png`.
  + Phân tích sâu thuật toán FEFO trong `InventoryAllocationEngine`: loại bỏ lô đã hết hạn hoặc chạm ngưỡng khóa an toàn, sắp xếp tăng dần theo `ExpDate`, thực hiện giữ chỗ mềm (`ReservedQuantity`) ở Giai đoạn 1 khi tạo đơn và trừ tồn kho vật lý (`PhysicalQuantity`) ở Giai đoạn 2 khi xuất hàng/thanh toán thành công, bọc trong Transaction có kiểm tra `RowVersion`.
- **Nghiệp vụ 3 (UC-03 & SD-03):** Bán hàng tại quầy Smart POS, Đồng bộ màn hình phụ khách hàng (Dual-Screen CFD), Xác thực đơn O2O Click & Collect và Thanh toán VietQR/Tiền mặt tự động in hóa đơn nhiệt.
  + Cung cấp mã PlantUML vẽ sơ đồ tuần tự, hộp hướng dẫn chèn sơ đồ và phân tích luồng quét mã vạch SKU/Barcode, tra cứu lô FEFO tại kho chi nhánh của thu ngân, sinh mã QR động Napas247 và trừ kho trực tiếp.
- **Nghiệp vụ 4 (UC-04 & SD-04):** Quản lý Vòng đời Ca thu ngân POS (`PosShifts`) và Kiểm toán – Đối soát Két tiền mặt (`PosDrawerTransactions`).
  + Hướng dẫn chèn trực tiếp file ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_3_3_So_Do_Tuan_Tu_SD03.png`.
  + Phân tích công thức tính tiền mặt lý thuyết trong két (`ExpectedCashBalance = OpeningCash + CashSalesTotal + CashInTotal - CashOutTotal - CashRefundTotal`), đối chiếu với tiền mặt kiểm đếm thực tế (`ActualCashCounted`) khi đóng ca để tính độ lệch (`DiscrepancyAmount`) và bắt buộc giải trình nếu lệch két.

LƯU Ý: Trình bày chi tiết từng bước trong bảng đặc tả, văn phong mạch lạc, không viết tắt hay cắt gọn.
```

---

## PHIÊN 5: CHƯƠNG 2 (PHẦN 2.3 - TẬP 2) – ĐẶC TẢ CHI TIẾT USE CASE & BIỂU ĐỒ TUẦN TỰ CHO NGHIỆP VỤ 5 ĐẾN 8

```text
Tiếp tục viết phần còn lại của Mục 2.3 trong CHƯƠNG 2: Đặc tả chi tiết Use Case, Biểu đồ Hoạt động và Biểu đồ Tuần tự (Sequence Diagrams) cho 4 nghiệp vụ cốt lõi tiếp theo (Từ Nghiệp vụ 5 đến Nghiệp vụ 8).
Tham chiếu trực tiếp:
- `d:\Lập trình web\OmnichannelBeautySolution\doc\OOAD BLUEPRINT v2.0 - OMNICHANNEL BEAUTY SOLUTION.md` (Phần 4: SD-05 đến SD-09)
- File ảnh sơ đồ tuần tự điều chuyển kho: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_3_4_So_Do_Tuan_Tu_SD04.png`
- Code thực tế: `Omnichannel.Infrastructure/Services/StockTransferService.cs`, `SkincareIngredientInteractionEngine.cs`, `FinanceService.cs`

YÊU CẦU NỘI DUNG CHI TIẾT (Mỗi nghiệp vụ đều phải có Bảng đặc tả 10 mục đầy đủ + Mã vẽ Biểu đồ Tuần tự & Hướng dẫn chèn ảnh + Đoạn văn phân tích kỹ thuật sâu sắc):

- **Nghiệp vụ 5 (UC-05 & SD-05):** Nhập lô mỹ phẩm mới vào kho, Giám sát hạn sử dụng FEFO và Định giá xả hàng cận date tự động (`ClearanceDiscountPercent`).
  + Phân tích quy trình thủ kho kiểm tra Số phiếu công bố Bộ Y tế, nhập Mã lô sản xuất (`BatchNumber`), Ngày sản xuất (`ManufacturingDate`), Hạn sử dụng (`ExpDate`), Giá vốn nhập lô (`UnitCost`) vào bảng `ProductBatches` và cộng tồn kho vào `StoreInventories` kèm bản ghi nhật ký `InventoryLogs`.
  + Phân tích tiến trình `AlertBackgroundWorker` tự động quét các lô sắp hết hạn theo các mốc 180 ngày, 90 ngày, 30 ngày để chuyển trạng thái `NEAR_EXPIRATION`, kích hoạt mức giảm giá xả kho cận date trên trang chủ và khóa tự động các lô đã hết hạn (`EXPIRED`).
- **Nghiệp vụ 6 (UC-06 & SD-06):** Điều chuyển hàng hóa liên chi nhánh 3 bước bảo toàn tổng tồn kho (`StockTransfers`).
  + Hướng dẫn chèn trực tiếp file ảnh có sẵn trong dự án: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_3_4_So_Do_Tuan_Tu_SD04.png`.
  + Phân tích chặt chẽ máy trạng thái 3 bước:
    * Bước 1 (`PENDING`): Quản lý chi nhánh tạo phiếu yêu cầu điều chuyển.
    * Bước 2 (`IN_TRANSIT`): Quản trị viên hoặc Kho xuất phê duyệt (`ApproveAndShipAsync`), hệ thống trừ `PhysicalQuantity` tại kho xuất theo đúng lô `ProductBatchId` và ghi log `TRANSFER_OUT`.
    * Bước 3 (`COMPLETED`): Thủ kho chi nhánh đích kiểm đếm thực tế và bấm xác nhận nhận hàng (`ReceiveTransferAsync`), hệ thống cộng `PhysicalQuantity` vào kho nhận cho đúng lô hàng đó và ghi log `TRANSFER_IN`. Nếu số lượng thực nhận thấp hơn số lượng xuất thì tự động tạo cảnh báo lệch kho mức nghiêm trọng.
- **Nghiệp vụ 7 (UC-07 & SD-07):** Trắc nghiệm Chẩn đoán Da thông minh (`RoutineQuiz` / `SmartFilter`), Kiểm tra tương kỵ hoạt chất mỹ phẩm trong Giỏ hàng (`SkincareIngredientInteractionEngine`) và Tư vấn AI Chatbot.
  + Phân tích thuật toán chấm điểm độ phù hợp của sản phẩm với loại da, mối quan tâm da và ngân sách của khách hàng để gợi ý phác đồ 4 bước (Làm sạch – Cân bằng – Đặc trị – Dưỡng ẩm) kèm nút mua trọn bộ 1-Click giảm 15%.
  + Phân tích cơ chế hoạt động của Phòng Lab Da Liễu ngay trong Giỏ hàng: khi giỏ hàng có các hoạt chất mạnh dùng chung (ví dụ Retinol kết hợp AHA/BHA hoặc Vitamin C nồng độ cao), động cơ `SkincareIngredientInteractionEngine` phát hiện xung đột, hiển thị cảnh báo mức độ kích ứng và tự động hướng dẫn khách hàng tách lịch dùng Sáng (AM) / Tối (PM) hoặc gợi ý thêm sản phẩm phục hồi màng ẩm B5/Ceramide.
- **Nghiệp vụ 8 (UC-08 & SD-08):** Tích/Tiêu điểm thưởng thành viên (`LoyaltyWallets`), Tự động xét thăng hạng VIP (`MemberTiers`) và Hạch toán giá vốn đích danh theo lô xuất (`Batch-Specific COGS`) vào Sổ cái Tài chính (`FinancialLedgers`).
  + Phân tích luồng cộng điểm tích lũy theo hệ số nhân của từng hạng thẻ (Silver, Gold, Diamond), cơ chế cấn trừ điểm thưởng trực tiếp vào tổng tiền thanh toán tại trang Checkout, và cách `FinanceService` nhân số lượng xuất của từng dòng `OrderDetail` với đúng `UnitCost` của lô `ProductBatch` tương ứng để ghi nhận doanh thu, giá vốn và lợi nhuận gộp chính xác tuyệt đối.

LƯU Ý: Giữ vững văn phong sinh viên năm cuối nghiêm túc, phân tích mạch lạc, không dùng ngoặc đơn tràn lan.
```

---

## PHIÊN 6: CHƯƠNG 2 (PHẦN 2.4 - TẬP 1) – THIẾT KẾ KIẾN TRÚC CLEAN ARCHITECTURE 4 TẦNG, BIỂU ĐỒ LỚP BCE, MA TRẬN PHÂN QUYỀN PBAC 51 QUYỀN & TỔNG QUAN CSDL 3NF

```text
Tiếp tục viết CHƯƠNG 2 – Mục 2.4: Thiết kế Kiến trúc Hệ thống và Cơ sở dữ liệu Chuẩn hóa 3NF (Phần 1: Mục 2.4.1, Mục 2.4.2 và Phần mở đầu + Nhóm bảng 1, 2 của Mục 2.4.3).
Tham chiếu trực tiếp:
- File ảnh kiến trúc: `d:\Lập trình web\OmnichannelBeautySolution\doc\Kien_Truc_Clean_Architecture_4_Tang.png`
- File tài liệu: `d:\Lập trình web\OmnichannelBeautySolution\doc\OOAD BLUEPRINT v2.0 - OMNICHANNEL BEAUTY SOLUTION.md` (Phần 1: Kiến trúc Clean Architecture & Pipeline, Phần 2: Ma trận 51 quyền PBAC)
- Code thực thể: `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Domain\Entities\IdentityEntities.cs`, `CatalogEntities.cs`

YÊU CẦU NỘI DUNG CHI TIẾT:

2.4.1. Thiết kế Kiến trúc tổng thể Clean Architecture 4 tầng và Biểu đồ lớp thực thi (BCE Class Diagram):
- Phân tích kiến trúc 4 tầng của giải pháp `OmnichannelBeautySolution` (`Omnichannel.Domain`, `Omnichannel.Application`, `Omnichannel.Infrastructure`, `Omnichannel.Web`).
- Hướng dẫn chèn trực tiếp file ảnh kiến trúc có sẵn trong dự án: `d:\Lập trình web\OmnichannelBeautySolution\doc\Kien_Truc_Clean_Architecture_4_Tang.png` (*Sơ đồ 2.18: Kiến trúc tổng thể Clean Architecture 4 tầng và luồng phụ thuộc của hệ thống OMNI BEAUTY*).
- Lập **Bảng 2.11: Bảng phân rã trách nhiệm kỹ thuật và vòng đời Dependency Injection của 4 tầng kiến trúc trong `Program.cs`**.
- Phân tích chi tiết chuỗi xử lý đường ống HTTP Request Pipeline gồm 9 bước thực tế trong `Program.cs` (từ `GlobalExceptionMiddleware`, `UseResponseCompression`, `SlugRedirectMiddleware`, `Multi-Scheme Authentication` cho đến `PermissionAuthorizationHandler`).
- Thiết kế **Sơ đồ 2.19: Biểu đồ Lớp kiến trúc BCE (Boundary – Control – Entity)** cho các phân hệ cốt lõi (cung cấp mã PlantUML kèm hướng dẫn chèn sơ đồ và phân tích rõ trách nhiệm của từng lớp Boundary Controller, Control Service và Entity Domain).

2.4.2. Thiết kế Ma trận Phân quyền Động (Granular Dynamic PBAC Matrix) và Cô lập dữ liệu đa chi nhánh:
- Phân tích kiến trúc phân quyền động dựa trên quyền hạn nguyên tử kết hợp cô lập dữ liệu chi nhánh (`StoreId` Isolation: Quản lý, Thu ngân và Thủ kho chi nhánh nào chỉ được truy xuất dữ liệu của chi nhánh đó; riêng `SuperAdmin` có tầm nhìn toàn chuỗi).
- Lập đầy đủ **Bảng 2.12: Ma trận phân rã 51 Quyền nguyên tử (`Permission Code`) thuộc 17 Phân hệ cho 6 Vai trò hệ thống (`SuperAdmin`, `StoreManager`, `Cashier`, `WarehouseStaff`, `ContentEditor`, `Customer`)** lấy chính xác từ `DbInitializer.cs` và tài liệu OOAD Blueprint v2.0.
- Giải thích cơ chế ghi đè quyền cá nhân (`UserPermissions` với cờ `IsGranted = 1` để cấp thêm quyền đặc cách hoặc `IsGranted = 0` để thu hồi quyền dù vai trò gốc có quyền đó).

2.4.3. Thiết kế Cơ sở dữ liệu quan hệ chuẩn 3NF và Sơ đồ Thực thể – Liên kết (ERD) – Phần 1:
- Trình bày tổng quan kiến trúc Cơ sở dữ liệu `OmnichannelBeautyDb` trên Microsoft SQL Server gồm **65 bảng chuẩn hóa 3NF** chia thành **17 phân nhóm nghiệp vụ**. Lập **Bảng 2.13: Bảng tổng hợp quy mô 17 phân nhóm dữ liệu (65 bảng) của hệ thống OMNI BEAUTY**.
- Chứng minh chặt chẽ việc thiết kế CSDL của hệ thống tuân thủ đầy đủ Dạng chuẩn 1 (1NF), Dạng chuẩn 2 (2NF) và Dạng chuẩn 3 (3NF) thông qua các ví dụ thực tế trong dự án (ví dụ: tách bảng `ProductBatches` và `StoreInventories`; tách bảng `ProductIngredients` lưu nồng độ phần trăm hoạt chất; lưu giá chốt lịch sử tại `OrderDetails` để không bị phụ thuộc bắc cầu khi giá gốc sản phẩm thay đổi).
- Cung cấp mã vẽ và hướng dẫn chèn **Sơ đồ 2.20: Sơ đồ Thực thể – Liên kết (ERD) tổng thể của các phân hệ cốt lõi**.
- Bắt đầu phần **Đặc tả chi tiết cấu trúc 22 Bảng dữ liệu cốt lõi (Core Data Dictionary)** với chuẩn bảng 6 cột (`Tên cột | Kiểu dữ liệu C# | Kiểu SQL Server | Nullable | Ràng buộc PK/FK/Index | Ý nghĩa nghiệp vụ & Giải thích vai trò`) cho 2 nhóm đầu tiên (9 bảng):
  + **Nhóm 1 – Định danh, Bảo mật & Phân quyền PBAC (4 bảng):**
    1. Bảng `Users` (Tài khoản người dùng và nhân sự chuỗi).
    2. Bảng `Roles` (Danh mục 6 vai trò hệ thống).
    3. Bảng `Permissions` (Danh mục 51 quyền hạn nguyên tử).
    4. Bảng `RolePermissions` & `UserPermissions` (Bảng ánh xạ quyền theo vai trò và ghi đè quyền cá nhân).
  + **Nhóm 2 – Danh mục, Biến thể Mỹ phẩm & Khoa học Da liễu (5 bảng):**
    5. Bảng `Categories` (Danh mục mỹ phẩm đa cấp đệ quy `ParentId`).
    6. Bảng `Brands` (Thương hiệu dược mỹ phẩm).
    7. Bảng `Products` (Sản phẩm gốc Master SKU, Số công bố Bộ Y tế, Chứng nhận Clean Beauty, Hạn mở nắp PAO).
    8. Bảng `ProductVariants` & `ProductSwatches` (Biến thể SKU theo dung tích ml/g và bảng màu Swatch mã Hex/Texture).
    9. Bảng `ActiveIngredients` & `ProductIngredients` (Từ điển hoạt chất khoa học, điểm EWG và nồng độ phần trăm).

LƯU Ý: Tất cả các bảng đặc tả CSDL phải ghi chính xác tuyệt đối kiểu dữ liệu SQL Server (`INT IDENTITY(1,1)`, `NVARCHAR(...)`, `DECIMAL(18,2)`, `BIT`, `DATETIME2`), chỉ rõ cột nào là Khóa chính (`PK`), cột nào là Khóa ngoại (`FK -> Tên_Bảng(Id)`), và có đoạn văn phân tích thiết kế bên dưới mỗi bảng.
```

---

## PHIÊN 7: CHƯƠNG 2 (PHẦN 2.4 - TẬP 2) – ĐẶC TẢ CHI TIẾT CÁC BẢNG CSDL CỐT LÕI (BẢNG 10 ĐẾN 22) & THIẾT KẾ KIẾN TRÚC GIAO DIỆN UI/UX

```text
Tiếp tục hoàn thiện phần còn lại của CHƯƠNG 2 (Phần tiếp theo của Mục 2.4.3: Đặc tả chi tiết Bảng 10 đến Bảng 22 trong Từ điển dữ liệu cốt lõi, và Mục 2.4.4: Thiết kế kiến trúc giao diện UI/UX).
Tham chiếu trực tiếp:
- Code thực thể: `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Domain\Entities\` (`InventoryEntities.cs`, `SalesEntities.cs`, `PosEntities.cs`, `CrmEntities.cs`, `LoyaltyEntities.cs`, `FinanceEntities.cs`, `PromotionEntities.cs`, `AuditEntities.cs`)
- Code Layouts: `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Web\Views\Shared\` (`_AdminLayout.cshtml`, `_PosLayout.cshtml`, `_WarehouseLayout.cshtml`, `_CmsLayout.cshtml`, `_Layout.cshtml`)

YÊU CẦU NỘI DUNG CHI TIẾT:

1. Tiếp tục Mục 2.4.3 – Đặc tả chi tiết cấu trúc 13 Bảng dữ liệu cốt lõi tiếp theo (Từ Bảng số 10 đến Bảng số 22, trình bày chuẩn bảng 6 cột kèm đoạn văn phân tích sâu quy tắc toàn vẹn dữ liệu bên dưới từng bảng):
  + **Nhóm 3 – Quản trị Kho Lô Hạn dùng FEFO & Điều chuyển Liên chi nhánh (5 bảng):**
    10. Bảng `Stores` (Danh mục Kho tổng HQ-001 và các Showroom chi nhánh).
    11. Bảng `ProductBatches` (Thực thể lõi quản lý Lô sản xuất `BatchNumber`, Ngày sản xuất `ManufacturingDate`, Hạn sử dụng `ExpDate`, Giá vốn nhập lô `UnitCost`, Phần trăm giảm giá xả kho cận date `ClearanceDiscountPercent`).
    12. Bảng `StoreInventories` (Tồn kho vật lý `PhysicalQuantity`, Tồn kho đang giữ chỗ `ReservedQuantity`, Khóa chống xung đột đồng thời `RowVersion` tại từng chi nhánh theo từng lô).
    13. Bảng `InventoryLogs` (Sổ cái truy vết mọi biến động nhập, xuất, giữ chỗ, hoàn trả và điều chuyển kho bất biến).
    14. Bảng `StockTransfers` & `StockTransferDetails` (Phiếu điều chuyển hàng hóa liên chi nhánh 3 bước và chi tiết từng lô điều chuyển).
  + **Nhóm 4 – Bán lẻ Đa kênh, Đơn hàng O2O & Ca Thu ngân POS (4 bảng):**
    15. Bảng `Orders` (Đơn hàng đa kênh Online/POS/O2O Click & Collect, trạng thái đơn, trạng thái thanh toán, mã PIN nhận hàng tại quầy, mã vận đơn GHN).
    16. Bảng `OrderDetails` (Chi tiết dòng hàng bán ra gắn chặt với `ProductBatchId` xuất theo thuật toán FEFO để truy xuất nguồn gốc lô và tính giá vốn đích danh).
    17. Bảng `Invoices` (Hóa đơn tài chính và lịch sử giao dịch thanh toán VNPay/MoMo/VietQR/Tiền mặt).
    18. Bảng `PosShifts` & `PosDrawerTransactions` (Quản lý vòng đời mở/đóng ca thu ngân, số dư đầu ca, tiền mặt lý thuyết, tiền thực đếm, độ lệch két và nhật ký thu chi tiền mặt tại két).
  + **Nhóm 5 – Hồ sơ Sắc đẹp CRM, Ví điểm Loyalty, Khuyến mãi, Tài chính & Nhật ký Kiểm toán (4 bảng):**
    19. Bảng `CustomerProfiles` & `RepurchaseSchedules` (Hồ sơ loại da, tình trạng da, tiền sử dị ứng hoạt chất và lịch dự báo ngày dùng hết mỹ phẩm để nhắc mua lại).
    20. Bảng `LoyaltyWallets`, `MemberTiers` & `LoyaltyTransactions` (Ví điểm thưởng, bậc hạng thành viên Silver/Gold/Diamond và lịch sử cộng/trừ điểm).
    21. Bảng `Promotions` & `Coupons` (Chiến dịch khuyến mãi tự động, Flash Sale và mã giảm giá).
    22. Bảng `FinancialLedgers`, `CodReconciliations` & `AuditLogs` (Sổ cái thu chi tài chính tự động, phiếu đối soát tiền thu hộ COD và bảng nhật ký kiểm toán lưu chuỗi JSON `OldValues` / `NewValues`).

2. Mục 2.4.4. Thiết kế Kiến trúc Giao diện Người dùng (UI/UX Design Specifications):
- Phân tích hệ thống ngôn ngữ thiết kế mỹ phẩm cao cấp (*Luxury Cosmetic Design System*: tông màu chủ đạo Rose Gold `#B76E79`, Deep Emerald `#1B4D3E`, Pearl White `#FBFBFB`, nghệ thuật chữ phân cấp rõ ràng cho tên thương hiệu, tên sản phẩm, nồng độ hoạt chất và giá tiền) kết hợp kiến trúc tùy biến giao diện động từ CSDL (`ThemeConfigs`, `PageLayouts`, `LayoutSections`).
- Phân tích thiết kế công thái học của **4 Razor Layouts quản trị chuyên biệt** trong dự án:
  1. `_AdminLayout.cshtml`: Bố cục điều hành tổng thể dành cho `SuperAdmin` và `StoreManager`.
  2. `_PosLayout.cshtml`: Bố cục Kiosk toàn màn hình không thanh cuộn dọc, hỗ trợ phím tắt `F1–F12` và đồng bộ màn hình phụ khách hàng dành riêng cho `Cashier`.
  3. `_WarehouseLayout.cshtml`: Bố cục bảng mật độ cao với hệ thống nhãn màu cảnh báo hạn dùng trực quan (Đỏ: Hết hạn / Dưới 30 ngày; Cam/Vàng: Cận date 30–180 ngày; Xanh lá: Lô mới an toàn) dành cho `WarehouseStaff`.
  4. `_CmsLayout.cshtml`: Bố cục trình dựng cây Menu đa hình, quản lý bài viết làm đẹp và trình kéo thả giao diện Live Preview dành cho `ContentEditor`.
- Lập **Bảng 2.36: Bảng tổng hợp thông số thiết kế công thái học của Storefront và 4 Layout quản trị chuyên biệt**.

Kết thúc bằng mục **Tiểu kết Chương 2**.
```

---

## PHIÊN 8: CHƯƠNG 3 (PHẦN 3.1) – MÔI TRƯỜNG TRIỂN KHAI, CẤU TRÚC MÃ NGUỒN SOLUTION & HIỆN THỰC HÓA 5 GIẢI PHÁP KỸ THUẬT – THUẬT TOÁN CỐT LÕI

```text
Bắt đầu viết CHƯƠNG 3: TRIỂN KHAI CÀI ĐẶT, KIỂM THỬ HỆ THỐNG VÀ ĐỊNH HƯỚNG PHÁT TRIỂN – Tập trung hoàn thiện trọn vẹn Mục 3.1: Môi trường triển khai, Tổ chức mã nguồn và Hiện thực hóa các giải pháp kỹ thuật cốt lõi.
Tham chiếu trực tiếp mã nguồn trong `d:\Lập trình web\OmnichannelBeautySolution`:
- `Omnichannel.Infrastructure\Services\InventoryAllocationEngine.cs` (Thuật toán FEFO)
- `Omnichannel.Infrastructure\Security\DynamicPermissionPolicyProvider.cs`, `PermissionAuthorizationHandler.cs` (Động cơ PBAC)
- `Omnichannel.Web\Middlewares\SlugRedirectMiddleware.cs` (Chuyển hướng SEO 301)
- `Omnichannel.Web\Helpers\IngredientParserHelper.cs` & `SkincareIngredientInteractionEngine.cs` (Bóc tách hoạt chất & kiểm tra xung đột)
- `Omnichannel.Web\Areas\Admin\Controllers\ThemeCustomizerController.cs` & `ThemeService.cs` (Visual Site Customizer)

YÊU CẦU NỘI DUNG CHI TIẾT:

3.1.1. Môi trường phát triển, công cụ cài đặt và cấu trúc tổ chức Solution (.NET 8):
- Trình bày chi tiết thông số môi trường triển khai thực tế (Hệ điều hành Windows, .NET 8.0 SDK, C# 12, Entity Framework Core 8.0, Microsoft SQL Server, Visual Studio 2022, Kestrel web server chạy trên cổng `http://localhost:5116`).
- Sơ đồ cây thư mục và **Bảng 3.1: Bảng mô tả chi tiết cấu trúc tổ chức 5 Projects trong Solution `OmnichannelBeautySolution.sln`** (`Omnichannel.Domain`, `Omnichannel.Application`, `Omnichannel.Infrastructure`, `Omnichannel.Web`, `Omnichannel.Tests`), chỉ rõ số lượng file thực tế và vai trò của từng thư mục con.

3.1.2. Hiện thực hóa các giải pháp kỹ thuật và thuật toán cốt lõi (Code & Algorithm Walkthrough):
Trình bày theo cấu trúc 4 lớp chuẩn mực cho từng giải pháp: *(1) Bài toán kỹ thuật cần giải quyết -> (2) Tư duy thiết kế giải thuật -> (3) Trích đoạn mã nguồn C# thực tế trong dự án (khoảng 15–25 dòng code cốt lõi nhất, sạch đẹp, chuẩn xác) -> (4) Diễn giải chi tiết luồng hoạt động của đoạn code*:
- **Giải pháp kỹ thuật 1:** Thuật toán Cấp phát Lô Hạn dùng **FEFO** (`InventoryAllocationEngine.cs`) và Cơ chế Giữ chỗ – Trừ kho 2 giai đoạn kết hợp khóa lạc quan `RowVersion` chống bán lố tồn kho (`OrderWorkflowService.cs`).
- **Giải pháp kỹ thuật 2:** Động cơ Phân quyền Động **PBAC 3 tầng** (`DynamicPermissionPolicyProvider.cs`, `PermissionAuthorizationHandler.cs`, `PermissionService.cs`), Kiến trúc Cách ly Phiên **Multi-Scheme Cookie** (`OmniBeauty.Admin.Session` vs `OmniBeauty.Customer.Session`, bộ lọc `BlockAdminShoppingAttribute`) và Cơ chế Tự động Ghi Nhật ký Kiểm toán (`AuditSaveChangesInterceptor`).
- **Giải pháp kỹ thuật 3:** Động cơ Bóc tách Thành phần Hoạt chất EWG Động (`IngredientParserHelper.cs`), Kiểm tra Tương kỵ Hoạt chất Da liễu trong Giỏ hàng (`SkincareIngredientInteractionEngine.cs`) và Trắc nghiệm Chẩn đoán Da 4 bước (`SkincareConsultantService.cs` / `RoutineQuizController.cs`).
- **Giải pháp kỹ thuật 4:** Động cơ Định tuyến Menu Đa hình kết hợp Middleware Chuyển hướng SEO 301 Tự động (`SlugRedirectMiddleware.cs`) và Trình Tùy biến Giao diện Trực quan không cần sửa code (`ThemeService.cs` & `ThemeCustomizerController.cs` với cơ chế In-Memory Cache Invalidation).
- **Giải pháp kỹ thuật 5:** Xử lý Thanh toán Đa phương thức (`VietQR Napas247`, chữ ký số `HMAC-SHA512 VNPay`, `MoMo`), Quy trình Giao nhận Đơn O2O Click & Collect bằng mã PIN 6 số tại quầy POS và Hạch toán Giá vốn Đích danh theo Lô (`PaymentService.cs`, `PosController.cs` & `FinanceService.cs`).

LƯU Ý: Trích dẫn đúng tên class, tên hàm thực tế trong codebase và giải thích bằng văn phong sinh viên năm cuối hiểu rõ từng dòng code mình viết.
```

---

## PHIÊN 9: CHƯƠNG 3 (PHẦN 3.2.1) – PHÂN TÍCH CHI TIẾT GIAO DIỆN PHÂN HỆ KHÁCH HÀNG (STOREFRONT & O2O) KÈM HƯỚNG DẪN CHỤP & CHÈN HÌNH ẢNH TRỰC QUAN

```text
Tiếp tục viết CHƯƠNG 3 – Mục 3.2: Kết quả đạt được về Giao diện và Chức năng hệ thống – Tập trung hoàn thiện trọn vẹn Mục 3.2.1: Phân hệ Giao diện Khách hàng (Luxury Storefront & Customer Portal).
Tham chiếu trực tiếp:
- Thư mục ảnh giao diện Storefront: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_1_Giao_Dien_Trang_Chu.png`, `Hinh_4_2_...png`, `Hinh_4_3_...png`
- Code Views: `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Web\Views\` (`Home/Index.cshtml`, `Product/Detail.cshtml`, `Cart/Index.cshtml`, `Checkout/Index.cshtml`, `RoutineQuiz/Index.cshtml`)

YÊU CẦU ĐẶC BIỆT QUAN TRỌNG VỀ HÌNH ẢNH GIAO DIỆN:
Ở MỖI màn hình giao diện dưới đây, bắt buộc phải trình bày đầy đủ 3 phần:
1. **Hộp `[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.X]`**: Chỉ rõ tên file ảnh đã có sẵn trong thư mục `d:\Lập trình web\OmnichannelBeautySolution\doc\` HOẶC hướng dẫn chi tiết cách mở trình duyệt tại `http://localhost:5116/...`, đăng nhập bằng tài khoản nào, bấm vào đâu, chọn dữ liệu gì để chụp được bức ảnh đẹp và rõ nét nhất chèn vào báo cáo Word.
2. **Mô tả bố cục trực quan và các thành phần giao diện**: Phân tích rõ các khu vực trên màn hình để người đọc nhìn vào ảnh là hiểu ngay.
3. **Phân tích luồng xử lý dữ liệu phía sau màn hình (Backend & Database Mapping)**: Giải thích khi người dùng bấm nút trên giao diện đó thì Controller/Service nào xử lý, kiểm tra điều kiện gì và lưu vào bảng nào.

DANH SÁCH 7 MÀN HÌNH GIAO DIỆN KHÁCH HÀNG CẦN PHÂN TÍCH CHI TIẾT TRONG MỤC 3.2.1:
1. **Hình 3.1 – Giao diện Trang chủ Luxury, Mega Menu Đa hình và Khu vực Flash Sale / Xả hàng Cận Date FEFO:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_1_Giao_Dien_Trang_Chu.png`.
   - URL thực tế: `http://localhost:5116/` và `http://localhost:5116/Home/ClearanceFefo`.
   - Phân tích: Thanh điều hướng Mega Menu đa cấp, Hero Banner, đồng hồ đếm ngược Flash Sale thời gian thực, khu vực sản phẩm xả kho cận date tự động lấy từ các lô `NEAR_EXPIRATION` và nút mở nhanh Bảng điều khiển thuyết trình (`Alt + D`).
2. **Hình 3.2 – Giao diện Chi tiết Sản phẩm Mỹ phẩm Chuẩn Quốc tế (Chọn Lô Mới vs Lô Cận Date FEFO, Studio Thử màu Swatch theo Tông da, Bảng thành phần EWG Động & Chứng nhận Clean Beauty):**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_2_Giao_Dien_Chi_Tiet_San_Pham_Full.png` (và `doc\Hinh_4_2_Giao_Dien_Chi_Tiet_San_Pham.png`).
   - URL thực tế: `http://localhost:5116/san-pham/{slug}`.
   - Phân tích: Bộ chọn mua giữa Lô Mới Date Xa (nguyên giá) và Lô Cận Date Xả Kho FEFO (giảm sâu 45% kèm hiển thị rõ mã lô `BatchNumber` và ngày hết hạn `ExpDate`), Studio thử màu son trực quan trên 3 tông da (Da sáng, Da trung bình, Da ngăm), khối huy hiệu Clean Beauty (`Alcohol-Free`, `Fragrance-Free`, `Pregnancy-Safe`), bảng bóc tách hoạt chất kèm điểm an toàn EWG động và khối tra cứu tồn kho Showroom O2O theo thành phố.
3. **Hình 3.3 – Giao diện Khu vực Đánh giá Thực tế Đồng điệu theo Hồ sơ Làn da ("Match My Skin Twin" Reviews):**
   - URL thực tế: Phần cuối trang `http://localhost:5116/san-pham/{slug}`.
   - Phân tích: Hệ thống huy hiệu hồ sơ làn da gắn trên từng bình luận (Loại da, Mối quan tâm da, Độ tuổi, Thời gian sử dụng, Nhãn xác thực đã mua lô FEFO chuẩn) và thanh bộ lọc tức thì bằng Alpine.js giúp khách hàng chỉ xem đánh giá từ những người có cùng loại da với mình.
4. **Hình 3.4 – Giao diện Trắc nghiệm Chẩn đoán Da Thông minh (AI Routine Quiz 4 Bước) & Bộ lọc Chuyên sâu SmartFilter:**
   - URL thực tế: `http://localhost:5116/tu-van-da` và `http://localhost:5116/SmartFilter`.
   - Phân tích: Quy trình trả lời 3 bước (Loại da -> Vấn đề da -> Ngân sách), thuật toán gợi ý phác đồ chăm sóc da 4 bước chuẩn khoa học và nút **"Thêm trọn bộ 4 bước vào giỏ hàng (Tiết kiệm 15%)"** tự động kích hoạt ưu đãi `COMBO15`.
5. **Hình 3.5 – Giao diện Khay So sánh Mỹ phẩm Chuyên sâu (Định giá trên từng ml/g & Đối sánh Hoạt chất/FEFO):**
   - URL thực tế: Chọn 2–3 sản phẩm vào thanh Compare Bar dưới đáy màn hình và mở Modal `/Product/CompareMatrix`.
   - Phân tích: Bảng đối sánh trực diện giúp người mua biết sản phẩm nào kinh tế hơn dựa trên đơn giá `VNĐ/ml`, so sánh hoạt chất chủ đạo, chứng nhận an toàn và hạn sử dụng lô hiện tại.
6. **Hình 3.6 – Giao diện Giỏ hàng Thông minh, Phòng Lab Kiểm tra Xung đột Hoạt chất (Skincare Conflict Checker) & Quầy Chọn Mẫu thử Miễn phí (Free Sample Bar):**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_3_Giao_Dien_Gio_Hang_Va_Canh_Bao_Xung_Dot.png`.
   - URL thực tế: `http://localhost:5116/Cart`.
   - Phân tích: Thanh tiến độ mở khóa quà tặng mẫu thử miễn phí khi đơn hàng đạt mốc `500.000₫` và `1.000.000₫`, cùng khung cảnh báo của Phòng Lab Da Liễu khi phát hiện 2 sản phẩm trong giỏ có hoạt chất đối kháng (ví dụ Retinol đi cùng AHA/BHA) kèm hướng dẫn chia lịch dùng Sáng/Tối.
7. **Hình 3.7 – Giao diện Thanh toán Đa kênh (Giao Tận Nơi vs Nhận Tại Showroom O2O 1 Giờ), Cấn trừ Điểm Loyalty & Vé Nhận Hàng QR Fast-Track:**
   - URL thực tế: `http://localhost:5116/Checkout` và trang kết quả đặt hàng thành công `Checkout/Success`. (Đăng nhập tài khoản `customer_vip` / `Admin@123`).
   - Phân tích: Chuyển đổi linh hoạt giữa Tab Giao hàng tiêu chuẩn (tính phí ship GHN tự động) và Tab Nhận tại Showroom trong 1 giờ (Freeship `0₫`, chọn Showroom còn hàng), áp dụng mã giảm giá Coupon, thanh trượt dùng điểm thưởng Loyalty giảm trừ trực tiếp vào hóa đơn, thanh toán VietQR/VNPay/MoMo và sinh tấm **Vé Nhận Hàng Ưu Tiên O2O** kèm mã QR và mã PIN 6 số `OB-XXXXXX`.

LƯU Ý: Viết chi tiết, sinh động, không lạm dụng đóng mở ngoặc đơn.
```

---

## PHIÊN 10: CHƯƠNG 3 (PHẦN 3.2.2) – PHÂN TÍCH CHI TIẾT GIAO DIỆN PHÂN HỆ QUẢN TRỊ ĐA KÊNH, SMART POS & KHO FEFO KÈM HƯỚNG DẪN CHỤP & CHÈN ẢNH

```text
Tiếp tục viết CHƯƠNG 3 – Mục 3.2.2: Phân hệ Giao diện Quản trị Điều hành & Bán lẻ Đa kênh (Admin Back-Office, Smart POS, Kho Lô FEFO, Visual Site Customizer & Tài chính).
Tham chiếu trực tiếp:
- Thư mục ảnh quản trị: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_4_...png` đến `Hinh_4_10_...png`
- Code Controllers: `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Web\Areas\Admin\Controllers\` (`DashboardController.cs`, `MenuController.cs`, `ThemeCustomizerController.cs`, `PermissionManagementController.cs`, `WarehouseController.cs`, `OrderController.cs`, `PosController.cs`, `FinanceController.cs`)
- Tài khoản đăng nhập kiểm thử: `d:\Lập trình web\OmnichannelBeautySolution\TEST_ACCOUNTS.md`

YÊU CẦU ĐẶC BIỆT QUAN TRỌNG:
Giống như Mục 3.2.1, ở MỖI màn hình quản trị dưới đây bắt buộc phải có:
1. **Hộp `[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.X]`**: Chỉ rõ file ảnh có sẵn trong thư mục `d:\Lập trình web\OmnichannelBeautySolution\doc\` HOẶC hướng dẫn đăng nhập vào `http://localhost:5116/admin/auth/login` bằng tài khoản nào (`superadmin`, `manager_cg`, `cashier01`, `warehouse01`), mở URL nào và thao tác gì để chụp ảnh.
2. **Mô tả trực quan bố cục giao diện và các chỉ số hiển thị trên màn hình**.
3. **Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau**.

DANH SÁCH 9 MÀN HÌNH QUẢN TRỊ CẦN PHÂN TÍCH CHI TIẾT TRONG MỤC 3.2.2:
1. **Hình 3.8 – Giao diện Trung tâm Điều hành Quản trị (Admin Dashboard), Biểu đồ Doanh thu Đa kênh & Trung tâm Thông báo Đẩy SignalR:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_4_Giao_Dien_Bang_Dieu_Khien_Admin_Dashboard.png`.
   - URL thực tế: `http://localhost:5116/Admin/Dashboard` (Tài khoản `superadmin` / `Admin@123`).
   - Phân tích: 6 thẻ KPI thời gian thực (Tổng doanh thu chuỗi, Đơn hàng chờ xử lý, Doanh số POS hôm nay, Cảnh báo lô cận date FEFO, Phiếu điều chuyển chờ duyệt, Khách hàng mới), biểu đồ xu hướng doanh thu Online vs POS và chuông thông báo đẩy SignalR cùng Hộp thư nội bộ (`/Admin/Message`).
2. **Hình 3.9 – Giao diện Quản trị Cây Menu Đa hình & Tự động Hóa Chuyển hướng SEO 301:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_5_Giao_Dien_Quan_Tri_Menu_Da_Hinh_SEO.png`.
   - URL thực tế: `http://localhost:5116/Admin/Menu`.
   - Phân tích: Cấu trúc cây phân cấp đa hình liên kết tới Danh mục, Sản phẩm, Bài viết hoặc Đường dẫn tĩnh và bảng lịch sử chuyển hướng `SlugRedirects`.
3. **Hình 3.10 – Giao diện Trình Tùy biến Giao diện Trực quan (No-Code Visual Site Customizer & Live Preview):**
   - URL thực tế: `http://localhost:5116/admin/customizer`.
   - Phân tích: Màn hình chia 2 cột (30% Sidebar kéo thả thứ tự Section, đổi bảng màu thương hiệu Primary/Secondary, tải ảnh Banner và 70% khung Live Preview phản hồi tức thì qua `postMessage` dưới 50ms), cơ chế lưu bản nháp `DraftConfigJson` tách biệt với bản xuất bản `PublishedConfigJson`.
4. **Hình 3.11 – Giao diện Ma trận Phân quyền Hạt nhân Động (PBAC Permission Matrix 51 Quyền x 6 Vai trò) & Nhật ký Kiểm toán (`AuditLog`):**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_6_Giao_Dien_Ma_Tran_Phan_Quyen_PBAC.png`.
   - URL thực tế: `http://localhost:5116/Admin/PermissionManagement` và `http://localhost:5116/Admin/AuditLog`.
   - Phân tích: Bảng lưới bật/tắt từng quyền nguyên tử theo từng phân hệ, giao diện cấp/thu hồi quyền cá nhân (`UserPermissions Override`) và màn hình tra cứu nhật ký kiểm toán hiển thị chi tiết dữ liệu cũ – dữ liệu mới dạng JSON Diff.
5. **Hình 3.12 – Giao diện Giám sát Tồn kho Lô Hạn dùng FEFO & Cảnh báo Màu sắc Trực quan:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_7_Giao_Dien_Giam_Sat_Ton_Kho_FEFO.png`.
   - URL thực tế: `http://localhost:5116/Admin/Warehouse` (Đăng nhập bằng `warehouse01` hoặc `superadmin`).
   - Phân tích: Bảng theo dõi từng lô sản xuất (`BatchNumber`), ngày sản xuất, ngày hết hạn, số ngày còn lại kèm nhãn màu cảnh báo (Đỏ/Vàng/Xanh), tồn kho vật lý, tồn kho đang giữ chỗ và chức năng điều chuyển hàng hóa liên chi nhánh 3 bước (`/Admin/StockTransfer`).
6. **Hình 3.13 – Giao diện Quản lý Đơn hàng Đa kênh & Truy vết Cấp phát Lô Hàng FEFO:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_8_Giao_Dien_Quan_Ly_Don_Hang_Cap_Phat_Lo.png`.
   - URL thực tế: `http://localhost:5116/Admin/Order`.
   - Phân tích: Quy trình duyệt đơn, kiểm tra từng dòng sản phẩm đã được thuật toán FEFO bóc tách từ mã lô nào, hạn dùng bao nhiêu, tích hợp đẩy đơn sang Giao Hàng Nhanh (GHN) sinh mã vận đơn và in phiếu đóng gói.
7. **Hình 3.14 – Giao diện Màn hình Bán hàng Tại quầy Smart POS (`_PosLayout`), Quét Mã vạch & Cổng Giao Đơn O2O Click & Collect:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_9_Giao_Dien_Man_Hinh_Ban_Hang_POS.png`.
   - URL thực tế: `http://localhost:5116/Admin/Pos` (Tài khoản `cashier01` / `Admin@123`).
   - Phân tích: Giao diện Kiosk 2 cột tốc độ cao, ô quét mã vạch EAN-13/SKU tự động chọn lô FEFO tại chi nhánh, nút **"Giao Đơn O2O Click & Collect"** để xác thực mã PIN 6 số `OB-XXXXXX` bàn giao hàng tại quầy, thanh toán QR động VietQR và in hóa đơn nhiệt khổ K80.
8. **Hình 3.15 – Giao diện Quản lý Mở/Đóng Ca Thu ngân POS & Kiểm toán Đối soát Két tiền mặt:**
   - File ảnh có sẵn: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_10_Giao_Dien_Dong_Mo_Ca_Va_Doi_Soat_Ket.png`.
   - URL thực tế: Modal Mở/Đóng ca trên `http://localhost:5116/Admin/Pos`.
   - Phân tích: Khai báo tiền mặt đầu ca, ghi phiếu thu/chi tiền mặt phát sinh trong ca, nhập số tiền mặt thực đếm cuối ca và hệ thống tự động tính toán độ lệch két để chống thất thoát.
9. **Hình 3.16 – Giao diện Quản trị Sổ cái Tài chính, Báo cáo Kết quả Kinh doanh P&L (Theo Giá vốn Đích danh Lô FEFO) & Đối soát COD:**
   - URL thực tế: `http://localhost:5116/Admin/Finance` và `http://localhost:5116/Admin/Report`.
   - Phân tích: Bảng cân đối Doanh thu – Giá vốn đích danh theo lô xuất – Chi phí vận hành – Lợi nhuận ròng, quản lý công nợ nhà cung cấp mỹ phẩm và đối soát tiền thu hộ COD với đơn vị vận chuyển.

LƯU Ý: Trình bày mạch lạc, chi tiết từng màn hình, tuyệt đối không gộp chung hay lược bỏ.
```

---

## PHIÊN 11: CHƯƠNG 3 (PHẦN 3.3 & 3.4) – KIỂM THỬ ĐA TẦNG (12 TÀI KHOẢN SEED DATA, UNIT/INTEGRATION TESTS XUNIT, 10 BẢNG TEST CASE HỘP ĐEN, KIỂM THỬ CHỊU TẢI K6) & ĐÁNH GIÁ, HƯỚNG PHÁT TRIỂN

```text
Tiếp tục viết hoàn chỉnh phần còn lại của CHƯƠNG 3: Mục 3.3 (Triển khai Kiểm thử và Đánh giá chất lượng hệ thống) và Mục 3.4 (Đánh giá kết quả đạt được, Hạn chế và Đề xuất hướng phát triển).
Tham chiếu trực tiếp:
- Danh sách tài khoản: `d:\Lập trình web\OmnichannelBeautySolution\TEST_ACCOUNTS.md`
- Danh mục 21 bộ test: `d:\Lập trình web\OmnichannelBeautySolution\Omnichannel.Tests\Services\` (231 tests PASSED 100%)
- File ảnh kết quả kiểm thử tải k6: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_5_1_Ket_Qua_Kiem_Thu_Chiu_Tai_K6.png`
- File báo cáo tối ưu hiệu năng: `d:\Lập trình web\OmnichannelBeautySolution\doc\PERFORMANCE_OPTIMIZATION_REPORT.md`

YÊU CẦU NỘI DUNG CHI TIẾT:

3.3. Triển khai Kiểm thử và Đánh giá chất lượng hệ thống
- 3.3.1. Chiến lược kiểm thử đa tầng (Testing Pyramid) và Dữ liệu khởi tạo chuẩn (Seed Data 12 tài khoản mẫu):
  + Phân tích phương pháp kiểm thử 4 tầng áp dụng cho dự án (Unit Test tầng Domain/Application, Integration Test với EF Core và Multi-Scheme Auth, Black-box Functional Test trên trình duyệt, và High-Concurrency Load Test với k6).
  + Lập **Bảng 3.2: Danh sách 12 Tài khoản kiểm thử tiêu chuẩn theo 6 Vai trò và các Chi nhánh cửa hàng trong `DbInitializer.cs` và `TEST_ACCOUNTS.md`** (Gồm đầy đủ: `superadmin`, `admin`, `manager_hq`, `manager_cg`, `cashier01`, `cashier02`, `warehouse01`, `warehouse02`, `editor01`, `customer01`, `customer02`, `customer_vip`, mật khẩu `Admin@123`, chi nhánh gán và mục đích kiểm thử phân quyền/cô lập dữ liệu).
- 3.3.2. Kết quả Kiểm thử Đơn vị và Kiểm thử Tích hợp Tự động (Automated Unit & Integration Tests với `xUnit`):
  + Lập **Bảng 3.3: Bảng tổng hợp kết quả thực thi các bộ Kiểm thử tự động trong dự án `Omnichannel.Tests`** (Bao gồm đầy đủ các bộ test thực tế trong thư mục `Omnichannel.Tests/Services/`: `InventoryAllocationEngineTests`, `PermissionEngineTests`, `MultiSchemeAuthTests`, `RoleBoundaryAndAuditInterceptorTests`, `PosShiftServiceTests`, `StockTransferServiceTests`, `StoreInventoryServiceTests`, `SkincareIngredientInteractionEngineTests`, `SkincareConsultantServiceTests`, `IngredientParserAndCleanBeautyTests`, `PromotionEngineTests`, `LoyaltyServiceTests`, `FinanceServiceTests`, `PaymentServiceTests`, `GhnShippingServiceTests`, `ThemeServiceTests`, `ChatbotServiceTests`, `SmartFilterServiceTests`, `AlertServiceTests`, `NotificationServiceTests`, `InternalMessageServiceTests`, `PasswordHasherServiceTests`, `StepUpElevationServiceTests` – Tất cả đều đạt trạng thái **PASSED 100%**).
  + Phân tích ý nghĩa của việc đạt 100% test tự động vượt qua trong việc bảo đảm không phát sinh lỗi hồi quy khi nâng cấp hệ thống.
- 3.3.3. Kịch bản và Kết quả Kiểm thử Chức năng Hộp đen (Black-box Test Cases):
  + Thiết kế **10 Bảng Test Case Hộp đen chi tiết (Bảng 3.4 đến Bảng 3.13, từ mã `TC01` đến `TC10`)** theo chuẩn bảng kiểm thử phần mềm chuyên nghiệp (`Mã bước | Hành động thực hiện | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái Pass/Fail`) cho 10 luồng nghiệp vụ quan trọng nhất:
    * **Bảng 3.4 (`TC01`):** Kiểm thử Đăng nhập, Cách ly Phiên Multi-Scheme Cookie và Chặn tài khoản Admin mua hàng cá nhân (`BlockAdminShopping`).
    * **Bảng 3.5 (`TC02`):** Kiểm thử Ma trận Phân quyền Động PBAC, Ghi đè quyền cá nhân và Cô lập dữ liệu giữa Showroom Cầu Giấy (`manager_cg`) với Kho Tổng (`manager_hq`).
    * **Bảng 3.6 (`TC03`):** Kiểm thử Chọn mua Lô Mới vs Lô Cận Date FEFO (-45%), Bảng thành phần EWG Động và Lọc đánh giá đồng điệu theo loại da (`Skin Twin`).
    * **Bảng 3.7 (`TC04`):** Kiểm thử Trắc nghiệm Chẩn đoán Da (`Routine Quiz AI`) và Thêm trọn bộ phác đồ 4 bước vào giỏ hàng 1-Click giảm 15%.
    * **Bảng 3.8 (`TC05`):** Kiểm thử Phòng Lab Phát hiện Xung đột Hoạt chất trong Giỏ hàng và Mở khóa Quầy Mẫu thử Miễn phí theo bậc giá trị đơn.
    * **Bảng 3.9 (`TC06`):** Kiểm thử Đặt hàng Trực tuyến (Giao tận nơi & Nhận tại Showroom O2O 1H), Đổi điểm Loyalty và Cấp phát Tồn kho FEFO 2 giai đoạn.
    * **Bảng 3.10 (`TC07`):** Kiểm thử Bán hàng tại Quầy Smart POS, Xác thực Mã PIN Nhận đơn O2O Click & Collect và Thanh toán VietQR in hóa đơn nhiệt.
    * **Bảng 3.11 (`TC08`):** Kiểm thử Vòng đời Mở/Đóng Ca Thu ngân POS và Phát hiện Chênh lệch Két tiền mặt cuối ca.
    * **Bảng 3.12 (`TC09`):** Kiểm thử Quy trình Điều chuyển Kho Liên chi nhánh 3 bước (`Pending -> InTransit -> Completed`) và Cảnh báo Lệch số lượng thực nhận.
    * **Bảng 3.13 (`TC10`):** Kiểm thử Trình Tùy biến Giao diện Trực quan (`No-Code Visual Customizer`), Xuất bản Giao diện mới và Tự động Ghi Nhật ký Kiểm toán (`AuditLog`).
- 3.3.4. Kiểm thử Chịu tải Đồng thời & Chống Bán lố Tồn kho Flash Sale bằng công cụ `k6`:
  + Hướng dẫn chèn trực tiếp file ảnh kết quả kiểm thử có sẵn trong dự án: `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_5_1_Ket_Qua_Kiem_Thu_Chiu_Tai_K6.png`.
  + Phân tích kịch bản giả lập 50 người dùng ảo (`50 VUs`) đồng thời gửi yêu cầu đặt mua một lô mỹ phẩm cận date chỉ còn đúng **15 sản phẩm** trong kho. Phân tích kết quả thực nghiệm: đúng 15 giao dịch đầu tiên thành công (`HTTP 200 OK`), 35 giao dịch đến sau nhận thông báo hết tồn kho khả dụng (`HTTP 409 Conflict`), tồn kho cuối cùng bằng `0` (tuyệt đối không xảy ra lỗi âm kho hay bán lố), thời gian phản hồi trung bình đạt mức tối ưu và `p(95) < 350ms`.

3.4. Đánh giá kết quả đạt được, Hạn chế và Đề xuất hướng phát triển (Roadmap v3.0+)
- 3.4.1. Đánh giá mức độ hoàn thành so với Mục tiêu và Giả thuyết nghiên cứu ban đầu (Lập **Bảng 3.14: Bảng đối chiếu mức độ hoàn thành các mục tiêu của khóa luận**).
- 3.4.2. Nhìn nhận thẳng thắn một số hạn chế còn tồn tại trong phạm vi thời gian thực hiện khóa luận.
- 3.4.3. Đề xuất 4 định hướng nâng cấp và phát triển hệ thống trong tương lai:
  1. Triển khai cụm bộ nhớ đệm phân tán **Redis Cluster & Thuật toán khóa phân tán RedLock** cho các đợt Flash Sale quy mô hàng chục nghìn người dùng.
  2. Xây dựng cổng đồng bộ tồn kho và đơn hàng thời gian thực với các sàn Thương mại điện tử (**Shopee Mall, TikTok Shop Mall, Lazada LazMall**).
  3. Tích hợp phát hành **Hóa đơn điện tử có mã của Cơ quan Thuế khởi tạo từ máy tính tiền POS** theo Nghị định 123/2020/NĐ-CP và Thông tư 78/2021/TT-BTC.
  4. Tích hợp công nghệ **Thử trang điểm ảo AR (Virtual Lipstick Try-On)** và **Camera AI Soi da trực tiếp trên trình duyệt bằng WebAssembly + MediaPipe FaceMesh**.

Kết thúc bằng mục **Tiểu kết Chương 3**.
```

---

## PHIÊN 12: PHẦN KẾT LUẬN CHUNG, DANH MỤC TÀI LIỆU THAM KHẢO CHUẨN HỌC THUẬT & BỘ 3 PHỤ LỤC (PHỤ LỤC A, B, C)

```text
Viết hoàn chỉnh phần cuối cùng của khóa luận: KẾT LUẬN CHUNG, DANH MỤC TÀI LIỆU THAM KHẢO và PHỤ LỤC (Phụ lục A, Phụ lục B, Phụ lục C).
Tham chiếu trực tiếp:
- Lược đồ 65 bảng: `d:\Lập trình web\OmnichannelBeautySolution\doc\OOAD BLUEPRINT v2.0 - OMNICHANNEL BEAUTY SOLUTION.md` (Phần 1: Lược đồ DDL và Danh mục 65 bảng)
- Hướng dẫn vận hành: `d:\Lập trình web\OmnichannelBeautySolution\README.md` và `scripts\test_system_performance_benchmark.ps1`
- Biểu mẫu in: `Omnichannel.Web\Areas\Admin\Views\Pos\PrintReceipt.cshtml`

YÊU CẦU NỘI DUNG CHI TIẾT:

1. KẾT LUẬN CHUNG (Khoảng 2 – 3 trang):
- Tổng kết lại toàn bộ quá trình nghiên cứu lý luận, khảo sát thực tiễn, phân tích thiết kế hướng đối tượng, chuẩn hóa cơ sở dữ liệu 3NF và lập trình hoàn thiện hệ thống quản lý bán hàng đa kênh cho chuỗi cửa hàng mỹ phẩm OMNI BEAUTY trên nền tảng .NET 8 Clean Architecture.
- Khẳng định những giá trị thực tiễn mà hệ thống mang lại cho doanh nghiệp (đồng bộ tồn kho Online – Offline thời gian thực, giải quyết bài toán quản lý hạn dùng mỹ phẩm bằng FEFO, chuẩn hóa kiểm soát ca thu ngân POS, cá nhân hóa tư vấn da liễu cho khách hàng và trao quyền tự chủ giao diện cho đội ngũ Marketing).

2. DANH MỤC TÀI LIỆU THAM KHẢO (Trình bày chuẩn quy định Bộ Giáo dục & Đào tạo / IEEE, chia rõ 3 nhóm nguồn có giá trị thực tế, tuyệt đối không bịa nguồn):
- **I. Văn bản pháp quy của Nhà nước:**
  + Thông tư số 06/2011/TT-BYT ngày 25/01/2011 của Bộ Y tế quy định về quản lý mỹ phẩm.
  + Nghị định số 52/2013/NĐ-CP ngày 16/05/2013 của Chính phủ về Thương mại điện tử và Nghị định số 85/2021/NĐ-CP sửa đổi, bổ sung một số điều của Nghị định số 52/2013/NĐ-CP.
  + Nghị định số 13/2023/NĐ-CP ngày 17/04/2023 của Chính phủ về Bảo vệ dữ liệu cá nhân.
  + Nghị định số 123/2020/NĐ-CP ngày 19/10/2020 của Chính phủ quy định về hóa đơn, chứng từ.
- **II. Giáo trình, Sách chuyên khảo và Báo cáo ngành tiếng Việt:**
  + Các giáo trình Phân tích và Thiết kế Hệ thống Thông tin, Cơ sở dữ liệu, Hệ thống thông tin quản lý của các trường đại học uy tín và Báo cáo Chỉ số Thương mại điện tử Việt Nam (VECOM 2025–2026).
- **III. Tài liệu học thuật và Kỹ thuật tiếng Anh:**
  + Robert C. Martin (2017), *Clean Architecture: A Craftsman's Guide to Software Structure and Design*, Prentice Hall.
  + Eric Evans (2003), *Domain-Driven Design: Tackling Complexity in the Heart of Software*, Addison-Wesley.
  + Martin Kleppmann (2017), *Designing Data-Intensive Applications*, O'Reilly Media.
  + Tài liệu kỹ thuật chính thức của Microsoft Corporation về *ASP.NET Core 8.0 & Entity Framework Core 8 Documentation*, tài liệu chuẩn *GS1 Application Identifiers (Batch/Lot & Expiry Date)* và *EWG Skin Deep® Cosmetics Database Methodology*.

3. HỆ THỐNG PHỤ LỤC (APPENDICES):
- **Phụ lục A – Danh mục Tổng hợp Toàn bộ 65 Bảng Cơ sở dữ liệu Chuẩn 3NF thuộc 17 Phân hệ của Hệ thống OMNI BEAUTY:**
  + Lập bảng tổng hợp toàn bộ 65 bảng trong `OmnichannelBeautyDb` (gồm cả 22 bảng cốt lõi đã đặc tả ở Chương 2 và 43 bảng phụ trợ thuộc các phân hệ CMS, ThemeBuilder, Outbox, Media, InternalMessage, Chat, Wishlist, StockAlert, Supplier...) kèm Khóa chính, Khóa ngoại và mô tả chức năng để hội đồng thấy trọn vẹn quy mô đồ sộ của dự án mà không làm loãng Chương 2.
- **Phụ lục B – Hướng dẫn Cài đặt, Cấu hình Chuỗi kết nối SQL Server, Khởi tạo Dữ liệu mẫu và Vận hành Hệ thống (Deployment & Operations Runbook):**
  + Trình bày từng bước lệnh cài đặt thực tế từ kho mã nguồn `https://github.com/ngocanh1725/ltw_ud_omnichannelbeautysolution`, cấu hình `appsettings.json`, chạy Migration & Seed Data tự động qua `DbInitializer`, lệnh khởi chạy ứng dụng (`dotnet run --project Omnichannel.Web`), lệnh chạy kiểm thử tự động (`dotnet test`) và lệnh chạy benchmark hiệu năng (`test_system_performance_benchmark.ps1`).
- **Phụ lục C – Một số Biểu mẫu Chứng từ Đầu ra Tiêu biểu của Hệ thống:**
  + Trình bày mẫu bố cục và hướng dẫn chụp/chèn 4 biểu mẫu in ra từ hệ thống: (1) Mẫu Hóa đơn nhiệt bán lẻ tại quầy POS khổ K80 (`/Admin/Pos/PrintReceipt/{id}`); (2) Mẫu Vé Nhận hàng Ưu tiên O2O Click & Collect kèm mã QR và PIN 6 số; (3) Mẫu Biên bản xuất – nhận điều chuyển kho liên chi nhánh theo lô FEFO; (4) Mẫu Báo cáo kết quả hoạt động kinh doanh P&L và Đối soát két tiền mặt cuối ca.
```
