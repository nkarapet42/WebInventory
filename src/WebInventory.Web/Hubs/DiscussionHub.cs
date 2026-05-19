using Microsoft.AspNetCore.SignalR;

namespace WebInventory.Web.Hubs;

public class DiscussionHub : Hub
{
    public async Task JoinInventory(string inventoryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(inventoryId));
    }

    public static string GetGroupName(string inventoryId)
    {
        return $"inventory-discussion-{inventoryId}";
    }
}
