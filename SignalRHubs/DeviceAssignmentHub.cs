using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.SignalRHubs
{
    public class DeviceAssignmentHub : Hub
    {
        public async Task JoinDeviceGroup(string deviceType, long storeId = 0)
        {
            var groupName = GetGroupName(deviceType, storeId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("GroupJoined", groupName);
        }

        public async Task LeaveDeviceGroup(string deviceType, long storeId = 0)
        {
            var groupName = GetGroupName(deviceType, storeId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SubscribeToAllChanges()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "assignments-all");
        }

        // The single source of truth for group naming. SignalR group names are
        // case-sensitive, so every publisher (QueueRepository,
        // TableChangeMonitorService) must build the name through this method -
        // hand-rolled interpolation that skipped the ToLower() sent messages to
        // a group no client had ever joined.
        public static string GetGroupName(string deviceType, long storeId)
        {
            if (string.IsNullOrEmpty(deviceType))
                return $"assignments-all-{storeId}";

            return $"assignments-{deviceType.ToLower()}-{storeId}";
        }
        //public async Task JoinDeviceGroup(string deviceType, long? storeId)
        //{
        //    var groupName = GetGroupName(deviceType, storeId);
        //    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        //}

        //public async Task LeaveDeviceGroup(string deviceType, long? storeId)
        //{
        //    var groupName = GetGroupName(deviceType, storeId);
        //    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        //}

        //private string GetGroupName(string deviceType, long? storeId)
        //{
        //    return $"assignments-{deviceType}-{storeId ?? 0}";
        //}
    }
}
