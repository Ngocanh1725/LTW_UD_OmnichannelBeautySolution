using System.Collections.Generic;
using Omnichannel.Application.DTOs.Finance;
using Omnichannel.Application.DTOs.Reports;

namespace Omnichannel.Application.Interfaces.Reports
{
    /// <summary>
    /// Giao diện dịch vụ kết xuất báo cáo đa định dạng (Excel ClosedXML & PDF QuestPDF)
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Xuất danh sách đơn hàng báo cáo doanh thu ra file Excel (.xlsx) với định dạng đẹp mắt
        /// </summary>
        byte[] ExportRevenueOrdersToExcel(List<OrderReportItemDto> orders, string periodLabel, string storeLabel);

        /// <summary>
        /// Xuất danh sách đơn hàng báo cáo doanh thu ra file CSV (UTF-8 with BOM)
        /// </summary>
        byte[] ExportRevenueOrdersToCsv(List<OrderReportItemDto> orders);

        /// <summary>
        /// Xuất báo cáo tài chính P&L ra file Excel (.xlsx)
        /// </summary>
        byte[] ExportProfitLossToExcel(ProfitLossReportDto pnl);

        /// <summary>
        /// Xuất báo cáo tài chính P&L ra file PDF bằng QuestPDF
        /// </summary>
        byte[] ExportProfitLossToPdf(ProfitLossReportDto pnl);

        /// <summary>
        /// Xuất sổ cái thu chi ra file Excel (.xlsx) kèm số dư lũy kế
        /// </summary>
        byte[] ExportLedgerToExcel(List<FinancialLedgerDto> entries, string storeLabel, string periodLabel);
    }
}
