using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Alerts;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.BackgroundServices
{
    /// <summary>
    /// Worker chạy ngầm kiểm tra và tạo cảnh báo thông minh (HSD FEFO, tồn kho, doanh thu bất thường).
    /// - Chu kỳ kiểm tra định kỳ 20 phút nhằm triệt tiêu nghẽn tài nguyên CPU / CSDL.
    /// - Hỗ trợ Channel trigger tức thời khi có sự kiện xuất / nhập kho mà không cần rút ngắn chu kỳ polling.
    /// - Quản lý IServiceScopeFactory độc lập, giải phóng DbContext ngay sau mỗi chu kỳ.
    /// - Phân luồng SignalR chính xác tới Group("Managers"), tuyệt đối không broadcast tràn lan tới Clients.All.
    /// </summary>
    public class AlertBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertBackgroundWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(20);

        // Kênh thông báo hàng đợi bất đồng bộ để kích hoạt kiểm tra theo sự kiện (Event-driven Trigger)
        private static readonly Channel<bool> _triggerChannel = Channel.CreateBounded<bool>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        public AlertBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<AlertBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Kích hoạt quét cảnh báo ngay lập tức khi phát sinh sự kiện xuất nhập kho hoặc đơn hàng
        /// </summary>
        public static void TriggerWarehouseCheck()
        {
            _triggerChannel.Writer.TryWrite(true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertBackgroundWorker đã khởi động. Chu kỳ quét định kỳ: {Interval} phút (kết hợp Event-driven Trigger).", _checkInterval.TotalMinutes);

            // Chờ 20 giây ban đầu để Database Migration, Seed Data và Server ổn định
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Quét kiểm tra lần đầu sau khi hệ thống sẵn sàng
            await RunAlertCheckAsync(stoppingToken);

            using var timer = new PeriodicTimer(_checkInterval);

            // Lắng nghe song song cả Timer định kỳ 20 phút VÀ Event Trigger từ kênh xuất/nhập kho
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                    var channelTask = _triggerChannel.Reader.WaitToReadAsync(stoppingToken).AsTask();

                    var completedTask = await Task.WhenAny(timerTask, channelTask);

                    if (completedTask == channelTask && await channelTask)
                    {
                        // Đọc hết các tín hiệu tồn đọng để gộp (debounce batching)
                        while (_triggerChannel.Reader.TryRead(out _)) { }
                        _logger.LogInformation("Nhận được tín hiệu xuất/nhập kho: Thực hiện quét cảnh báo kho sớm...");
                    }

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await RunAlertCheckAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi vòng lặp điều phối AlertBackgroundWorker. Tự động phục hồi sau 1 phút.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("AlertBackgroundWorker đã dừng an toàn.");
        }

        private async Task RunAlertCheckAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                // Giới hạn timeout 30 giây cho mỗi chu kỳ quét cảnh báo nền, ngăn chặn rò rỉ tác vụ treo
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                var cycleToken = cts.Token;

                // Khởi tạo Scope độc lập và giải phóng ngay khi thoát khối using
                using var scope = _scopeFactory.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                var hubContext = scope.ServiceProvider.GetService<IHubContext<SystemHub>>();

                int newAlertsCount = await alertService.CheckAndGenerateAlertsAsync(cycleToken);

                if (newAlertsCount > 0)
                {
                    var latestAlerts = await alertService.GetLatestAlertsAsync(newAlertsCount, cancellationToken: cycleToken);
                    var topAlert = latestAlerts.FirstOrDefault();

                    if (topAlert != null && hubContext != null)
                    {
                        var unreadTotal = await alertService.GetUnreadCountAsync(cancellationToken: cancellationToken);

                        // CHỈ Broadcast tới Group "Managers" của quản trị viên nội bộ, triệt tiêu việc spam client khách hàng
                        await hubContext.Clients.Group("Managers").SendAsync("ReceiveAlert", new
                        {
                            alert = topAlert,
                            newCount = newAlertsCount,
                            unreadTotal = unreadTotal
                        }, cancellationToken);

                        await hubContext.Clients.Group("Managers").SendAsync("ReceiveNotification", topAlert.Message, topAlert.Severity.ToLower(), cancellationToken);
                    }

                    // Tự động dispatch thông báo có cấu trúc theo Role SuperAdmin & StoreManager
                    var notificationService = scope.ServiceProvider.GetService<INotificationService>();
                    if (notificationService != null && topAlert != null)
                    {
                        await notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                        {
                            Type = "ALERT",
                            Severity = topAlert.Severity,
                            Title = $"🤖 AI: {topAlert.Title}",
                            Message = topAlert.Message,
                            Role = "SuperAdmin",
                            Url = topAlert.ActionUrl
                        }, cancellationToken);

                        if (topAlert.StoreId.HasValue)
                        {
                            await notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                            {
                                Type = "ALERT",
                                Severity = topAlert.Severity,
                                Title = $"🤖 AI: {topAlert.Title}",
                                Message = topAlert.Message,
                                Role = "StoreManager",
                                StoreId = topAlert.StoreId,
                                Url = topAlert.ActionUrl
                            }, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AlertBackgroundWorker đã hủy tác vụ đang chạy theo yêu cầu dừng của host.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi RunAlertCheckAsync");
            }
        }
    }
}
