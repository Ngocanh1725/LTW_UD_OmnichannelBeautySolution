using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Omnichannel.Web.Hubs
{
    public class SystemHub : Hub
    {
        // Hub rỗng để Controller có thể Inject IHubContext<SystemHub> và gửi tin nhắn
        public async Task SendNotification(string message, string type = "success")
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }
    }
}
