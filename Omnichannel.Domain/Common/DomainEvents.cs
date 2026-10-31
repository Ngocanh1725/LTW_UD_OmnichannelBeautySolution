using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Common
{
    /// <summary>
    /// Hợp đồng cơ sở cho mọi Sự kiện Miền (Domain Event) bất biến trong hệ thống.
    /// </summary>
    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOnUtc { get; }
    }

    /// <summary>
    /// Bản ghi cơ sở cho Domain Event với định danh duy nhất và dấu thời gian UTC.
    /// </summary>
    public abstract record DomainEventBase : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Lớp cơ sở trừu tượng cho các Thực thể Gốc (Aggregate Root) có khả năng sinh sự kiện miền.
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

    // =========================================================================
    // CÁC SỰ KIỆN MIỀN CỐT LÕI (CORE DOMAIN EVENTS THEO OOAD BLUEPRINT v2.0)
    // =========================================================================

    /// <summary>
    /// Phát sinh khi một đơn hàng mới được tạo (Storefront hoặc POS).
    /// </summary>
    public sealed record OrderPlacedDomainEvent(
        int OrderId,
        string OrderCode,
        int? CustomerId,
        int StoreId,
        decimal TotalAmount,
        string OrderChannel) : DomainEventBase;

    /// <summary>
    /// Phát sinh khi đơn hàng được xác nhận thanh toán thành công (VietQR, VNPay, MoMo, Tiền mặt).
    /// </summary>
    public sealed record PaymentConfirmedDomainEvent(
        int OrderId,
        string OrderCode,
        int? CustomerId,
        int StoreId,
        decimal PaidAmount,
        string PaymentMethod,
        string? GatewayTransactionNo) : DomainEventBase;

    /// <summary>
    /// Phát sinh khi một lô hàng mỹ phẩm chạm ngưỡng cảnh báo cận date FEFO (ExpDate <= AlertDaysThreshold).
    /// </summary>
    public sealed record BatchNearExpiryDomainEvent(
        int BatchId,
        string BatchCode,
        int ProductId,
        int DaysRemaining,
        int RemainingStockQuantity) : DomainEventBase;

    /// <summary>
    /// Phát sinh khi thu ngân đóng ca bán hàng POS và kết chuyển đối soát tiền mặt két.
    /// </summary>
    public sealed record PosShiftClosedDomainEvent(
        int ShiftId,
        int StoreId,
        int CashierUserId,
        decimal SystemExpectedCash,
        decimal ActualCountedCash,
        decimal DiscrepancyAmount) : DomainEventBase;

    /// <summary>
    /// Phát sinh khi hoàn tất nhập kho điều chuyển liên chi nhánh 3 bước.
    /// </summary>
    public sealed record StockTransferCompletedDomainEvent(
        int TransferId,
        string TransferCode,
        int SourceStoreId,
        int DestinationStoreId,
        int TotalReceivedQuantity) : DomainEventBase;
}
