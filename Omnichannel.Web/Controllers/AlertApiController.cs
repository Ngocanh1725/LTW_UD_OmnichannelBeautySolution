using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Omnichannel.Application.DTOs.Alerts;
using Omnichannel.Application.Interfaces.Alerts;
using Omnichannel.Web.Extensions;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertApiController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly IHubContext<SystemHub> _hubContext;

        public AlertApiController(IAlertService alertService, IHubContext<SystemHub> hubContext)
        {
            _alertService = alertService;
            _hubContext = hubContext;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var summary = await _alertService.GetAlertSummaryAsync();
            return Ok(new
            {
                success = true,
                count = summary.UnreadCount,
                danger = summary.DangerCount,
                warning = summary.WarningCount,
                successCount = summary.SuccessCount,
                info = summary.InfoCount
            });
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest([FromQuery] int take = 10)
        {
            take = Math.Clamp(take, 1, 50);
            var alerts = await _alertService.GetLatestAlertsAsync(take);
            var unreadCount = await _alertService.GetUnreadCountAsync();

            return Ok(new
            {
                success = true,
                unreadCount,
                alerts
            });
        }

        [HttpPost("mark-read/{id:int}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var success = await _alertService.MarkAsReadAsync(id);
            var unreadCount = await _alertService.GetUnreadCountAsync();

            return Ok(new
            {
                success,
                unreadCount
            });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var updated = await _alertService.MarkAllAsReadAsync();
            return Ok(new
            {
                success = true,
                updatedCount = updated,
                unreadCount = 0
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            var success = await _alertService.DeleteAlertAsync(id);
            var unreadCount = await _alertService.GetUnreadCountAsync();

            return Ok(new
            {
                success,
                unreadCount
            });
        }

        [HttpPost("trigger-check")]
        public async Task<IActionResult> TriggerCheck()
        {
            var newCount = await _alertService.CheckAndGenerateAlertsAsync();
            var unreadCount = await _alertService.GetUnreadCountAsync();

            if (newCount > 0)
            {
                var latestAlerts = await _alertService.GetLatestAlertsAsync(newCount);
                var topAlert = latestAlerts.FirstOrDefault();
                if (topAlert != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveAlert", new
                    {
                        alert = topAlert,
                        newCount,
                        unreadTotal = unreadCount
                    });
                }
            }

            return Ok(new
            {
                success = true,
                newCount,
                unreadCount
            });
        }
    }
}
