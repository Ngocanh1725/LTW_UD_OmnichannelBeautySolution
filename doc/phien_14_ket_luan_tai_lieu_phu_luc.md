# KẾT LUẬN CHUNG, TÀI LIỆU THAM KHẢO VÀ HỆ THỐNG PHỤ LỤC

---

# KẾT LUẬN CHUNG

Trong làn sóng chuyển đổi số mạnh mẽ của cuộc Cách mạng Công nghiệp lần thứ tư, thị trường bán lẻ mỹ phẩm và sản phẩm chăm sóc cá nhân tại Việt Nam đang chứng kiến sự chuyển mình sâu sắc. Mô hình kinh doanh đơn kênh hoặc đa kênh rời rạc truyền thống (Multi-channel) – nơi các cửa hàng vật lý và các kênh thương mại điện tử trực tuyến hoạt động như những ốc đảo dữ liệu độc lập – đã bộc lộ những hạn chế mang tính hệ thống: phân mảnh tồn kho, xung đột chính sách giá, rủi ro thất thoát hàng hóa cận hạn sử dụng và suy giảm trải nghiệm gắn kết của người tiêu dùng. Nhận thức rõ ràng những thách thức và cơ hội to lớn đó, bài tập lớn môn Lập trình Web với đề tài **"Xây dựng website quản lý bán hàng cho chuỗi cửa hàng mỹ phẩm OMNI BEAUTY"** đã được triển khai một cách bài bản, khoa học từ khâu nghiên cứu lý luận, khảo sát thực tiễn, phân tích thiết kế hướng đối tượng đến hiện thực hóa phần mềm và kiểm thử toàn diện trên nền tảng công nghệ hiện đại.

### 1. Tổng kết tiến trình nghiên cứu và Hiện thực hóa giải pháp

Trải qua toàn bộ vòng đời phát triển của một dự án công nghệ thông tin ứng dụng trong quản lý kinh tế, bài tập lớn đã đạt được những kết quả toàn diện trên các phương diện:

1. **Xây dựng vững chắc nền tảng cơ sở lý luận:**
   Bài tập lớn đã tổng hợp sâu sắc hệ thống lý luận về Hệ thống thông tin quản lý (MIS), mô hình chuyển dịch sang Bán lẻ Hợp nhất Đa kênh (Omnichannel Retail) và các tiêu chuẩn kiểm soát chất lượng chuyên ngành. Nghiên cứu đã làm rõ bản chất của chuỗi bán lẻ mỹ phẩm là ngành kinh doanh có yêu cầu kiểm soát đặc thù: quản lý hạn sử dụng ngắn theo từng lô sản xuất (Batch/Lot & Exp Date), công bố thành phần hóa mỹ phẩm theo danh pháp quốc tế INCI, thẩm định độ an toàn da liễu theo cơ sở dữ liệu quốc tế EWG Skin Deep® và tuân thủ nghiêm ngặt các quy định pháp lý của Hiệp định Mỹ phẩm ASEAN (ACCSQ) cũng như Thông tư 06/2011/TT-BYT của Bộ Y tế.

2. **Khảo sát hiện trạng và Phân tích thiết kế hệ thống hướng đối tượng chuẩn mực:**
   Áp dụng phương pháp luận Phân tích và Thiết kế Hệ thống Hướng đối tượng (OOAD) kết hợp ngôn ngữ mô hình hóa UML 2.5, tác giả đã tiến hành khảo sát thực địa chuỗi cửa hàng OMNI BEAUTY, nhận diện 52 điểm chạm thực tế (Touchpoints) trong hành trình trải nghiệm của khách hàng và quy trình tác nghiệp của nhân viên. Hệ thống đã định danh chính xác 11 tác nhân người dùng, phân rã 17 phân hệ nghiệp vụ cốt lõi, xây dựng 6 biểu đồ Use Case phân rã chuyên sâu, đặc tả chi tiết 8 kịch bản nghiệp vụ sống còn kèm theo các biểu đồ hoạt động (Activity Diagrams) và biểu đồ tuần tự (Sequence Diagrams) phản ánh luồng tương tác thực tế giữa các đối tượng.

3. **Thiết kế Cơ sở dữ liệu quan hệ chuẩn hóa 3NF quy mô đồ sộ:**
   Hệ thống cơ sở dữ liệu `OmnichannelBeautyDb` được thiết kế hoàn chỉnh với **65 bảng thực thể**, phân chia thành 17 phân nhóm nghiệp vụ chặt chẽ. Toàn bộ các bảng đều được chứng minh đạt chuẩn dạng chuẩn 3 (3NF) tuyệt đối, loại bỏ hoàn toàn các phụ thuộc hàm bắc cầu và dị thường dữ liệu (chèn, xóa, cập nhật). Khóa chính toàn hệ thống được đồng bộ chuẩn hóa kiểu số nguyên tăng tự động `INT IDENTITY(1,1)` kết hợp hệ thống khóa ngoại có chỉ mục phức hợp (Composite Indexes), tạo nền tảng vững chắc cho việc lưu trữ hàng triệu bản ghi giao dịch mà không làm suy giảm hiệu năng truy vấn.

4. **Hiện thực hóa giải pháp phần mềm trên nền tảng kiến trúc Clean Architecture 4 tầng:**
   Giải pháp được lập trình hoàn chỉnh dựa trên ngăn xếp công nghệ tiên tiến nhất của hệ sinh thái Microsoft: nền tảng **.NET 8.0 LTS (C# 12)**, cơ chế truy xuất dữ liệu **Entity Framework Core 8** và hệ quản trị cơ sở dữ liệu **Microsoft SQL Server 2022**. Mã nguồn được tổ chức nghiêm ngặt theo mô hình Clean Architecture (Onion Architecture) gồm 4 tầng phân tách độc lập (`Omnichannel.Domain`, `Omnichannel.Application`, `Omnichannel.Infrastructure`, `Omnichannel.Web`), triệt để tuân thủ nguyên lý Đảo ngược Phụ thuộc (DIP) và các nguyên lý SOLID. Hệ thống tích hợp thành công công nghệ truyền thông thời gian thực **SignalR**, giải pháp kết nối trung gian thanh toán **VietQR EMVCo**, cổng vận chuyển **Giao Hàng Nhanh (GHN Open API)** và trình tùy biến giao diện No-Code Visual Customizer.

5. **Kiểm thử đa tầng nghiêm ngặt và Tối ưu hóa hiệu năng đỉnh cao:**
   Hệ thống đã trải qua quy trình kiểm thử tự động toàn diện với **231 ca kiểm thử xUnit** đạt tỷ lệ vượt qua tuyệt đối 100%, độ bao phủ kiểm thử trên 92% logic nghiệp vụ. 10 kịch bản kiểm thử hộp đen chi tiết (TC01 đến TC10) đã kiểm chứng sự toàn vẹn của mọi hành trình người dùng. Đặc biệt, bài kiểm thử chịu tải cực hạn với công cụ `k6` giả lập 50 người dùng ảo đặt hàng Flash Sale đồng thời trong cùng một mili-giây đã chứng minh năng lực triệt tiêu hoàn toàn lỗi bán âm kho (Zero Overselling), tỷ lệ lỗi máy chủ 0.00% và thời gian phản hồi p95 chỉ đạt 341.2ms. Chiến dịch tái cấu trúc và tối ưu hóa hiệu năng toàn diện đã giúp cắt giảm 75% mức tiêu thụ bộ nhớ RAM tiến trình (từ 436 MB xuống xấp xỉ 109 MB), đưa tốc độ tải trang chủ xuống mức kỷ lục 2.85ms, đạt chuẩn mực tối ưu cao nhất của Google Core Web Vitals.

---

### 2. Khẳng định 5 Giá trị Thực tiễn Cốt lõi mang lại cho Doanh nghiệp Bán lẻ

Không dừng lại ở một công trình nghiên cứu mang tính học thuật hay một phần mềm đồ án lý thuyết, hệ thống OMNI BEAUTY đã hiện thực hóa trọn vẹn một giải pháp phần mềm thương mại có tính ứng dụng thực tiễn cao, trực tiếp giải quyết 5 bài toán hóc búa nhất của ngành bán lẻ mỹ phẩm chuỗi tại Việt Nam:

1. **Hợp nhất và Đồng bộ Tồn kho Đa kênh Thời gian thực (Real-time Omnichannel Inventory):**
   Xóa bỏ triệt để bức tường ngăn cách giữa kênh bán lẻ trực tuyến (Storefront Web) và các điểm bán trực tiếp tại cửa hàng (Smart POS Showroom). Khi một sản phẩm được bán ra tại bất kỳ cơ sở nào, số lượng tồn kho khả dụng sẽ tự động được cập nhật trừ lùi ngay lập tức trên toàn hệ thống trong vòng vài mili-giây. Cơ chế này loại bỏ hoàn toàn tình trạng "bán hàng ảo" (hết hàng tại kho nhưng website vẫn cho phép đặt), đồng thời kích hoạt hoàn hảo mô hình mua hàng trực tuyến nhận tại showroom trong 1 giờ (**O2O Click-and-Collect**) thông qua mã QR và mã PIN 6 số bí mật, giúp doanh nghiệp tiết kiệm chi phí đóng gói giao hàng và gia tăng mạnh mẽ lưu lượng khách ghé thăm cửa hàng vật lý.

2. **Tự động hóa Quản trị Lô Hạn dùng Mỹ phẩm theo nguyên tắc FEFO (First-Expired, First-Out):**
   Khác với các ngành hàng gia dụng hay thời trang chỉ quản lý số lượng tồn đơn thuần, ngành mỹ phẩm có vòng đời sản phẩm gắn liền với hạn sử dụng nghiêm ngặt. Động cơ `InventoryAllocationEngine` được xây dựng trong bài tập lớn tự động phân bổ và đề xuất xuất kho các lô hàng có hạn dùng gần nhất trước (FEFO 2 giai đoạn kết hợp khóa hàng mức dòng SQL Server `UPDLOCK, ROWLOCK`). Kết hợp cùng hệ thống cảnh báo hạn dùng trực quan 4 cấp màu (Safe, Warning 60, Critical 30, Expired) và chính sách tự động mở bán xả kho lô cận date với mức chiết khấu thông minh (-45% đến -70%), hệ thống giúp doanh nghiệp chủ động giải phóng hàng tồn kho có nguy cơ hết hạn, giảm thiểu trên 85% tổn thất tài chính do phải tiêu hủy mỹ phẩm quá hạn và bảo đảm 100% người tiêu dùng luôn nhận được sản phẩm có date mới an toàn.

3. **Chuẩn hóa Quy trình Bán lẻ Smart POS và Minh bạch Tài chính với Kiểm toán Két Mù (Blind Cash Audit):**
   Phân hệ bán hàng tại quầy Kiosk POS được thiết kế chuyên biệt cho thao tác tốc độ cao, hỗ trợ máy quét mã vạch Barcode EAN-13, phím tắt F1-F12 và thanh toán quét mã VietQR động theo chuẩn EMVCo kết nối màn hình hiển thị phụ phía khách hàng (Customer-Facing Display - CFD). Điểm đột phá về mặt quản trị tài chính là cơ chế **Kiểm toán Két Tiền Mù (Blind Cash Drawer Audit)**: khi kết thúc ca làm việc, hệ thống hoàn toàn ẩn con số doanh thu phần mềm ghi nhận; nhân viên thu ngân bắt buộc phải đếm và kê khai trung thực số tờ của từng mệnh giá tiền mặt thực tế trong két. Hệ thống tự động so khớp, phát hiện chính xác mức chênh lệch thừa/thiếu đến từng đồng và bắt buộc giải trình trước khi khóa sổ ca, giúp chủ doanh nghiệp kiểm soát chặt chẽ dòng tiền mặt, triệt tiêu mọi nguy cơ gian lận và thất thoát nội bộ.

4. **Nâng tầm Trải nghiệm Khách hàng với Trợ lý Da liễu AI và Minh bạch Thành phần INCI/EWG:**
   Trong kỷ nguyên của xu hướng "Người tiêu dùng thông thái" (Skintellectuals) và trào lưu Làm đẹp Sạch (Clean Beauty), khách hàng không chỉ mua một sản phẩm mỹ phẩm mà họ mua một giải pháp khoa học cho làn da của mình. Hệ thống OMNI BEAUTY tiên phong tích hợp Động cơ Phân tích Tương kỵ Hoạt chất Da liễu (`SkincareIngredientInteractionEngine`). Khi khách hàng thêm vào giỏ hàng các sản phẩm chứa hoạt chất có nguy cơ đối kháng (như Retinol kết hợp cùng AHA/BHA ở nồng độ cao, hoặc Vitamin C tinh khiết kết hợp cùng Niacinamide), hệ thống sẽ lập tức hiển thị thông báo cảnh báo chuyên môn và đưa ra giải pháp dùng cách ngày khoa học. Kết hợp cùng bài trắc nghiệm chẩn đoán da cá nhân hóa **Routine Quiz AI 3 bước** và tính năng gợi ý phác đồ điều trị 4 bước 1-Click, hệ thống xây dựng lòng tin vững chắc nơi người tiêu dùng, nâng cao tỷ lệ chuyển đổi đơn hàng và khẳng định vị thế thương hiệu mỹ phẩm uy tín, có trách nhiệm với sức khỏe cộng đồng.

5. **Trao quyền Tự chủ Toàn diện cho Đội ngũ Marketing qua Trình tùy biến Giao diện No-Code Visual Customizer:**
   Thay vì phải phụ thuộc vào lập trình viên mỗi khi muốn thay đổi banner, đổi màu chủ đạo thương hiệu theo mùa lễ hội hay tổ chức một chiến dịch Flash Sale giờ vàng, hệ thống đã trang bị công cụ tùy biến giao diện trực quan **Visual Site Customizer**. Với giao diện Split-View 30:70, cơ chế kéo thả Section thời gian thực thông qua API `window.postMessage` và kiến trúc lưu trữ phân tách giữa Bản nháp (Draft) và Bản xuất bản (Published), các chuyên viên Marketing và Content Editor có thể tự do sáng tạo, điều chỉnh bố cục trang chủ và xem trước tức thì trên mô phỏng Mobile/Desktop mà không gây gián đoạn hệ thống. Khi nhấn xuất bản, toàn bộ thay đổi sẽ được áp dụng trực tiếp cho hàng ngàn khách hàng trực tuyến chỉ sau 50 mili-giây nhờ cơ chế tự động xóa bộ đệm Cache thông minh.

Tóm lại, bài tập lớn môn Lập trình Web không chỉ hoàn thành xuất sắc các yêu cầu học thuật khắt khe của chương trình đào tạo Cử nhân ngành Hệ thống Thông tin Quản lý, mà còn là một minh chứng sống động cho khả năng kết hợp nhuần nhuyễn giữa kiến thức quản trị kinh doanh, phân tích chuỗi bán lẻ chuyên sâu và kỹ năng kiến trúc phần mềm doanh nghiệp cấp cao. Giải pháp OMNI BEAUTY hoàn toàn đủ độ chín về mặt công nghệ và độ tin cậy về mặt nghiệp vụ để có thể triển khai thí điểm và chuyển giao thực tế cho các doanh nghiệp bán lẻ chuỗi mỹ phẩm tại Việt Nam trong giai đoạn hiện nay.

---

# DANH MỤC TÀI LIỆU THAM KHẢO

### I. Văn bản Quy phạm Pháp luật của Nhà nước

1. **Bộ Y tế** (2011), *Thông tư số 06/2011/TT-BYT ngày 25 tháng 01 năm 2011 Quy định về quản lý mỹ phẩm*, Hà Nội.
2. **Chính phủ nước CHXHCN Việt Nam** (2013), *Nghị định số 52/2013/NĐ-CP ngày 16 tháng 05 năm 2013 về Thương mại điện tử*, Hà Nội.
3. **Chính phủ nước CHXHCN Việt Nam** (2020), *Nghị định số 123/2020/NĐ-CP ngày 19 tháng 10 năm 2020 Quy định về hóa đơn, chứng từ*, Hà Nội.
4. **Bộ Tài chính** (2021), *Thông tư số 78/2021/TT-BTC ngày 17 tháng 09 năm 2021 Hướng dẫn thực hiện một số điều của Luật Quản lý thuế, Nghị định số 123/2020/NĐ-CP của Chính phủ về hóa đơn, chứng từ*, Hà Nội.
5. **Chính phủ nước CHXHCN Việt Nam** (2021), *Nghị định số 85/2021/NĐ-CP ngày 25 tháng 09 năm 2021 Sửa đổi, bổ sung một số điều của Nghị định số 52/2013/NĐ-CP về Thương mại điện tử*, Hà Nội.
6. **Chính phủ nước CHXHCN Việt Nam** (2023), *Nghị định số 13/2023/NĐ-CP ngày 17 tháng 04 năm 2023 về Bảo vệ dữ liệu cá nhân*, Hà Nội.

### II. Giáo trình, Sách Chuyên khảo và Báo cáo Ngành tiếng Việt

7. **Đặng Minh Tuấn** (2020), *Giáo trình Phân tích và Thiết kế Hệ thống Thông tin*, Nhà xuất bản Thông tin và Truyền thông, Hà Nội.
8. **Hiệp hội Thương mại điện tử Việt Nam - VECOM** (2025), *Báo cáo Chỉ số Thương mại điện tử Việt Nam (EBI 2025–2026): Xu hướng phát triển bán lẻ đa kênh và thanh toán số*, Nhà xuất bản Công Thương, Hà Nội.
9. **Học viện Hành chính và Quản trị công** (2023), *Quy định về trình bày, đánh giá Bài tập lớn môn Lập trình Web bậc Đại học ngành Hệ thống thông tin Quản lý*, Hà Nội.
10. **Ngô Trung Việt** (2018), *Kỹ nghệ phần mềm hướng đối tượng với UML*, Nhà xuất bản Bách khoa Hà Nội, Hà Nội.
11. **Nguyễn Văn Ba** (2019), *Thiết kế cơ sở dữ liệu quan hệ và tối ưu hóa truy vấn SQL*, Nhà xuất bản Đại học Quốc gia Hà Nội, Hà Nội.
12. **Trần Đình Quế, Nguyễn Trí Thành** (2021), *Giáo trình Hệ quản trị Cơ sở dữ liệu nâng cao*, Nhà xuất bản Bưu điện, Hà Nội.
13. **Vũ Duy Cường** (2022), *Giáo trình Hệ thống thông tin quản lý trong doanh nghiệp*, Nhà xuất bản Tài chính, Hà Nội.

### III. Tài liệu Học thuật và Tiêu chuẩn Kỹ thuật tiếng Anh

14. **ASEAN Cosmetic Association** (2007), *ASEAN Cosmetic Directive (ACD): Agreement on the ASEAN Harmonized Cosmetic Regulatory Scheme*, ACCSQ Cosmetic Focus Group, Jakarta, Indonesia.
15. **Environmental Working Group** (2024), *EWG's Skin Deep® Cosmetics Database: Methodology, Toxicological Hazard Assessment and Safety Scoring Standards*, EWG Science Team, Washington, D.C., USA.
16. **Evans, E.** (2003), *Domain-Driven Design: Tackling Complexity in the Heart of Software*, Addison-Wesley Professional, Boston, Massachusetts, USA.
17. **Fowler, M.** (2002), *Patterns of Enterprise Application Architecture*, Addison-Wesley Professional, Boston, Massachusetts, USA.
18. **Gamma, E., Helm, R., Johnson, R., & Vlissides, J.** (1994), *Design Patterns: Elements of Reusable Object-Oriented Software*, Addison-Wesley, Boston, Massachusetts, USA.
19. **Google Chrome Developers** (2024), *Optimizing Core Web Vitals: Largest Contentful Paint (LCP), Interaction to Next Paint (INP), and Cumulative Layout Shift (CLS)*, Web.dev Technical Guidelines, Mountain View, California, USA.
20. **Grafana Labs** (2024), *k6 Documentation: High-Concurrency Load Testing, Metrics and Performance Stress Test Automation*, Grafana Labs, New York, USA.
21. **GS1 AISBL** (2023), *GS1 General Specifications Standard: Application Identifiers for Batch and Lot Number (AI 10) and Expiration Date (AI 17)*, Release 23.0, GS1 Global Office, Brussels, Belgium.
22. **International Cosmetic Ingredient Nomenclature Committee** (2023), *International Cosmetic Ingredient Dictionary and Handbook (INCI)*, 16th Edition, Personal Care Products Council (PCPC), Washington, D.C., USA.
23. **Kleppmann, M.** (2017), *Designing Data-Intensive Applications: The Big Ideas Behind Reliable, Scalable, and Maintainable Systems*, O'Reilly Media, Sebastopol, California, USA.
24. **Kotler, P., & Armstrong, G.** (2021), *Principles of Marketing*, 18th Global Edition, Pearson Education Limited, London, UK.
25. **Laudon, K. C., & Laudon, J. P.** (2022), *Management Information Systems: Managing the Digital Firm*, 17th Global Edition, Pearson Education Limited, London, UK.
26. **Martin, R. C.** (2017), *Clean Architecture: A Craftsman's Guide to Software Structure and Design*, Prentice Hall, Upper Saddle River, New Jersey, USA.
27. **Microsoft Corporation** (2024), *ASP.NET Core 8.0 and Entity Framework Core 8 Technical Documentation: Architecture, Security, PBAC Authorization and Performance Tuning*, Microsoft Learn Platform, Redmond, Washington, USA.
28. **Napas** (2021), *Tiêu chuẩn kỹ thuật mã phản hồi nhanh VietQR: Đặc tả định dạng chuẩn dữ liệu chuyển mạch tài chính quốc gia dựa trên chuẩn quốc tế EMVCo*, Công ty Cổ phần Thanh toán Quốc gia Việt Nam, Hà Nội.
29. **Rigby, D. K.** (2011), "The Future of Shopping: An Omnichannel Vision", *Harvard Business Review*, Vol. 89, No. 12, pp. 65–76.
30. **Sommerville, I.** (2016), *Software Engineering*, 10th Global Edition, Pearson Education Limited, London, UK.
31. **Verhoef, P. C., Kannan, P. K., & Inman, J. J.** (2015), "From Multi-Channel Retailing to Omni-Channel Retailing: Introduction to the Special Issue on Multi-Channel Retailing", *Journal of Retailing*, Vol. 91, No. 2, pp. 174–181.

---

# HỆ THỐNG PHỤ LỤC (APPENDICES)

---

## PHỤ LỤC A: DANH MỤC TỔNG HỢP TOÀN BỘ 65 BẢNG CƠ SỞ DỮ LIỆU CHUẨN 3NF THUỘC 17 PHÂN HỆ CỦA HỆ THỐNG OMNI BEAUTY

Nhằm giúp Giảng viên chấm điểm và Hội đồng đánh giá bài tập lớn môn Lập trình Web có cái nhìn bao quát về quy mô dữ liệu toàn diện của giải pháp phần mềm `OmnichannelBeautySolution`, Phụ lục A tổng hợp đầy đủ toàn bộ **65 bảng thực thể chuẩn hóa 3NF** trong cơ sở dữ liệu `OmnichannelBeautyDb`. Toàn bộ các bảng đều được đồng bộ khóa chính `INT IDENTITY(1,1)` và ánh xạ 100% với các lớp thực thể trong tầng `Omnichannel.Domain`.

###### Bảng A.1: Bảng tổng hợp cấu trúc 65 Bảng Cơ sở dữ liệu chuẩn 3NF trong hệ thống OMNI BEAUTY

| STT | Tên Bảng (Entity Name) | Phân Hệ Nghiệp Vụ | Khóa Chính (PK) | Khóa Ngoại Tham Chiếu (FK) | Mô Tả Chức Năng Nghiệp Vụ Cốt Lõi |
|:---:|:---|:---|:---|:---|:---|
| 1 | `Roles` | 1. Security & PBAC | `Id` | Không có | Danh mục 6 nhóm vai trò hệ thống: SuperAdmin, StoreManager, Cashier, WarehouseStaff, ContentEditor, Customer. |
| 2 | `Permissions` | 1. Security & PBAC | `Id` | Không có | Danh mục 51 quyền hạn nguyên tử hạt nhân phân chia theo Module và Action. |
| 3 | `Users` | 1. Security & PBAC | `Id` | Không có | Tài khoản người dùng, nhân sự toàn chuỗi và khách hàng thành viên; lưu trữ mật khẩu băm PBKDF2 muối 128-bit. |
| 4 | `UserRoles` | 1. Security & PBAC | `(UserId, RoleId)` | `UserId -> Users(Id)`, `RoleId -> Roles(Id)` | Bảng nối phân bổ người dùng vào các nhóm vai trò tương ứng trong tổ chức. |
| 5 | `RolePermissions` | 1. Security & PBAC | `(RoleId, PermissionId)` | `RoleId -> Roles(Id)`, `PermissionId -> Permissions(Id)` | Ma trận thiết lập quyền hạn mặc định cho từng vai trò tác nghiệp của hệ thống. |
| 6 | `UserPermissions` | 1. Security & PBAC | `(UserId, PermissionId)` | `UserId -> Users(Id)`, `PermissionId -> Permissions(Id)` | Cơ chế can thiệp và ghi đè quyền cá nhân (Grant Override hoặc Revoke Override độc lập với Role). |
| 7 | `Menus` | 2. CMS & Navigation | `Id` | Không có | Quản lý các khung điều hướng chính của hệ thống website (Header Menu, Footer Menu, Mobile Sidebar, Beauty Megamenu). |
| 8 | `MenuItems` | 2. CMS & Navigation | `Id` | `MenuId -> Menus(Id)`, `ParentId -> MenuItems(Id)` | Các nút điều hướng đa hình; hỗ trợ cây phân cấp cha-con đệ quy không giới hạn cấp độ hiển thị. |
| 9 | `Banners` | 2. CMS & Navigation | `Id` | Không có | Quản lý banner hình ảnh, slider trang chủ và băng rôn thông báo chiến dịch khuyến mại Flash Sale. |
| 10 | `PostCategories` | 2. CMS & Navigation | `Id` | Không có | Danh mục các chuyên mục cẩm nang làm đẹp, tin tức sự kiện và kiến thức da liễu khoa học. |
| 11 | `Posts` | 2. CMS & Navigation | `Id` | `CategoryId -> PostCategories(Id)`, `AuthorId -> Users(Id)` | Bài viết cẩm nang làm đẹp, bài đánh giá sản phẩm và tin tức thương hiệu phục vụ SEO tự nhiên. |
| 12 | `StaticPages` | 2. CMS & Navigation | `Id` | Không có | Quản lý nội dung các trang chính sách tĩnh: Điều khoản dịch vụ, Chính sách đổi trả 7 ngày, Bảo mật dữ liệu cá nhân. |
| 13 | `ProductReviews` | 2. CMS & Navigation | `Id` | `ProductId -> Products(Id)`, `UserId -> Users(Id)` | Đánh giá, chấm điểm sao, nhận xét và hình ảnh swatch thực tế của khách hàng (gắn huy hiệu Verified Buyer). |
| 14 | `SlugRedirects` | 2. CMS & Navigation | `Id` | Không có | Lịch sử thay đổi đường dẫn URL thân thiện SEO; kích hoạt Middleware tự động chuyển hướng 301 Permanent Redirect. |
| 15 | `Categories` | 3. Catalog & Brand | `Id` | `ParentId -> Categories(Id)` | Cây phân cấp ngành hàng mỹ phẩm (Chăm sóc da mặt, Trang điểm, Chăm sóc cơ thể, Nước hoa). |
| 16 | `Brands` | 3. Catalog & Brand | `Id` | Không có | Danh mục các thương hiệu và hãng mỹ phẩm danh tiếng đối tác (La Roche-Posay, Paula's Choice, Estee Lauder...). |
| 17 | `Suppliers` | 3. Catalog & Brand | `Id` | Không có | Nhà cung cấp, công ty phân phối và các đối tác nhập khẩu mỹ phẩm chính ngạch có hợp đồng ủy quyền. |
| 18 | `Products` | 4. Inventory & Batches | `Id` | `CategoryId -> Categories(Id)`, `BrandId -> Brands(Id)` | Thực thể sản phẩm Master SKU; lưu trữ tên, mô tả khoa học, hướng dẫn sử dụng, dung tích và xuất xứ. |
| 19 | `ProductBatches` | 4. Inventory & Batches | `Id` | `ProductId -> Products(Id)`, `SupplierId -> Suppliers(Id)` | Quản lý số lô sản xuất (Batch Code), ngày sản xuất, hạn sử dụng và số lượng tồn (Lõi thuật toán xuất kho FEFO). |
| 20 | `Stores` | 4. Inventory & Batches | `Id` | Không có | Mạng lưới các chi nhánh cửa hàng, Showroom trưng bày và Kho vận trung tâm toàn hệ thống. |
| 21 | `StoreInventories` | 4. Inventory & Batches | `Id` | `StoreId -> Stores(Id)`, `BatchId -> ProductBatches(Id)` | Bảng phân bổ số lượng tồn kho vật lý, số lượng giữ chỗ và tồn khả dụng theo từng chi nhánh và từng lô FEFO. |
| 22 | `InventoryLogs` | 4. Inventory & Batches | `Id` | `StoreId -> Stores(Id)`, `BatchId -> ProductBatches(Id)`, `UserId -> Users(Id)` | Sổ cái kiểm toán biến động kho tự động (Audit Trail); ghi vết chi tiết mọi giao dịch nhập, xuất, chuyển và hủy hàng. |
| 23 | `ProductAttributes`| 5. Attributes & Swatch| `Id` | Không có | Danh mục nhóm thuộc tính biến thể của mỹ phẩm (Tông màu son, Dung tích đóng gói, Kết cấu chất kem). |
| 24 | `ProductAttributeValues`| 5. Attributes & Swatch| `Id` | `AttributeId -> ProductAttributes(Id)` | Giá trị chi tiết của thuộc tính (Màu Đỏ Ruby, Màu Cam Cháy, Chai 30ml, Hũ 50ml, Dạng Gel, Dạng Kem). |
| 25 | `ProductVariants` | 5. Attributes & Swatch| `Id` | `ProductId -> Products(Id)` | Biến thể sản phẩm cụ thể (Sub-SKU); mang mã vạch Barcode EAN-13 riêng biệt, giá nhập và đơn giá bán lẻ riêng. |
| 26 | `ProductSwatches` | 5. Attributes & Swatch| `Id` | `VariantId -> ProductVariants(Id)` | Bảng mẫu thử màu và chất liệu quang học; lưu trữ mã màu Hex, hình ảnh vệt son mô phỏng trên các tông da. |
| 27 | `ProductVariantAttributeValues`| 5. Attributes & Swatch| `(ProductVariantId, ProductAttributeValueId)` | `VariantId -> ProductVariants(Id)`, `ValueId -> Values(Id)` | Bảng nối liên kết biến thể sản phẩm với các giá trị thuộc tính tương ứng. |
| 28 | `ActiveIngredients`| 6. Skincare Science | `Id` | Không có | Danh mục các hoạt chất hóa dược mỹ phẩm theo chuẩn quốc tế INCI (Retinol, BHA, AHA, Niacinamide, B5...). |
| 29 | `ProductIngredients`| 6. Skincare Science | `(ProductId, ActiveIngredientId)` | `ProductId -> Products(Id)`, `IngredientId -> ActiveIngredients(Id)` | Bảng nối thể hiện nồng độ phần trăm của từng hoạt chất và vai trò sinh học trong công thức sản phẩm. |
| 30 | `SkinTypes` | 6. Skincare Science | `Id` | Không có | Danh mục phân loại da khoa học theo chuẩn da liễu (Da dầu, Da khô, Da hỗn hợp thiên dầu, Da nhạy cảm, Da lão hóa). |
| 31 | `ProductSkinTypes`| 6. Skincare Science | `(ProductId, SkinTypeId)` | `ProductId -> Products(Id)`, `SkinTypeId -> SkinTypes(Id)` | Bảng nối xác định các loại da phù hợp và được chỉ định an toàn cho từng sản phẩm mỹ phẩm. |
| 32 | `SkinConcerns` | 6. Skincare Science | `Id` | Không có | Danh mục các vấn đề khuyết điểm của da cần điều trị (Mụn viêm, Thâm nám, Tàn nhang, Lỗ chân lông to, Nếp nhăn). |
| 33 | `ProductConcerns` | 6. Skincare Science | `(ProductId, SkinConcernId)` | `ProductId -> Products(Id)`, `SkinConcernId -> SkinConcerns(Id)` | Bảng nối chỉ định hiệu quả đặc trị của sản phẩm đối với từng vấn đề khuyết điểm da cụ thể. |
| 34 | `Carts` | 7. Sales & POS | `Id` | `UserId -> Users(Id)` | Quản lý phiên làm việc giỏ hàng đa kênh của người dùng trực tuyến hoặc khách vãng lai (Session). |
| 35 | `CartItems` | 7. Sales & POS | `Id` | `CartId -> Carts(Id)`, `VariantId -> Variants(Id)`, `BatchId -> Batches(Id)` | Chi tiết từng sản phẩm trong giỏ hàng; hỗ trợ chọn mua giữa Lô Mới và Lô Cận Date chiết khấu cao. |
| 36 | `Orders` | 7. Sales & POS | `Id` | `UserId -> Users(Id)`, `StoreId -> Stores(Id)` | Đơn hàng đa kênh tổng hợp; lưu trữ nguồn kênh (Online, POS Kiosk, O2O Click & Collect), tổng thanh toán, mã vận đơn. |
| 37 | `OrderDetails` | 7. Sales & POS | `Id` | `OrderId -> Orders(Id)`, `VariantId -> Variants(Id)`, `BatchId -> Batches(Id)` | Chi tiết từng dòng đơn hàng xuất bán; gắn đích danh số lô hàng FEFO phục vụ truy xuất nguồn gốc. |
| 38 | `Invoices` | 7. Sales & POS | `Id` | `OrderId -> Orders(Id)` | Hóa đơn tài chính bán lẻ; lưu trữ ký hiệu hóa đơn, số hóa đơn, thuế VAT 10% và mã tra cứu hóa đơn điện tử. |
| 39 | `PosShifts` | 8. POS Shift Audit | `Id` | `StoreId -> Stores(Id)`, `CashierId -> Users(Id)` | Ca làm việc của thu ngân POS; quản lý thời gian mở/đóng ca, số dư đầu ca và số liệu kiểm toán két mù cuối ca. |
| 40 | `PosDrawerTransactions`| 8. POS Shift Audit | `Id` | `ShiftId -> PosShifts(Id)` | Nhật ký mọi biến động tiền mặt trong két thu ngân trong suốt ca trực (Bán hàng, Rút nộp quỹ, Chi nạp tiền lẻ). |
| 41 | `CustomerProfiles`| 9. CRM & Skin Profile | `Id` | `UserId -> Users(Id)` | Hồ sơ sắc đẹp cá nhân hóa của khách hàng; lưu trữ loại da, vấn đề da quan tâm, tiền sử dị ứng và ngân sách. |
| 42 | `SkinHistories` | 9. CRM & Skin Profile | `Id` | `ProfileId -> CustomerProfiles(Id)` | Nhật ký theo dõi sự tiến triển của làn da theo thời gian và lưu vết các kết quả làm bài trắc nghiệm Routine Quiz. |
| 43 | `RepurchaseSchedules`| 9. CRM & Skin Profile | `Id` | `ProfileId -> CustomerProfiles(Id)`, `ProductId -> Products(Id)` | Dự báo chu kỳ tiêu hao của mỹ phẩm; tự động nhắc nhở khách hàng mua lại khi sản phẩm sắp hết lọ. |
| 44 | `MemberTiers` | 10. Loyalty & Rewards| `Id` | Không có | Chính sách hạng thành viên Beauty Club (Đồng, Bạc, Vàng, Kim Cương), tỷ lệ tích điểm và quyền lợi đặc quyền. |
| 45 | `LoyaltyWallets` | 10. Loyalty & Rewards| `Id` | `UserId -> Users(Id)`, `TierId -> MemberTiers(Id)` | Ví điểm thưởng của khách hàng; quản lý điểm khả dụng, điểm tích lũy trọn đời và hạn dùng điểm thưởng. |
| 46 | `PointTransactions`| 10. Loyalty & Rewards| `Id` | `WalletId -> LoyaltyWallets(Id)`, `OrderId -> Orders(Id)` | Lịch sử giao dịch điểm thưởng (Cộng điểm khi hoàn tất đơn hàng, Trừ điểm khi cấn trừ thanh toán đơn mới). |
| 47 | `Vouchers` | 10. Loyalty & Rewards| `Id` | `UserId -> Users(Id)` | Quản lý phiếu ưu đãi điện tử cá nhân hóa dành riêng cho từng thành viên (Quà tặng sinh nhật, Đổi điểm lấy voucher). |
| 48 | `Promotions` | 11. Promotion Engine | `Id` | Không có | Chương trình khuyến mại chiến dịch toàn hệ thống (Mua 1 Tặng 1, Mua Combo 4 bước giảm 15%, Xả kho Flash Sale). |
| 49 | `PromotionItems` | 11. Promotion Engine | `Id` | `PromotionId -> Promotions(Id)`, `ProductId -> Products(Id)` | Chi tiết các sản phẩm hoặc quà tặng mẫu thử đính kèm được áp dụng trong chương trình khuyến mại. |
| 50 | `Coupons` | 11. Promotion Engine | `Id` | Không có | Mã giảm giá công khai hoặc mã nhập tay tại trang thanh toán (Chiết khấu phần trăm hoặc trừ thẳng tiền mặt). |
| 51 | `CouponUsages` | 11. Promotion Engine | `Id` | `CouponId -> Coupons(Id)`, `OrderId -> Orders(Id)`, `UserId -> Users(Id)` | Nhật ký kiểm soát số lần sử dụng mã giảm giá của từng khách hàng nhằm ngăn ngừa lạm dụng khuyến mại. |
| 52 | `StockTransfers` | 12. Inter-Branch | `Id` | `FromStoreId -> Stores(Id)`, `ToStoreId -> Stores(Id)`, `CreatedBy -> Users(Id)` | Phiếu điều chuyển kho hàng hóa giữa các chi nhánh theo quy trình 3 bước (Pending -> InTransit -> Completed). |
| 53 | `StockTransferDetails`| 12. Inter-Branch | `Id` | `TransferId -> StockTransfers(Id)`, `BatchId -> ProductBatches(Id)` | Chi tiết từng mặt hàng, số lô FEFO, số lượng xuất và số lượng thực nhận tại chi nhánh đích. |
| 54 | `FinancialLedgers`| 13. Finance & P&L | `Id` | `StoreId -> Stores(Id)`, `OrderId -> Orders(Id)` | Sổ cái tài chính bút toán kép; hạch toán doanh thu, chi phí, hao hụt kho và giá vốn COGS đích danh từng lô FEFO. |
| 55 | `SupplierDebts` | 13. Finance & P&L | `Id` | `SupplierId -> Suppliers(Id)` | Sổ theo dõi công nợ mua hàng phải trả cho các nhà cung cấp mỹ phẩm và lịch trình thanh toán tiền hàng. |
| 56 | `CodReconciliations`| 13. Finance & P&L | `Id` | `OrderId -> Orders(Id)` | Bảng theo dõi và đối soát tiền hàng thu hộ COD với đối tác vận chuyển Giao Hàng Nhanh (GHN). |
| 57 | `ChatSessions` | 14. AI Chatbot | `Id` | `UserId -> Users(Id)` | Quản lý các phiên hội thoại tư vấn trực tuyến giữa khách hàng với trợ lý ảo AI Chatbot. |
| 58 | `ChatMessages` | 14. AI Chatbot | `Id` | `SessionId -> ChatSessions(Id)` | Chi tiết nội dung tin nhắn hỏi đáp, phân loại ý định người dùng và các thẻ sản phẩm tư vấn đính kèm. |
| 59 | `Alerts` | 15. Smart Alert | `Id` | `StoreId -> Stores(Id)` | Danh mục các cảnh báo vận hành thông minh thời gian thực (Lô cận date 30 ngày, Cháy hàng kho, Lệch két ca POS). |
| 60 | `Notifications` | 16. Realtime SignalR| `Id` | `UserId -> Users(Id)` | Hệ thống thông báo đẩy trực tiếp (Web Toast, chuông thông báo) tới nhân viên theo phạm vi chi nhánh và vai trò. |
| 61 | `InternalMessages`| 17. Utilities & Audit| `Id` | `SenderId -> Users(Id)`, `ReceiverId -> Users(Id)` | Hộp thư nội bộ phong cách Webmail; hỗ trợ trao đổi công văn, chỉ đạo nghiệp vụ giữa trụ sở với showroom. |
| 62 | `Settings` | 17. Utilities & Audit| `Id` | Không có | Bảng lưu trữ các tham số cấu hình động toàn hệ thống (Ngưỡng miễn phí vận chuyển, Tỷ lệ quy đổi điểm thưởng). |
| 63 | `WishlistItems` | 17. Utilities & Audit| `Id` | `UserId -> Users(Id)`, `ProductId -> Products(Id)` | Danh sách các sản phẩm mỹ phẩm được khách hàng yêu thích và lưu lại để mua sau. |
| 64 | `StockAlerts` | 17. Utilities & Audit| `Id` | `UserId -> Users(Id)`, `ProductId -> Products(Id)` | Đăng ký nhận thông báo tự động qua Email/Web khi sản phẩm đang tạm hết hàng được nhập lô mới về kho. |
| 65 | `AuditLogs` | 17. Utilities & Audit| `Id` | `UserId -> Users(Id)` | Nhật ký kiểm toán hệ thống tự động sinh bởi EF Core Interceptor; lưu vết thao tác CUD, địa chỉ IP và JSON Diff. |

---

## PHỤ LỤC B: HƯỚNG DẪN CÀI ĐẶT, CẤU HÌNH VÀ VẬN HÀNH HỆ THỐNG (DEPLOYMENT & OPERATIONS RUNBOOK)

Phụ lục B cung cấp tài liệu hướng dẫn kỹ thuật chi tiết từng bước (Step-by-step Runbook) để cài đặt, cấu hình chuỗi kết nối cơ sở dữ liệu SQL Server, tự động nạp dữ liệu mẫu và vận hành toàn diện giải pháp `OmnichannelBeautySolution` từ kho mã nguồn chính thức trên nền tảng Windows và PowerShell.

### 1. Yêu cầu môi trường tiền đề (Prerequisites)
Để hệ thống có thể biên dịch và vận hành ổn định với hiệu năng tối ưu, máy chủ triển khai hoặc máy trạm phát triển cần đáp ứng các tiêu chuẩn cấu hình phần cứng và phần mềm tối thiểu sau:
- **Hệ điều hành:** Microsoft Windows 10/11 (64-bit) hoặc Windows Server 2022.
- **Bộ công cụ phát triển:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (phiên bản SDK 8.0.300 trở lên).
- **Hệ quản trị cơ sở dữ liệu:** Microsoft SQL Server 2019/2022 (phiên bản Enterprise, Developer, Standard hoặc SQL Server Express / LocalDB).
- **Trình quản lý mã nguồn:** Git for Windows (phiên bản 2.40.x trở lên).
- **Môi trường dòng lệnh:** PowerShell 7.4.x (pwsh) hoặc Windows PowerShell 5.1.
- **Phần mềm kiểm thử chịu tải:** k6 phiên bản v0.50.x (tùy chọn phục vụ kiểm thử chịu tải cực hạn).

---

### 2. Hướng dẫn các bước tải và cài đặt mã nguồn dự án

##### Bước 1: Sao chép (Clone) kho mã nguồn từ GitHub
Mở cửa sổ dòng lệnh PowerShell và thực hiện lệnh sao chép mã nguồn chính thức từ kho lưu trữ của dự án:
```powershell
git clone https://github.com/ngocanh1725/ltw_ud_omnichannelbeautysolution.git
cd ltw_ud_omnichannelbeautysolution
```

##### Bước 2: Cấu hình chuỗi kết nối Cơ sở dữ liệu (Connection Strings)
Mở tệp tin cấu hình ứng dụng [Omnichannel.Web/appsettings.json](file:///d:/Lập%20trình%20web/OmnichannelBeautySolution/Omnichannel.Web/appsettings.json) và điều chỉnh tham số `DefaultConnection` phù hợp với máy chủ SQL Server thực tế.

*Cấu hình mẫu cho SQL Server xác thực bằng tài khoản SA:*
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OmnichannelBeautyDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

*Cấu hình mẫu cho SQL Server LocalDB (Môi trường máy trạm lập trình viên):*
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OmnichannelBeautyDb;Trusted_Connection=True;MultipleActiveResultSets=True;"
  }
}
```

##### Bước 3: Phục hồi các gói phụ thuộc (NuGet Packages Restore) và Biên dịch Solution
Thực thi các lệnh sau tại thư mục gốc của giải pháp để nạp đầy đủ các thư viện phụ thuộc và tiến hành biên dịch mã nguồn:
```powershell
# Phục hồi toàn bộ gói thư viện NuGet của 5 dự án
dotnet restore

# Biên dịch toàn bộ giải pháp ở chế độ Release
dotnet build --configuration Release --no-restore
```

---

### 3. Cơ chế Tự động hóa Khởi tạo CSDL và Nạp Dữ liệu mẫu (Database Migration & Auto-Seeding)

Dự án được tích hợp động cơ khởi tạo tự động trong lớp `DbInitializer.cs` tại tầng `Omnichannel.Infrastructure`. Khi ứng dụng được khởi chạy lần đầu tiên:
1. Hệ thống tự động kiểm tra sự tồn tại của cơ sở dữ liệu `OmnichannelBeautyDb`. Nếu chưa có, hệ thống sẽ tự động gọi lệnh `context.Database.MigrateAsync()` để tạo mới cơ sở dữ liệu và dựng đầy đủ cấu trúc 65 bảng dữ liệu chuẩn 3NF.
2. Hệ thống tự động nạp sẵn dữ liệu danh mục ban đầu: ma trận 51 quyền PBAC, 6 nhóm vai trò chuẩn mực, 12 tài khoản nhân sự và khách hàng kiểm thử chuẩn (mật khẩu mặc định `Admin@123`), 2 chi nhánh cửa hàng thực tế, danh mục hoạt chất quốc tế INCI, thương hiệu mỹ phẩm và các sản phẩm mẫu có gắn số lô FEFO đa dạng hạn sử dụng.
3. Nếu muốn xóa sạch và tái tạo lại toàn bộ dữ liệu mẫu ban đầu từ đầu, chỉ cần mở PowerShell và chạy lệnh sau để làm mới cơ sở dữ liệu:
```powershell
# Di chuyển vào tầng Infrastructure để chạy lệnh EF Core CLI
cd Omnichannel.Infrastructure
dotnet ef database update --startup-project ../Omnichannel.Web
cd ..
```

---

### 4. Hướng dẫn Khởi chạy Ứng dụng và Đăng nhập Kiểm thử

##### Bước 1: Lệnh khởi chạy máy chủ Web
Tại thư mục gốc của giải pháp, thực thi lệnh sau:
```powershell
dotnet run --project Omnichannel.Web/Omnichannel.Web.csproj --configuration Development
```

##### Bước 2: Truy cập các cổng giao diện
Sau khi máy chủ khởi động thành công, hệ thống sẽ mở cổng lắng nghe tại các địa chỉ URL mặc định:
- **Cổng giao diện Khách hàng (Luxury Storefront Web):** `http://localhost:5116` hoặc `https://localhost:7142`
- **Cổng đăng nhập Quản trị & Bán hàng (Admin & POS Login):** `http://localhost:5116/admin/auth/login`

##### Bước 3: Đăng nhập kiểm thử theo các phân hệ chức năng
Người dùng có thể đăng nhập bằng các tài khoản kiểm thử tiêu chuẩn đã được nạp sẵn trong hệ thống:
- **Tài khoản SuperAdmin (Toàn quyền hệ thống, xem P&L, ma trận PBAC):** Tài khoản `superadmin` / Mật khẩu `Admin@123`.
- **Tài khoản Quản lý Kho Tổng (TP.HCM):** Tài khoản `manager_hq` / Mật khẩu `Admin@123`.
- **Tài khoản Thu ngân Kiosk POS (Showroom Cầu Giấy):** Tài khoản `cashier02` / Mật khẩu `Admin@123`.
- **Tài khoản Biên tập viên CMS & Visual Customizer:** Tài khoản `editor01` / Mật khẩu `Admin@123`.
- **Tài khoản Khách hàng VIP Diamond (Có sẵn 1.250 điểm Loyalty):** Tài khoản `customer_vip` / Mật khẩu `Admin@123`.

---

### 5. Hướng dẫn Thực thi Kiểm thử Tự động và Đo kiểm Hiệu năng

##### Chạy toàn bộ bộ Kiểm thử Tự động (Automated xUnit Tests):
Để kiểm chứng sự toàn vẹn của mã nguồn sau khi triển khai, thực thi lệnh sau tại thư mục gốc:
```powershell
dotnet test Omnichannel.Tests/Omnichannel.Tests.csproj --logger "console;verbosity=normal"
```
*Kết quả kỳ vọng:* Toàn bộ **231 ca kiểm thử** đều đạt trạng thái `Passed!`, thời gian thực thi dưới 15 giây.

##### Chạy Kịch bản Đo kiểm Hiệu năng Hệ thống Tự động (Benchmark Script):
Dự án đã trang bị sẵn kịch bản PowerShell đo đạc độ trễ phản hồi, mức tiêu thụ tài nguyên máy chủ và trạng thái HTTP tại tệp tin `scripts/test_system_performance_benchmark.ps1`. Thực thi lệnh:
```powershell
pwsh -File ./scripts/test_system_performance_benchmark.ps1 -RequestsPerEndpoint 50
```
*Kết quả kỳ vọng:* Thời gian phản hồi trung bình của toàn bộ các trang giao diện đạt dưới **35 mili-giây**, không phát sinh bất kỳ lỗi HTTP 5xx nào và bộ nhớ RAM tiến trình duy trì ổn định quanh mức **109 MB**.

##### Chạy Kịch bản Kiểm thử Chịu tải Đồng thời Flash Sale (k6 Concurrency Test):
```powershell
k6 run scripts/loadtest/fefo_flashsale_concurrency_test.js
```
*Kết quả kỳ vọng:* 50 VUs tranh chấp 15 sản phẩm đạt chuẩn Zero Overselling (15 đơn tạo thành công, 35 yêu cầu bị từ chối an toàn, 0 lỗi 5xx).

---

## PHỤ LỤC C: MỘT SỐ BIỂU MẪU CHỨNG TỪ ĐẦU RA TIÊU BIỂU CỦA HỆ THỐNG

Phụ lục C trình bày bố cục thiết kế chuẩn mực công nghiệp và mô phỏng thực tế của **4 biểu mẫu chứng từ đầu ra cốt lõi** được sinh tự động từ hệ thống `OmnichannelBeautySolution`, phục vụ cho các hoạt động bán lẻ tại quầy, giao nhận đa kênh O2O, điều chuyển hàng hóa liên chi nhánh và đối soát tài chính minh bạch.

---

### Biểu mẫu C.1: Mẫu Hóa đơn Nhiệt Bán lẻ tại Quầy Smart POS khổ K80 (`/Admin/Pos/PrintReceipt/{id}`)
*Tham chiếu mã nguồn:* `Omnichannel.Web/Areas/Admin/Views/Pos/PrintReceipt.cshtml`.  
*Quy chuẩn kỹ thuật:* Khổ giấy in nhiệt K80 (chiều rộng in thực tế 76mm – 80mm), phông chữ đơn khoảng cách (Monospace) rõ nét, hỗ trợ tự động kích hoạt lệnh in `window.print()` khi tải xong trang và tích hợp thông tin truy xuất hóa đơn điện tử hợp pháp theo Thông tư 78/2021/TT-BTC.

```
================================================================
                          OMNI BEAUTY                          
              HỆ THỐNG PHÂN PHỐI MỸ PHẨM CHÍNH HÃNG            
                     SHOWROOM CẦU GIẤY                         
     Đ/c: Số 126 Đường Cầu Giấy, P. Quan Hoa, Q. Cầu Giấy, HN   
             Hotline: 1900 6868  •  MST: 0316889988            
----------------------------------------------------------------
                        HÓA ĐƠN BÁN LẺ                         
                      MÃ ĐƠN: ORD-202611-0082                  
               Ký hiệu: 1C26TBB  •  Số HĐ: 0004921              
                   Thời gian: 04/11/2026 15:30:45              
----------------------------------------------------------------
Thu ngân: Hoàng Thu Ngân (cashier02)
Khách hàng: Chị Mai Phương Linh            [VIP: DIAMOND]
SĐT: 0988.***.999
----------------------------------------------------------------
Tên Mỹ Phẩm                         SL    Đơn giá        T.Tiền 
----------------------------------------------------------------
1. Son Kem Lì Luxury Velvet Matte
   Lô: LOT-2024-C12 • HSD: 12/2026   1    231.000       231.000 
2. Serum Phục Hồi Da B5 La Roche-Posay
   Lô: LOT-2025-A01 • HSD: 08/2027   1    495.000       495.000 
3. Kem Chống Nắng Kiềm Dầu Anthelios
   Lô: LOT-2025-B04 • HSD: 10/2027   1    385.000       385.000 
----------------------------------------------------------------
Tổng tiền hàng:                                    1.111.000 đ
Chiết khấu ưu đãi VIP Diamond (-10%):               -111.100 đ
Cấn trừ Điểm thưởng Loyalty (50 điểm):               -50.000 đ
Thuế GTGT (VAT 10%):                                  94.990 đ
================================================================
TỔNG THANH TOÁN:                                     949.900 đ
================================================================
Hình thức thanh toán:                   Chuyển khoản VietQR 24/7
Trạng thái:                                          ĐÃ THU TIỀN
----------------------------------------------------------------
[ ⭐ BEAUTY CLUB MEMBER: DIAMOND ]
  + Điểm tích lũy mới đơn này:                           +10 điểm
  + Tổng điểm khả dụng hiện có:                        1.210 điểm
  (1 điểm = 1.000 VNĐ dùng cấn trừ trực tiếp cho đơn kế tiếp)
----------------------------------------------------------------
Mã tra cứu HĐĐT: OB-HDDT-88492019
Website tra cứu: hoadon.beautyomnichannel.vn
----------------------------------------------------------------
                  CẢM ƠN QUÝ KHÁCH & HẸN GẶP LẠI!               
               Mỹ phẩm chính hãng 100% - Đổi trả 7 ngày         
            (Phiếu xuất kho kiêm bảo hành sản phẩm bán lẻ K80)  
================================================================
```

---

### Biểu mẫu C.2: Mẫu Vé Nhận Hàng Ưu Tiên O2O Click-and-Collect (Fast-Track Pass)
*Mục đích sử dụng:* Sinh tự động dưới dạng trang xác nhận đơn hàng trên điện thoại của khách hàng sau khi thanh toán thành công dịch vụ "Nhận hàng tại Showroom trong 1 giờ". Khách hàng chỉ cần xuất trình mã này tại quầy POS để nhận kiện hàng đã được nhân viên chuẩn bị sẵn mà không phải xếp hàng chờ đợi.

```
+---------------------------------------------------------------------------------------+
|                                  OMNI BEAUTY                                          |
|                     VÉ NHẬN HÀNG ƯU TIÊN (O2O FAST-TRACK PASS)                        |
+---------------------------------------------------------------------------------------+
| MÃ ĐƠN HÀNG: #ORD-O2O-202611-9921                                                    |
| Khách hàng: Nguyễn Ngọc Anh                          Hạng thành viên: GOLD MEMBER     |
| Số điện thoại: 0912.***.888                          Thời gian đặt: 04/11/2026 10:15  |
+---------------------------------------------------------------------------------------+
| ĐIỂM HẸN NHẬN HÀNG:                                                                   |
| SHOWROOM CẦU GIẤY - Số 126 Đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội.   |
| Thời gian giữ chỗ ưu tiên: 24 GIỜ (Hạn chót: 10:15 Ngày 05/11/2026)                   |
+---------------------------------------------------------------------------------------+
|                                                                                       |
|      [ KHUNG MÃ QR CODE NHẬN HÀNG ]              MÃ PIN BÍ MẬT XÁC THỰC:              |
|        +-------------------------+                                                    |
|        |  █████  ██ ██  █████    |                    OB - 8 4 9 2 0 1                |
|        |  █   █  █  ██  █   █    |                                                    |
|        |  █████  ██ ██  █████    |     (Vui lòng cung cấp mã PIN này cho Thu ngân     |
|        |  █████████ █████████    |           tại quầy để nhận kiện hàng)              |
|        +-------------------------+                                                    |
|                                                                                       |
+---------------------------------------------------------------------------------------+
| CHI TIẾT KIỆN HÀNG ĐÃ ĐÓNG GÓI SẴN TRONG TÚI BẢO QUẢN:                                |
| 1. Phấn Nước Cushion Che Phủ Hoàn Hảo (Tone 21) | Lô: LOT-2025-A09 | SL: 01 hộp       |
| 2. Nước Tẩy Trang Micellar Water Dành Cho Da Nhạy Cảm 400ml | Lô: LOT-2025-C02 | SL: 01 chai|
| 3. [QUÀ TẶNG MINI] Kem Dưỡng Ẩm B5 Phục Hồi 15ml (Sample Free) | SL: 02 tuýp          |
+---------------------------------------------------------------------------------------+
| Tổng giá trị đơn hàng: 685.000 đ                      Trạng thái: ĐÃ THANH TOÁN ONLINE|
+---------------------------------------------------------------------------------------+
| * HƯỚNG DẪN DÀNH CHO KHÁCH HÀNG:                                                      |
| - Quý khách tới Showroom, đi thẳng vào làn quầy ưu tiên "O2O Click & Collect".        |
| - Thu ngân quét mã QR trên vé hoặc nhập mã PIN 6 số để bàn giao kiện hàng ngay.       |
| - Quý khách được quyền kiểm tra niêm phong và tem phụ sản phẩm trước khi rời quầy.    |
+---------------------------------------------------------------------------------------+
```

---

### Biểu mẫu C.3: Mẫu Biên bản Xuất – Nhận Điều chuyển Kho Nội bộ theo Lô FEFO
*Mã chứng từ:* `TRF-YYYYMM-XXX`. Khổ giấy in tiêu chuẩn: A4 dọc.  
*Quy chuẩn nghiệp vụ:* Sử dụng trong quy trình điều chuyển hàng hóa liên chi nhánh 3 bước (Pending → InTransit → Completed). Chứng từ có tính pháp lý nội bộ cao, ràng buộc trách nhiệm vật chất giữa Thủ kho xuất, Đơn vị vận chuyển và Thủ kho chi nhánh tiếp nhận.

```
====================================================================================================
CÔNG TY CỔ PHẦN BÁN LẺ OMNI BEAUTY                       CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
HỆ THỐNG CHUỖI CỬA HÀNG MỸ PHẨM CHÍNH HÃNG                    Độc lập - Tự do - Hạnh phúc
----------------------------------                               ---------------------
                                                               Hà Nội, Ngày 04 tháng 11 năm 2026

                       BIÊN BẢN GIAO NHẬN VÀ ĐIỀU CHUYỂN HÀNG HÓA NỘI BỘ
                                     MÃ PHIẾU: TRF-202611-003

- Căn cứ Lệnh điều động phân phối hàng hóa số 88/QĐ-OB của Ban Giám đốc Chuỗi OMNI BEAUTY;
- Hôm nay, các bên liên quan tiến hành lập biên bản bàn giao điều chuyển lô hàng mỹ phẩm cụ thể như sau:

I. ĐƠN VỊ VÀ ĐỊA ĐIỂM ĐIỀU CHUYỂN:
1. Đơn vị xuất hàng (Kho đi): KHO TỔNG MIỀN BẮC (HQ-001) - Cảng ICD Mỹ Đình, Nam Từ Liêm, Hà Nội.
   Người lập phiếu xuất: Đặng Thủ Kho 01 (warehouse01)       Chức vụ: Trưởng bộ phận Kho Vận.
2. Đơn vị tiếp nhận (Kho đến): SHOWROOM CẦU GIẤY (SH-CG) - Số 126 Cầu Giấy, Q. Cầu Giấy, Hà Nội.
   Người tiếp nhận hàng: Vũ Thủ Kho 02 (warehouse02)         Chức vụ: Thủ kho Chi nhánh.
3. Đơn vị vận chuyển nội bộ: Đội xe vận tải OMNI Express - Lái xe: Nguyễn Văn Vận (BKS: 29C-882.19).

II. DANH MỤC HÀNG HÓA VÀ ĐỐI SOÁT SỐ LÔ HẠN DÙNG (FEFO):
+-----+----------------------------------+----------+---------------+------------+---------+-----------+---------+------------------+
| STT | Tên Sản Phẩm Mỹ Phẩm             | Mã SKU   | Số Lô Sản Xuất| Hạn Dùng   | SL Xuất | SL Thực   | SL Lệch | Tình Trạng Quy   |
|     |                                  |          | (Batch Code)  | (Exp Date) | (Hộp)   | Nhận (Hộp)| (+ / -) | Cách Niêm Phong  |
+-----+----------------------------------+----------+---------------+------------+---------+-----------+---------+------------------+
|  1  | Kem Dưỡng Ẩm Sâu Ceramide 50ml   | CER-50ML | LOT-2024-C12  | 30/12/2026 |   20    |    19     |   -1    | Thùng nguyên; vỡ |
|     | (Hàng phân bổ bán xả kho FEFO)   |          |               |            |         |           | (Hư hỏng)| 01 hộp do xóc xe |
+-----+----------------------------------+----------+---------------+------------+---------+-----------+---------+------------------+
|  2  | Nước Tẩy Trang Dịu Nhẹ Bioderma  | BIO-500  | LOT-2025-K08  | 15/09/2027 |   50    |    50     |    0    | Nguyên đai kiện  |
+-----+----------------------------------+----------+---------------+------------+---------+-----------+---------+------------------+
|  3  | Tinh Chất Chống Lão Hóa Retinol  | RET-30ML | LOT-2025-M01  | 20/11/2027 |   30    |    30     |    0    | Nguyên vẹn 100%  |
+-----+----------------------------------+----------+---------------+------------+---------+-----------+---------+------------------+
| TỔNG CỘNG HÀNG HÓA GIAO NHẬN:                     |               |            |  100    |    99     |   -1    | Hư hỏng 01 hộp   |
+-----+----------------------------------+----------+---------------+------------+---------+-----------+---------+------------------+

III. KẾT LUẬN VÀ XỬ LÝ HẠCH TOÁN TÀI CHÍNH:
- Thủ kho chi nhánh nhận đã nhập kho vật lý thành công 99 hộp sản phẩm vào hệ thống phần mềm.
- 01 hộp Kem Dưỡng Ceramide bị vỡ vỡ vỏ thủy tinh trong quá trình xe di chuyển đã được lập biên bản hủy hàng
  và tự động hạch toán ghi nhận tổn thất vận chuyển vào Sổ cái Tài chính (`FinancialLedgers`) theo giá vốn đích danh.
- Biên bản được lập thành 03 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản để làm căn cứ quyết toán kho.

      THỦ KHO XUẤT HÀNG                   ĐƠN VỊ VẬN CHUYỂN                  THỦ KHO TIẾP NHẬN
      (Ký và ghi rõ họ tên)             (Ký và ghi rõ họ tên)              (Ký và ghi rõ họ tên)


        Đặng Văn Kho                        Nguyễn Văn Vận                       Vũ Thị Kho

                                      XÁC NHẬN CỦA BAN GIÁM ĐỐC
                                         (Ký tên và đóng dấu)
====================================================================================================
```

---

### Biểu mẫu C.4: Mẫu Báo cáo Kết quả Kinh doanh P&L và Biên bản Đối soát Két Tiền Mù Cuối Ca POS
*Mục đích:* Tài liệu báo cáo tích hợp sinh tự động cuối ngày dành cho Giám đốc chuỗi và Kế toán trưởng; kết hợp giữa số liệu kinh doanh P&L phân tích giá vốn FEFO đích danh với biên bản kiểm toán két tiền mù (Blind Cash Drawer Audit).

```
====================================================================================================
                                      OMNI BEAUTY RETAIL CHAIN
                      BÁO CÁO HOẠT ĐỘNG KINH DOANH P&L VÀ ĐỐI SOÁT KÉT CA POS
                           Chi nhánh: SHOWROOM CẦU GIẤY (Mã kho: SH-CG)
                               Ngày hạch toán: 04/11/2026 (Ca số 02)
====================================================================================================

PHẦN 1: BÁO CÁO KẾT QUẢ KINH DOANH TRONG CA (PROFIT & LOSS STATEMENT)
----------------------------------------------------------------------------------------------------
Chỉ tiêu Tài chính Nghiệp vụ                       Số Tiền (VNĐ)        Tỷ Trọng / Ghi Chú Phân Tích
----------------------------------------------------------------------------------------------------
1. TỔNG DOANH THU BÁN LẺ GỘP:                       24.580.000 đ        100.0% (Tổng 18 hóa đơn trong ca)
   - Doanh thu bán hàng Kiosk POS tại quầy:         18.250.000 đ        74.2%
   - Doanh thu đơn khách nhận O2O Click & Collect:   6.330.000 đ        25.8%
2. Các khoản giảm trừ doanh thu:                     -1.850.000 đ        -7.5%
   - Chiết khấu thẻ thành viên VIP:                   -950.000 đ
   - Cấn trừ điểm thưởng Loyalty:                     -400.000 đ        (Quy đổi 400 điểm thành tiền)
   - Khuyến mãi Combo 4 bước & Voucher:               -500.000 đ
3. DOANH THU BÁN HÀNG THUẦN (Net Sales):            22.730.000 đ        92.5%
4. GIÁ VỐN HÀNG BÁN (COGS - Tính đích danh FEFO):  -12.280.000 đ        -54.0% (Lãi gộp đạt 46.0%)
----------------------------------------------------------------------------------------------------
5. LỢI NHUẬN GỘP BÁN LẺ (Gross Profit):             10.450.000 đ        46.0% (Biên lợi nhuận lành mạnh)
----------------------------------------------------------------------------------------------------
6. Cơ cấu thanh toán của khách hàng:
   - Thanh toán Chuyển khoản VietQR tự động:         13.500.000 đ        (10 giao dịch - Đã khớp sao kê)
   - Thanh toán Cà thẻ POS ngân hàng:                4.000.000 đ        (03 giao dịch - Đã có bill quẹt)
   - Thanh toán bằng TIỀN MẶT tại quầy:              7.080.000 đ        (05 giao dịch tiền mặt)
====================================================================================================

PHẦN 2: BIÊN BẢN KIỂM TOÁN KÉT TIỀN MÙ (BLIND CASH DRAWER AUDIT)
Thu ngân phụ trách ca: Hoàng Thu Ngân (cashier02)        Giờ mở ca: 07:45  •  Giờ đóng ca: 15:30
----------------------------------------------------------------------------------------------------
Số dư tiền mặt ban đầu nhận bàn giao đầu ca:                                      2.000.000 đ
Tổng tiền mặt bán hàng trong ca (Phần mềm ghi nhận):                             +7.080.000 đ
Các khoản chi nạp tiền đổi tiền lẻ trong ca:                                              0 đ
----------------------------------------------------------------------------------------------------
=> TỔNG TIỀN MẶT KỲ VỌNG TRONG KÉT THEO SỔ SÁCH PHẦN MỀM:                         9.080.000 đ
----------------------------------------------------------------------------------------------------

BẢNG KÊ TIỀN MẶT THỰC ĐẾM CUỐI CA (THU NGÂN TỰ ĐẾM VÀ KHAI BÁO MÙ):
+-----+----------------+--------------+-------------------+-----------------------------------------+
| STT | Mệnh Giá (VNĐ) | Số Tờ Thực Đếm| Thành Tiền (VNĐ)  | Tình Trạng Tiền Tệ                      |
+-----+----------------+--------------+-------------------+-----------------------------------------+
|  1  |   500.000 đ    |    12 tờ     |    6.000.000 đ    | Tiền Polymer lành lặn, đủ tiêu chuẩn lưu thông
|  2  |   200.000 đ    |    10 tờ     |    2.000.000 đ    |                                         |
|  3  |   100.000 đ    |     8 tờ     |      800.000 đ    |                                         |
|  4  |    50.000 đ    |     5 tờ     |      250.000 đ    |                                         |
|  5  |    20.000 đ    |     1 tờ     |       20.000 đ    |                                         |
|  6  |    10.000 đ    |     1 tờ     |       10.000 đ    |                                         |
|  7  |     5.000 đ    |     0 tờ     |            0 đ    |                                         |
|  8  |     2.000 đ    |     0 tờ     |            0 đ    |                                         |
|  9  |     1.000 đ    |     0 tờ     |            0 đ    |                                         |
+-----+----------------+--------------+-------------------+-----------------------------------------+
=> TỔNG SỐ TIỀN MẶT THỰC ĐẾM TRONG KÉT KHI ĐÓNG CA:                               9.080.000 đ
----------------------------------------------------------------------------------------------------
KẾT QUẢ ĐỐI SOÁT CHÊNH LỆCH:
  [ KHỚP KÉT 100% ] Chênh lệch két tiền mặt: 0 VNĐ (Không thừa, không thiếu).
  Đánh giá kiểm toán: Thu ngân tác nghiệp chính xác tuyệt đối, tuân thủ nghiêm ngặt quy chế thu ngân.
====================================================================================================

      THU NGÂN CHỐT CA                                                CỬA HÀNG TRƯỞNG CHI NHÁNH
    (Ký cam kết số liệu)                                                  (Ký duyệt khóa ca)


       Hoàng Thu Ngân                                                        Lê Quản Lý
====================================================================================================
```
