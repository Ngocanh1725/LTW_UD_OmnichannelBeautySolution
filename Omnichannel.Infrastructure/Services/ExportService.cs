using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using Omnichannel.Application.DTOs.Finance;
using Omnichannel.Application.DTOs.Reports;
using Omnichannel.Application.Interfaces.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Omnichannel.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        public ExportService()
        {
            // Cấu hình giấy phép QuestPDF Community & kích hoạt system fonts
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        #region 1. Xuất Báo Cáo Doanh Thu (Excel & CSV)
        public byte[] ExportRevenueOrdersToExcel(List<OrderReportItemDto> orders, string periodLabel, string storeLabel)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BaoCaoDoanhThu");

            // Cài đặt chung
            ws.ShowGridLines = true;
            ws.Style.Font.FontName = "Segoe UI";
            ws.Style.Font.FontSize = 10;

            // 1. Tiêu đề chính
            ws.Cell("A1").Value = "HỆ THỐNG QUẢN LÝ BÁN LẺ MỸ PHẨM CANMAKE BEAUTY";
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromArgb(124, 58, 237); // Purple 600

            ws.Cell("A2").Value = "BÁO CÁO DOANH THU & DANH SÁCH ĐƠN HÀNG CHI TIẾT";
            ws.Cell("A2").Style.Font.FontSize = 12;
            ws.Cell("A2").Style.Font.Bold = true;

            ws.Cell("A3").Value = $"Kỳ báo cáo: {periodLabel} | Chi nhánh: {storeLabel} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;

            // 2. Thẻ tóm tắt chỉ số
            int summaryStartRow = 5;
            decimal totalRev = orders.Sum(o => o.TotalAmount);
            int totalCount = orders.Count;
            decimal aov = totalCount > 0 ? totalRev / totalCount : 0;

            ws.Cell(summaryStartRow, 1).Value = "Tổng Doanh Thu:";
            ws.Cell(summaryStartRow, 1).Style.Font.Bold = true;
            ws.Cell(summaryStartRow, 2).Value = totalRev;
            ws.Cell(summaryStartRow, 2).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(summaryStartRow, 2).Style.Font.Bold = true;
            ws.Cell(summaryStartRow, 2).Style.Font.FontColor = XLColor.FromHtml("#059669");

            ws.Cell(summaryStartRow, 4).Value = "Tổng Đơn Hàng:";
            ws.Cell(summaryStartRow, 4).Style.Font.Bold = true;
            ws.Cell(summaryStartRow, 5).Value = totalCount;
            ws.Cell(summaryStartRow, 5).Style.Font.Bold = true;

            ws.Cell(summaryStartRow, 7).Value = "Giá Trị TB Đơn (AOV):";
            ws.Cell(summaryStartRow, 7).Style.Font.Bold = true;
            ws.Cell(summaryStartRow, 8).Value = aov;
            ws.Cell(summaryStartRow, 8).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(summaryStartRow, 8).Style.Font.Bold = true;

            // 3. Tiêu đề bảng
            int headerRow = 7;
            string[] headers = { "STT", "Mã Đơn Hàng", "Ngày Đặt", "Chi Nhánh", "Kênh Bán", "Khách Hàng", "Số Điện Thoại", "Hình Thức TT", "Trạng Thái Đơn", "Trạng Thái TT", "Tổng Tiền (VNĐ)" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(124, 58, 237); // Purple Primary
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(headerRow).Height = 26;

            // 4. Đổ dữ liệu
            int currentRow = headerRow + 1;
            int stt = 1;

            foreach (var o in orders)
            {
                ws.Cell(currentRow, 1).Value = stt++;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = o.OrderCode;
                ws.Cell(currentRow, 2).Style.Font.Bold = true;

                ws.Cell(currentRow, 3).Value = o.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 4).Value = o.StoreName;

                ws.Cell(currentRow, 5).Value = o.OrderSource == "WEBSITE" ? "Website Online" : "Cửa hàng (POS)";
                ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 6).Value = o.CustomerName;
                ws.Cell(currentRow, 7).Value = o.CustomerPhone;

                ws.Cell(currentRow, 8).Value = o.PaymentMethod;
                ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 9).Value = o.OrderStatus;
                ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 10).Value = o.PaymentStatus;
                ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 11).Value = o.TotalAmount;
                ws.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(currentRow, 11).Style.Font.Bold = true;

                // Màu xen kẽ
                if (stt % 2 == 0)
                {
                    ws.Range(currentRow, 1, currentRow, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                }

                currentRow++;
            }

            // 5. Dòng tổng cộng
            ws.Cell(currentRow, 1).Value = "TỔNG CỘNG";
            ws.Range(currentRow, 1, currentRow, 10).Merge();
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(currentRow, 11).Value = totalRev;
            ws.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(currentRow, 11).Style.Font.Bold = true;
            ws.Cell(currentRow, 11).Style.Font.FontColor = XLColor.FromHtml("#059669");

            var totalRange = ws.Range(currentRow, 1, currentRow, 11);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromArgb(241, 245, 249);
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportRevenueOrdersToCsv(List<OrderReportItemDto> orders)
        {
            var sb = new StringBuilder();
            // UTF-8 BOM for Excel opening properly without Vietnamese character corruption
            sb.Append('\uFEFF');

            sb.AppendLine("STT,MaDonHang,NgayDat,ChiNhanh,KenhBan,KhachHang,SoDienThoai,PhuongThucTT,TrangThaiDon,TrangThaiTT,TongTien");

            int stt = 1;
            foreach (var o in orders)
            {
                string kenh = o.OrderSource == "WEBSITE" ? "Website Online" : "Tại quầy (POS)";
                string khachHang = $"\"{o.CustomerName.Replace("\"", "\"\"")}\"";
                string chiNhanh = $"\"{o.StoreName.Replace("\"", "\"\"")}\"";

                sb.AppendLine($"{stt++},{o.OrderCode},{o.CreatedAt:yyyy-MM-dd HH:mm},{chiNhanh},{kenh},{khachHang},{o.CustomerPhone},{o.PaymentMethod},{o.OrderStatus},{o.PaymentStatus},{o.TotalAmount.ToString(CultureInfo.InvariantCulture)}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        #endregion

        #region 2. Xuất Báo Cáo Lãi Lỗ P&L (Excel & PDF)
        public byte[] ExportProfitLossToExcel(ProfitLossReportDto pnl)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BaoCao_PnL");

            ws.ShowGridLines = true;
            ws.Style.Font.FontName = "Segoe UI";
            ws.Style.Font.FontSize = 10;

            // Tiêu đề
            ws.Cell("A1").Value = "CANMAKE BEAUTY - BÁO CÁO TÀI CHÍNH";
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromArgb(79, 70, 229); // Indigo 600

            ws.Cell("A2").Value = "BÁO CÁO KẾT QUẢ KINH DOANH (P&L - PROFIT AND LOSS)";
            ws.Cell("A2").Style.Font.FontSize = 12;
            ws.Cell("A2").Style.Font.Bold = true;

            string storeText = pnl.StoreName ?? "Toàn Hệ Thống";
            ws.Cell("A3").Value = $"Chi nhánh: {storeText} | Từ ngày: {pnl.FromDate:dd/MM/yyyy} đến {pnl.ToDate:dd/MM/yyyy} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;

            // Bảng 1: Báo Cáo Tổng Hợp P&L
            int startRow = 6;
            ws.Cell(startRow, 1).Value = "CHỈ TIÊU TÀI CHÍNH";
            ws.Cell(startRow, 2).Value = "SỐ TIỀN (VNĐ)";
            ws.Cell(startRow, 3).Value = "TỶ LỆ (%)";
            ws.Cell(startRow, 4).Value = "GHI CHÚ / PHƯƠNG PHÁP TÍNH";

            var headRange = ws.Range(startRow, 1, startRow, 4);
            headRange.Style.Font.Bold = true;
            headRange.Style.Font.FontColor = XLColor.White;
            headRange.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 70, 229);

            var items = new (string Name, decimal Amount, decimal Pct, string Note, bool Bold, XLColor Color)[]
            {
                ("1. Doanh Thu Thuần Bán Hàng", pnl.Revenue, 100m, "Tổng doanh số từ các đơn đã thanh toán hoàn tất", true, XLColor.Black),
                ("2. Giá Vốn Hàng Bán (COGS)", pnl.CostOfGoodsSold, pnl.Revenue > 0 ? (pnl.CostOfGoodsSold / pnl.Revenue) * 100 : 0, "Tính theo phương pháp bình quân gia quyền (WAC) & lô FEFO", false, XLColor.DarkGray),
                ("3. Lợi Nhuận Gộp (Gross Profit)", pnl.GrossProfit, pnl.GrossMarginPercent, "Lãi gộp = Doanh thu thuần - Giá vốn (COGS)", true, XLColor.FromHtml("#059669")),
                ("4. Chi Phí Vận Hành (Operating Expenses)", pnl.OperatingExpenses, pnl.Revenue > 0 ? (pnl.OperatingExpenses / pnl.Revenue) * 100 : 0, "Chi phí thuê mặt bằng, điện nước, marketing từ Sổ cái", false, XLColor.FromHtml("#E11D48")),
                ("5. Lợi Nhuận Ròng (Net Profit)", pnl.NetProfit, pnl.NetMarginPercent, "Lãi ròng = Lợi nhuận gộp - Chi phí vận hành", true, pnl.NetProfit >= 0 ? XLColor.FromHtml("#059669") : XLColor.FromHtml("#DC2626"))
            };

            int r = startRow + 1;
            foreach (var item in items)
            {
                ws.Cell(r, 1).Value = item.Name;
                ws.Cell(r, 2).Value = item.Amount;
                ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(r, 3).Value = item.Pct / 100m;
                ws.Cell(r, 3).Style.NumberFormat.Format = "0.00%";
                ws.Cell(r, 4).Value = item.Note;

                if (item.Bold)
                {
                    ws.Range(r, 1, r, 4).Style.Font.Bold = true;
                }
                ws.Cell(r, 2).Style.Font.FontColor = item.Color;
                r++;
            }

            // Bảng 2: Phân bổ P&L theo từng chi nhánh
            r += 2;
            ws.Cell(r, 1).Value = "CHI TIẾT HIỆU QUẢ KINH DOANH THEO TỪNG CHI NHÁNH (STORE P&L BREAKDOWN)";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontSize = 11;
            r++;

            string[] storeHeaders = { "Chi Nhánh", "Diện Tích (m²)", "Doanh Thu (VNĐ)", "Giá Vốn COGS (VNĐ)", "Lãi Gộp (VNĐ)", "Chi Phí (VNĐ)", "Lãi Ròng (VNĐ)", "Doanh Thu / m²" };
            for (int i = 0; i < storeHeaders.Length; i++)
            {
                var cell = ws.Cell(r, i + 1);
                cell.Value = storeHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(71, 85, 105); // Slate 600
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            r++;

            foreach (var st in pnl.BreakdownByStore)
            {
                ws.Cell(r, 1).Value = st.StoreName;
                ws.Cell(r, 2).Value = st.Area;
                ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(r, 3).Value = st.Revenue;
                ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0 ₫";

                ws.Cell(r, 4).Value = st.Cogs;
                ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0 ₫";

                ws.Cell(r, 5).Value = st.GrossProfit;
                ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(r, 5).Style.Font.Bold = true;
                ws.Cell(r, 5).Style.Font.FontColor = XLColor.FromHtml("#059669");

                ws.Cell(r, 6).Value = st.OperatingExpenses;
                ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0 ₫";

                ws.Cell(r, 7).Value = st.NetProfit;
                ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(r, 7).Style.Font.Bold = true;
                ws.Cell(r, 7).Style.Font.FontColor = st.NetProfit >= 0 ? XLColor.FromHtml("#059669") : XLColor.FromHtml("#DC2626");

                ws.Cell(r, 8).Value = st.SalesPerSqft;
                ws.Cell(r, 8).Style.NumberFormat.Format = "#,##0 ₫/m²";
                r++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportProfitLossToPdf(ProfitLossReportDto pnl)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CANMAKE BEAUTY").FontSize(14).Bold().FontColor(Colors.Indigo.Medium);
                                c.Item().Text("BÁO CÁO LÃI LỖ & HIỆU QUẢ KINH DOANH (P&L REPORT)").FontSize(11).Bold();
                                c.Item().Text($"Kỳ báo cáo: Từ {pnl.FromDate:dd/MM/yyyy} đến {pnl.ToDate:dd/MM/yyyy}").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                            });

                            row.ConstantItem(150).Column(c =>
                            {
                                c.Item().AlignRight().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                                c.Item().AlignRight().Text($"Phạm vi: {pnl.StoreName ?? "Toàn hệ thống"}").FontSize(8).Bold();
                            });
                        });

                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    // Body
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // 4 Thẻ KPI nổi bật
                        col.Item().Row(row =>
                        {
                            // Card 1: Doanh Thu
                            row.RelativeItem().PaddingRight(5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                            {
                                c.Item().Text("DOANH THU THUẦN").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{pnl.Revenue:N0} đ").FontSize(12).Bold().FontColor(Colors.Indigo.Darken2);
                                c.Item().Text("Đã thanh toán").FontSize(7).FontColor(Colors.Green.Medium);
                            });

                            // Card 2: Giá vốn
                            row.RelativeItem().PaddingRight(5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                            {
                                c.Item().Text("GIÁ VỐN (COGS)").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{pnl.CostOfGoodsSold:N0} đ").FontSize(12).Bold().FontColor(Colors.Amber.Darken3);
                                c.Item().Text("Theo bình quân WAC").FontSize(7).FontColor(Colors.Grey.Medium);
                            });

                            // Card 3: Lãi Gộp
                            row.RelativeItem().PaddingRight(5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                            {
                                c.Item().Text("LỢI NHUẬN GỘP").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{pnl.GrossProfit:N0} đ").FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                                c.Item().Text($"Biên gộp: {pnl.GrossMarginPercent}%").FontSize(7).Bold().FontColor(Colors.Green.Darken1);
                            });

                            // Card 4: Lãi Ròng
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                            {
                                c.Item().Text("LỢI NHUẬN RÒNG").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{pnl.NetProfit:N0} đ").FontSize(12).Bold().FontColor(pnl.NetProfit >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                c.Item().Text($"Biên ròng: {pnl.NetMarginPercent}%").FontSize(7).Bold().FontColor(pnl.NetProfit >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1);
                            });
                        });

                        col.Item().PaddingTop(15).Text("1. BẢNG TỔNG HỢP CHỈ TIÊU TÀI CHÍNH P&L").FontSize(10).Bold().FontColor(Colors.Indigo.Darken2);

                        // Table P&L Summary
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(4);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Indigo.Medium).Padding(5).Text("Chỉ Tiêu Tài Chính").FontColor(Colors.White).Bold();
                                h.Cell().Background(Colors.Indigo.Medium).Padding(5).AlignRight().Text("Số Tiền (VNĐ)").FontColor(Colors.White).Bold();
                                h.Cell().Background(Colors.Indigo.Medium).Padding(5).AlignCenter().Text("Tỷ Lệ (%)").FontColor(Colors.White).Bold();
                                h.Cell().Background(Colors.Indigo.Medium).Padding(5).Text("Ghi Chú").FontColor(Colors.White).Bold();
                            });

                            var rows = new (string Name, decimal Val, decimal Pct, string Note, bool Bold, string Color)[]
                            {
                                ("1. Doanh thu thuần", pnl.Revenue, 100m, "Đơn hàng hoàn tất", true, Colors.Grey.Darken4),
                                ("2. Giá vốn hàng bán (COGS)", pnl.CostOfGoodsSold, pnl.Revenue > 0 ? (pnl.CostOfGoodsSold / pnl.Revenue) * 100 : 0, "Bình quân gia quyền lô FEFO", false, Colors.Grey.Darken2),
                                ("3. Lợi nhuận gộp", pnl.GrossProfit, pnl.GrossMarginPercent, "Doanh thu - Giá vốn", true, Colors.Green.Darken2),
                                ("4. Chi phí vận hành", pnl.OperatingExpenses, pnl.Revenue > 0 ? (pnl.OperatingExpenses / pnl.Revenue) * 100 : 0, "Từ Sổ cái (điện, mặt bằng...)", false, Colors.Red.Darken2),
                                ("5. Lợi nhuận ròng", pnl.NetProfit, pnl.NetMarginPercent, "Lãi gộp - Chi phí", true, pnl.NetProfit >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2)
                            };

                            foreach (var r in rows)
                            {
                                var c1 = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r.Name);
                                if (r.Bold) c1.Bold();

                                var c2 = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{r.Val:N0} đ").FontColor(r.Color);
                                if (r.Bold) c2.Bold();

                                var c3 = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{r.Pct:F2}%");
                                if (r.Bold) c3.Bold();

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r.Note).FontSize(8).FontColor(Colors.Grey.Medium);
                            }
                        });

                        col.Item().PaddingTop(15).Text("2. PHÂN BỔ HIỆU QUẢ THEO CHI NHÁNH (STORE BREAKDOWN)").FontSize(10).Bold().FontColor(Colors.Indigo.Darken2);

                        // Table Store Breakdown
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Tên store
                                cols.RelativeColumn(2); // DT
                                cols.RelativeColumn(2); // COGS
                                cols.RelativeColumn(2); // Lãi gộp
                                cols.RelativeColumn(2); // Chi phí
                                cols.RelativeColumn(2); // Lãi ròng
                                cols.RelativeColumn(2); // DT/m2
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).Text("Chi Nhánh").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Doanh Thu").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Giá Vốn").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Lãi Gộp").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Chi Phí").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Lãi Ròng").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Doanh Thu/m²").FontColor(Colors.White).Bold().FontSize(8);
                            });

                            foreach (var s in pnl.BreakdownByStore)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(s.StoreName).FontSize(8).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{s.Revenue:N0}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{s.Cogs:N0}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{s.GrossProfit:N0}").FontSize(8).FontColor(Colors.Green.Darken2);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{s.OperatingExpenses:N0}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{s.NetProfit:N0}").FontSize(8).Bold().FontColor(s.NetProfit >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{s.SalesPerSqft:N0}").FontSize(8).FontColor(Colors.Indigo.Medium);
                            }
                        });
                    });

                    // Footer
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().Text("Tài liệu nội bộ - Canmake Beauty © 2026").FontSize(7).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Trang ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
        #endregion

        #region 3. Xuất Sổ Cái Tài Chính (Excel)
        public byte[] ExportLedgerToExcel(List<FinancialLedgerDto> entries, string storeLabel, string periodLabel)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("SoCai_ThuChi");

            ws.ShowGridLines = true;
            ws.Style.Font.FontName = "Segoe UI";
            ws.Style.Font.FontSize = 10;

            // 1. Tiêu đề
            ws.Cell("A1").Value = "CANMAKE BEAUTY - PHÂN HỆ TÀI CHÍNH KẾ TOÁN";
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromArgb(79, 70, 229);

            ws.Cell("A2").Value = "SỔ CÁI NHẬT KÝ GIAO DỊCH THU CHI (GENERAL ACCOUNTING LEDGER)";
            ws.Cell("A2").Style.Font.FontSize = 12;
            ws.Cell("A2").Style.Font.Bold = true;

            ws.Cell("A3").Value = $"Chi nhánh: {storeLabel} | Kỳ: {periodLabel} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;

            // 2. Thẻ KPI tổng thu - tổng chi - số dư
            decimal totalThu = entries.Where(e => e.Direction == "THU").Sum(e => e.Amount);
            decimal totalChi = entries.Where(e => e.Direction == "CHI").Sum(e => e.Amount);
            decimal balance = totalThu - totalChi;

            ws.Cell(5, 1).Value = "Tổng Thu Trong Kỳ:";
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(5, 2).Value = totalThu;
            ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(5, 2).Style.Font.Bold = true;
            ws.Cell(5, 2).Style.Font.FontColor = XLColor.FromHtml("#059669");

            ws.Cell(5, 4).Value = "Tổng Chi Trong Kỳ:";
            ws.Cell(5, 4).Style.Font.Bold = true;
            ws.Cell(5, 5).Value = totalChi;
            ws.Cell(5, 5).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(5, 5).Style.Font.Bold = true;
            ws.Cell(5, 5).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

            ws.Cell(5, 7).Value = "Chênh Lệch Thu Chi (Số Dư):";
            ws.Cell(5, 7).Style.Font.Bold = true;
            ws.Cell(5, 8).Value = balance;
            ws.Cell(5, 8).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(5, 8).Style.Font.Bold = true;
            ws.Cell(5, 8).Style.Font.FontColor = balance >= 0 ? XLColor.FromHtml("#059669") : XLColor.FromHtml("#DC2626");

            // 3. Tiêu đề cột
            int headerRow = 7;
            string[] headers = { "STT", "Mã Bút Toán", "Ngày Ghi Sổ", "Chi Nhánh", "Nghiệp Vụ", "Loại Bút Toán", "Ghi Nợ (Debit)", "Ghi Có (Credit)", "Số Tiền Giao Dịch", "Số Dư Lũy Kế", "Diễn Giải", "Người Lập" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 70, 229);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int r = headerRow + 1;
            int stt = 1;
            foreach (var e in entries)
            {
                ws.Cell(r, 1).Value = stt++;
                ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(r, 2).Value = e.LedgerCode;
                ws.Cell(r, 2).Style.Font.Bold = true;

                ws.Cell(r, 3).Value = e.EntryDate.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(r, 4).Value = e.StoreName;

                ws.Cell(r, 5).Value = e.Direction;
                ws.Cell(r, 5).Style.Font.Bold = true;
                ws.Cell(r, 5).Style.Font.FontColor = e.Direction == "THU" ? XLColor.FromHtml("#059669") : XLColor.FromHtml("#DC2626");
                ws.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(r, 6).Value = e.EntryType;
                ws.Cell(r, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(r, 7).Value = e.DebitAmount;
                ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0 ₫";

                ws.Cell(r, 8).Value = e.CreditAmount;
                ws.Cell(r, 8).Style.NumberFormat.Format = "#,##0 ₫";

                ws.Cell(r, 9).Value = (e.Direction == "THU" ? "+" : "-") + e.Amount.ToString("N0") + " ₫";
                ws.Cell(r, 9).Style.Font.Bold = true;
                ws.Cell(r, 9).Style.Font.FontColor = e.Direction == "THU" ? XLColor.FromHtml("#059669") : XLColor.FromHtml("#DC2626");

                ws.Cell(r, 10).Value = e.RunningBalance;
                ws.Cell(r, 10).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(r, 10).Style.Font.Bold = true;

                ws.Cell(r, 11).Value = e.Description;
                ws.Cell(r, 12).Value = e.CreatedByName ?? "System";

                if (stt % 2 == 0)
                {
                    ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                }

                r++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        #endregion
    }
}
