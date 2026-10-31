using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Alerts;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.BackgroundServices
{
    public class AlertBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertBackgroundWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

        public AlertBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<AlertBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertBackgroundWorker đã khởi động. Chu kỳ kiểm tra cảnh báo: {Interval} phút.", _checkInterval.TotalMinutes);

            // Chờ 10 giây ban đầu để Database và ứng dụng khởi động hoàn tất
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Chạy kiểm tra lần đầu ngay sau khi khởi động
            await RunAlertCheckAsync(stoppingToken);

            // Chạy định kỳ mỗi 15 phút
            using var timer = new PeriodicTimer(_checkInterval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAlertCheckAsync(stoppingToken);
            }

            _logger.LogInformation("AlertBackgroundWorker đã dừng.");
        }

        private async Task RunAlertCheckAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                var hubContext = scope.ServiceProvider.GetService<IHubContext<SystemHub>>();

                int newAlertsCount = await alertService.CheckAndGenerateAlertsAsync(cancellationToken);

                if (newAlertsCount > 0 && hubContext != null)
                {
                    var latestAlerts = await alertService.GetLatestAlertsAsync(newAlertsCount, cancellationToken: cancellationToken);
                    var topAlert = latestAlerts.FirstOrDefault();

                    if (topAlert != null)
                    {
                        var unreadTotal = await alertService.GetUnreadCountAsync(cancellationToken: cancellationToken);

                        // Broadcast sự kiện cảnh báo mới qua SignalR
                        await hubContext.Clients.All.SendAsync("ReceiveAlert", new
                        {
                            alert = topAlert,
                            newCount = newAlertsCount,
                            unreadTotal = unreadTotal
                        }, cancellationToken);

                        await hubContext.Clients.All.SendAsync("ReceiveNotification", topAlert.Message, topAlert.Severity.ToLower(), cancellationToken);
                    }

                    // Tự động tạo và dispatch thông báo cho các role liên quan (SuperAdmin, StoreManager)
                    var notificationService = scope.ServiceProvider.GetService<Omnichannel.Application.Interfaces.Notifications.INotificationService>();
                    if (notificationService != null && topAlert != null)
                    {
                        await notificationService.CreateAndSendNotificationAsync(new Omnichannel.Application.DTOs.Notifications.CreateNotificationDto
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
                            await notificationService.CreateAndSendNotificationAsync(new Omnichannel.Application.DTOs.Notifications.CreateNotificationDto
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi AlertBackgroundWorker");
            }
        }
    }
}
